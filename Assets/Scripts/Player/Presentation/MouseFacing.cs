using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(80), DisallowMultipleComponent, RequireComponent(typeof(PlayerInputReader))]
    public sealed class MouseFacing : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private BoxCollider2D playerCollider;
        [SerializeField] private PlayerLedgeClimb ledgeClimb;
        [SerializeField] private PlayerCrateInteraction crateInteraction;
        [SerializeField] private PlayerRopeInteraction ropeInteraction;
        [SerializeField] private bool spriteFacesRight = true;
        private float unflippedColliderOffsetX;
        public bool FacingRight => visual == null || visual.flipX != spriteFacesRight;
        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (playerCollider == null) playerCollider = GetComponent<BoxCollider2D>();
            if (ledgeClimb == null) ledgeClimb = GetComponent<PlayerLedgeClimb>();
            if (crateInteraction == null) crateInteraction = GetComponent<PlayerCrateInteraction>();
            if (ropeInteraction == null) ropeInteraction = GetComponent<PlayerRopeInteraction>();
            if (playerCollider != null) unflippedColliderOffsetX = playerCollider.offset.x;
            MirrorCollider();
        }

        private void FixedUpdate() => RefreshFacing();
        private void LateUpdate() => RefreshFacing();

        private void OnEnable()
        {
            if (ropeInteraction != null) ropeInteraction.JumpLaunched += FaceRopeJump;
        }

        private void OnDisable()
        {
            if (ropeInteraction != null) ropeInteraction.JumpLaunched -= FaceRopeJump;
        }

        private void FaceRopeJump(Vector2 velocity) => FaceDirection(velocity.x);

        private void RefreshFacing()
        {
            float direction = ledgeClimb != null && ledgeClimb.Active ? ledgeClimb.Direction : input.Horizontal;
            if ((ledgeClimb == null || !ledgeClimb.Active) && crateInteraction != null && crateInteraction.HeldCrate != null)
                direction = crateInteraction.HeldCrate.transform.position.x - transform.position.x;
            FaceDirection(direction);
        }

        private void FaceDirection(float direction)
        {
            if (visual != null && Mathf.Abs(direction) > 0.01f)
                visual.flipX = (direction < 0f) == spriteFacesRight;
            MirrorCollider();
        }

        private void MirrorCollider()
        {
            if (visual == null || playerCollider == null) return;
            Vector2 offset = playerCollider.offset;
            float mirroredX = visual.flipX ? -unflippedColliderOffsetX : unflippedColliderOffsetX;
            if (Mathf.Approximately(offset.x, mirroredX)) return;
            offset.x = mirroredX;
            playerCollider.offset = offset;
            // The custom movement queries colliders with automatic transform synchronization disabled.
            Physics2D.SyncTransforms();
        }
    }
}
