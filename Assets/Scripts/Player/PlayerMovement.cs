using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(100), DisallowMultipleComponent, RequireComponent(typeof(PlayerInputReader), typeof(SimulatedBody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PlayerCrateInteraction crateInteraction;
        [SerializeField] private PlayerRopeInteraction ropeInteraction;
        [SerializeField] private PlayerSwimming swimming;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField] private PlayerLadderInteraction ladderInteraction;
        [SerializeField] private PlayerLedgeClimb ledgeClimb;
        private GravitySettings Gravity => physicsSettings != null ? physicsSettings.gravity : PhysicsSettings.DefaultGravity;
        private PlayerMovementSettings Movement => physicsSettings != null ? physicsSettings.player : PhysicsSettings.DefaultPlayer;
        // World units per second; 1 world unit = 32 artwork pixels.
        public Vector2 Velocity { get; private set; }
        public bool IsSliding { get; private set; }
        private float coyoteRemaining;
        private float bufferRemaining;
        private bool jumpCut;
        private float ropeLaunchRemaining;
        private float groundSpeed;
        private float slopeSlideSpeed;
        private bool followingGround;
        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (crateInteraction == null) crateInteraction = GetComponent<PlayerCrateInteraction>();
            if (ropeInteraction == null) ropeInteraction = GetComponent<PlayerRopeInteraction>();
            if (swimming == null) swimming = GetComponent<PlayerSwimming>();
            if (ladderInteraction == null) ladderInteraction = GetComponent<PlayerLadderInteraction>();
            if (ledgeClimb == null) ledgeClimb = GetComponent<PlayerLedgeClimb>();
        }
        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            bool jumpPressed = input.ConsumeJump();
            if (swimming != null) swimming.UpdateControlTimers(dt);
            bool inWater = swimming != null && swimming.CheckWater();
            if (ledgeClimb != null && ledgeClimb.Tick(dt, out Vector2 climbVelocity))
            {
                ResetSurfaceMotion();
                if (swimming != null) swimming.ResetStrokeControl();
                if (crateInteraction != null) crateInteraction.Release();
                bufferRemaining = coyoteRemaining = ropeLaunchRemaining = 0f;
                Velocity = climbVelocity;
                return;
            }
            ropeLaunchRemaining = Mathf.Max(0f, ropeLaunchRemaining - dt);
            Vector2 ladderVelocity = Velocity;
            bool ladderReleased = false;
            bool ladderHandled = ladderInteraction != null && (ropeInteraction == null || !ropeInteraction.Attached)
                && ladderInteraction.Tick(input.Horizontal, input.Vertical, jumpPressed, Velocity, dt,
                    out ladderVelocity, out ladderReleased);
            if (ladderHandled)
            {
                ResetSurfaceMotion();
                if (swimming != null) swimming.ResetStrokeControl();
                if (crateInteraction != null) crateInteraction.Release();
                bufferRemaining = coyoteRemaining = 0f;
                jumpCut = false;
                Velocity = ladderVelocity;
                if (!ladderReleased) return;
                ropeLaunchRemaining = Movement.launchControlTime;
                jumpPressed = false;
            }
            if (!ladderHandled && ropeInteraction != null && ropeInteraction.Tick(input.Horizontal, input.Vertical, input.GrabHeld,
                jumpPressed, Velocity, dt, out Vector2 ropeVelocity, out bool released))
            {
                ResetSurfaceMotion();
                if (swimming != null) swimming.ResetStrokeControl();
                if (crateInteraction != null) crateInteraction.Release();
                bufferRemaining = coyoteRemaining = 0f;
                jumpCut = false;
                Velocity = ropeVelocity;
                if (!released) return;
                // Preserve the rope impulse while allowing gentle steering after release.
                ropeLaunchRemaining = Movement.launchControlTime;
                jumpPressed = false;
            }
            if (inWater)
            {
                ResetSurfaceMotion();
                if (crateInteraction != null) crateInteraction.Release();
                bufferRemaining = coyoteRemaining = ropeLaunchRemaining = 0f;
                // A swim stroke is independent of jump hold duration, including at the surface.
                jumpCut = true;
                Vector2 swimVelocity = swimming.Step(Velocity, input.Horizontal, jumpPressed, dt);
                body.Move(swimVelocity * dt);
                if (TryStartLedgeClimb()) return;
                if (body.HitWall) swimVelocity.x = 0f;
                if (body.HitCeiling || (body.Grounded && swimVelocity.y < 0f)) swimVelocity.y = 0f;
                Velocity = swimVelocity;
                return;
            }
            body.RefreshGround(Movement.maxSlopeAngle, followingGround || Velocity.y <= 0f);
            bool onSteepSlope = body.OnSteepSlope;
            bool surfaceMotion = body.HasGroundContact;
            bufferRemaining = jumpPressed ? Movement.jumpBuffer : Mathf.Max(0f, bufferRemaining - dt);
            coyoteRemaining = onSteepSlope ? 0f
                : body.Grounded ? Movement.coyoteTime : Mathf.Max(0f, coyoteRemaining - dt);
            Vector2 velocity = Velocity;
            if (surfaceMotion)
            {
                Vector2 normal = body.GroundNormal;
                Vector2 tangent = new Vector2(normal.y, -normal.x);
                float downhillSign = Mathf.Sign(tangent.y) * -1f;
                if (!followingGround)
                    groundSpeed = Mathf.Clamp(velocity.x / Mathf.Max(0.01f, tangent.x), -Movement.speed, Movement.speed);
                groundSpeed = onSteepSlope ? 0f : Mathf.MoveTowards(groundSpeed,
                    input.Horizontal * Movement.speed, Movement.groundAcceleration * dt);

                float slideAngle = Mathf.Clamp(Movement.slopeSlideAngle, 0f, Movement.maxSlopeAngle);
                IsSliding = Movement.slideOnSteepSlopes && body.GroundAngle > slideAngle + 0.01f;
                slopeSlideSpeed = IsSliding ? Mathf.Min(Movement.slopeSlideMaxSpeed,
                    slopeSlideSpeed + Movement.slopeSlideAcceleration * Mathf.Abs(tangent.y) * dt) : 0f;
                // In the intermediate range, uphill input can brake the downhill drift.
                if (IsSliding && !onSteepSlope && input.Horizontal * downhillSign < 0f)
                    slopeSlideSpeed = Mathf.MoveTowards(slopeSlideSpeed, 0f,
                        Movement.groundAcceleration * Mathf.Abs(input.Horizontal) * dt);
                velocity = tangent * (groundSpeed + downhillSign * slopeSlideSpeed);
            }
            else
            {
                ResetSurfaceMotion();
                if (swimming != null && swimming.HasStrokeControl)
                    velocity.x = swimming.SurfaceHorizontal(velocity.x, input.Horizontal, dt);
                else
                    velocity.x = Mathf.MoveTowards(velocity.x, input.Horizontal * Movement.speed,
                        (ropeLaunchRemaining > 0f ? Movement.airAcceleration * Movement.launchControlMultiplier
                            : Movement.airAcceleration) * dt);
            }
            if (bufferRemaining > 0f && coyoteRemaining > 0f)
            {
                surfaceMotion = false;
                ResetSurfaceMotion();
                velocity.y = JumpPhysics.LaunchSpeed(Gravity.jumpHeight, Gravity.playerGravity);
                bufferRemaining = coyoteRemaining = 0f;
                jumpCut = false;
            }
            if (!surfaceMotion && !input.JumpHeld && velocity.y > 0f && !jumpCut)
            {
                velocity.y *= Gravity.jumpReleaseMultiplier;
                jumpCut = true;
            }
            float currentGravity = Gravity.playerGravity * (velocity.y <= 0f ? Gravity.fallGravityMultiplier : 1f);
            // Ground motion already follows the tangent; projecting gravity here would cause unwanted drift.
            float verticalDisplacement = surfaceMotion ? velocity.y * dt
                : JumpPhysics.Integrate(ref velocity.y, currentGravity, Gravity.playerTerminalSpeed, dt);
            Vector2 displacement = new Vector2(velocity.x * dt, verticalDisplacement);
            if (crateInteraction != null)
            {
                displacement.x = crateInteraction.ResolveHorizontal(displacement.x, input.GrabHeld,
                    body.Grounded && surfaceMotion && !IsSliding);
                velocity.x = displacement.x / dt;
            }
            Collider2D heldShape = crateInteraction != null && crateInteraction.HeldCrate != null
                ? crateInteraction.HeldCrate.Body.Shape : null;
            Vector2 start = transform.position;
            body.MovePlayer(displacement, Movement.maxStepHeight, Movement.maxSlopeAngle, heldShape, surfaceMotion);
            followingGround = surfaceMotion && body.HasGroundContact;
            if (surfaceMotion) velocity = ((Vector2)transform.position - start) / dt;
            if (TryStartLedgeClimb()) return;
            if (body.HitWall && !surfaceMotion) velocity.x = 0f;
            if (body.HitWall && surfaceMotion && !onSteepSlope) groundSpeed = 0f;
            if (body.HitCeiling || (!surfaceMotion && body.Grounded && velocity.y < 0f)) velocity.y = 0f;
            Velocity = velocity;
        }
        private bool TryStartLedgeClimb()
        {
            if (!body.HitWall || body.OnSteepSlope || ledgeClimb == null || !ledgeClimb.TryBegin(input.Horizontal)) return false;
            ResetSurfaceMotion();
            if (crateInteraction != null) crateInteraction.Release();
            if (swimming != null) swimming.ResetStrokeControl();
            bufferRemaining = coyoteRemaining = ropeLaunchRemaining = 0f;
            Velocity = Vector2.zero;
            return true;
        }
        private void OnDisable()
        {
            ResetSurfaceMotion();
            Velocity = Vector2.zero;
            coyoteRemaining = bufferRemaining = ropeLaunchRemaining = 0f;
            if (swimming != null) swimming.ResetStrokeControl();
        }

        private void ResetSurfaceMotion()
        {
            followingGround = false;
            groundSpeed = slopeSlideSpeed = 0f;
            IsSliding = false;
        }
    }
}
