using UnityEngine;

namespace MousePlatformer
{
    [ExecuteAlways, DefaultExecutionOrder(235), DisallowMultipleComponent]
    public sealed class DeskSurfaceAnchor : MonoBehaviour
    {
        [SerializeField, Tooltip("Sprites belonging to this decoration. Their combined lower bound rests on the desk, without separating the parts.")]
        private SpriteRenderer[] visuals = new SpriteRenderer[0];
        [SerializeField, Tooltip("Desk surface whose upper world bound supports this decoration.")]
        private SpriteRenderer deskSurface;
        [SerializeField, Tooltip("In Edit Mode, automatically derives Height Offset from the authored position relative to the desk. During gameplay the captured offset stays fixed.")]
        private bool autoOffsetFromEditorPosition = true;
        [SerializeField, Tooltip("Additional world offset from the desk. Zero places the decoration directly on the surface.")]
        private float heightOffset;

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.IsPlaying(gameObject))
            {
                if (autoOffsetFromEditorPosition) CaptureHeightOffset();
                return;
            }
#endif
            AlignToSurface();
        }

        public void Configure(SpriteRenderer surface, SpriteRenderer[] defaultVisuals)
        {
            deskSurface = surface;
            if (visuals == null || visuals.Length == 0) visuals = defaultVisuals;
        }

        public void AlignToSurface()
        {
#if UNITY_EDITOR
            if (autoOffsetFromEditorPosition && !Application.IsPlaying(gameObject))
            {
                CaptureHeightOffset();
                return;
            }
#endif
            if (!TryGetBounds(out Bounds bounds)) return;
            Vector3 position = transform.position;
            position.y += deskSurface.bounds.max.y + heightOffset - bounds.min.y;
            if (transform.position != position) transform.position = position;
        }

        private void CaptureHeightOffset()
        {
            if (!TryGetBounds(out Bounds bounds)) return;
            float offset = bounds.min.y - deskSurface.bounds.max.y;
            if (Mathf.Approximately(heightOffset, offset)) return;
            heightOffset = offset;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        private bool TryGetBounds(out Bounds bounds)
        {
            bounds = default;
            if (deskSurface == null || deskSurface.sprite == null || visuals == null) return false;
            bool hasBounds = false;
            foreach (SpriteRenderer visual in visuals)
            {
                if (visual == null || visual.sprite == null) continue;
                if (!hasBounds) bounds = visual.bounds;
                else bounds.Encapsulate(visual.bounds);
                hasBounds = true;
            }
            return hasBounds;
        }
    }
}
