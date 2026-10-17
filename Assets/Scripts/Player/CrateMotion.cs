using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(-100), DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class CrateMotion : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField] private CrateBuoyancy buoyancy;
        [SerializeField] private CrateRopeAttachment tether;
        [SerializeField] private PushableCrate pushable;
        [SerializeField] private Vector2 initialVelocity;
        [SerializeField, Min(0f)] private float airDrag = 0.35f;
        private SimulatedBody2D passenger;
        private GravitySettings Gravity => physicsSettings != null ? physicsSettings.gravity : PhysicsSettings.DefaultGravity;
        public SimulatedBody2D Body => body;
        public Vector2 Velocity { get; private set; }
        public float SubmergedFraction { get; private set; }
        public bool InWater => SubmergedFraction > 0f;
        public float Weight => pushable != null ? pushable.Weight : 1f;

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (buoyancy == null) buoyancy = GetComponent<CrateBuoyancy>();
            if (tether == null) tether = GetComponent<CrateRopeAttachment>();
            if (pushable == null) pushable = GetComponent<PushableCrate>();
            Velocity = initialVelocity;
        }

        private void FixedUpdate()
        {
            const int substeps = 4;
            float dt = Time.fixedDeltaTime / substeps;
            Vector2 frameStart = transform.position;
            for (int i = 0; i < substeps; i++)
            {
                SubmergedFraction = WaterVolume.SubmergedFraction(body.Shape.bounds);
                Vector2 velocity = Velocity;
                if (buoyancy != null && buoyancy.isActiveAndEnabled)
                    velocity = buoyancy.Integrate(velocity, Gravity.crateGravity, SubmergedFraction, dt);
                else
                    velocity.y -= Gravity.crateGravity * dt;
                velocity.x *= Mathf.Exp(-airDrag * (1f - SubmergedFraction) * dt);
                velocity.y = Mathf.Clamp(velocity.y, -Gravity.crateTerminalSpeed, Gravity.crateTerminalSpeed);
                Vector2 start = transform.position;
                MoveConstrained(velocity * dt, null);
                velocity = ((Vector2)transform.position - start) / dt;
                if (body.Grounded && !InWater && (tether == null || !tether.Attached)) velocity.x = 0f;
                Velocity = tether != null ? tether.ConstrainVelocity(velocity) : velocity;
            }
            FrameVelocity = ((Vector2)transform.position - frameStart) / Time.fixedDeltaTime;
        }

        public Vector2 FrameVelocity { get; private set; }

        public void SetPassenger(SimulatedBody2D candidate) => passenger = candidate;
        public bool IsPassenger(SimulatedBody2D candidate) => passenger == candidate;

        public void RemovePassenger(SimulatedBody2D candidate)
        {
            if (passenger == candidate) passenger = null;
        }

        public Vector2 PushBySwimming(Vector2 displacement, SimulatedBody2D swimmer, float dt)
        {
            if (!isActiveAndEnabled || !InWater || dt <= 0f) return Vector2.zero;
            Vector2 start = transform.position;
            MoveConstrained(displacement, swimmer.Shape);
            Vector2 moved = (Vector2)transform.position - start;
            Vector2 velocity = Velocity;
            if (Mathf.Abs(displacement.x) > 0.00001f) velocity.x = moved.x / dt;
            if (Mathf.Abs(displacement.y) > 0.00001f) velocity.y = moved.y / dt;
            Velocity = tether != null ? tether.ConstrainVelocity(velocity) : velocity;
            return moved;
        }

        private void MoveConstrained(Vector2 displacement, Collider2D ignored)
        {
            Vector2 start = transform.position;
            Vector2 target = start + displacement;
            if (tether != null) target = tether.ConstrainPosition(target);
            MoveSafely(target - start, ignored);
            // Resolve the rope and solid geometry together without teleporting through obstacles.
            for (int i = 0; i < 4 && tether != null && tether.Attached; i++)
            {
                Vector2 position = transform.position;
                Vector2 correction = tether.ConstrainPosition(position) - position;
                if (correction.sqrMagnitude < 0.00000001f) break;
                MoveSafely(correction, ignored);
            }
        }

        private void MoveSafely(Vector2 displacement, Collider2D ignored)
        {
            Collider2D excluded = ignored;
            if (passenger != null && passenger.Shape != ignored)
            {
                displacement = passenger.AllowedDisplacement(displacement, body.Shape);
                excluded = passenger.Shape;
            }
            else if (InWater && displacement.y > 0f)
            {
                displacement.y = LiftSwimmer(displacement.y, ignored);
            }
            body.Move(displacement, ignored: excluded);
        }

        private float LiftSwimmer(float amount, Collider2D ignored)
        {
            Collider2D hit = body.Probe(Vector2.up, amount + body.Skin, out float distance, ignored);
            if (hit == null || !hit.TryGetComponent(out PlayerSwimming swimming) || !swimming.InWater
                || !hit.TryGetComponent(out SimulatedBody2D swimmer)) return amount;
            float gap = Mathf.Max(0f, distance - body.Skin);
            float lift = Mathf.Max(0f, amount - gap);
            Vector2 allowed = swimmer.AllowedDisplacement(Vector2.up * lift, body.Shape);
            Vector2 before = swimmer.transform.position;
            swimmer.Move(allowed, ignored: body.Shape);
            return Mathf.Min(amount, gap + swimmer.transform.position.y - before.y);
        }
    }
}
