using GameJamPlus.Gameplay;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MousePlatformer
{
    [MovedFrom(true, sourceNamespace: "MousePlatformer", sourceAssembly: "Assembly-CSharp", sourceClassName: "CrateRopeAttachment")]
    [RequireComponent(typeof(PropsMotion), typeof(SimulatedBody2D))]
    public sealed class RopeAttachment : MonoBehaviour
    {
        [SerializeField] private RopeSimulation rope;
        [SerializeField] private Pushable pushable;
        [SerializeField] private PropsMotion motion;
        [SerializeField, Tooltip("Optional crushing damage. Disabled while the rope is intact and enabled when it is cut.")]
        private FallingCrushHazard crushHazard;
        [SerializeField] private Vector2 attachmentOffset;
        private bool tailAttached;
        public bool Attached { get; private set; }
        public bool ConstrainsMotion => isActiveAndEnabled && Attached && rope != null && rope.isActiveAndEnabled;
        public bool HasRopeConnection => Attached || tailAttached;
        public bool InWater => motion != null && motion.InWater;
        public bool Floats => motion != null && (motion.InWater || motion.UsesAirLift);
        public RopeSimulation Rope => rope;
        public PropsMotion Motion => motion;
        public float RopePassengerWeight => HasRopeConnection && rope != null && rope.isActiveAndEnabled
            ? rope.LoadRiderWeight(this) : 0f;
        public BalloonLift LiftingBalloon => Attached && rope != null && rope.HasIntactLoadConnection
            && rope.BalloonAnchor != null && rope.BalloonAnchor.isActiveAndEnabled
            && rope.transform.position.y > AttachmentPoint.y + 0.01f ? rope.BalloonAnchor : null;
        public float LiftCapacity => LiftingBalloon != null ? LiftingBalloon.LiftCapacity : 0f;
        public Vector2 AttachmentPoint => transform.TransformPoint(attachmentOffset);

        private void Awake()
        {
            if (pushable == null) pushable = GetComponent<Pushable>();
            if (motion == null) motion = GetComponent<PropsMotion>();
            if (crushHazard == null) crushHazard = GetComponent<FallingCrushHazard>();
            motion.RegisterRopeAttachment(this);
        }

        private void OnEnable()
        {
            if (rope == null || !rope.BindLoad(this)) return;
            Attached = true;
            motion.RegisterRopeAttachment(this);
            RefreshBodyControls();
            rope.Cut += HandleCut;
        }

        public Vector2 ConstrainPosition(Vector2 position)
        {
            if (!ConstrainsMotion) return position;
            Vector2 offset = (Vector2)transform.TransformVector(attachmentOffset);
            Vector2 anchor = rope.transform.position;
            return anchor + Vector2.ClampMagnitude(position + offset - anchor, rope.Length) - offset;
        }

        public Vector2 ConstrainVelocity(Vector2 velocity)
        {
            if (!ConstrainsMotion) return velocity;
            Vector2 radial = AttachmentPoint - (Vector2)rope.transform.position;
            if (radial.magnitude < rope.Length - rope.TautTolerance) return velocity;
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
                RefreshBodyControls();
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
            RefreshBodyControls();
        }

        private void RefreshBodyControls()
        {
            bool anchored = false;
            bool hasBalloon = false;
            bool supportedFromAbove = false;
            bool multipleAttachments = motion.RopeAttachments.Count > 1;
            foreach (RopeAttachment attachment in motion.RopeAttachments)
            {
                if (attachment == null || !attachment.ConstrainsMotion) continue;
                anchored = true;
                hasBalloon |= attachment.Rope.BalloonAnchor != null;
                supportedFromAbove |= attachment.Rope.transform.position.y > attachment.AttachmentPoint.y + 0.01f;
            }
            // One cut must not release controls still held by another intact rope.
            if (crushHazard != null) crushHazard.enabled = multipleAttachments ? !supportedFromAbove : !anchored;
            if (pushable != null) pushable.enabled = !anchored || hasBalloon || multipleAttachments;
        }

        private void OnDisable() => Detach();
    }
}
