using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(PropsMotion), typeof(SimulatedBody2D))]
    public sealed class BalloonLift : MonoBehaviour
    {
        [SerializeField] private RopeSimulation rope;
        [SerializeField] private RopeAttachment attachment;
        [SerializeField] private PropsMotion motion;
        [SerializeField, Min(0.01f), Tooltip("Payload weight neutralized by this balloon, with Hover Weight Tolerance around it.")]
        private float liftCapacity = 0.5f;
        [SerializeField, Min(0f), Tooltip("Neutral weight margin on either side of Lift Capacity. Default 0.1 gives a 0.4-0.6 hover band.")]
        private float hoverWeightTolerance = 0.1f;
        [SerializeField, Min(0f), Tooltip("Upward lift acceleration in air, compared with this body's effective gravity (world gravity × Gravity Scale). Migrated balloon bodies retain gravity 66.")]
        private float liftAcceleration = 78f;
        [SerializeField, Min(0f), Tooltip("Transfers the connected rider's weight and pendulum tension to the balloon. Cuts stop transferring detached fragments.")]
        private float swingInfluence = 0.35f;
        public float AirResistance => motion.Body.AirDrag;
        public float LiftCapacity => liftCapacity;
        public float HoverWeightTolerance => Mathf.Clamp(hoverWeightTolerance, 0f, liftCapacity);
        public PropsMotion Motion => motion;
        public float RopePassengerWeight => rope != null && rope.isActiveAndEnabled && rope.BalloonAnchor == this
            ? rope.AnchorRiderWeight : 0f;
        public RopeAttachment Payload => rope != null && rope.BalloonAnchor == this
            && rope.HasIntactLoadConnection && rope.Load.Motion != null && !rope.Load.Motion.UsesAirLift
            ? rope.Load : null;
        public bool PayloadTaut => Payload != null && Payload.LiftingBalloon == this
            && Vector2.Distance(rope.transform.position, Payload.AttachmentPoint) >= rope.Length - rope.TautTolerance;

        private void Awake()
        {
            if (motion == null) motion = GetComponent<PropsMotion>();
        }

        public Vector2 ConstrainPosition(Vector2 position)
        {
            RopeAttachment payload = Payload;
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
            Vector2 riderPull = Vector2.zero;
            if (attachment != null && attachment.HasRopeConnection && attachment.Rope != null
                && attachment.Rope.isActiveAndEnabled)
                riderPull = attachment.Rope.LoadPull(attachment, velocity);
            else if (rope != null && rope.isActiveAndEnabled)
                riderPull = rope.AnchorPull(velocity);
            // Vertical body weight is included in the load balance; only swing steering is added here.
            acceleration.x += riderPull.x * swingInfluence / Mathf.Max(0.01f, motion != null ? motion.Weight : 1f);
            return acceleration;
        }

        public Vector2 Integrate(Vector2 velocity, float gravity, Collider2D shape, float supportedWeight, float dt)
        {
            float netCapacity = liftCapacity - supportedWeight;
            float remainingCapacity = Mathf.Sign(netCapacity) * Mathf.Max(0f, Mathf.Abs(netCapacity) - HoverWeightTolerance);
            float capacityRatio = remainingCapacity / Mathf.Max(0.0001f, liftCapacity - HoverWeightTolerance);
            Vector2 acceleration = Vector2.up * ((liftAcceleration - gravity) * capacityRatio);
            acceleration += ExternalAcceleration(velocity, shape);

            // Analytic drag avoids frame-rate dependent terminal speeds.
            float airDrag = AirResistance;
            float decay = Mathf.Exp(-airDrag * dt);
            float response = airDrag > 0.0001f ? (1f - decay) / airDrag : dt;
            velocity = velocity * decay + acceleration * response;
            if (acceleration.y >= 0f)
            {
                float availableLiftRatio = acceleration.y / Mathf.Max(0.0001f, Mathf.Abs(liftAcceleration - gravity));
                velocity.y = Mathf.Min(velocity.y, motion.Body.MaxSpeed * Mathf.Clamp01(availableLiftRatio));
            }
            return LimitVelocity(velocity);
        }

        public Vector2 LimitVelocity(Vector2 velocity)
        {
            return motion.Body.LimitVelocity(velocity);
        }
    }
}
