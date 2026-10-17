using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(200), DisallowMultipleComponent]
    public sealed class PiranhaPatrol : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private bool spriteFacesRight;
        [SerializeField, Min(0f)] private float speed = 1.5f;
        [SerializeField, Min(0.001f)] private float arrivalDistance = 0.02f;
        [SerializeField] private Vector2 initialDirection = Vector2.left;
        [SerializeField] private Vector2[] waypoints =
        {
            new Vector2(-2f, 0f), new Vector2(-2f, 1.5f),
            new Vector2(2f, 1.5f), new Vector2(2f, 0f)
        };
        private Vector2 origin;
        private Vector2 facing;
        private int waypointIndex;
        private bool chasing;
        private bool returning;
        private Vector2 returnPoint;
        private Vector2 chaseTarget;
        private float chaseSpeed;
        public int WaypointCount => waypoints != null ? waypoints.Length : 0;
        public Vector2 PathOrigin => Application.isPlaying ? origin : (Vector2)transform.position;
        public Vector2 FacingDirection => Application.isPlaying ? facing : NormalizedInitialDirection;
        private Vector2 NormalizedInitialDirection => initialDirection.sqrMagnitude > 0.0001f ? initialDirection.normalized : Vector2.left;

        private void Awake()
        {
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            origin = transform.position;
            facing = NormalizedInitialDirection;
            UpdateVisual();
        }

        public Vector2 WaypointPosition(int index) => PathOrigin + waypoints[index];
        public void SetWaypointPosition(int index, Vector2 position) => waypoints[index] = position - PathOrigin;

        public void Chase(Vector2 target, float movementSpeed)
        {
            chasing = true;
            returning = false;
            chaseTarget = target;
            chaseSpeed = Mathf.Max(0f, movementSpeed);
        }

        public void ResumePatrol()
        {
            if (!chasing) return;
            chasing = false;
            if (WaypointCount == 0) return;
            float nearest = float.PositiveInfinity;
            Vector2 position = transform.position;
            // Rejoin the nearest segment instead of teleporting or skipping the return journey.
            for (int i = 0; i < WaypointCount; i++)
            {
                int next = (i + 1) % WaypointCount;
                Vector2 a = WaypointPosition(i);
                Vector2 delta = WaypointPosition(next) - a;
                float t = delta.sqrMagnitude > 0.000001f ? Mathf.Clamp01(Vector2.Dot(position - a, delta) / delta.sqrMagnitude) : 0f;
                Vector2 point = a + delta * t;
                float distance = (point - position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                returnPoint = point;
                waypointIndex = next;
            }
            returning = true;
        }

        private void FixedUpdate()
        {
            if (chasing)
            {
                MoveTowards(chaseTarget, chaseSpeed * Time.fixedDeltaTime);
                return;
            }
            if (WaypointCount == 0) return;
            waypointIndex = Mathf.Clamp(waypointIndex, 0, WaypointCount - 1);
            Vector2 target = returning ? returnPoint : WaypointPosition(waypointIndex);
            MoveTowards(target, Mathf.Max(0f, speed) * Time.fixedDeltaTime);
            if (Vector2.Distance(transform.position, target) > arrivalDistance) return;
            if (returning) returning = false;
            else waypointIndex = (waypointIndex + 1) % WaypointCount;
        }

        private void MoveTowards(Vector2 target, float distance)
        {
            Vector2 start = transform.position;
            Vector2 next = Vector2.MoveTowards(start, target, distance);
            Vector2 delta = next - start;
            if (delta.sqrMagnitude > 0.00000001f) facing = delta.normalized;
            transform.position = new Vector3(next.x, next.y, transform.position.z);
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (visual != null && Mathf.Abs(facing.x) > 0.001f)
                visual.flipX = (facing.x < 0f) == spriteFacesRight;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < WaypointCount; i++)
            {
                Vector2 point = WaypointPosition(i);
                Gizmos.DrawWireSphere(point, 0.08f);
                if (WaypointCount > 1) Gizmos.DrawLine(point, WaypointPosition((i + 1) % WaypointCount));
            }
            if (Application.isPlaying && returning)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, returnPoint);
            }
        }
    }
}
