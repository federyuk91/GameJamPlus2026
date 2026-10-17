using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(CrateMotion), typeof(PushableCrate))]
    public sealed class CrateRopeAttachment : MonoBehaviour
    {
        [SerializeField] private RopeSimulation rope;
        [SerializeField] private PushableCrate pushable;
        [SerializeField] private Vector2 attachmentOffset;
        public bool Attached { get; private set; }
        public Vector2 AttachmentPoint => transform.TransformPoint(attachmentOffset);

        private void Awake()
        {
            if (pushable == null) pushable = GetComponent<PushableCrate>();
        }

        private void OnEnable()
        {
            if (rope == null || !rope.BindLoad(this)) return;
            Attached = true;
            pushable.enabled = false;
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
            return velocity - normal * Mathf.Max(0f, Vector2.Dot(velocity, normal));
        }

        private void HandleCut(RopeSimulation source) => Detach();

        private void Detach()
        {
            if (!Attached) return;
            Attached = false;
            rope.Cut -= HandleCut;
            rope.ReleaseLoad(this);
            pushable.enabled = true;
        }

        private void OnDisable() => Detach();
    }
}
