using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(-100), DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class CrateMotion : MonoBehaviour
    {
        private static readonly List<CrateMotion> activeCrates = new List<CrateMotion>();
        private static double coordinatedTick = double.NegativeInfinity;
        private readonly List<CrateMotion> verticalStack = new List<CrateMotion>(8);
        private readonly HashSet<Collider2D> stackShapes = new HashSet<Collider2D>();
        private readonly RaycastHit2D[] stackHits = new RaycastHit2D[64];
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField] private CrateBuoyancy buoyancy;
        [SerializeField] private CrateRopeAttachment tether;
        [SerializeField] private PushableCrate pushable;
        [SerializeField] private BalloonLift balloon;
        [SerializeField] private Vector2 initialVelocity;
        [SerializeField, Min(0f)] private float airDrag = 0.35f;
        [SerializeField, Min(0.01f), Tooltip("Maximum rising/falling speed in water, also applied to supported dry crates.")]
        private float maximumWaterVerticalSpeed = 2f;
        private bool coordinatedVertical;
        private float verticalFrameDisplacement;
        private float verticalTravel;
        private float VerticalSpeedLimit => Airborne ? balloon.MaximumVerticalSpeed : maximumWaterVerticalSpeed;
        private float RemainingVerticalTravel => Mathf.Max(0f, VerticalSpeedLimit * Time.fixedDeltaTime - verticalTravel);
        private SimulatedBody2D passenger;
        private GravitySettings Gravity => physicsSettings != null ? physicsSettings.gravity : PhysicsSettings.DefaultGravity;
        public SimulatedBody2D Body => body;
        public Vector2 Velocity { get; private set; }
        public float SubmergedFraction { get; private set; }
        public bool InWater => SubmergedFraction > 0f;
        private bool Airborne => balloon != null && balloon.isActiveAndEnabled;
        public bool UsesAirLift => Airborne;
        public float BalloonWeightReduction => tether != null ? tether.LiftCapacity : 0f;
        private bool InternalBalloonTether => tether != null && tether.LiftingBalloon != null
            && coordinatedVertical && tether.LiftingBalloon.Motion.coordinatedVertical;
        public float Weight => pushable != null ? pushable.Weight : 1f;
        public float PushWeight => pushable != null ? pushable.StackWeight
            : CrateFlotation.EffectiveWeight(Weight, WaterVolume.SubmergedFraction(body.Shape.bounds));
        public float PassengerDownwardAcceleration { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            activeCrates.Clear();
            coordinatedTick = double.NegativeInfinity;
        }

        private void OnEnable()
        {
            if (!activeCrates.Contains(this)) activeCrates.Add(this);
        }

        private void OnDisable() => activeCrates.Remove(this);

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (buoyancy == null) buoyancy = GetComponent<CrateBuoyancy>();
            if (tether == null) tether = GetComponent<CrateRopeAttachment>();
            if (pushable == null) pushable = GetComponent<PushableCrate>();
            if (balloon == null) balloon = GetComponent<BalloonLift>();
            Velocity = initialVelocity;
        }

        private void FixedUpdate()
        {
            CoordinateVerticalGroups();
            const int substeps = 4;
            float dt = Time.fixedDeltaTime / substeps;
            Vector2 frameStart = transform.position;
            for (int i = 0; i < substeps; i++)
            {
                SubmergedFraction = WaterVolume.SubmergedFraction(body.Shape.bounds);
                Vector2 velocity = Velocity;
                if (Airborne)
                {
                    Vector2 balloonStart = transform.position;
                    float sharedBalloonVelocity = velocity.y;
                    velocity = balloon.Integrate(velocity, Gravity.crateGravity + PassengerAcceleration, body.Shape, dt);
                    if (coordinatedVertical) velocity.y = 0f;
                    MoveConstrained(velocity * dt, null);
                    velocity = ((Vector2)transform.position - balloonStart) / dt;
                    if (coordinatedVertical) velocity.y = sharedBalloonVelocity;
                    Velocity = ConstrainVelocity(velocity);
                    continue;
                }
                Vector2 wind = TurbineWind.AccelerationAt(body.Shape);
                velocity.x += wind.x / Weight * dt;
                float drag = ActiveBuoyancy ? buoyancy.Drag(SubmergedFraction) : 0f;
                velocity.x *= Mathf.Exp(-drag * dt);
                float sharedVerticalVelocity = velocity.y;
                if (coordinatedVertical) velocity.y = 0f;
                else if (InWater)
                    velocity.y = CrateFlotation.IntegrateVertical(velocity.y, VerticalForce(wind.y), Weight, drag,
                        SurfaceStiffness(), ActiveBuoyancy ? buoyancy.VerticalDampingRatio : 1f,
                        Mathf.Min(Gravity.crateTerminalSpeed, maximumWaterVerticalSpeed), dt);
                else
                {
                    velocity.y += (wind.y / Weight - Gravity.crateGravity - PassengerAcceleration) * dt;
                    velocity.y = Mathf.Clamp(velocity.y, -Gravity.crateTerminalSpeed, Gravity.crateTerminalSpeed);
                }
                velocity.x *= Mathf.Exp(-airDrag * (1f - SubmergedFraction) * dt);
                Vector2 start = transform.position;
                MoveConstrained(velocity * dt, null);
                velocity = ((Vector2)transform.position - start) / dt;
                if (coordinatedVertical) velocity.y = sharedVerticalVelocity;
                if (body.Grounded && !InWater && (tether == null || !tether.Attached)) velocity.x = 0f;
                Velocity = ConstrainVelocity(velocity);
            }
            FrameVelocity = ((Vector2)transform.position - frameStart) / Time.fixedDeltaTime;
            FrameVelocity += Vector2.up * (verticalFrameDisplacement / Time.fixedDeltaTime);
        }

        private bool ActiveBuoyancy => buoyancy != null && buoyancy.isActiveAndEnabled;
        private float PassengerAcceleration => passenger != null && passenger.isActiveAndEnabled
            ? Mathf.Max(0f, PassengerDownwardAcceleration) : 0f;

        private float VerticalForce(float verticalWind)
        {
            if (Airborne)
                return (balloon.ExternalAcceleration(Velocity, body.Shape).y - PassengerAcceleration) * Weight;
            float lift = ActiveBuoyancy ? buoyancy.LiftStrength * SubmergedFraction : 0f;
            return lift + verticalWind + VerticalGravityForce()
                - PassengerAcceleration * Weight;
        }

        private float VerticalGravityForce()
        {
            if (Airborne) return 0f;
            float effectiveWeight = CrateFlotation.EffectiveWeight(Weight, SubmergedFraction)
                - (InternalBalloonTether ? BalloonWeightReduction : 0f);
            return -Gravity.crateGravity * effectiveWeight;
        }

        private float SurfaceStiffness()
        {
            if (Airborne) return 0f;
            if (SubmergedFraction <= 0f || SubmergedFraction >= 1f) return 0f;
            float lift = ActiveBuoyancy ? buoyancy.LiftStrength : 0f;
            return (lift + Gravity.crateGravity * Weight * 0.5f) / Mathf.Max(0.01f, body.Shape.bounds.size.y);
        }

        private static void CoordinateVerticalGroups()
        {
            if (coordinatedTick == Time.fixedTimeAsDouble) return;
            coordinatedTick = Time.fixedTimeAsDouble;
            Physics2D.SyncTransforms();
            foreach (CrateMotion crate in activeCrates)
            {
                crate.coordinatedVertical = false;
                crate.verticalFrameDisplacement = 0f;
                crate.verticalTravel = 0f;
                crate.verticalStack.Clear();
                crate.SubmergedFraction = WaterVolume.SubmergedFraction(crate.body.Shape.bounds);
            }
            // Build every group before moving any crate, independently of MonoBehaviour execution order.
            foreach (CrateMotion crate in activeCrates) crate.FindStackRoot().verticalStack.Add(crate);
            foreach (CrateMotion root in activeCrates)
            {
                if (root.verticalStack.Count < 2) continue;
                bool touchesWater = false;
                bool hasBalloon = false;
                foreach (CrateMotion member in root.verticalStack)
                {
                    touchesWater |= member.InWater;
                    hasBalloon |= member.Airborne;
                }
                if (!touchesWater && !hasBalloon) continue;
                root.verticalStack.Sort(CompareStackHeight);
                root.SimulateVerticalGroup();
            }
        }

        private static int CompareStackHeight(CrateMotion left, CrateMotion right)
        {
            int height = left.body.Shape.bounds.center.y.CompareTo(right.body.Shape.bounds.center.y);
            return height != 0 ? height : left.GetInstanceID().CompareTo(right.GetInstanceID());
        }

        private CrateMotion FindStackRoot()
        {
            if (Airborne) return balloon.PayloadTaut ? balloon.Payload.Motion.FindStackRoot() : this;
            CrateMotion current = this;
            for (int level = 0; level < activeCrates.Count; level++)
            {
                SimulatedBody2D supportBody = current.body;
                float tolerance = supportBody.Skin * 4f;
                Collider2D hit = supportBody.Probe(Vector2.down, tolerance, out float distance);
                if (hit == null || !hit.TryGetComponent(out CrateMotion below) || !below.isActiveAndEnabled
                    || below.Airborne
                    || !activeCrates.Contains(below) || hit != below.body.Shape
                    || hit.bounds.center.y >= supportBody.Shape.bounds.center.y - supportBody.Skin
                    || distance > supportBody.Skin + below.body.Skin) break;
                current = below;
            }
            return current;
        }

        private void SimulateVerticalGroup()
        {
            float mass = 0f;
            float verticalVelocity = 0f;
            stackShapes.Clear();
            foreach (CrateMotion member in verticalStack)
            {
                member.coordinatedVertical = true;
                mass += member.Weight;
                verticalVelocity += member.Velocity.y * member.Weight;
                stackShapes.Add(member.body.Shape);
            }
            verticalVelocity /= mass;
            float dt = Time.fixedDeltaTime / 4f;
            for (int step = 0; step < 4; step++)
            {
                float force = 0f;
                float stiffness = 0f;
                float drag = 0f;
                float dampingRatio = 1f;
                float speedLimit = float.PositiveInfinity;
                float balloonLiftForce = 0f;
                float gravityForce = 0f;
                float neutralForceTolerance = 0f;
                foreach (CrateMotion member in verticalStack)
                {
                    member.SubmergedFraction = WaterVolume.SubmergedFraction(member.body.Shape.bounds);
                    force += member.VerticalForce(TurbineWind.AccelerationAt(member.body.Shape).y);
                    gravityForce += member.VerticalGravityForce();
                    stiffness += member.SurfaceStiffness();
                    if (member.ActiveBuoyancy)
                    {
                        drag += member.buoyancy.Drag(member.SubmergedFraction) * member.Weight;
                        dampingRatio = Mathf.Max(dampingRatio, member.buoyancy.VerticalDampingRatio);
                    }
                    if (member.Airborne)
                    {
                        drag += member.balloon.AirResistance * member.Weight;
                        balloonLiftForce += (member.balloon.LiftCapacity - member.balloon.HoverWeightTolerance)
                            * member.Gravity.crateGravity;
                        neutralForceTolerance += member.balloon.HoverWeightTolerance * member.Gravity.crateGravity;
                    }
                    speedLimit = Mathf.Min(speedLimit, Mathf.Min(member.VerticalSpeedLimit, member.Gravity.crateTerminalSpeed));
                }
                // Apply one dead band to the whole load; external wind and water forces remain independent.
                float neutralizedGravity = neutralForceTolerance > 0f
                    && Mathf.Abs(gravityForce) <= neutralForceTolerance + 0.0001f
                    ? gravityForce : Mathf.Clamp(gravityForce, -neutralForceTolerance, neutralForceTolerance);
                force -= neutralizedGravity;
                verticalVelocity = CrateFlotation.IntegrateVertical(verticalVelocity, force, mass, drag / mass,
                    stiffness, dampingRatio, speedLimit, dt);
                // Remaining lift sets ascent speed: neutral payloads cannot keep rising from inherited momentum.
                if (neutralForceTolerance > 0f || balloonLiftForce > 0f)
                    verticalVelocity = Mathf.Min(verticalVelocity,
                        speedLimit * Mathf.Clamp01(force / Mathf.Max(0.0001f, balloonLiftForce)));
                float travel = AllowedStackVertical(verticalVelocity * dt);
                float actualTravel = 0f;
                // Move leading crates first so solid casts never treat the stack itself as a ceiling/floor.
                for (int index = 0; index < verticalStack.Count; index++)
                {
                    CrateMotion member = verticalStack[travel > 0f ? verticalStack.Count - 1 - index : index];
                    float before = member.transform.position.y;
                    member.MoveConstrained(Vector2.up * travel, null, sharedVerticalMove: true);
                    float moved = member.transform.position.y - before;
                    member.verticalFrameDisplacement += moved;
                    member.Velocity = new Vector2(member.Velocity.x, moved / dt);
                    actualTravel += moved;
                }
                verticalVelocity = actualTravel / (verticalStack.Count * dt);
            }
        }

        private float AllowedStackVertical(float amount)
        {
            if (Mathf.Abs(amount) < 0.000001f) return 0f;
            Vector2 direction = Vector2.up * Mathf.Sign(amount);
            float allowed = Mathf.Abs(amount);
            foreach (CrateMotion member in verticalStack)
            {
                allowed = Mathf.Min(allowed, member.RemainingVerticalTravel);
                allowed = Mathf.Min(allowed, member.AllowedTetherVertical(direction, allowed));
                if (member.passenger != null && member.passenger.isActiveAndEnabled)
                {
                    // The player is carried after crate simulation; reserve clearance for the whole tick's travel.
                    Vector2 previousCarry = Vector2.up * member.verticalFrameDisplacement;
                    Vector2 permittedCarry = member.passenger.AllowedDisplacement(previousCarry + direction * allowed, member.body.Shape);
                    allowed = Mathf.Min(allowed, Mathf.Max(0f, Vector2.Dot(direction, permittedCarry - previousCarry)));
                }
                BoxCollider2D shape = member.body.Shape;
                Vector2 scale = member.transform.lossyScale;
                Vector2 size = Vector2.Scale(shape.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
                int count = Physics2D.BoxCast(member.transform.TransformPoint(shape.offset), size,
                    member.transform.eulerAngles.z, direction, member.body.SolidFilter, stackHits, allowed + member.body.Skin);
                for (int index = 0; index < count; index++)
                {
                    RaycastHit2D hit = stackHits[index];
                    if (hit.collider == null || stackShapes.Contains(hit.collider) || hit.collider.transform.IsChildOf(member.transform)
                        || (member.passenger != null && hit.collider == member.passenger.Shape)
                        || Vector2.Dot(hit.normal, direction) >= -0.01f) continue;
                    float gap = Mathf.Max(0f, hit.distance - member.body.Skin);
                    if (amount > 0f && hit.collider.TryGetComponent(out PlayerSwimming swimming) && swimming.InWater
                        && hit.collider.TryGetComponent(out SimulatedBody2D swimmer))
                    {
                        float lift = Mathf.Max(0f, allowed - gap);
                        float clearance = Mathf.Max(0f, swimmer.AllowedDisplacement(Vector2.up * lift, shape).y);
                        allowed = Mathf.Min(allowed, gap + clearance);
                    }
                    else allowed = Mathf.Min(allowed, gap);
                }
            }
            return Mathf.Sign(amount) * allowed;
        }

        private float AllowedTetherVertical(Vector2 direction, float requested)
        {
            if (InternalBalloonTether) return requested;
            if (tether == null || !tether.Attached) return requested;
            Vector2 start = transform.position;
            Vector2 target = start + direction * requested;
            if ((tether.ConstrainPosition(target) - target).sqrMagnitude < 0.0000000001f) return requested;
            float low = 0f;
            float high = requested;
            for (int iteration = 0; iteration < 12; iteration++)
            {
                float middle = (low + high) * 0.5f;
                target = start + direction * middle;
                if ((tether.ConstrainPosition(target) - target).sqrMagnitude < 0.0000000001f) low = middle;
                else high = middle;
            }
            return low;
        }

        public Vector2 FrameVelocity { get; private set; }

        private Vector2 ConstrainVelocity(Vector2 velocity)
        {
            if (tether != null) velocity = tether.ConstrainVelocity(velocity);
            if (Airborne) return balloon.LimitVelocity(balloon.ConstrainVelocity(velocity));
            if (InWater || coordinatedVertical)
                velocity.y = Mathf.Clamp(velocity.y, -maximumWaterVerticalSpeed, maximumWaterVerticalSpeed);
            return velocity;
        }

        public void SetPassenger(SimulatedBody2D candidate) => passenger = candidate;
        public bool IsPassenger(SimulatedBody2D candidate) => passenger == candidate;

        public void RemovePassenger(SimulatedBody2D candidate)
        {
            if (passenger == candidate) passenger = null;
        }

        public Vector2 PushBySwimming(Vector2 displacement, SimulatedBody2D swimmer, float dt)
        {
            if (!isActiveAndEnabled || !InWater || dt <= 0f || PushWeight >= PushableCrate.PlayerPushWeightLimit) return Vector2.zero;
            displacement.y = Mathf.Clamp(displacement.y, -maximumWaterVerticalSpeed * dt, maximumWaterVerticalSpeed * dt);
            Vector2 start = transform.position;
            MoveConstrained(displacement, swimmer.Shape);
            Vector2 moved = (Vector2)transform.position - start;
            Vector2 velocity = Velocity;
            if (Mathf.Abs(displacement.x) > 0.00001f) velocity.x = moved.x / dt;
            if (Mathf.Abs(displacement.y) > 0.00001f) velocity.y = moved.y / dt;
            Velocity = ConstrainVelocity(velocity);
            return moved;
        }

        private void MoveConstrained(Vector2 displacement, Collider2D ignored, bool sharedVerticalMove = false)
        {
            Vector2 start = transform.position;
            Vector2 target = start + displacement;
            bool constrainTether = tether != null && !(sharedVerticalMove && InternalBalloonTether);
            if (constrainTether) target = tether.ConstrainPosition(target);
            if (Airborne && !(sharedVerticalMove && coordinatedVertical)) target = balloon.ConstrainPosition(target);
            MoveSafely(target - start, ignored);
            // Resolve the rope and solid geometry together without teleporting through obstacles.
            for (int i = 0; i < 4 && constrainTether && tether.Attached; i++)
            {
                Vector2 position = transform.position;
                Vector2 correction = tether.ConstrainPosition(position) - position;
                if (correction.sqrMagnitude < 0.00000001f) break;
                MoveSafely(correction, ignored);
            }
        }

        private void MoveSafely(Vector2 displacement, Collider2D ignored)
        {
            bool limitVertical = InWater || coordinatedVertical || Airborne;
            if (limitVertical) displacement.y = Mathf.Clamp(displacement.y, -RemainingVerticalTravel, RemainingVerticalTravel);
            float beforeY = transform.position.y;
            Collider2D excluded = ignored;
            if (passenger != null && passenger.Shape != ignored)
            {
                displacement = passenger.AllowedDisplacement(displacement, body.Shape);
                excluded = passenger.Shape;
            }
            else if (displacement.y > 0f)
            {
                displacement.y = LiftSupportedBody(displacement.y, ignored);
            }
            if (pushable != null)
            {
                pushable.MoveHorizontal(displacement.x, excluded);
                body.Move(Vector2.up * displacement.y, ignored: excluded);
            }
            else body.Move(displacement, ignored: excluded);
            if (limitVertical) verticalTravel += Mathf.Abs(transform.position.y - beforeY);
        }

        private float LiftSupportedBody(float amount, Collider2D ignored)
        {
            Collider2D hit = body.Probe(Vector2.up, amount + body.Skin, out float distance, ignored);
            if (hit == null) return amount;
            float gap = Mathf.Max(0f, distance - body.Skin);
            float lift = Mathf.Max(0f, amount - gap);
            if (hit.TryGetComponent(out CrateMotion crate) && crate.isActiveAndEnabled
                && hit == crate.Body.Shape && hit.bounds.center.y > body.Shape.bounds.center.y + body.Skin)
            {
                Vector2 before = crate.transform.position;
                // Each upper crate resolves its own tether, passenger and next stack level before the support rises.
                crate.MoveConstrained(Vector2.up * lift, body.Shape);
                float moved = Mathf.Max(0f, crate.transform.position.y - before.y);
                return Mathf.Min(amount, gap + moved);
            }
            if (!InWater || !hit.TryGetComponent(out PlayerSwimming swimming) || !swimming.InWater
                || !hit.TryGetComponent(out SimulatedBody2D swimmer)) return amount;
            Vector2 allowed = swimmer.AllowedDisplacement(Vector2.up * lift, body.Shape);
            Vector2 swimmerBefore = swimmer.transform.position;
            swimmer.Move(allowed, ignored: body.Shape);
            return Mathf.Min(amount, gap + swimmer.transform.position.y - swimmerBefore.y);
        }
    }
}
