using UnityEngine;
using UnityEngine.Tilemaps;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class RoomDefinition : MonoBehaviour
    {
        [SerializeField, 
         Tooltip("Rettangolo locale usato per evitare sovrapposizioni: X/Y indicano l’angolo inferiore sinistro; Width/Height devono essere maggiori di zero.")]
        private Rect footprint = new Rect(-8f, -6f, 16f, 12f);
        [SerializeField] private RoomPort[] ports = new RoomPort[0];
        [SerializeField] private Grid layoutGrid;
        public Grid LayoutGrid
        {
            get
            {
                if (layoutGrid == null) layoutGrid = GetComponentInChildren<Grid>(true);
                if (layoutGrid == null) layoutGrid = GetComponentInParent<Grid>();
                return layoutGrid;
            }
        }
        public Vector2 GridOffset => (Vector2)LayoutGrid.transform.position - (Vector2)transform.position;
        public Rect Footprint => footprint;
        public int PortCount => ports != null ? ports.Length : 0;
        public RoomPort Port(int index) => ports[index];

        public bool Validate(out string error)
        {
            if (footprint.width <= 0f || footprint.height <= 0f)
            {
                error = "L'ingombro deve avere dimensioni positive.";
                return false;
            }
            Grid grid = LayoutGrid;
            if (grid == null || grid.cellLayout != GridLayout.CellLayout.Rectangle
                || grid.cellSwizzle != GridLayout.CellSwizzle.XYZ || grid.cellGap.sqrMagnitude > 0.000001f)
            {
                error = "Assegnare una Grid rettangolare XY, senza Cell Gap.";
                return false;
            }
            if (grid.cellSize.x <= 0f || grid.cellSize.y <= 0f
                || Quaternion.Angle(grid.transform.rotation, Quaternion.identity) > 0.01f
                || (grid.transform.lossyScale - Vector3.one).sqrMagnitude > 0.000001f)
            {
                error = "La Grid deve avere celle positive, rotazione zero e scala mondo 1.";
                return false;
            }
            foreach (Tilemap map in GetComponentsInChildren<Tilemap>(true))
            {
                Vector3 local = grid.transform.InverseTransformPoint(map.transform.position);
                Vector2 cells = new Vector2(local.x / grid.cellSize.x, local.y / grid.cellSize.y);
                if (Mathf.Abs(cells.x - Mathf.Round(cells.x)) > 0.001f
                    || Mathf.Abs(cells.y - Mathf.Round(cells.y)) > 0.001f
                    || Quaternion.Angle(map.transform.rotation, Quaternion.identity) > 0.01f
                    || (map.transform.lossyScale - Vector3.one).sqrMagnitude > 0.000001f)
                {
                    error = map.name + ": allineare origine, rotazione e scala della Tilemap alla Grid della stanza.";
                    return false;
                }
            }
            int directions = 0;
            for (int i = 0; i < PortCount; i++)
            {
                RoomPort port = ports[i];
                if (port == null || !port.transform.IsChildOf(transform))
                {
                    error = "Ogni uscita deve essere assegnata e appartenere alla stanza.";
                    return false;
                }
                int bit = 1 << (int)port.Direction;
                if ((directions & bit) != 0)
                {
                    error = "Una stanza puo avere al massimo un'uscita per direzione.";
                    return false;
                }
                directions |= bit;
            }
            error = string.Empty;
            return true;
        }

        public Vector2 SnapPortPosition(Vector2 position, RoomDirection direction)
        {
            Grid grid = LayoutGrid;
            if (grid == null) return position;
            Vector3 local = grid.transform.InverseTransformPoint(position);
            // An exit lies on a cell edge along its axis, and at a cell center across it.
            Vector2 bias = direction == RoomDirection.Up || direction == RoomDirection.Down
                ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f);
            local.x = (Mathf.Round(local.x / grid.cellSize.x - bias.x) + bias.x) * grid.cellSize.x;
            local.y = (Mathf.Round(local.y / grid.cellSize.y - bias.y) + bias.y) * grid.cellSize.y;
            return grid.transform.TransformPoint(local);
        }

        public Vector2 PortOffset(int index)
        {
            RoomPort port = ports[index];
            return SnapPortPosition(port.Position, port.Direction) - (Vector2)transform.position;
        }

        public Rect WorldFootprint()
        {
            Vector3 a = transform.TransformPoint(new Vector3(footprint.xMin, footprint.yMin, 0f));
            Vector3 b = transform.TransformPoint(new Vector3(footprint.xMax, footprint.yMin, 0f));
            Vector3 c = transform.TransformPoint(new Vector3(footprint.xMin, footprint.yMax, 0f));
            Vector3 d = transform.TransformPoint(new Vector3(footprint.xMax, footprint.yMax, 0f));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x, c.x, d.x), Mathf.Min(a.y, b.y, c.y, d.y),
                Mathf.Max(a.x, b.x, c.x, d.x), Mathf.Max(a.y, b.y, c.y, d.y));
        }

        private void OnDrawGizmosSelected()
        {
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(footprint.center, new Vector3(footprint.width, footprint.height, 0f));
            Gizmos.matrix = previous;
            for (int i = 0; i < PortCount; i++)
            {
                if (ports[i] == null) continue;
                Gizmos.color = Color.green;
                Vector2 snapped = SnapPortPosition(ports[i].Position, ports[i].Direction);
                Gizmos.DrawWireSphere(snapped, 0.1f);
                Gizmos.DrawLine(snapped, snapped + RoomPort.Axis(ports[i].Direction) * 0.5f);
                if ((snapped - ports[i].Position).sqrMagnitude > 0.000001f)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(ports[i].Position, snapped);
                }
            }
        }
    }
}
