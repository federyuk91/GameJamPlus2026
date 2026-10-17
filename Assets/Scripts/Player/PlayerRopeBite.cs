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
        [SerializeField] private PlayerDigging digging;
        [SerializeField] private PlayerRopeInteraction ropeInteraction;
        [SerializeField] private Vector2 mouthOffset = new Vector2(0.45f, -0.1f);
        [SerializeField, Min(0.01f)] private float reach = 0.4f;
        [SerializeField, Min(0.01f)] private float holdDuration = 0.6f;
        [SerializeField, Tooltip("Diagnostica del comando condiviso durante il gioco.")]
        private string lastAttempt;
        [SerializeField, Range(0f, 1f)] private float biteProgress;
        [SerializeField] private Collider2D blockingCollider;
        private readonly RaycastHit2D[] obstacles = new RaycastHit2D[16];
        private RopeSimulation target;
        private int targetSegment = -1;
        private float heldTime;
        private bool consumedHold;
        private bool biteChosen;
        public float BiteProgress => biteProgress;
        public bool CanStartDiggingWithSharedInput => isActiveAndEnabled && !consumedHold && !biteChosen;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (facing == null) facing = GetComponent<MouseFacing>();
            if (digging == null) digging = GetComponent<PlayerDigging>();
            if (ropeInteraction == null) ropeInteraction = GetComponent<PlayerRopeInteraction>();
        }

        private void FixedUpdate()
        {
            if (!input.BiteHeld)
            {
                consumedHold = false;
                biteChosen = false;
                if (digging != null) digging.ReleaseInput();
                ResetTarget();
                blockingCollider = null;
                lastAttempt = "In attesa di F / Est";
                return;
            }
            if (consumedHold) return;
            // Always evaluate reachable ropes before digging can consume the shared input.
            Physics2D.SyncTransforms();
            blockingCollider = null;
            TryFindCutTarget(out RopeSimulation nearest, out int segment, true);
            if (nearest == null || !nearest.CanBeCut)
            {
                ResetTarget();
                if (digging != null && digging.InputClaimed)
                {
                    lastAttempt = "Scavo attivo: nessuna corda raggiungibile";
                    return;
                }
                if (!biteChosen && digging != null && digging.TryClaimInput())
                    lastAttempt = "Scavo avviato";
                else lastAttempt = blockingCollider != null ? "Corda coperta da un collider" : "Nessuna corda tagliabile davanti alla bocca";
                return;
            }
            if (digging != null) digging.ReleaseInput();
            blockingCollider = null;
            biteChosen = true;
            if (target != nearest)
            {
                target = nearest;
                heldTime = 0f;
            }
            targetSegment = segment;
            heldTime += Time.fixedDeltaTime;
            biteProgress = Mathf.Clamp01(heldTime / Mathf.Max(0.01f, holdDuration));
            lastAttempt = "Morso in corso";
            if (heldTime >= holdDuration && target.TryCutSegment(targetSegment))
            {
                consumedHold = true;
                ResetTarget();
                lastAttempt = "Corda tagliata: rilasciare F / Est";
            }
        }

        public bool CanCutRope => isActiveAndEnabled && body != null && !consumedHold
            && TryFindCutTarget(out _, out _, false);

        private bool TryFindCutTarget(out RopeSimulation nearest, out int segment, bool updateDiagnostics)
        {
            Transform plane = visual != null ? visual : transform;
            float sign = facing == null || facing.FacingRight ? 1f : -1f;
            Vector2 mouth = plane.TransformPoint(new Vector2(mouthOffset.x * sign, mouthOffset.y));
            Vector2 forward = plane.TransformDirection(Vector2.right * sign);
            nearest = null;
            segment = -1;
            float best = reach * reach;
            // A held rope can be brought to the mouth regardless of the facing direction.
            if (ropeInteraction != null
                && ropeInteraction.TryGetGrip(out RopeSimulation heldRope, out int gripSegment, out Vector2 gripPoint)
                && heldRope.CanBeCut && HasLineOfSight(mouth, gripPoint - mouth, updateDiagnostics))
            {
                nearest = heldRope;
                segment = gripSegment;
            }
            if (nearest == null)
            {
                foreach (RopeSimulation rope in ropes ?? System.Array.Empty<RopeSimulation>())
                {
                    if (rope == null || !rope.isActiveAndEnabled || !rope.CanBeCut || rope.Chain == null) continue;
                    for (int i = 0; i < rope.Chain.SegmentCount; i++)
                    {
                        if (!rope.Chain.IsSegmentIntact(i)) continue;
                        Vector2 a = rope.Chain[i];
                        Vector2 delta = rope.Chain[i + 1] - a;
                        float t = Mathf.Clamp01(Vector2.Dot(mouth - a, delta) / Mathf.Max(delta.sqrMagnitude, 0.000001f));
                        Vector2 point = a + delta * t;
                        Vector2 offset = point - mouth;
                        float squared = offset.sqrMagnitude;
                        if (squared > best || Vector2.Dot(offset, forward) < -0.05f || !HasLineOfSight(mouth, offset, updateDiagnostics)) continue;
                        best = squared;
                        nearest = rope;
                        segment = i;
                    }
                }
            }
            return nearest != null && nearest.CanBeCut;
        }

        private bool HasLineOfSight(Vector2 origin, Vector2 offset, bool updateDiagnostics)
        {
            float distance = offset.magnitude;
            if (distance <= 0.0001f) return true;
            int count = Physics2D.Raycast(origin, offset / distance, body.SolidFilter, obstacles, distance);
            for (int i = 0; i < count; i++)
            {
                Collider2D obstacle = obstacles[i].collider;
                if (obstacle == body.Shape || obstacle.transform.IsChildOf(transform)) continue;
                if (obstacles[i].distance < distance - 0.01f)
                {
                    if (updateDiagnostics) blockingCollider = obstacle;
                    return false;
                }
            }
            return true;
        }

        private void ResetTarget()
        {
            target = null;
            targetSegment = -1;
            heldTime = 0f;
            biteProgress = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            Transform plane = visual != null ? visual : transform;
            float sign = facing == null || facing.FacingRight ? 1f : -1f;
            Vector2 mouth = plane.TransformPoint(new Vector2(mouthOffset.x * sign, mouthOffset.y));
            Vector2 forward = plane.TransformDirection(Vector2.right * sign);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(mouth, reach);
            Gizmos.DrawLine(mouth, mouth + forward * reach);
        }

        private void OnDisable()
        {
            if (digging != null) digging.ReleaseInput();
            biteChosen = false;
            consumedHold = false;
            ResetTarget();
        }
    }
}
