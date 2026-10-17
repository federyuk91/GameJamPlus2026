using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class RoomDefinition : MonoBehaviour
    {
        [SerializeField, InspectorName("Ingombro della stanza"),
         Tooltip("Rettangolo locale usato per evitare sovrapposizioni: X/Y indicano l’angolo inferiore sinistro; Width/Height devono essere maggiori di zero.")]
        private Rect footprint = new Rect(-8f, -6f, 16f, 12f);
        [SerializeField] private RoomPort[] ports = new RoomPort[0];
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
                Gizmos.DrawWireSphere(ports[i].Position, 0.1f);
                Gizmos.DrawLine(ports[i].Position, ports[i].Position + RoomPort.Axis(ports[i].Direction) * 0.5f);
            }
        }
    }
}
