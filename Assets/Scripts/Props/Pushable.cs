using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MousePlatformer
{
    [MovedFrom(true, sourceNamespace: "MousePlatformer", sourceAssembly: "Assembly-CSharp", sourceClassName: "PushableCrate")]
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(PropsMotion))]
    public sealed class Pushable : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PropsMotion motion;
        [SerializeField] private RopeAttachment ropeAttachment;
        [SerializeField] private bool showGizmos = true;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private readonly List<Pushable> carriedCrates = new List<Pushable>(8);
        private readonly List<int> supportIndices = new List<int>(8);
        private readonly List<float> displacements = new List<float>(8);
        public SimulatedBody2D Body => body != null ? body : body = GetComponent<SimulatedBody2D>();
        public float Weight => Body.Weight;
        private PropsMotion Motion => motion != null ? motion : motion = GetComponent<PropsMotion>();
        private float SupportedWeight => CrateFlotation.EffectiveWeight(Weight, WaterVolume.SubmergedFraction(Body.Shape.bounds), Motion.FullSubmersionWeightMultiplier)
            - (ropeAttachment != null ? ropeAttachment.LiftCapacity : 0f);
        public float EffectiveWeight => Mathf.Max(0f, SupportedWeight);

        private void Awake()
        {
            if (ropeAttachment == null) ropeAttachment = GetComponent<RopeAttachment>();
        }
        public float StackWeight
        {
            get
            {
                CollectCarriedCrates(true);
                float total = 0f;
                foreach (Pushable crate in carriedCrates) total += crate.SupportedWeight;
                return Mathf.Max(0f, total);
            }
        }
        public bool ShowGizmos => showGizmos;

        public void EnableStacking() => Motion.EnableStacking();

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
                Physics2D.OverlapBox(center, size, 0f, supportBody.MotionFilter, overlaps);
                foreach (Collider2D hit in overlaps)
                {
                    if (!supportBody.IsMotionObstacle(hit)) continue;
                    if (!hit.TryGetComponent(out Pushable candidate) || !candidate.gameObject.activeInHierarchy
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
