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
        [SerializeField] private PlayerSwimming swimming;
        [SerializeField] private PlayerAnimation playerAnimation;
        [SerializeField, Tooltip("Offset verticale applicato solo alla posa di nuoto, senza modificare il collider o le altre animazioni.")]
        private float swimmingVisualOffsetY;
        private Vector3 restLocalPosition;
        private Quaternion restLocalRotation;

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (digging == null) digging = GetComponent<PlayerDigging>();
            if (facing == null) facing = GetComponent<MouseFacing>();
            if (swimming == null) swimming = GetComponent<PlayerSwimming>();
            if (playerAnimation == null) playerAnimation = GetComponent<PlayerAnimation>();
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
            // La posa di nuoto resta ancorata al corpo anche quando il collider tocca il fondo.
            if (swimming != null && swimming.isActiveAndEnabled && swimming.InWater)
            {
                if (playerAnimation != null && playerAnimation.IsSwimmingPose)
                    visualPivot.localPosition += Vector3.up * swimmingVisualOffsetY;
                return;
            }
            GroundSurfaceAlignment.Align(body, visualPivot, Vector3.zero);
        }

        private void OnDisable()
        {
            if (visualPivot == null) return;
            visualPivot.localPosition = restLocalPosition;
            visualPivot.localRotation = restLocalRotation;
        }
    }
}
