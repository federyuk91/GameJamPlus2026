using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(CrateMotion))]
    public sealed class BalloonLift : MonoBehaviour
    {
        [SerializeField] private RopeSimulation rope;
        [SerializeField] private CrateRopeAttachment attachment;
        [SerializeField] private CrateMotion motion;
        [SerializeField, Min(0.01f), Tooltip("Payload weight neutralized by this balloon, with Hover Weight Tolerance around it.")]
        private float liftCapacity = 0.5f;
        [SerializeField, Min(0f), Tooltip("Neutral weight margin on either side of Lift Capacity. Default 0.1 gives a 0.4-0.6 hover band.")]
        private float hoverWeightTolerance = 0.1f;
        [SerializeField, Min(0f), Tooltip("Upward acceleration in air. Must exceed Crate Gravity to rise; the default gravity is 66.")]
        private float liftAcceleration = 78f;
        [SerializeField, Min(0f)] private float airResistance = 2.5f;
        [SerializeField, Min(0f), Tooltip("Transfers the connected rider's weight and pendulum tension to the balloon. Cuts stop transferring detached fragments.")]
        private float swingInfluence = 0.35f;
        [SerializeField, Min(0.01f)] private float maximumHorizontalSpeed = 3f;
        [SerializeField, Min(0.01f)] private float maximumVerticalSpeed = 1.5f;

        public float MaximumVerticalSpeed => maximumVerticalSpeed;
        public float AirResistance => airResistance;
        public float LiftCapacity => liftCapacity;
        public float HoverWeightTolerance => Mathf.Clamp(hoverWeightTolerance, 0f, liftCapacity);
        public CrateMotion Motion => motion;
        public CrateRopeAttachment Payload => rope != null && rope.BalloonAnchor == this
            && rope.HasIntactLoadConnection && rope.Load.Motion != null && !rope.Load.Motion.UsesAirLift
            ? rope.Load : null;
        public bool PayloadTaut => Payload != null && Payload.LiftingBalloon == this
            && Vector2.Distance(rope.transform.position, Payload.AttachmentPoint) >= rope.Length - 0.01f;

        private void Awake()
        {
            if (motion == null) motion = GetComponent<CrateMotion>();
        }

        public Vector2 ConstrainPosition(Vector2 position)
        {
            CrateRopeAttachment payload = Payload;
            if (payload == null) return position;
            Vector2 offset = (Vector2)rope.transform.position - (Vector2)transform.position;
            return payload.AttachmentPoint + Vector2.ClampMagnitude(position + offset - payload.AttachmentPoint, rope.Length) - offset;
        }

        public Vector2 ConstrainVelocity(Vector2 velocity)
        {
            if (!PayloadTaut) return velocity;
            Vector2 normal = ((Vector2)rope.transform.position - Payload.AttachmentPoint).normalized;
            return velocity - normal * Mathf.Max(0f, Vector2.Dot(velocity - Payload.Motion.Velocity, normal));
        }

        public Vector2 ExternalAcceleration(Vector2 velocity, Collider2D shape)
        {
            Vector2 acceleration = TurbineWind.AccelerationAt(shape);
            if (attachment != null && attachment.HasRopeConnection && attachment.Rope != null
                && attachment.Rope.isActiveAndEnabled)
                acceleration += attachment.Rope.LoadPull(attachment, velocity) * swingInfluence;
            else if (rope != null && rope.isActiveAndEnabled)
                acceleration += rope.AnchorPull(velocity) * swingInfluence;
            return acceleration;
        }

        public Vector2 Integrate(Vector2 velocity, float gravity, Collider2D shape, float dt)
        {
            Vector2 acceleration = Vector2.up * (liftAcceleration - gravity);
            acceleration += ExternalAcceleration(velocity, shape);

            // Analytic drag avoids frame-rate dependent terminal speeds.
            float decay = Mathf.Exp(-airResistance * dt);
            float response = airResistance > 0.0001f ? (1f - decay) / airResistance : dt;
            velocity = velocity * decay + acceleration * response;
            return LimitVelocity(velocity);
        }

        public Vector2 LimitVelocity(Vector2 velocity)
        {
            return new Vector2(Mathf.Clamp(velocity.x, -maximumHorizontalSpeed, maximumHorizontalSpeed),
                Mathf.Clamp(velocity.y, -maximumVerticalSpeed, maximumVerticalSpeed));
        }
    }
}
