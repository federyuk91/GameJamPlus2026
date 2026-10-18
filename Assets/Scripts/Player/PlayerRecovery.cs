using System.Collections.Generic;
using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(PlayerMovement))]
    public sealed class PlayerRecovery : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerRopeBite ropeBite;
        private readonly List<RaycastHit2D> surfaceHits = new List<RaycastHit2D>(32);
        private readonly List<RaycastHit2D> supportHits = new List<RaycastHit2D>(8);
        private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
        public Collider2D Shape => body != null ? body.Shape : null;

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (ropeBite == null) ropeBite = GetComponent<PlayerRopeBite>();
        }

        public bool TryRecover(float searchRadius, float sampleSpacing, float flatSurfaceAngle)
        {
            if (!isActiveAndEnabled || body == null || movement == null) return false;
            Physics2D.SyncTransforms();
            Bounds bounds = body.Shape.bounds;
            Vector2 originalCenter = bounds.center;
            Vector2 centerOffset = originalCenter - (Vector2)transform.position;
            Vector2 size = bounds.size;
            float radius = Mathf.Max(1f, searchRadius);
            float spacing = Mathf.Max(0.05f, sampleSpacing);
            float minimumNormalY = Mathf.Cos(Mathf.Clamp(flatSurfaceAngle, 0f, 10f) * Mathf.Deg2Rad);
            float gap = Mathf.Max(body.Skin * 2f, Physics2D.defaultContactOffset * 2f);
            float bestDistance = radius * radius;
            Vector2 destination = Vector2.zero;
            bool found = false;
            int columns = Mathf.CeilToInt(radius / spacing);
            float verticalSpacing = Mathf.Min(spacing, Mathf.Max(0.05f, size.y * 0.5f));
            int rows = Mathf.CeilToInt(radius / verticalSpacing);
            for (int column = 0; column <= columns * 2; column++)
            {
                int signedColumn = column % 2 == 0 ? -column / 2 : (column + 1) / 2;
                float dx = signedColumn * spacing;
                if (Mathf.Abs(dx) > radius || dx * dx > bestDistance) continue;
                for (int row = 0; row <= rows * 2; row++)
                {
                    int signedRow = row % 2 == 0 ? -row / 2 : (row + 1) / 2;
                    float upper = signedRow * verticalSpacing;
                    float lower = upper - verticalSpacing;
                    float nearestY = lower <= 0f && upper >= 0f ? 0f : Mathf.Min(Mathf.Abs(lower), Mathf.Abs(upper));
                    if (dx * dx + nearestY * nearestY > bestDistance) continue;
                    // Short rays also find floors under ceilings inside the same CompositeCollider2D.
                    Vector2 origin = originalCenter + new Vector2(dx, upper - size.y * 0.5f - gap);
                    surfaceHits.Clear();
                    Physics2D.Raycast(origin, Vector2.down, body.SolidFilter, surfaceHits, verticalSpacing);
                    foreach (RaycastHit2D hit in surfaceHits)
                    {
                        if (hit.collider == null || IsPlayerCollider(hit.collider) || hit.distance <= 0f
                            || hit.normal.y < minimumNormalY) continue;
                        Vector2 center = hit.point + Vector2.up * (size.y * 0.5f + gap);
                        float squared = (center - originalCenter).sqrMagnitude;
                        if (squared > bestDistance || !HasFullSupport(hit.point, size.x, gap, minimumNormalY)
                            || !CanOccupy(center, size)) continue;
                        bestDistance = squared;
                        destination = center - centerOffset;
                        found = true;
                    }
                }
            }

            GameManager manager = GameManager.Instance;
            if (!found && manager != null && manager.SpawnPoints != null)
            {
                bestDistance = float.PositiveInfinity;
                foreach (SpawnPoint spawn in manager.SpawnPoints)
                {
                    if (spawn == null || !spawn.CanSpawn) continue;
                    Vector2 center = spawn.Position + centerOffset;
                    float squared = (center - originalCenter).sqrMagnitude;
                    if (squared >= bestDistance || !CanOccupy(center, size)) continue;
                    destination = spawn.Position;
                    bestDistance = squared;
                    found = true;
                }
            }
            if (!found)
            {
                Debug.LogWarning("Recovery Zone: no clear surface within the search radius and no valid Spawn Point available.", this);
                return false;
            }
            if (ropeBite != null) ropeBite.Cancel();
            movement.TeleportTo(destination);
            if (manager != null) manager.SnapCameraToPlayer();
            return true;
        }

        private bool HasFullSupport(Vector2 floor, float width, float gap, float minimumNormalY)
        {
            float half = Mathf.Max(0f, width * 0.5f - body.Skin);
            for (int sample = -1; sample <= 1; sample++)
            {
                Vector2 origin = floor + new Vector2(sample * half, gap + 0.05f);
                supportHits.Clear();
                Physics2D.Raycast(origin, Vector2.down, body.SolidFilter, supportHits, gap + 0.1f);
                bool supported = false;
                foreach (RaycastHit2D hit in supportHits)
                {
                    if (hit.collider == null || IsPlayerCollider(hit.collider) || hit.normal.y < minimumNormalY
                        || Mathf.Abs(hit.point.y - floor.y) > 0.03f) continue;
                    supported = true;
                    break;
                }
                if (!supported) return false;
            }
            return true;
        }

        private bool CanOccupy(Vector2 center, Vector2 size)
        {
            overlaps.Clear();
            Physics2D.OverlapBox(center, size, 0f, body.SolidFilter, overlaps);
            foreach (Collider2D obstacle in overlaps)
                if (obstacle != null && !IsPlayerCollider(obstacle)) return false;

            ContactFilter2D allLayers = ContactFilter2D.noFilter;
            allLayers.useTriggers = true;
            overlaps.Clear();
            Physics2D.OverlapBox(center, size, 0f, allLayers, overlaps);
            foreach (Collider2D area in overlaps)
            {
                if (area == null || IsPlayerCollider(area)) continue;
                if (area.TryGetComponent(out RecoveryZone recovery) && recovery.isActiveAndEnabled) return false;
                if (area.TryGetComponent(out LethalContact lethal) && lethal.isActiveAndEnabled) return false;
                if (area.TryGetComponent(out MouseTrap trap) && trap.isActiveAndEnabled) return false;
                if (area.TryGetComponent(out ElectricTrap electricTrap) && electricTrap.IsDangerous) return false;
                if (area.TryGetComponent(out StarvationDeathZone starvation) && starvation.isActiveAndEnabled) return false;
            }
            return true;
        }

        private bool IsPlayerCollider(Collider2D candidate) => candidate == body.Shape || candidate.transform.IsChildOf(transform);
    }
}
