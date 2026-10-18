using MousePlatformer;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [DefaultExecutionOrder(1000), DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class RopeBiteProgressView : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private PlayerRopeBite bite;
        [SerializeField] private Vector3 worldOffset = new Vector3(0.55f, 1f, 0f);
        [SerializeField, Min(8f)] private float diameter = 24f;
        [SerializeField, Min(1f)] private float thickness = 3f;
        [SerializeField] private Color trackColor = new Color(0f, 0f, 0f, 0.7f);
        [SerializeField] private Color fillColor = new Color(1f, 0.9f, 0.55f, 1f);

        private VisualElement root;
        private VisualElement ring;
        private float progress;

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
        }

        private void LateUpdate()
        {
            VisualElement currentRoot = document != null ? document.rootVisualElement : null;
            if (currentRoot != root || ring == null)
            {
                RemoveRing();
                root = currentRoot;
                if (root == null) return;
                ring = new VisualElement { name = "rope-bite-progress", pickingMode = PickingMode.Ignore };
                ring.style.position = Position.Absolute;
                ring.style.display = DisplayStyle.None;
                ring.generateVisualContent += DrawRing;
                root.Add(ring);
            }

            bool visible = bite != null && bite.isActiveAndEnabled && bite.BiteProgress > 0f
                && gameplayCamera != null && root.panel != null && Time.timeScale > 0f;
            Vector3 worldPosition = visible ? bite.transform.position + worldOffset : Vector3.zero;
            if (visible) visible = gameplayCamera.WorldToViewportPoint(worldPosition).z > 0f;
            ring.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;

            Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, worldPosition, gameplayCamera);
            Vector2 localPosition = root.WorldToLocal(panelPosition);
            ring.style.left = localPosition.x - diameter * 0.5f;
            ring.style.top = localPosition.y - diameter * 0.5f;
            ring.style.width = diameter;
            ring.style.height = diameter;
            progress = bite.BiteProgress;
            ring.MarkDirtyRepaint();
        }

        private void DrawRing(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            float stroke = Mathf.Clamp(thickness, 1f, diameter * 0.25f);
            Vector2 center = new Vector2(diameter * 0.5f, diameter * 0.5f);
            float radius = (diameter - stroke) * 0.5f;
            painter.lineWidth = stroke;
            painter.strokeColor = trackColor;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.Stroke();
            painter.strokeColor = fillColor;
            painter.BeginPath();
            painter.Arc(center, radius, -90f, -90f + Mathf.Clamp01(progress) * 360f);
            painter.Stroke();
        }

        private void OnDisable() => RemoveRing();

        private void RemoveRing()
        {
            if (ring != null)
            {
                ring.generateVisualContent -= DrawRing;
                ring.RemoveFromHierarchy();
            }
            ring = null;
            root = null;
        }
    }
}
