using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerInputReader), typeof(SimulatedBody2D))]
    public sealed class PlayerDigging : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PlayerRopeInteraction rope;
        [SerializeField] private PlayerLadderInteraction ladder;
        [SerializeField] private PlayerLedgeClimb ledge;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField] private DiggableTerrain[] terrains;
        private DiggableTerrain terrain;
        public bool InputClaimed { get; private set; }
        public bool IsDigging { get; private set; }
        public bool CanStartDigging => isActiveAndEnabled && body != null && Settings.enabled && !AttachedElsewhere()
            && (TryFindTerrain(Vector2.left, out _) || TryFindTerrain(Vector2.right, out _)
                || TryFindTerrain(Vector2.up, out _) || TryFindTerrain(Vector2.down, out _));
        private DiggingSettings Settings => physicsSettings != null ? physicsSettings.digging : PhysicsSettings.DefaultDigging;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
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
            if (!InputClaimed || !input.BiteHeld || !Settings.enabled || terrain == null
                || !terrain.isActiveAndEnabled || AttachedElsewhere()) return false;
            Vector2 direction = Vector2.ClampMagnitude(new Vector2(input.Horizontal, input.Vertical), 1f);
            Vector2 center = body.Shape.bounds.center;
            float radius = RadiusFor(terrain);
            // Outside the terrain, normal gravity resumes even if the shared button stays held.
            if (!terrain.Intersects(center, radius)) return false;
            Vector2 displacement = direction * Mathf.Max(0f, Settings.speed) * dt;
            Vector2 forward = direction.sqrMagnitude > 0.01f ? direction.normalized * Settings.reach : Vector2.zero;
            // Only the selected terrain can be removed; other solids still constrain movement.
            terrain.BlockedByOtherSolid(body, displacement, out Vector2 allowed);
            if (direction.sqrMagnitude > 0.01f)
            {
                terrain.Excavate(center, center + allowed + forward, radius);
                IsDigging = true;
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
            terrain = null;
        }

        private void OnDisable() => ReleaseInput();
    }
}
