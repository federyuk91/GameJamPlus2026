using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(Grid))]
    public sealed class ManualGridAlignment : MonoBehaviour
    {
        public bool showGizmos = true;
        [SerializeField] private Grid referenceGrid;
        public Grid ReferenceGrid => referenceGrid != null ? referenceGrid : referenceGrid = GetComponent<Grid>();

        public bool TryGetDisplacement(Grid child, out Vector3 displacement, out string error)
        {
            displacement = Vector3.zero;
            Grid reference = ReferenceGrid;
            if (child == null || reference == null
                || reference.cellLayout != GridLayout.CellLayout.Rectangle
                || reference.cellSwizzle != GridLayout.CellSwizzle.XYZ
                || child.cellLayout != reference.cellLayout || child.cellSwizzle != reference.cellSwizzle)
            {
                error = "Use rectangular XY grids.";
                return false;
            }
            Vector3 referenceSize = Vector3.Scale(reference.cellSize, reference.transform.lossyScale);
            Vector3 childSize = Vector3.Scale(child.cellSize, child.transform.lossyScale);
            Vector3 referenceGap = Vector3.Scale(reference.cellGap, reference.transform.lossyScale);
            Vector3 childGap = Vector3.Scale(child.cellGap, child.transform.lossyScale);
            if (((Vector2)(referenceSize - childSize)).sqrMagnitude > 0.000001f
                || ((Vector2)(referenceGap - childGap)).sqrMagnitude > 0.000001f
                || Quaternion.Angle(reference.transform.rotation, child.transform.rotation) > 0.01f)
            {
                error = child.name + ": cell size, Cell Gap or rotation differs from the reference grid.";
                return false;
            }
            Vector3 step = reference.cellSize + reference.cellGap;
            if (step.x <= 0f || step.y <= 0f)
            {
                error = "Reference grid cells must have positive dimensions.";
                return false;
            }
            Vector3 local = reference.transform.InverseTransformPoint(child.transform.position);
            local.x = Mathf.Round(local.x / step.x) * step.x;
            local.y = Mathf.Round(local.y / step.y) * step.y;
            displacement = reference.transform.TransformPoint(local) - child.transform.position;
            error = string.Empty;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            Grid grid = ReferenceGrid;
            if (grid == null) return;
            Vector3 step = grid.cellSize + grid.cellGap;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = grid.transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            for (int i = -8; i <= 8; i++)
            {
                Gizmos.DrawLine(new Vector3(i * step.x, -8f * step.y, 0f), new Vector3(i * step.x, 8f * step.y, 0f));
                Gizmos.DrawLine(new Vector3(-8f * step.x, i * step.y, 0f), new Vector3(8f * step.x, i * step.y, 0f));
            }
            Gizmos.matrix = previous;
        }
    }
}
