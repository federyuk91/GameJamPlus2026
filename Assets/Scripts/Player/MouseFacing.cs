using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer), typeof(PlayerInputReader))]
    public sealed class MouseFacing : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private bool spriteFacesRight = true;
        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (visual == null) visual = GetComponent<SpriteRenderer>();
        }
        private void LateUpdate()
        {
            if (Mathf.Abs(input.Horizontal) > 0.01f)
                visual.flipX = (input.Horizontal < 0f) == spriteFacesRight;
        }
    }
}
