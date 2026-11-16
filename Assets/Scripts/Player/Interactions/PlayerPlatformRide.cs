using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PlayerPlatformRide : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        private PropsMotion support;
        private Vector3 supportLocalPoint;
        private Vector2 supportWorldPoint;
        public bool HasSupport => support != null;
        public Vector2 SupportVelocity => support != null ? support.FrameVelocity : Vector2.zero;

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
        }

        public void Carry()
        {
            if (support == null) return;
            if (!support.isActiveAndEnabled || !support.Body.Shape.enabled)
            {
                Release();
                return;
            }
            Vector2 position = support.transform.TransformPoint(supportLocalPoint);
            body.Move(position - supportWorldPoint, ignored: support.Body.Shape);
            supportWorldPoint = position;
        }

        public Vector2 UpdateSupport(bool canRide, bool inheritOnRelease)
        {
            Vector2 detachedVelocity = Vector2.zero;
            PropsMotion candidate = null;
            if (canRide && body.Grounded && body.GroundCollider != null)
                body.GroundCollider.TryGetComponent(out candidate);
            if (candidate != null && !candidate.isActiveAndEnabled) candidate = null;
            if (support != candidate)
            {
                // Ground movement is relative to the carrier; preserve its motion when stepping off.
                if (support != null && candidate == null && inheritOnRelease)
                    detachedVelocity = support.FrameVelocity;
                Release();
                support = candidate;
                if (support != null) support.SetPassenger(body);
            }
            if (support != null)
            {
                Bounds bounds = body.Shape.bounds;
                supportWorldPoint = new Vector2(bounds.center.x, bounds.min.y);
                supportLocalPoint = support.transform.InverseTransformPoint(
                    new Vector3(supportWorldPoint.x, supportWorldPoint.y, transform.position.z));
            }
            return detachedVelocity;
        }

        public void Release()
        {
            if (support != null) support.RemovePassenger(body);
            support = null;
        }

        private void OnDisable() => Release();
    }
}
