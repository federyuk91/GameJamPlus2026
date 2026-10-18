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
        [SerializeField] private PlayerPlatformRide platformRide;
        [SerializeField] private PlayerDigging digging;
        private GravitySettings Gravity => physicsSettings != null ? physicsSettings.gravity : PhysicsSettings.DefaultGravity;
        private PlayerMovementSettings Movement => physicsSettings != null ? physicsSettings.player : PhysicsSettings.DefaultPlayer;
        private SwimMotion Water => physicsSettings != null ? physicsSettings.water : PhysicsSettings.DefaultWater;
        // World units per second; 1 world unit = 32 artwork pixels.
        public Vector2 Velocity { get; private set; }
        public bool IsSliding { get; private set; }
        public bool IsMovingOnGround { get; private set; }
        public bool CanInteractWithCrates { get; private set; }
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
            if (platformRide == null) platformRide = GetComponent<PlayerPlatformRide>();
            if (digging == null) digging = GetComponent<PlayerDigging>();
        }
        private void FixedUpdate()
        {
            if (platformRide != null) platformRide.Carry();
            TickMovement();
            bool canRide = (ropeInteraction == null || !ropeInteraction.Attached)
                && (ladderInteraction == null || !ladderInteraction.Attached)
                && (ledgeClimb == null || !ledgeClimb.Active)
                && (digging == null || !digging.InputClaimed);
            if (platformRide != null) platformRide.UpdateSupport(canRide);
        }

        private void TickMovement()
        {
            CanInteractWithCrates = false;
            IsMovingOnGround = false;
            float dt = Time.fixedDeltaTime;
            bool jumpPressed = input.ConsumeJump();
            if (swimming != null) swimming.UpdateControlTimers(dt);
            bool inWater = swimming != null && swimming.CheckWater();
            if (ledgeClimb != null && ledgeClimb.Tick(dt, input.Horizontal, input.Vertical, jumpPressed,
                out Vector2 climbVelocity, out bool ledgeReleased))
            {
                ResetSurfaceMotion();
                if (swimming != null) swimming.ResetStrokeControl();
                if (crateInteraction != null) crateInteraction.Release();
                bufferRemaining = coyoteRemaining = ropeLaunchRemaining = 0f;
                Velocity = climbVelocity;
                if (!ledgeReleased) return;
                ropeLaunchRemaining = Movement.launchControlTime;
                jumpCut = !jumpPressed;
                jumpPressed = false;
            }
            ropeLaunchRemaining = Mathf.Max(0f, ropeLaunchRemaining - dt);
            Vector2 ladderVelocity = Velocity;
            bool ladderReleased = false;
            bool ladderHandled = ladderInteraction != null && (digging == null || !digging.InputClaimed)
                && (ropeInteraction == null || !ropeInteraction.Attached)
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
            if (!ladderHandled && (digging == null || !digging.InputClaimed)
                && ropeInteraction != null && ropeInteraction.Tick(input.Horizontal, input.Vertical, input.GrabHeld,
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
            if (digging != null && digging.Tick(dt, out Vector2 digVelocity))
            {
                ResetSurfaceMotion();
                if (swimming != null) swimming.ResetStrokeControl();
                if (crateInteraction != null) crateInteraction.Release();
                bufferRemaining = coyoteRemaining = ropeLaunchRemaining = 0f;
                Velocity = digVelocity;
                return;
            }
            body.RefreshGround(Movement.maxSlopeAngle, followingGround || Velocity.y <= 0f);
            bool walkingUnderwater = inWater && body.Grounded && !jumpPressed;
            if (inWater && !walkingUnderwater)
            {
                ResetSurfaceMotion();
                if (crateInteraction != null) crateInteraction.Release();
                bufferRemaining = coyoteRemaining = ropeLaunchRemaining = 0f;
                // A swim stroke is independent of jump hold duration, including at the surface.
                jumpCut = true;
                Vector2 swimVelocity = swimming.Step(Velocity, input.Horizontal, jumpPressed, dt);
                Vector2 swimDisplacement = swimVelocity * dt;
                if (crateInteraction != null) swimDisplacement = crateInteraction.ResolveSwimming(swimDisplacement, dt);
                body.Move(swimDisplacement);
                if (TryStartLedgeClimb(false, swimVelocity.x)) return;
                if (body.HitWall) swimVelocity.x = 0f;
                if (body.HitCeiling || (body.Grounded && swimVelocity.y < 0f)) swimVelocity.y = 0f;
                Velocity = swimVelocity;
                return;
            }
            if (walkingUnderwater)
            {
                swimming.ResetStrokeControl();
                ropeLaunchRemaining = 0f;
            }
            bool onSteepSlope = body.OnSteepSlope;
            bool surfaceMotion = body.HasGroundContact;
            bufferRemaining = walkingUnderwater ? 0f
                : jumpPressed ? Movement.jumpBuffer : Mathf.Max(0f, bufferRemaining - dt);
            coyoteRemaining = onSteepSlope || walkingUnderwater ? 0f
                : body.Grounded ? Movement.coyoteTime : Mathf.Max(0f, coyoteRemaining - dt);
            Vector2 velocity = Velocity;
            if (surfaceMotion)
            {
                Vector2 normal = body.GroundNormal;
                Vector2 tangent = new Vector2(normal.y, -normal.x);
                float downhillSign = Mathf.Sign(tangent.y) * -1f;
                float walkSpeed = Movement.speed * (walkingUnderwater ? Mathf.Max(0f, Water.bottomWalkSpeedMultiplier) : 1f);
                if (!followingGround)
                    groundSpeed = Mathf.Clamp(velocity.x / Mathf.Max(0.01f, tangent.x), -walkSpeed, walkSpeed);
                if (walkingUnderwater) groundSpeed = Mathf.Clamp(groundSpeed, -walkSpeed, walkSpeed);
                groundSpeed = onSteepSlope ? 0f : Mathf.MoveTowards(groundSpeed,
                    input.Horizontal * walkSpeed, Movement.groundAcceleration * dt);

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
                if (platformRide != null && platformRide.HasSupport)
                {
                    velocity += platformRide.SupportVelocity;
                    ropeLaunchRemaining = Movement.launchControlTime;
                }
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
                if (walkingUnderwater)
                    displacement = crateInteraction.ResolveSwimming(displacement, dt);
                else
                    displacement.x = crateInteraction.ResolveHorizontal(displacement.x, input.GrabHeld,
                        body.Grounded && surfaceMotion && !IsSliding);
                velocity.x = displacement.x / dt;
            }
            Collider2D heldShape = crateInteraction != null && crateInteraction.HeldCrate != null
                ? crateInteraction.HeldCrate.Body.Shape : null;
            Vector2 start = transform.position;
            bool groundedApproach = surfaceMotion && body.Grounded;
            body.MovePlayer(displacement, Movement.maxStepHeight, Movement.maxSlopeAngle, heldShape, surfaceMotion);
            followingGround = surfaceMotion && body.HasGroundContact;
            if (surfaceMotion) velocity = ((Vector2)transform.position - start) / dt;
            if (TryStartLedgeClimb(groundedApproach, velocity.x)) return;
            if (body.HitWall && !surfaceMotion) velocity.x = 0f;
            if (body.HitWall && surfaceMotion && !onSteepSlope) groundSpeed = 0f;
            if (body.HitCeiling || (!surfaceMotion && body.Grounded && velocity.y < 0f)) velocity.y = 0f;
            Velocity = velocity;
            CanInteractWithCrates = surfaceMotion && body.Grounded && !walkingUnderwater && !IsSliding;
            IsMovingOnGround = surfaceMotion && body.Grounded && !walkingUnderwater && !IsSliding
                && Mathf.Abs(input.Horizontal) > 0.1f && Mathf.Abs(velocity.x) > 0.05f;
        }
        private bool TryStartLedgeClimb(bool fromGround, float horizontalVelocity)
        {
            if ((!fromGround && body.OnSteepSlope) || input.Vertical < -0.1f || ledgeClimb == null) return false;
            float direction = Mathf.Abs(input.Horizontal) >= 0.1f ? input.Horizontal : fromGround ? 0f : horizontalVelocity;
            if (!ledgeClimb.TryBegin(direction, fromGround)) return false;
            ResetSurfaceMotion();
            if (crateInteraction != null) crateInteraction.Release();
            if (swimming != null) swimming.ResetStrokeControl();
            bufferRemaining = coyoteRemaining = ropeLaunchRemaining = 0f;
            Velocity = Vector2.zero;
            return true;
        }
        public void TeleportTo(Vector2 destination)
        {
            if (crateInteraction != null) crateInteraction.Release();
            if (ropeInteraction != null) ropeInteraction.Detach();
            if (ladderInteraction != null) ladderInteraction.Detach();
            if (ledgeClimb != null) ledgeClimb.Cancel();
            if (platformRide != null) platformRide.Release();
            if (digging != null) digging.ReleaseInput();
            if (input != null) input.ConsumeJump();
            CanInteractWithCrates = false;
            IsMovingOnGround = false;
            ResetSurfaceMotion();
            Velocity = Vector2.zero;
            coyoteRemaining = bufferRemaining = ropeLaunchRemaining = 0f;
            jumpCut = false;
            Vector3 position = transform.position;
            transform.position = new Vector3(destination.x, destination.y, position.z);
            Physics2D.SyncTransforms();
            body.RefreshGround(Movement.maxSlopeAngle);
            if (swimming != null)
            {
                swimming.ResetStrokeControl();
                swimming.CheckWater();
            }
        }

        private void OnDisable()
        {
            CanInteractWithCrates = false;
            IsMovingOnGround = false;
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
