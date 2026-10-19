using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(CrateMotion), typeof(PushableCrate))]
    public sealed class CrateRopeAttachment : MonoBehaviour
    {
        [SerializeField] private RopeSimulation rope;
        [SerializeField] private PushableCrate pushable;
        [SerializeField] private CrateMotion motion;
        [SerializeField] private Vector2 attachmentOffset;
        private bool tailAttached;
        public bool Attached { get; private set; }
        public bool HasRopeConnection => Attached || tailAttached;
        public bool InWater => motion != null && motion.InWater;
        public bool Floats => motion != null && (motion.InWater || motion.UsesAirLift);
        public RopeSimulation Rope => rope;
        public CrateMotion Motion => motion;
        public BalloonLift LiftingBalloon => Attached && rope != null && rope.HasIntactLoadConnection
            && rope.BalloonAnchor != null && rope.BalloonAnchor.isActiveAndEnabled
            && rope.transform.position.y > AttachmentPoint.y + 0.01f ? rope.BalloonAnchor : null;
        public float LiftCapacity => LiftingBalloon != null ? LiftingBalloon.LiftCapacity : 0f;
        public Vector2 AttachmentPoint => transform.TransformPoint(attachmentOffset);

        private void Awake()
        {
            if (pushable == null) pushable = GetComponent<PushableCrate>();
            if (motion == null) motion = GetComponent<CrateMotion>();
        }

        private void OnEnable()
        {
            if (rope == null || !rope.BindLoad(this)) return;
            Attached = true;
            pushable.enabled = rope.BalloonAnchor != null;
            rope.Cut += HandleCut;
        }

        public Vector2 ConstrainPosition(Vector2 position)
        {
            if (!Attached) return position;
            Vector2 offset = (Vector2)transform.TransformVector(attachmentOffset);
            Vector2 anchor = rope.transform.position;
            return anchor + Vector2.ClampMagnitude(position + offset - anchor, rope.Length) - offset;
        }

        public Vector2 ConstrainVelocity(Vector2 velocity)
        {
            if (!Attached) return velocity;
            Vector2 radial = AttachmentPoint - (Vector2)rope.transform.position;
            if (radial.magnitude < rope.Length - 0.01f) return velocity;
            Vector2 normal = radial.normalized;
            Vector2 anchorVelocity = LiftingBalloon != null ? LiftingBalloon.Motion.Velocity : Vector2.zero;
            return velocity - normal * Mathf.Max(0f, Vector2.Dot(velocity - anchorVelocity, normal));
        }

        private void HandleCut(RopeSimulation source)
        {
            if (source.KeepCrateSideAfterCut && source.Chain.EndpointConnectedSegmentCount > 0)
            {
                // Release the floor constraint while keeping the remaining rope tied to the crate.
                Attached = false;
                tailAttached = true;
                pushable.enabled = true;
                return;
            }
            Detach();
        }

        public void Detach()
        {
            if (!HasRopeConnection) return;
            Attached = false;
            tailAttached = false;
            rope.Cut -= HandleCut;
            rope.ReleaseLoad(this);
            pushable.enabled = true;
        }

        private void OnDisable() => Detach();
    }
}
