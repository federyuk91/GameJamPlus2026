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
        private float coyoteRemaining;
        private float bufferRemaining;
        private bool jumpCut;
        private float ropeLaunchRemaining;
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
            bufferRemaining = jumpPressed ? Movement.jumpBuffer : Mathf.Max(0f, bufferRemaining - dt);
            coyoteRemaining = body.Grounded ? Movement.coyoteTime : Mathf.Max(0f, coyoteRemaining - dt);
            Vector2 velocity = Velocity;
            if (swimming != null && swimming.HasStrokeControl && !body.Grounded)
                velocity.x = swimming.SurfaceHorizontal(velocity.x, input.Horizontal, dt);
            else
                velocity.x = Mathf.MoveTowards(velocity.x, input.Horizontal * Movement.speed,
                    (ropeLaunchRemaining > 0f ? Movement.airAcceleration * Movement.launchControlMultiplier
                        : body.Grounded ? Movement.groundAcceleration : Movement.airAcceleration) * dt);
            if (body.Grounded && velocity.y < 0f) velocity.y = 0f;
            if (bufferRemaining > 0f && coyoteRemaining > 0f)
            {
                velocity.y = JumpPhysics.LaunchSpeed(Gravity.jumpHeight, Gravity.playerGravity);
                bufferRemaining = coyoteRemaining = 0f;
                jumpCut = false;
            }
            if (!input.JumpHeld && velocity.y > 0f && !jumpCut)
            {
                velocity.y *= Gravity.jumpReleaseMultiplier;
                jumpCut = true;
            }
            float currentGravity = Gravity.playerGravity * (velocity.y <= 0f ? Gravity.fallGravityMultiplier : 1f);
            float verticalDisplacement = JumpPhysics.Integrate(ref velocity.y, currentGravity, Gravity.playerTerminalSpeed, dt);
            Vector2 displacement = new Vector2(velocity.x * dt, verticalDisplacement);
            if (crateInteraction != null)
            {
                displacement.x = crateInteraction.ResolveHorizontal(displacement.x, input.GrabHeld,
                    body.Grounded && velocity.y <= 0f);
                velocity.x = displacement.x / dt;
            }
            body.Move(displacement, crateInteraction != null && crateInteraction.HeldCrate != null
                ? crateInteraction.HeldCrate.Body.Shape : null);
            if (TryStartLedgeClimb()) return;
            if (body.HitWall) velocity.x = 0f;
            if (body.HitCeiling || (body.Grounded && velocity.y < 0f)) velocity.y = 0f;
            Velocity = velocity;
        }
        private bool TryStartLedgeClimb()
        {
            if (!body.HitWall || ledgeClimb == null || !ledgeClimb.TryBegin(input.Horizontal)) return false;
            if (crateInteraction != null) crateInteraction.Release();
            if (swimming != null) swimming.ResetStrokeControl();
            bufferRemaining = coyoteRemaining = ropeLaunchRemaining = 0f;
            Velocity = Vector2.zero;
            return true;
        }
        private void OnDisable()
        {
            Velocity = Vector2.zero;
            coyoteRemaining = bufferRemaining = ropeLaunchRemaining = 0f;
            if (swimming != null) swimming.ResetStrokeControl();
        }
    }
}
