using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerInputReader))]
    public sealed class MouseFacing : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private PlayerLedgeClimb ledgeClimb;
        [SerializeField] private bool spriteFacesRight = true;
        public bool FacingRight => visual == null || visual.flipX != spriteFacesRight;
        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (ledgeClimb == null) ledgeClimb = GetComponent<PlayerLedgeClimb>();
        }
        private void LateUpdate()
        {
            float direction = ledgeClimb != null && ledgeClimb.Active ? ledgeClimb.Direction : input.Horizontal;
            if (visual != null && Mathf.Abs(direction) > 0.01f)
                visual.flipX = (direction < 0f) == spriteFacesRight;
        }
    }
}
