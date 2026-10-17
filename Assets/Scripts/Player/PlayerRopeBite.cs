using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(90), DisallowMultipleComponent, RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerRopeBite : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private MouseFacing facing;
        [SerializeField] private Transform visual;
        [SerializeField] private RopeSimulation[] ropes;
        [SerializeField] private Vector2 mouthOffset = new Vector2(0.45f, -0.1f);
        [SerializeField, Min(0.01f)] private float reach = 0.4f;
        [SerializeField, Min(0.01f)] private float holdDuration = 0.6f;
        private readonly RaycastHit2D[] obstacles = new RaycastHit2D[16];
        private RopeSimulation target;
        private int targetSegment = -1;
        private float heldTime;
        private bool consumedHold;
        public float BiteProgress => Mathf.Clamp01(heldTime / Mathf.Max(0.01f, holdDuration));

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (facing == null) facing = GetComponent<MouseFacing>();
        }

        private void FixedUpdate()
        {
            if (!input.BiteHeld)
            {
                consumedHold = false;
                ResetTarget();
                return;
            }
            if (consumedHold || ropes == null) return;
            Transform plane = visual != null ? visual : transform;
            float sign = facing == null || facing.FacingRight ? 1f : -1f;
            Vector2 mouth = plane.TransformPoint(new Vector2(mouthOffset.x * sign, mouthOffset.y));
            Vector2 forward = plane.TransformDirection(Vector2.right * sign);
            RopeSimulation nearest = null;
            int segment = -1;
            float best = reach * reach;
            foreach (RopeSimulation rope in ropes)
            {
                if (rope == null || !rope.isActiveAndEnabled || rope.Chain == null) continue;
                for (int i = 0; i < rope.Chain.SegmentCount; i++)
                {
                    if (!rope.Chain.IsSegmentIntact(i)) continue;
                    Vector2 a = rope.Chain[i];
                    Vector2 delta = rope.Chain[i + 1] - a;
                    float t = Mathf.Clamp01(Vector2.Dot(mouth - a, delta) / Mathf.Max(delta.sqrMagnitude, 0.000001f));
                    Vector2 point = a + delta * t;
                    Vector2 offset = point - mouth;
                    float squared = offset.sqrMagnitude;
                    if (squared > best || Vector2.Dot(offset, forward) < -0.05f || !HasLineOfSight(mouth, offset)) continue;
                    best = squared;
                    nearest = rope;
                    segment = i;
                }
            }
            if (nearest == null || !nearest.CanBeCut)
            {
                ResetTarget();
                return;
            }
            if (target != nearest)
            {
                target = nearest;
                heldTime = 0f;
            }
            targetSegment = segment;
            heldTime += Time.fixedDeltaTime;
            if (heldTime >= holdDuration && target.TryCutSegment(targetSegment))
            {
                consumedHold = true;
                ResetTarget();
            }
        }

        private bool HasLineOfSight(Vector2 origin, Vector2 offset)
        {
            float distance = offset.magnitude;
            if (distance <= 0.0001f) return true;
            int count = Physics2D.Raycast(origin, offset / distance, body.SolidFilter, obstacles, distance);
            for (int i = 0; i < count; i++)
            {
                Collider2D obstacle = obstacles[i].collider;
                if (obstacle == body.Shape || obstacle.transform.IsChildOf(transform)) continue;
                if (obstacles[i].distance < distance - 0.01f) return false;
            }
            return true;
        }

        private void ResetTarget()
        {
            target = null;
            targetSegment = -1;
            heldTime = 0f;
        }

        private void OnDisable()
        {
            consumedHold = false;
            ResetTarget();
        }
    }
}
