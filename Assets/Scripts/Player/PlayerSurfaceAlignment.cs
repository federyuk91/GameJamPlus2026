using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PlayerSurfaceAlignment : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private Transform visualPivot;
        private Vector3 restLocalPosition;
        private Quaternion restLocalRotation;

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (visualPivot == null) return;
            restLocalPosition = visualPivot.localPosition;
            restLocalRotation = visualPivot.localRotation;
        }

        private void LateUpdate()
        {
            if (visualPivot == null) return;
            visualPivot.localPosition = restLocalPosition;
            visualPivot.localRotation = restLocalRotation;
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
