using System.Collections.Generic;
using System;
using UnityEngine;
using GameJamPlus.Gameplay;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(PlayerMovement))]
    public sealed class PlayerSwimming : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerLedgeClimb ledgeClimb;
        [SerializeField] private MouseFacing facing;
        [Header("Water collider")]
        [SerializeField, Tooltip("Usa dimensioni e offset differenti in acqua, mantenendo lo stesso BoxCollider2D e i suoi riferimenti. Il cambio attende spazio libero se la nuova forma interseca un solido.")]
        private bool useWaterCollider = true;
        [SerializeField, Tooltip("Dimensioni locali del collider in acqua, anche camminando sul fondo.")]
        private Vector2 waterColliderSize = new Vector2(0.35f, 0.2f);
        [SerializeField, Tooltip("Offset locale del collider in acqua rispetto al Transform del corpo.")]
        private Vector2 waterColliderOffset = new Vector2(0f, -0.35f);
        [Header("Swimming")]
        [Tooltip("Multiplies this animal's Move Speed to determine horizontal swimming speed. 1 uses normal movement speed; 0.5 halves it. The body's Max Speed still limits motion.")]
        [SerializeField, Min(0f)] private float swimSpeedMultiplier = 1f;
        [SerializeField] private SwimControlSettings controls = new SwimControlSettings();
        [SerializeField, Min(0f), Tooltip("Spinta orizzontale in unità/s per ogni pressione del comando Coda in acqua. Non si ripete tenendo premuto.")]
        private float tailImpulse = 1.25f;
        [SerializeField, Min(0.01f), Tooltip("Durata del breve incremento del limite di velocità durante la spinta di coda. Il bonus si riduce gradualmente.")]
        private float tailBoostDuration = 0.25f;
        private float tailBoostRemaining;
        public bool HasTailBoost => isActiveAndEnabled && tailBoostRemaining > 0f;
        [SerializeField, Min(0.1f)] private float poolObservationDuration = 20f;
        private float consecutiveWaterTime;
        private PhysicsSettings WorldSettings => (body != null ? body : body = GetComponent<SimulatedBody2D>()).Settings;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private readonly List<Collider2D> shapeOverlaps = new List<Collider2D>(8);
        private BoxCollider2D playerShape;
        private Vector2 dryColliderSize;
        private Vector2 dryColliderOffset;
        private bool waterShapeApplied;
        public bool InWater { get; private set; }
        private float contactingSurface = float.NegativeInfinity;
        public bool CanGrabLedge => !InWater || body.Shape.bounds.center.y > contactingSurface;
        public event Action StrokePerformed;
        private float strokeControlRemaining;
        private bool nearSurface;
        private bool surfaceJumpActive;
        private SwimMotion Settings => WorldSettings != null ? WorldSettings.water : PhysicsSettings.DefaultWater;
        public bool HasStrokeControl => isActiveAndEnabled && strokeControlRemaining > 0f;
        public float SwimSpeed => Mathf.Max(0f, playerMovement.MovementSettings.moveSpeed) * Mathf.Max(0f, swimSpeedMultiplier);
        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
            if (ledgeClimb == null) ledgeClimb = GetComponent<PlayerLedgeClimb>();
            if (facing == null) facing = GetComponent<MouseFacing>();
            playerShape = body.Shape;
            dryColliderSize = playerShape.size;
            dryColliderOffset = playerShape.offset;
        }
        public void UpdateControlTimers(float dt)
        {
            strokeControlRemaining = Mathf.Max(0f, strokeControlRemaining - dt);
            tailBoostRemaining = Mathf.Max(0f, tailBoostRemaining - dt);
        }
        public void ResetStrokeControl()
        {
            strokeControlRemaining = 0f;
            tailBoostRemaining = 0f;
        }

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.LoopState.Playing || body == null || !body.Shape.enabled) return;
            FindWaterContacts();
            bool touchingWater = false;
            foreach (Collider2D candidate in overlaps)
            {
                if (!IsWaterContact(candidate, out _)) continue;
                touchingWater = true;
                break;
            }
            if (!touchingWater)
            {
                consecutiveWaterTime = 0f;
                return;
            }
            float duration = Mathf.Max(0.1f, poolObservationDuration);
            if (consecutiveWaterTime >= duration) return;
            consecutiveWaterTime += Time.fixedDeltaTime;
            if (consecutiveWaterTime >= duration) GameSession.RecordObservation(TestObservation.LovesPool);
        }

        public bool CheckWater()
        {
            nearSurface = false;
            if (!isActiveAndEnabled) { InWater = false; surfaceJumpActive = false; return false; }
            if (body == null) body = GetComponent<SimulatedBody2D>();
            Physics2D.SyncTransforms();
            FindWaterContacts();
            InWater = false;
            contactingSurface = float.NegativeInfinity;
            bool holdingLedge = ledgeClimb != null && ledgeClimb.Active;
            foreach (Collider2D candidate in overlaps)
            {
                if (!IsWaterContact(candidate, out WaterVolume volume)) continue;
                InWater = true;
                // La presa mantiene la forma usata per calcolare aggancio e destinazione.
                if (!holdingLedge) UpdateColliderShape(useWaterCollider);
                Bounds playerBounds = body.Shape.bounds;
                float surfaceY = volume.Shape.bounds.max.y;
                contactingSurface = Mathf.Max(contactingSurface, surfaceY);
                if (playerBounds.max.y >= surfaceY - Settings.surfaceJumpDepth
                    && !WaterVolume.ContainsPoint(new Vector2(playerBounds.center.x, surfaceY + 0.001f)))
                    nearSurface = true;
            }
            if (!InWater)
            {
                surfaceJumpActive = false;
                if (!holdingLedge) UpdateColliderShape(false);
            }
            return InWater;
        }

        private void FindWaterContacts()
        {
            Physics2D.SyncTransforms();
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            overlaps.Clear();
            body.Shape.Overlap(filter, overlaps);
        }

        private bool IsWaterContact(Collider2D candidate, out WaterVolume volume)
        {
            volume = candidate.GetComponent<WaterVolume>();
            // Basta la quota inferiore sotto la superficie: il punto centrale del bordo
            // non deve essere contenuto nell'acqua, ma il corpo deve intersecare quel volume.
            return volume != null && volume.isActiveAndEnabled && candidate.isTrigger
                && body.Shape.bounds.min.y < volume.Shape.bounds.max.y;
        }

        private void UpdateColliderShape(bool useWaterShape)
        {
            if (playerMovement != null && playerMovement.IsFlattened) return;
            if (playerShape == null || waterShapeApplied == useWaterShape || !playerShape.enabled) return;
            Vector2 size = useWaterShape
                ? new Vector2(Mathf.Max(0.001f, waterColliderSize.x), Mathf.Max(0.001f, waterColliderSize.y))
                : dryColliderSize;
            Vector2 offset = useWaterShape ? waterColliderOffset : dryColliderOffset;
            Vector3 scale = playerShape.transform.lossyScale;
            Vector2 worldSize = Vector2.Scale(size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            Vector2 worldCenter = playerShape.transform.TransformPoint(offset);
            // Conserva la quota del bordo inferiore durante il cambio di forma: altrimenti
            // il nuovo collider potrebbe uscire dall'acqua e far ripetere il cambio ogni tick.
            float verticalShift = playerShape.bounds.min.y - (worldCenter.y - worldSize.y * 0.5f);
            worldCenter.y += verticalShift;
            // Una piccola tolleranza esclude i semplici contatti con pavimento e pareti.
            worldSize = new Vector2(Mathf.Max(0.0001f, worldSize.x - 0.001f), Mathf.Max(0.0001f, worldSize.y - 0.001f));
            shapeOverlaps.Clear();
            Physics2D.OverlapBox(worldCenter, worldSize,
                playerShape.transform.eulerAngles.z, body.SolidFilter, shapeOverlaps);
            foreach (Collider2D obstacle in shapeOverlaps)
            {
                if (obstacle == playerShape || obstacle.transform.IsChildOf(transform)) continue;
                if (body.IsMotionObstacle(obstacle)) return;
            }
            playerShape.size = size;
            playerShape.offset = offset;
            body.transform.position += Vector3.up * verticalShift;
            waterShapeApplied = useWaterShape;
            Physics2D.SyncTransforms();
            body.RefreshGround();
        }

        public Vector2 Step(Vector2 velocity, float horizontal, bool strokePressed, float dt, bool tailPressed = false)
        {
            if (tailPressed && InWater && dt > 0f && tailImpulse > 0f)
            {
                float direction = Mathf.Abs(horizontal) > 0.01f ? Mathf.Sign(horizontal)
                    : facing == null || facing.FacingRight ? 1f : -1f;
                velocity.x += direction * tailImpulse;
                tailBoostRemaining = Mathf.Max(0.01f, tailBoostDuration);
            }
            if (velocity.y <= 0f) surfaceJumpActive = false;
            if (strokePressed)
            {
                surfaceJumpActive = InWater && nearSurface;
                strokeControlRemaining = Mathf.Max(0f, controls.strokeControlTime);
                if (InWater && dt > 0f) StrokePerformed?.Invoke();
            }
            // Preserve the surface impulse until the body leaves water instead of capping it on the next tick.
            float speed = SwimSpeed + tailImpulse * Mathf.Clamp01(tailBoostRemaining / Mathf.Max(0.01f, tailBoostDuration));
            return Settings.Step(velocity, horizontal, strokePressed, dt, body.GravityAcceleration, speed, body.WaterDrag, controls, HasStrokeControl, surfaceJumpActive);
        }

        public float SurfaceHorizontal(float velocity, float horizontal, float dt)
            => Settings.StepHorizontal(velocity, horizontal, dt, SwimSpeed, HasStrokeControl, controls);

        private void OnDisable()
        {
            if (waterShapeApplied && playerShape != null)
            {
                playerShape.size = dryColliderSize;
                playerShape.offset = dryColliderOffset;
                waterShapeApplied = false;
            }
            consecutiveWaterTime = 0f;
            InWater = false;
            nearSurface = surfaceJumpActive = false;
            ResetStrokeControl();
        }
    }
}
