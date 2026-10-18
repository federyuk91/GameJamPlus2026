using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(85), DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PlayerSurfaceAlignment : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private Transform visualPivot;
        [SerializeField] private PlayerDigging digging;
        [SerializeField] private MouseFacing facing;
        private Vector3 restLocalPosition;
        private Quaternion restLocalRotation;

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (digging == null) digging = GetComponent<PlayerDigging>();
            if (facing == null) facing = GetComponent<MouseFacing>();
            if (visualPivot == null) return;
            restLocalPosition = visualPivot.localPosition;
            restLocalRotation = visualPivot.localRotation;
        }

        private void LateUpdate()
        {
            if (visualPivot == null) return;
            visualPivot.localPosition = restLocalPosition;
            visualPivot.localRotation = restLocalRotation;
            if (digging != null && digging.isActiveAndEnabled && digging.IsDigging && digging.Direction.sqrMagnitude > 0.01f)
            {
                Vector2 forward = facing == null || facing.FacingRight ? Vector2.right : Vector2.left;
                float digAngle = Vector2.SignedAngle(forward, digging.Direction);
                visualPivot.rotation = Quaternion.AngleAxis(digAngle, Vector3.forward) * restLocalRotation;
                return;
            }
            if (!body.HasGroundContact) return;

            Vector2 normal = body.GroundNormal;
            Vector2 feet = visualPivot.position;
            // Anchor the visual feet to the surface while the collision box stays upright.
            feet += normal * (body.Skin - Vector2.Dot(feet - body.GroundPoint, normal));
            visualPivot.position = new Vector3(feet.x, feet.y, visualPivot.position.z);
            float angle = Vector2.SignedAngle(Vector2.up, normal);
            visualPivot.rotation = Quaternion.AngleAxis(angle, Vector3.forward) * restLocalRotation;
        }

        private void OnDisable()
        {
            if (visualPivot == null) return;
            visualPivot.localPosition = restLocalPosition;
            visualPivot.localRotation = restLocalRotation;
        }
    }
}
