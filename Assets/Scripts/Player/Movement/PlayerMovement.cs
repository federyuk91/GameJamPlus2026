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
        [SerializeField] private MouseFacing facing;
        [SerializeField] private PlayerSnakeBoxInteraction snakeBox;
        [SerializeField] private PlayerFlattening flattening;
        public PlayerFlattening Flattening => flattening;
        public bool IsFlattened => flattening != null && flattening.IsFlattened;
        public PlayerSnakeBoxInteraction SnakeBoxInteraction => snakeBox;
        public bool IsHidden => snakeBox != null && snakeBox.IsInside;
        public bool IsMovingWhileHidden => IsHidden && snakeBox.IsMoving;
        private PhysicsSettings WorldSettings => (body != null ? body : body = GetComponent<SimulatedBody2D>()).Settings;
        [SerializeField] private PlayerMovementSettings movementSettings = new PlayerMovementSettings();
        [SerializeField] private PlayerLadderInteraction ladderInteraction;
        [SerializeField] private PlayerLedgeClimb ledgeClimb;
        [SerializeField] private PlayerPlatformRide platformRide;
        [SerializeField] private PlayerDigging digging;
        private SharedPlayerMovementSettings Movement => WorldSettings != null ? WorldSettings.player : PhysicsSettings.DefaultPlayer;
        public PlayerMovementSettings MovementSettings => movementSettings;
        public SimulatedBody2D Body => body != null ? body : body = GetComponent<SimulatedBody2D>();
        private SwimMotion Water => WorldSettings != null ? WorldSettings.water : PhysicsSettings.DefaultWater;
        // World units per second; 1 world unit = 32 artwork pixels.
        private Vector2 currentVelocity;
        public Vector2 Velocity
        {
            get => currentVelocity;
            private set => currentVelocity = Body.LimitVelocity(value);
        }
        public bool IsSliding { get; private set; }
        public bool IsMovingOnGround { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool CanInteractWithCrates { get; private set; }
        public bool IsHangingFromLedge => ledgeClimb != null && ledgeClimb.State == PlayerLedgeClimb.LedgeState.Hanging;
        private bool cameraControlActive;
        public bool CanStartCameraControl
        {
            get
            {
                if (!isActiveAndEnabled || IsHidden || body == null || input == null || input.GameplayActionsLocked
                    || IsSliding || Mathf.Abs(input.Horizontal) > 0.1f || Mathf.Abs(input.Vertical) > 0.1f
                    || (digging != null && digging.InputClaimed)) return false;
                if (IsHangingFromLedge) return true;
                if (ledgeClimb != null && ledgeClimb.Active) return false;
                if (ropeInteraction != null && ropeInteraction.Attached) return Mathf.Abs(ropeInteraction.ClimbSpeed) < 0.01f;
                if (ladderInteraction != null && ladderInteraction.Attached) return Mathf.Abs(ladderInteraction.ClimbSpeed) < 0.01f;
                return body.Grounded && Velocity.sqrMagnitude < 0.01f;
            }
        }

        public void SetCameraControlActive(bool active)
        {
            if (cameraControlActive == active) return;
            cameraControlActive = active;
            if (input != null) input.ConsumeJump();
            if (!active) return;
            if (crateInteraction != null) crateInteraction.Release();
            if (platformRide != null) platformRide.Release();
            CanInteractWithCrates = false;
            IsMovingOnGround = false;
            Velocity = Vector2.zero;
            ResetSurfaceMotion();
        }
        public bool CanLookVertically => isActiveAndEnabled && body != null && body.Grounded && !IsSliding
            && input != null && Mathf.Abs(input.Horizontal) < 0.1f && Velocity.sqrMagnitude < 0.01f
            && (ropeInteraction == null || !ropeInteraction.Attached)
            && (ladderInteraction == null || !ladderInteraction.Attached)
            && (ledgeClimb == null || !ledgeClimb.Active)
            && (digging == null || !digging.InputClaimed);
        private float coyoteRemaining;
        private float bufferRemaining;
        private bool jumpCut;
        private float ropeLaunchRemaining;
        private float groundSpeed;
        private float runningJumpSpeed;
        private float slopeVelocity;
        private bool followingGround;
        private bool jumpedFromSupport;
        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (crateInteraction == null) crateInteraction = GetComponent<PlayerCrateInteraction>();
            if (ropeInteraction == null) ropeInteraction = GetComponent<PlayerRopeInteraction>();
            if (swimming == null) swimming = GetComponent<PlayerSwimming>();
            if (facing == null) facing = GetComponent<MouseFacing>();
            if (snakeBox == null) snakeBox = GetComponent<PlayerSnakeBoxInteraction>();
            if (flattening == null) flattening = GetComponent<PlayerFlattening>();
            if (ladderInteraction == null) ladderInteraction = GetComponent<PlayerLadderInteraction>();
            if (ledgeClimb == null) ledgeClimb = GetComponent<PlayerLedgeClimb>();
            if (platformRide == null) platformRide = GetComponent<PlayerPlatformRide>();
            if (digging == null) digging = GetComponent<PlayerDigging>();
        }
        private void FixedUpdate()
        {
            IsSprinting = false;
            bool tailPressed = input.ConsumeTailPress();
            body.BeginMovementStep(Time.fixedDeltaTime);
            if (input.IsParalyzed)
            {
                input.ConsumeJump();
                Velocity = Vector2.zero;
                coyoteRemaining = bufferRemaining = ropeLaunchRemaining = 0f;
                CanInteractWithCrates = IsMovingOnGround = false;
                if (crateInteraction != null) crateInteraction.Release();
                ResetSurfaceMotion();
                return;
            }
            if (IsHidden)
            {
                CanInteractWithCrates = IsMovingOnGround = false;
                ResetSurfaceMotion();
                Velocity = snakeBox.Tick(Time.fixedDeltaTime);
                return;
            }
            if (cameraControlActive)
            {
                input.ConsumeJump();
                return;
            }
            if (platformRide != null) platformRide.Carry();
            jumpedFromSupport = false;
            TickMovement(tailPressed);
            bool canRide = (ropeInteraction == null || !ropeInteraction.Attached)
                && (ladderInteraction == null || !ladderInteraction.Attached)
                && (ledgeClimb == null || !ledgeClimb.Active)
                && (digging == null || !digging.InputClaimed);
            if (platformRide != null)
            {
                bool inheritSupportMotion = canRide && !jumpedFromSupport && (swimming == null || !swimming.InWater);
                Velocity += platformRide.UpdateSupport(canRide, inheritSupportMotion);
            }
        }

        private void TickMovement(bool tailPressed)
        {
            CanInteractWithCrates = false;
            IsMovingOnGround = false;
            float dt = Time.fixedDeltaTime;
            bool jumpPressed = input.ConsumeJump();
            if (swimming != null) swimming.UpdateControlTimers(dt);
            bool inWater = swimming != null && swimming.CheckWater();
            if (flattening != null)
            {
                bool wasFlattened = IsFlattened;
                bool canFlatten = !input.GameplayActionsLocked && body.Grounded
                    && (ledgeClimb == null || !ledgeClimb.Active)
                    && (ladderInteraction == null || !ladderInteraction.Attached)
                    && (ropeInteraction == null || !ropeInteraction.Attached)
                    && (digging == null || !digging.InputClaimed);
                flattening.Tick(input.Vertical, inWater, canFlatten, dt);
                if (wasFlattened && !IsFlattened && inWater) swimming.CheckWater();
            }
            if (IsFlattened)
            {
                jumpPressed = false;
                bufferRemaining = coyoteRemaining = 0f;
                runningJumpSpeed = 0f;
                if (crateInteraction != null) crateInteraction.Release();
            }
            bool traversalReleased = false;
            // Una presa già acquisita ha precedenza sul nuoto, anche con i piedi immersi.
            bool ledgeInteractionPressed = input.LedgeInteractionPressed;
            if (ledgeClimb != null && ledgeClimb.Tick(dt, input.Horizontal, input.Vertical, jumpPressed, ledgeInteractionPressed,
                out Vector2 climbVelocity, out bool ledgeReleased))
            {
                if (ledgeInteractionPressed && ledgeClimb.State == PlayerLedgeClimb.LedgeState.Climbing)
                    input.ConsumeInteraction();
                if (ledgeReleased) input.ClearLedgeInteraction();
                ResetSurfaceMotion();
                if (swimming != null) swimming.ResetStrokeControl();
                if (crateInteraction != null) crateInteraction.Release();
                bufferRemaining = coyoteRemaining = ropeLaunchRemaining = 0f;
                Velocity = climbVelocity;
                if (!ledgeReleased) return;
                traversalReleased = true;
                ropeLaunchRemaining = body.Grounded ? 0f : Movement.launchControlTime;
                jumpCut = !jumpPressed;
                if (!body.Grounded) jumpPressed = false;
            }
            ropeLaunchRemaining = Mathf.Max(0f, ropeLaunchRemaining - dt);
            Vector2 ladderVelocity = Velocity;
            bool ladderReleased = false;
            bool ladderHandled = !IsFlattened && !traversalReleased && ladderInteraction != null && (digging == null || !digging.InputClaimed)
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
                traversalReleased = true;
                ropeLaunchRemaining = Movement.launchControlTime;
                jumpPressed = false;
            }
            if (!IsFlattened && !traversalReleased && !ladderHandled && (digging == null || !digging.InputClaimed)
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
            body.RefreshGround(followingGround || Velocity.y <= 0f);
            bool walkingUnderwater = inWater && body.Grounded && !jumpPressed && !tailPressed && !swimming.HasTailBoost;
            if (inWater && !walkingUnderwater)
            {
                ResetSurfaceMotion();
                if (crateInteraction != null) crateInteraction.Release();
                bufferRemaining = coyoteRemaining = ropeLaunchRemaining = 0f;
                // A swim stroke is independent of jump hold duration, including at the surface.
                jumpCut = true;
                Vector2 swimVelocity = swimming.Step(Velocity, input.Horizontal, jumpPressed, dt, tailPressed);
                Vector2 swimDisplacement = swimVelocity * dt;
                if (crateInteraction != null && !IsFlattened) swimDisplacement = crateInteraction.ResolveSwimming(swimDisplacement, dt);
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
                float walkSpeed = movementSettings.moveSpeed * (IsFlattened ? 0.5f : 1f)
                    * (walkingUnderwater ? Mathf.Max(0f, Water.bottomWalkSpeedMultiplier) : 1f);
                bool sprinting = !IsFlattened && input.SprintHeld && !walkingUnderwater && !onSteepSlope && !input.CrateGrabHeld;
                float targetSpeed = sprinting ? Mathf.Max(walkSpeed, movementSettings.maxRunSpeed) : walkSpeed;
                if (!followingGround)
                    groundSpeed = Mathf.Clamp(velocity.x / Mathf.Max(0.01f, tangent.x), -targetSpeed, targetSpeed);
                if (walkingUnderwater || IsFlattened) groundSpeed = Mathf.Clamp(groundSpeed, -walkSpeed, walkSpeed);
                float requestedSpeed = input.Horizontal * targetSpeed;
                if (onSteepSlope) groundSpeed = 0f;
                else if (sprinting && Mathf.Abs(requestedSpeed) > walkSpeed
                    && groundSpeed * Mathf.Sign(requestedSpeed) < Mathf.Abs(requestedSpeed))
                {
                    float walkingTarget = Mathf.Sign(requestedSpeed) * walkSpeed;
                    float runTime = dt;
                    if (groundSpeed * Mathf.Sign(requestedSpeed) < walkSpeed)
                    {
                        float walkingTime = movementSettings.groundAcceleration > 0f
                            ? Mathf.Abs(walkingTarget - groundSpeed) / movementSettings.groundAcceleration : float.PositiveInfinity;
                        groundSpeed = Mathf.MoveTowards(groundSpeed, walkingTarget, movementSettings.groundAcceleration * dt);
                        runTime = Mathf.Max(0f, dt - walkingTime);
                    }
                    groundSpeed = Mathf.MoveTowards(groundSpeed, requestedSpeed, movementSettings.sprintAcceleration * runTime);
                }
                else groundSpeed = Mathf.MoveTowards(groundSpeed, requestedSpeed, movementSettings.groundAcceleration * dt);
                runningJumpSpeed = walkingUnderwater ? 0f : Mathf.Min(Mathf.Abs(groundSpeed), targetSpeed);

                float surfaceGravity = walkingUnderwater ? Water.GravityAcceleration(body.GravityAcceleration) : body.GravityAcceleration;
                Vector2 gravityForce = Vector2.down * (surfaceGravity * body.Weight);
                Vector2 passiveVelocity = GroundMotion.Integrate(tangent * slopeVelocity, gravityForce,
                    body.Weight, normal, body.GroundFriction, body.MaxSpeed, dt);
                slopeVelocity = Vector2.Dot(passiveVelocity, tangent);
                // Uphill control can brake the passive slide without reversing it into an extra motor.
                if (!onSteepSlope && input.Horizontal * downhillSign < 0f)
                    slopeVelocity = Mathf.MoveTowards(slopeVelocity, 0f,
                        movementSettings.groundAcceleration * Mathf.Abs(input.Horizontal) * dt);
                IsSliding = Mathf.Abs(slopeVelocity) > 0.0001f;
                velocity = body.LimitVelocity(tangent * (groundSpeed + slopeVelocity));
            }
            else
            {
                ResetSurfaceMotion(true);
                if (swimming != null && swimming.HasStrokeControl)
                    velocity.x = swimming.SurfaceHorizontal(velocity.x, input.Horizontal, dt);
                else
                    velocity.x = Mathf.MoveTowards(velocity.x, input.Horizontal
                        * (IsFlattened ? movementSettings.moveSpeed * 0.5f
                            : input.Horizontal * velocity.x > 0f ? Mathf.Max(movementSettings.moveSpeed, runningJumpSpeed) : movementSettings.moveSpeed),
                        (ropeLaunchRemaining > 0f ? movementSettings.airAcceleration * Movement.launchControlMultiplier
                            : movementSettings.airAcceleration) * dt);
            }
            if (bufferRemaining > 0f && coyoteRemaining > 0f)
            {
                surfaceMotion = false;
                ResetSurfaceMotion(true);
                velocity.y = JumpPhysics.LaunchSpeed(movementSettings.jumpHeight, body.GravityAcceleration);
                if (platformRide != null && platformRide.HasSupport)
                {
                    velocity += platformRide.SupportVelocity;
                    jumpedFromSupport = true;
                    ropeLaunchRemaining = Movement.launchControlTime;
                }
                bufferRemaining = coyoteRemaining = 0f;
                jumpCut = false;
            }
            if (!surfaceMotion && !input.JumpHeld && velocity.y > 0f && !jumpCut)
            {
                velocity.y *= movementSettings.jumpReleaseMultiplier;
                jumpCut = true;
            }
            float currentGravity = body.GravityAcceleration * (velocity.y <= 0f ? movementSettings.fallGravityMultiplier : 1f);
            // Ground motion already follows the tangent; projecting gravity here would cause unwanted drift.
            float verticalDisplacement = surfaceMotion ? velocity.y * dt
                : JumpPhysics.Integrate(ref velocity.y, currentGravity, body.MaxSpeed, dt);
            velocity = body.LimitVelocity(velocity);
            Vector2 displacement = Vector2.ClampMagnitude(new Vector2(velocity.x * dt, verticalDisplacement), body.MaxSpeed * dt);
            if (crateInteraction != null)
            {
                if (walkingUnderwater && !IsFlattened)
                    displacement = crateInteraction.ResolveSwimming(displacement, dt);
                else
                    displacement.x = crateInteraction.ResolveHorizontal(displacement.x, !IsFlattened && input.CrateGrabHeld,
                        body.Grounded && surfaceMotion && !IsSliding);
                velocity.x = displacement.x / dt;
            }
            Collider2D heldShape = crateInteraction != null && crateInteraction.HeldCrate != null
                ? crateInteraction.HeldCrate.Body.Shape : null;
            Vector2 start = transform.position;
            bool groundedApproach = surfaceMotion && body.Grounded;
            body.MovePlayer(displacement, Movement.maxStepHeight, heldShape, surfaceMotion);
            followingGround = surfaceMotion && body.HasGroundContact;
            if (!body.Grounded && crateInteraction != null) crateInteraction.Release();
            if (surfaceMotion) velocity = ((Vector2)transform.position - start) / dt;
            if (TryStartLedgeClimb(groundedApproach, velocity.x)) return;
            if (body.HitWall && !surfaceMotion) velocity.x = 0f;
            if (body.HitWall && surfaceMotion && !onSteepSlope) groundSpeed = 0f;
            if (body.HitCeiling || (!surfaceMotion && body.Grounded && velocity.y < 0f)) velocity.y = 0f;
            Velocity = velocity;
            CanInteractWithCrates = !IsFlattened && surfaceMotion && body.Grounded && !walkingUnderwater && !IsSliding;
            IsMovingOnGround = surfaceMotion && body.Grounded && !walkingUnderwater && !IsSliding
                && Mathf.Abs(input.Horizontal) > 0.1f && Mathf.Abs(velocity.x) > 0.05f;
            IsSprinting = !IsFlattened && IsMovingOnGround && input.SprintHeld && !input.CrateGrabHeld;
        }
        private bool TryStartLedgeClimb(bool fromGround, float horizontalVelocity)
        {
            if (IsFlattened) return false;
            if (crateInteraction != null && crateInteraction.SuppressAutomaticClimb && input.CrateGrabHeld) return false;
            fromGround = fromGround && body.Grounded;
            if ((!fromGround && body.OnSteepSlope) || input.Vertical < -0.1f || ledgeClimb == null) return false;
            // Il nuoto dipende dal bordo inferiore; l'aggancio richiede solo il centro emerso.
            bool touchingWater = swimming != null && swimming.CheckWater();
            if (touchingWater && (input.GameplayActionsLocked || !swimming.CanGrabLedge)) return false;
            if (touchingWater) fromGround = false;
            float direction = Mathf.Abs(input.Horizontal) >= 0.1f ? input.Horizontal : fromGround ? 0f : horizontalVelocity;
            // Vicino alla superficie si cerca automaticamente un bordo davanti al muso,
            // anche da fermi. Portata e verifiche della presa restano quelle ordinarie.
            if (touchingWater && Mathf.Abs(direction) < 0.1f && facing != null)
                direction = facing.FacingRight ? 1f : -1f;
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
            if (flattening != null) flattening.ResetPosture();
            if (input != null) input.ConsumeTailPress();
            if (snakeBox != null) snakeBox.Cancel();
            if (input != null) input.ClearLedgeInteraction();
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
            body.RefreshGround();
            if (swimming != null)
            {
                swimming.ResetStrokeControl();
                swimming.CheckWater();
            }
        }

        private void OnDisable()
        {
            if (snakeBox != null) snakeBox.Cancel();
            IsSprinting = false;
            CanInteractWithCrates = false;
            IsMovingOnGround = false;
            ResetSurfaceMotion();
            Velocity = Vector2.zero;
            coyoteRemaining = bufferRemaining = ropeLaunchRemaining = 0f;
            if (swimming != null) swimming.ResetStrokeControl();
        }

        private void ResetSurfaceMotion(bool preserveRunningJump = false)
        {
            if (!preserveRunningJump) runningJumpSpeed = 0f;
            followingGround = false;
            groundSpeed = slopeVelocity = 0f;
            IsSliding = false;
        }
    }
}
