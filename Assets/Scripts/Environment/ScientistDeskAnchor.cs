using UnityEngine;
using UnityEngine.Rendering;

namespace MousePlatformer
{
    [ExecuteAlways, DefaultExecutionOrder(240), DisallowMultipleComponent]
    public sealed class ScientistDeskAnchor : MonoBehaviour
    {
        [SerializeField, Tooltip("Scientist bust whose cut lower edge must remain hidden by the desk.")]
        private SpriteRenderer scientist;
        [SerializeField, Tooltip("Desk surface renderer. Its upper world bound defines the bust's baseline.")]
        private SpriteRenderer deskSurface;
        [SerializeField, Tooltip("Sorting Group of the fixed desk plane, used to keep the bust behind the desk.")]
        private SortingGroup deskSortingGroup;
        [SerializeField, Tooltip("Opaque wall plane. The bust must draw above it or the wall would hide the scientist completely.")]
        private SortingGroup wallSortingGroup;
        [SerializeField, Tooltip("Desk decorations in front of the scientist. Keeps the bust and lens behind these objects.")]
        private SortingGroup decorationsSortingGroup;
        [SerializeField, Min(0f), Tooltip("World units of the bust hidden below the desk's upper edge.")]
        private float coverDepth = 0.1875f;
        [SerializeField, Min(0.01f), Tooltip("Minimum world Z separation behind the desk; leaves room for the lens in front of the bust.")]
        private float minimumDepthBehindDesk = 0.125f;

        public SpriteRenderer Scientist => scientist;

        public void ConfigureDepthPlanes(SortingGroup desk, SortingGroup wall, SortingGroup decorations)
        {
            deskSortingGroup = desk;
            wallSortingGroup = wall;
            decorationsSortingGroup = decorations;
        }

        private void LateUpdate() => AlignToDesk();

        public void AlignToDesk()
        {
            if (scientist == null || scientist.sprite == null || deskSurface == null || deskSurface.sprite == null) return;
            Vector3 position = scientist.transform.position;
            position.y += deskSurface.bounds.max.y - Mathf.Max(0f, coverDepth) - scientist.bounds.min.y;
            position.z = Mathf.Max(position.z, deskSurface.transform.position.z + Mathf.Max(0.01f, minimumDepthBehindDesk));
            if (scientist.transform.position != position) scientist.transform.position = position;
            if (deskSortingGroup == null) return;
            if (scientist.sortingLayerID != deskSortingGroup.sortingLayerID)
                scientist.sortingLayerID = deskSortingGroup.sortingLayerID;
            int maximumOrder = Mathf.Max(short.MinValue, deskSortingGroup.sortingOrder - 2);
            if (decorationsSortingGroup != null && decorationsSortingGroup.sortingLayerID == scientist.sortingLayerID)
                maximumOrder = Mathf.Min(maximumOrder, Mathf.Max(short.MinValue, decorationsSortingGroup.sortingOrder - 2));
            int minimumOrder = short.MinValue;
            if (wallSortingGroup != null && wallSortingGroup.sortingLayerID == scientist.sortingLayerID)
                minimumOrder = Mathf.Min(short.MaxValue - 1, wallSortingGroup.sortingOrder + 1);
            // Leave a sorting slot for the lens, while keeping the opaque wall behind the bust.
            int sortingOrder = minimumOrder <= maximumOrder
                ? Mathf.Clamp(scientist.sortingOrder, minimumOrder, maximumOrder)
                : maximumOrder;
            if (scientist.sortingOrder != sortingOrder) scientist.sortingOrder = sortingOrder;
        }
    }
}
