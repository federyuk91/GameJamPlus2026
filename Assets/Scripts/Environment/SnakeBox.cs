using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D), typeof(SimulatedBody2D))]
    public sealed class SnakeBox : MonoBehaviour
    {
        private static readonly List<SnakeBox> active = new List<SnakeBox>();
        public static IReadOnlyList<SnakeBox> Active => active;
        [SerializeField] private BoxCollider2D shape;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField, Tooltip("Pivot grafico alla base della scatola: segue l'inclinazione del supporto senza ruotare il collider.")]
        private Transform visualPivot;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private AnimationClip enterFromLeft;
        [SerializeField] private AnimationClip enterFromRight;
        [SerializeField, Min(0f)] private float moveSpeed = 2f;
        [SerializeField, Min(0f)] private float interactionReach = 0.25f;
        [SerializeField, Min(0f)] private float feetTolerance = 0.2f;
        [SerializeField, Min(0f), Tooltip("Massima correzione verso l'alto se la scatola viene posizionata leggermente dentro il terreno. Non cambia il limite delle pendenze.")]
        private float initialGroundTolerance = 0.2f;
        private readonly List<RaycastHit2D> placementHits = new List<RaycastHit2D>(8);
        private readonly List<Collider2D> placementOverlaps = new List<Collider2D>(8);
        private bool placementReady;
        private Vector3 restVisualPosition;
        private Quaternion restVisualRotation;
        private PlayerSnakeBoxInteraction occupant;
        private int currentMotion;
        public BoxCollider2D Shape => shape;
        public bool HasWalkableSupport => body != null && body.Grounded;
        public Vector2 SupportNormal => body.GroundNormal;
        public Vector2 SupportPoint => body.GroundPoint;
        public bool IsOccupied => occupant != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => active.Clear();
        private void Awake()
        {
            if (shape == null) shape = GetComponent<BoxCollider2D>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (visualPivot != null)
            {
                restVisualPosition = visualPivot.localPosition;
                restVisualRotation = visualPivot.localRotation;
            }
            if (closedSprite == null && visual != null) closedSprite = visual.sprite;
        }
        private void OnEnable()
        {
            placementReady = false;
            if (!active.Contains(this)) active.Add(this);
        }
        private void EnsureGroundPlacement()
        {
            if (placementReady || body == null || shape == null || !shape.enabled) return;
            placementReady = true;
            Physics2D.SyncTransforms();
            Bounds bounds = shape.bounds;
            Vector2 size = new Vector2(Mathf.Max(0.001f, bounds.size.x - 0.001f), Mathf.Max(0.001f, bounds.size.y - 0.001f));
            placementOverlaps.Clear();
            Physics2D.OverlapBox(bounds.center, size, 0f, body.SolidFilter, placementOverlaps);
            bool penetrating = false;
            foreach (Collider2D obstacle in placementOverlaps)
            {
                if (obstacle == shape || obstacle.transform.IsChildOf(transform)) continue;
                if (occupant != null && obstacle.transform.IsChildOf(occupant.transform)) continue;
                penetrating = true;
                break;
            }
            if (!penetrating || initialGroundTolerance <= 0f) return;

            // Un cast che nasce dentro un solido non fornisce una normale di superficie
            // affidabile. Si cerca il vero appoggio dall'esterno prima di iniziare a muoversi.
            Vector2 raised = (Vector2)bounds.center + Vector2.up * initialGroundTolerance;
            placementHits.Clear();
            Physics2D.BoxCast(raised, size, 0f, Vector2.down, body.SolidFilter, placementHits, initialGroundTolerance);
            float nearest = float.PositiveInfinity;
            Vector2 normal = Vector2.up;
            foreach (RaycastHit2D hit in placementHits)
            {
                if (hit.collider == shape || hit.collider.transform.IsChildOf(transform)) continue;
                if (occupant != null && hit.collider.transform.IsChildOf(occupant.transform)) continue;
                if (hit.distance <= 0f || hit.normal.y <= 0.01f || hit.distance >= nearest) continue;
                nearest = hit.distance;
                normal = hit.normal;
            }
            if (float.IsPositiveInfinity(nearest)) return;
            float shift = initialGroundTolerance - Mathf.Max(0f, nearest - body.Skin / normal.y);
            Vector2 candidate = (Vector2)bounds.center + Vector2.up * shift;
            placementOverlaps.Clear();
            Physics2D.OverlapBox(candidate, size, 0f, body.SolidFilter, placementOverlaps);
            foreach (Collider2D obstacle in placementOverlaps)
            {
                if (obstacle == shape || obstacle.transform.IsChildOf(transform)) continue;
                if (occupant != null && obstacle.transform.IsChildOf(occupant.transform)) continue;
                return;
            }
            transform.position += Vector3.up * shift;
            Physics2D.SyncTransforms();
            body.RefreshGround();
        }
        private void OnDisable()
        {
            active.Remove(this);
            if (occupant != null) occupant.Cancel();
        }
        public bool CanEnter(SimulatedBody2D player)
        {
            if (!isActiveAndEnabled || IsOccupied || shape == null || !shape.enabled) return false;
            EnsureGroundPlacement();
            body.SetMaxSlopeAngle(player.MaxSlopeAngle);
            body.RefreshGround();
            Bounds box = shape.bounds;
            Bounds animal = player.Shape.bounds;
            float entryFloor = box.min.y;
            if (body.Grounded)
            {
                Vector2 normal = body.GroundNormal;
                Vector2 point = body.GroundPoint;
                entryFloor = point.y - normal.x * (animal.center.x - point.x) / Mathf.Max(0.01f, normal.y);
                entryFloor += Mathf.Abs(normal.x) * animal.extents.x / Mathf.Max(0.01f, normal.y);
            }
            return Mathf.Abs(animal.min.y - entryFloor) <= feetTolerance
                && animal.max.x >= box.min.x - interactionReach && animal.min.x <= box.max.x + interactionReach;
        }
        public bool TryOccupy(PlayerSnakeBoxInteraction player)
        {
            if (IsOccupied || enterFromLeft == null || enterFromRight == null) return false;
            occupant = player;
            return true;
        }
        public void Release(PlayerSnakeBoxInteraction player)
        {
            if (occupant != player) return;
            occupant = null;
            ShowClosed();
        }
        public float EntryDuration(bool fromLeft) => Mathf.Max(0.01f, (fromLeft ? enterFromLeft : enterFromRight).length);
        public void SampleEntry(bool fromLeft, float progress)
        {
            currentMotion = 0;
            animator.speed = 0f;
            animator.Play(fromLeft ? "EnterFromLeft" : "EnterFromRight", 0, Mathf.Clamp(progress, 0f, 0.9999f));
            animator.Update(0f);
        }
        public void ShowClosed()
        {
            currentMotion = 0;
            if (animator != null) { animator.speed = 0f; animator.Play("SnakeBox", 0, 0f); animator.Update(0f); }
            if (visual != null) visual.sprite = closedSprite;
        }
        public Vector2 Move(float horizontal, float dt, SimulatedBody2D player)
        {
            EnsureGroundPlacement();
            body.SetMaxSlopeAngle(player.MaxSlopeAngle);
            body.BeginMovementStep(dt);
            body.RefreshGround();
            Vector2 normal = body.Grounded ? body.GroundNormal : Vector2.up;
            Vector2 tangent = new Vector2(normal.y, -normal.x);
            Vector2 requested = tangent * (horizontal * Mathf.Min(moveSpeed, player.MaxSpeed) * dt);
            Vector2 start = transform.position;
            // Riusa sweep e proiezione sulle rampe del topo, senza introdurre gravità o salti.
            body.MovePlayer(requested, 0f, followGround: true);
            Vector2 displacement = (Vector2)transform.position - start;
            int motion = Mathf.Abs(displacement.x) > 0.00001f ? (displacement.x > 0f ? 1 : -1) : 0;
            if (motion == 0) ShowClosed();
            else if (motion != currentMotion)
            {
                animator.speed = 1f;
                animator.Play(motion > 0 ? "MoveToRight" : "MoveToLeft", 0, 0f);
                currentMotion = motion;
            }
            return displacement;
        }
        private void LateUpdate()
        {
            if (visualPivot == null || body == null) return;
            EnsureGroundPlacement();
            visualPivot.localPosition = restVisualPosition;
            visualPivot.localRotation = restVisualRotation;
            body.RefreshGround();
            if (!body.HasGroundContact) return;
            Vector2 normal = body.GroundNormal;
            Vector2 feet = visualPivot.position;
            feet += normal * (body.Skin - Vector2.Dot(feet - body.GroundPoint, normal));
            visualPivot.position = new Vector3(feet.x, feet.y, visualPivot.position.z);
            float slopeAngle = Vector2.SignedAngle(Vector2.up, normal);
            visualPivot.rotation = Quaternion.AngleAxis(slopeAngle, Vector3.forward) * restVisualRotation;
        }
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || body == null || !body.HasGroundContact) return;
            Vector2 normal = body.GroundNormal;
            Vector2 point = body.GroundPoint;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(point, 0.025f);
            Gizmos.DrawLine(point, point + normal * 0.4f);
            Gizmos.color = Color.cyan;
            Vector2 tangent = new Vector2(normal.y, -normal.x) * 0.3f;
            Gizmos.DrawLine(point - tangent, point + tangent);
        }
    }
}
