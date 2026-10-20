using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PlayerPlatformRide : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        private PropsMotion support;
        private Vector2 previousPosition;
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
            Vector2 position = support.transform.position;
            body.Move(position - previousPosition, ignored: support.Body.Shape);
            previousPosition = position;
        }

        public void UpdateSupport(bool canRide)
        {
            PropsMotion candidate = null;
            if (canRide && body.Grounded && body.GroundCollider != null)
                body.GroundCollider.TryGetComponent(out candidate);
            if (candidate != null && !candidate.isActiveAndEnabled) candidate = null;
            if (support != candidate)
            {
                Release();
                support = candidate;
                if (support != null) support.SetPassenger(body);
            }
            if (support != null) previousPosition = support.transform.position;
        }

        public void Release()
        {
            if (support != null) support.RemovePassenger(body);
            support = null;
        }

        private void OnDisable() => Release();
    }
}
