using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerInputReader))]
    public sealed class MouseFacing : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private PlayerLedgeClimb ledgeClimb;
        [SerializeField] private PlayerCrateInteraction crateInteraction;
        [SerializeField] private bool spriteFacesRight = true;
        public bool FacingRight => visual == null || visual.flipX != spriteFacesRight;
        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (ledgeClimb == null) ledgeClimb = GetComponent<PlayerLedgeClimb>();
            if (crateInteraction == null) crateInteraction = GetComponent<PlayerCrateInteraction>();
        }
        private void LateUpdate()
        {
            float direction = ledgeClimb != null && ledgeClimb.Active ? ledgeClimb.Direction : input.Horizontal;
            if ((ledgeClimb == null || !ledgeClimb.Active) && crateInteraction != null && crateInteraction.HeldCrate != null)
                direction = crateInteraction.HeldCrate.transform.position.x - transform.position.x;
            if (visual != null && Mathf.Abs(direction) > 0.01f)
                visual.flipX = (direction < 0f) == spriteFacesRight;
        }
    }
}
