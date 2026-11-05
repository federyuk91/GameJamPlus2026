using System.Collections.Generic;
using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(90), DisallowMultipleComponent]
    public sealed class PlayerSnakeBoxInteraction : MonoBehaviour
    {
        private enum Phase { Entering, Inside, Exiting }
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private CollectibleCollector collector;
        [SerializeField] private PlayerStats stats;
        private SnakeBox box;
        private Phase phase;
        private float elapsed;
        private bool fromLeft;
        private float lastDirection = 1f;
        private bool exitRequested;
        private bool originalTrigger;
        private bool collectorEnabled;
        private Vector2 entryPosition;
        private Vector2 exitPosition;
        private Renderer[] visuals;
        private bool[] rendererEnabled;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private readonly List<RaycastHit2D> exitHits = new List<RaycastHit2D>(8);
        [SerializeField, Tooltip("Esito dell'ultimo tentativo di uscita, aggiornato in Play Mode.")]
        private string lastExitAttempt;
        [SerializeField] private Collider2D exitBlocker;
        public SnakeBox InteractionTarget { get; private set; }
        public bool IsInside => box != null;
        public Collider2D CoverCollider => box != null ? box.Shape : null;
        public event System.Action UsageChanged;
        public bool IsMoving { get; private set; }
        public float TargetDistanceSquared => InteractionTarget != null
            ? ((Vector2)InteractionTarget.Shape.bounds.center - (Vector2)body.Shape.bounds.center).sqrMagnitude : float.PositiveInfinity;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (collector == null) collector = GetComponent<CollectibleCollector>();
            if (stats == null) stats = GetComponent<PlayerStats>();
        }
        public void RefreshInteractionTarget()
        {
            InteractionTarget = null;
            if (IsInside || !movement.CanInteractWithCrates || input.GameplayActionsLocked) return;
            Physics2D.SyncTransforms();
            float best = float.PositiveInfinity;
            foreach (SnakeBox candidate in SnakeBox.Active)
            {
                if (candidate.gameObject.scene != gameObject.scene || !candidate.CanEnter(body)) continue;
                float distance = ((Vector2)candidate.Shape.bounds.center - (Vector2)body.Shape.bounds.center).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                InteractionTarget = candidate;
            }
        }
        private void Update()
        {
            if (IsInside && (stats != null && stats.IsDead)) { Cancel(); return; }
            if (Time.timeScale <= 0f || input.GameplayActionsLocked) { IsMoving = false; return; }
            if (IsInside)
            {
                bool interactionPressed = input.SnakeBoxPressed;
                // Una pressione durante l'ingresso resta valida e viene eseguita appena termina.
                if (phase != Phase.Exiting && (interactionPressed || input.CameraJumpPressed)) exitRequested = true;
                if (interactionPressed) input.ConsumeInteraction();
                return;
            }
            if (!input.SnakeBoxPressed) return;
            RefreshInteractionTarget();
            SnakeBox target = InteractionTarget;
            if (target == null || !target.TryOccupy(this)) return;
            entryPosition = transform.position;
            fromLeft = body.Shape.bounds.center.x <= target.Shape.bounds.center.x;
            lastDirection = fromLeft ? 1f : -1f;
            originalTrigger = body.Shape.isTrigger;
            collectorEnabled = collector != null && collector.enabled;
            visuals = GetComponentsInChildren<Renderer>(true);
            rendererEnabled = new bool[visuals.Length];
            for (int index = 0; index < visuals.Length; index++)
            {
                rendererEnabled[index] = visuals[index].enabled;
                visuals[index].enabled = false;
            }
            body.Shape.isTrigger = true;
            if (collector != null) collector.enabled = false;
            // TeleportTo libera prese, casse e richieste di salto prima di acquisire il nuovo stato.
            movement.TeleportTo(entryPosition);
            box = target;
            phase = Phase.Entering;
            elapsed = 0f;
            input.ConsumeAllInteractions();
            input.ConsumeJump();
            FollowBox();
            box.SampleEntry(fromLeft, 0f);
            UsageChanged?.Invoke();
        }
        // PlayerMovement delega qui l'intero tick: non vengono eseguiti salto, nuoto o traversal.
        public Vector2 Tick(float dt)
        {
            IsMoving = false;
            input.ConsumeJump();
            GameManager manager = GameManager.Instance;
            if (input.GameplayActionsLocked || (manager != null && manager.State != GameManager.LoopState.Playing)) return Vector2.zero;
            if (phase == Phase.Entering || phase == Phase.Exiting)
            {
                elapsed += dt;
                float progress = Mathf.Clamp01(elapsed / box.EntryDuration(fromLeft));
                box.SampleEntry(fromLeft, phase == Phase.Exiting ? 1f - progress : progress);
                if (progress >= 1f)
                {
                    if (phase == Phase.Exiting) { Finish(exitPosition); return Vector2.zero; }
                    phase = Phase.Inside;
                    box.ShowClosed();
                }
                return Vector2.zero;
            }
            if (exitRequested)
            {
                exitRequested = false;
                if (TryExitPosition(-lastDirection, out exitPosition))
                {
                    fromLeft = lastDirection > 0f;
                    phase = Phase.Exiting;
                    elapsed = 0f;
                    box.SampleEntry(fromLeft, 1f);
                    return Vector2.zero;
                }
            }
            Vector2 displacement = box.Move(input.Horizontal, dt, body);
            IsMoving = Mathf.Abs(displacement.x) > 0.00001f;
            if (IsMoving) lastDirection = Mathf.Sign(displacement.x);
            FollowBox();
            return dt > 0f ? displacement / dt : Vector2.zero;
        }
        private void FollowBox()
        {
            Physics2D.SyncTransforms();
            Bounds bounds = box.Shape.bounds;
            Bounds animal = body.Shape.bounds;
            transform.position += new Vector3(bounds.center.x - animal.center.x, bounds.min.y - animal.min.y, 0f);
            Physics2D.SyncTransforms();
        }
        private bool TryExitPosition(float direction, out Vector2 position)
        {
            Physics2D.SyncTransforms();
            exitBlocker = null;
            Bounds bounds = box.Shape.bounds;
            Bounds animal = body.Shape.bounds;
            Vector2 center = new Vector2((direction < 0f ? bounds.min.x - animal.extents.x : bounds.max.x + animal.extents.x)
                + direction * (body.Skin + 0.01f), bounds.min.y + animal.extents.y + body.Skin);
            if (box.HasWalkableSupport)
            {
                Vector2 normal = box.SupportNormal;
                Vector2 support = box.SupportPoint;
                // Sul lato in salita il terreno può essere più alto della base della scatola.
                float floorY = support.y - normal.x * (center.x - support.x) / Mathf.Max(0.01f, normal.y);
                floorY += Mathf.Abs(normal.x) * animal.extents.x / Mathf.Max(0.01f, normal.y);
                center.y = Mathf.Max(center.y, floorY + animal.extents.y + body.Skin / Mathf.Max(0.01f, normal.y));
            }
            position = (Vector2)transform.position + center - (Vector2)animal.center;
            Vector2 size = new Vector2(Mathf.Max(0.001f, animal.size.x - 0.001f), Mathf.Max(0.001f, animal.size.y - 0.001f));
            if (ExitAreaIsClear(center, size))
            {
                lastExitAttempt = "Uscita libera";
                return true;
            }

            // Il riparo può essere appoggiato leggermente dentro il terreno. Cerca un'uscita
            // sopra il supporto entro la tolleranza dei gradini, invece di considerare il pavimento un muro.
            float stepHeight = body.Settings != null ? body.Settings.player.maxStepHeight : PhysicsSettings.DefaultPlayer.maxStepHeight;
            float lift = Mathf.Max(body.Skin * 2f, stepHeight);
            Vector2 raised = center + Vector2.up * lift;
            if (!ExitAreaIsClear(raised, size))
            {
                lastExitAttempt = "Uscita bloccata da un ostacolo";
                return false;
            }
            exitHits.Clear();
            Physics2D.BoxCast(raised, size, 0f, Vector2.down, body.SolidFilter, exitHits, lift + body.Skin);
            float descent = lift;
            bool supportFound = false;
            foreach (RaycastHit2D hit in exitHits)
            {
                if (hit.collider == body.Shape || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.normal.y <= 0.01f || Vector2.Angle(hit.normal, Vector2.up) > body.MaxSlopeAngle + 0.01f) continue;
                descent = Mathf.Min(descent, Mathf.Max(0f, hit.distance - body.Skin / hit.normal.y));
                supportFound = true;
            }
            if (!supportFound || !ExitAreaIsClear(raised + Vector2.down * descent, size))
            {
                lastExitAttempt = "Spazio insufficiente sopra il terreno di uscita";
                return false;
            }
            center = raised + Vector2.down * descent;
            position = (Vector2)transform.position + center - (Vector2)animal.center;
            exitBlocker = null;
            lastExitAttempt = "Uscita allineata al terreno";
            return true;
        }
        private bool ExitAreaIsClear(Vector2 center, Vector2 size)
        {
            overlaps.Clear();
            Physics2D.OverlapBox(center, size, 0f, body.SolidFilter, overlaps);
            foreach (Collider2D obstacle in overlaps)
            {
                if (obstacle == body.Shape || obstacle.transform.IsChildOf(transform)) continue;
                exitBlocker = obstacle;
                return false;
            }
            exitBlocker = null;
            return true;
        }
        private void LateUpdate()
        {
            if (!IsInside || visuals == null) return;
            // Anche l'ombra può aggiornare i suoi renderer: resta nascosta per tutta l'occupazione.
            foreach (Renderer visual in visuals) if (visual != null) visual.enabled = false;
        }
        private void Finish(Vector2 position)
        {
            SnakeBox previous = box;
            box = null;
            IsMoving = false;
            exitRequested = false;
            if (previous != null) previous.Release(this);
            body.Shape.isTrigger = originalTrigger;
            if (collector != null) collector.enabled = collectorEnabled;
            if (visuals != null)
                for (int index = 0; index < visuals.Length; index++)
                    if (visuals[index] != null) visuals[index].enabled = rendererEnabled[index];
            input.ConsumeAllInteractions();
            input.ConsumeJump();
            movement.TeleportTo(position);
            UsageChanged?.Invoke();
        }
        public void Cancel()
        {
            if (!IsInside) return;
            Finish(TryExitPosition(-lastDirection, out Vector2 position) ? position : entryPosition);
        }
        private void OnDisable() => Cancel();
    }
}
