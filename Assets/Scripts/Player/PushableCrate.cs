using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(CrateMotion))]
    public sealed class PushableCrate : MonoBehaviour
    {
        public const float PlayerPushWeightLimit = 5f;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private CrateRopeAttachment ropeAttachment;
        [SerializeField] private bool showGizmos = true;
        [SerializeField, Min(0.01f), Tooltip("Dry weight. Weight decreases progressively to 50% at full immersion. Effective stack weight divides player push speed; 5 or more blocks pushing.")]
        private float weight = 1f;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private readonly List<PushableCrate> carriedCrates = new List<PushableCrate>(8);
        private readonly List<int> supportIndices = new List<int>(8);
        private readonly List<float> displacements = new List<float>(8);
        public SimulatedBody2D Body => body != null ? body : body = GetComponent<SimulatedBody2D>();
        public float Weight => Mathf.Max(0.01f, weight);
        private float SupportedWeight => CrateFlotation.EffectiveWeight(Weight, WaterVolume.SubmergedFraction(Body.Shape.bounds))
            - (ropeAttachment != null ? ropeAttachment.LiftCapacity : 0f);
        public float EffectiveWeight => Mathf.Max(0f, SupportedWeight);

        private void Awake()
        {
            if (ropeAttachment == null) ropeAttachment = GetComponent<CrateRopeAttachment>();
        }
        public float StackWeight
        {
            get
            {
                CollectCarriedCrates(true);
                float total = 0f;
                foreach (PushableCrate crate in carriedCrates) total += crate.SupportedWeight;
                return Mathf.Max(0f, total);
            }
        }
        public bool ShowGizmos => showGizmos;

        public float MoveHorizontal(float amount, Collider2D ignored = null)
        {
            if (Mathf.Approximately(amount, 0f)) return 0f;
            CollectCarriedCrates();
            displacements.Clear();
            float moved = Body.MoveHorizontal(amount, ignored);
            displacements.Add(moved);
            for (int i = 1; i < carriedCrates.Count; i++)
            {
                int supportIndex = supportIndices[i];
                SimulatedBody2D supportBody = carriedCrates[supportIndex].Body;
                // Carry only the support's actual movement; keep the upper crate's own collisions active.
                float carried = carriedCrates[i].Body.MoveHorizontal(displacements[supportIndex], supportBody.Shape);
                displacements.Add(carried);
            }
            return moved;
        }

        private void CollectCarriedCrates(bool includeDisabled = false)
        {
            carriedCrates.Clear();
            supportIndices.Clear();
            carriedCrates.Add(this);
            supportIndices.Add(-1);
            Physics2D.SyncTransforms();
            for (int i = 0; i < carriedCrates.Count; i++)
            {
                SimulatedBody2D supportBody = carriedCrates[i].Body;
                Bounds bounds = supportBody.Shape.bounds;
                float tolerance = supportBody.Skin * 4f;
                Vector2 center = new Vector2(bounds.center.x, bounds.max.y + tolerance * 0.5f);
                Vector2 size = new Vector2(bounds.size.x, tolerance);
                Physics2D.OverlapBox(center, size, 0f, supportBody.SolidFilter, overlaps);
                foreach (Collider2D hit in overlaps)
                {
                    if (!hit.TryGetComponent(out PushableCrate candidate) || !candidate.gameObject.activeInHierarchy
                        || (!includeDisabled && !candidate.enabled)
                        || carriedCrates.Contains(candidate) || hit != candidate.Body.Shape) continue;
                    float overlapWidth = Mathf.Min(bounds.max.x, hit.bounds.max.x) - Mathf.Max(bounds.min.x, hit.bounds.min.x);
                    if (overlapWidth <= supportBody.Skin || hit.bounds.min.y < bounds.max.y - supportBody.Skin) continue;
                    Collider2D support = candidate.Body.Probe(Vector2.down, tolerance + candidate.Body.Skin, out float distance);
                    if (support != supportBody.Shape || distance > supportBody.Skin + candidate.Body.Skin) continue;
                    carriedCrates.Add(candidate);
                    supportIndices.Add(i);
                }
            }
        }
    }
}
