using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerInputReader), typeof(SimulatedBody2D))]
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerDigging : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PlayerMovement playerMovement;
        [Tooltip("Multiplies this animal's Move Speed to determine digging movement speed. 0.3 uses 30% of normal movement speed. The body's Max Speed still limits motion.")]
        [SerializeField, Min(0f)] private float digSpeedMultiplier = 0.3f;
        [SerializeField] private PlayerRopeInteraction rope;
        [SerializeField] private PlayerLadderInteraction ladder;
        [SerializeField] private PlayerLedgeClimb ledge;
        private PhysicsSettings WorldSettings => (body != null ? body : body = GetComponent<SimulatedBody2D>()).Settings;
        [SerializeField] private DiggableTerrain[] terrains;
        private DiggableTerrain terrain;
        public bool InputClaimed { get; private set; }
        public bool IsDigging { get; private set; }
        public Vector2 Direction { get; private set; }
        public float DigSpeed => Mathf.Max(0f, playerMovement.MovementSettings.moveSpeed) * Mathf.Max(0f, digSpeedMultiplier);
        public bool CanStartDigging => isActiveAndEnabled && body != null && Settings.enabled && !AttachedElsewhere()
            && (TryFindTerrain(Vector2.left, out _) || TryFindTerrain(Vector2.right, out _)
                || TryFindTerrain(Vector2.up, out _) || TryFindTerrain(Vector2.down, out _));
        private DiggingSettings Settings => WorldSettings != null ? WorldSettings.digging : PhysicsSettings.DefaultDigging;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        }

        public bool TryClaimInput()
        {
            if (!isActiveAndEnabled || !Settings.enabled || AttachedElsewhere()) return false;
            Vector2 direction = Vector2.ClampMagnitude(new Vector2(input.Horizontal, input.Vertical), 1f);
            if (direction.sqrMagnitude < 0.01f || !TryFindTerrain(direction, out DiggableTerrain candidate)) return false;
            terrain = candidate;
            InputClaimed = true;
            return true;
        }

        private bool TryFindTerrain(Vector2 direction, out DiggableTerrain found)
        {
            found = null;
            if (terrains == null) return false;
            Vector2 center = body.Shape.bounds.center;
            foreach (DiggableTerrain candidate in terrains)
            {
                if (candidate == null || !candidate.isActiveAndEnabled) continue;
                if (!candidate.HasEarth(center, center + direction.normalized * Settings.reach, RadiusFor(candidate))) continue;
                found = candidate;
                return true;
            }
            return false;
        }

        private bool AttachedElsewhere()
        {
            return (rope != null && rope.Attached) || (ladder != null && ladder.Attached)
                || (ledge != null && ledge.Active);
        }

        private float RadiusFor(DiggableTerrain candidate)
        {
            // Include raster rounding so the box fits even at the minimum configured radius.
            return Mathf.Max(Settings.radius, body.Shape.bounds.extents.magnitude + body.Skin * 2f + candidate.CellWorldRadius);
        }

        public bool Tick(float dt, out Vector2 velocity)
        {
            velocity = Vector2.zero;
            IsDigging = false;
            Direction = Vector2.zero;
            if (!InputClaimed || !input.BiteHeld || !Settings.enabled || terrain == null
                || !terrain.isActiveAndEnabled || AttachedElsewhere()) return false;
            Vector2 direction = Vector2.ClampMagnitude(new Vector2(input.Horizontal, input.Vertical), 1f);
            Vector2 center = body.Shape.bounds.center;
            float radius = RadiusFor(terrain);
            // Outside the terrain, normal gravity resumes even if the shared button stays held.
            if (!terrain.Intersects(center, radius)) return false;
            Vector2 displacement = direction * DigSpeed * dt;
            Vector2 forward = direction.sqrMagnitude > 0.01f ? direction.normalized * Settings.reach : Vector2.zero;
            // Only the selected terrain can be removed; other solids still constrain movement.
            terrain.BlockedByOtherSolid(body, displacement, out Vector2 allowed);
            if (direction.sqrMagnitude > 0.01f)
            {
                terrain.Excavate(center, center + allowed + forward, radius);
                IsDigging = true;
                Direction = direction.normalized;
            }
            Vector2 start = transform.position;
            body.Move(allowed);
            if (dt > 0f) velocity = ((Vector2)transform.position - start) / dt;
            return true;
        }

        public void ReleaseInput()
        {
            InputClaimed = false;
            IsDigging = false;
            Direction = Vector2.zero;
            terrain = null;
        }

        private void OnDisable() => ReleaseInput();
    }
}
