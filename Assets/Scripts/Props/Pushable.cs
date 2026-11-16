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
        [SerializeField, Min(0f), Tooltip("Maximum small step climbed only during player push/pull. 0 disables stepping; 0.0625 units = 2 pixels at 32 PPU. Requires ground contact and a clear up/across/down route.")]
        private float maxPushStepHeight = 0.0625f;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private readonly List<Pushable> carriedCrates = new List<Pushable>(8);
        private readonly List<int> supportIndices = new List<int>(8);
        private readonly List<float> displacements = new List<float>(8);
        public SimulatedBody2D Body => body != null ? body : body = GetComponent<SimulatedBody2D>();
        public float Weight => Body.Weight;
        private PropsMotion Motion => motion != null ? motion : motion = GetComponent<PropsMotion>();
        private float SupportedWeight => CrateFlotation.EffectiveWeight(Weight, WaterVolume.SubmergedFraction(Body.Shape.bounds), Motion.FullSubmersionWeightMultiplier)
            - Motion.BalloonWeightReduction;
        public float EffectiveWeight => Mathf.Max(0f, SupportedWeight);

        private void Awake()
        {
            if (ropeAttachment == null) ropeAttachment = GetComponent<RopeAttachment>();
            if (ropeAttachment != null) Motion.RegisterRopeAttachment(ropeAttachment);
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
        internal bool UsesSupportPivot => Motion.UsesSupportPivot;

        internal void AccumulateStackMass(ref float mass, ref float moment)
        {
            CollectCarriedCrates(true);
            for (int i = 1; i < carriedCrates.Count; i++)
            {
                Pushable crate = carriedCrates[i];
                mass += crate.Weight;
                moment += crate.Body.Shape.bounds.center.x * crate.Weight;
            }
        }

        public void EnableStacking() => Motion.EnableStacking();

        public float AllowedHorizontalByPlayer(float amount, Collider2D ignored)
        {
            float allowed = Body.AllowedHorizontal(amount, ignored, Mathf.Max(0f, maxPushStepHeight));
            return Motion.AllowedTetherDisplacement(Vector2.right * allowed, ignoreBalloon: true).x;
        }

        public float MoveHorizontalByPlayer(float amount, Collider2D ignored)
        {
            BalloonLift balloon = Motion.LiftingBalloon;
            if (balloon == null || !balloon.PayloadTaut || Mathf.Approximately(amount, 0f))
                return MoveHorizontal(AllowedHorizontalByPlayer(amount, ignored), ignored, Mathf.Max(0f, maxPushStepHeight));
            float allowed = AllowedHorizontalByPlayer(amount, ignored);
            // Let the free anchor follow first, rather than stretching the rope and lifting the crate off its support.
            float followed = balloon.Motion.FollowPushedPayload(allowed, Body.Shape);
            float followedAlongPush = Mathf.Max(0f, Mathf.Sign(amount) * followed);
            float shared = Mathf.Sign(amount) * Mathf.Min(Mathf.Abs(allowed), followedAlongPush);
            return MoveHorizontal(shared, ignored, Mathf.Max(0f, maxPushStepHeight));
        }

        public float MoveHorizontal(float amount, Collider2D ignored = null, float maxStepHeight = 0f)
        {
            if (Mathf.Approximately(amount, 0f)) return 0f;
            CollectCarriedCrates();
            float moved = Body.MoveHorizontal(amount, ignored, maxStepHeight, followSlope: true);
            CarryCrates(moved);
            return moved;
        }

        internal SimulatedBody2D.GroundTravel PlanSurfacePush(float amount, Collider2D ignored, Vector2 normal)
            => Body.PlanSurfacePush(amount, ignored, maxPushStepHeight, normal);

        internal void ApplySurfacePush(SimulatedBody2D.GroundTravel travel, Collider2D ignored)
        {
            CollectCarriedCrates();
            Body.ApplyGroundTravel(travel, ignored);
            CarryCrates(travel.Displacement.x);
        }

        private void CarryCrates(float moved)
        {
            displacements.Clear();
            displacements.Add(moved);
            for (int i = 1; i < carriedCrates.Count; i++)
            {
                int supportIndex = supportIndices[i];
                SimulatedBody2D supportBody = carriedCrates[supportIndex].Body;
                // Carry only the support's actual movement; keep the upper crate's own collisions active.
                float carried = carriedCrates[i].Body.MoveHorizontal(displacements[supportIndex], supportBody.Shape);
                displacements.Add(carried);
            }
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
                BoxCollider2D supportShape = supportBody.Shape;
                Transform supportTransform = supportShape.transform;
                Vector2 normal = supportTransform.up;
                Vector2 top = supportTransform.TransformPoint(supportShape.offset + Vector2.up * (supportShape.size.y * 0.5f));
                float width = supportTransform.TransformVector(Vector3.right * supportShape.size.x).magnitude;
                float tolerance = supportBody.Skin * 4f;
                Vector2 center = top + normal * (tolerance * 0.5f);
                Vector2 size = new Vector2(width, tolerance);
                Physics2D.OverlapBox(center, size, supportTransform.eulerAngles.z, supportBody.MotionFilter, overlaps);
                foreach (Collider2D hit in overlaps)
                {
                    if (!supportBody.IsMotionObstacle(hit)) continue;
                    if (!hit.TryGetComponent(out Pushable candidate) || !candidate.gameObject.activeInHierarchy
                        || (!includeDisabled && !candidate.enabled)
                        || carriedCrates.Contains(candidate) || hit != candidate.Body.Shape) continue;
                    if (Vector2.Dot((Vector2)hit.bounds.center - top, normal) <= 0f) continue;
                    float gap = supportBody.Skin + candidate.Body.Skin;
                    Collider2D support = candidate.Body.Probe(Vector2.down, gap / Mathf.Max(0.01f, normal.y),
                        out float distance, out Vector2 hitNormal);
                    if (support != supportShape || Vector2.Dot(hitNormal, normal) < 0.99f || distance * hitNormal.y > gap) continue;
                    carriedCrates.Add(candidate);
                    supportIndices.Add(i);
                }
            }
        }
    }
}
