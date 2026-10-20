using System;
using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(-50), DisallowMultipleComponent]
    public sealed class RopeSimulation : MonoBehaviour
    {
        private static readonly List<RopeSimulation> activeRopes = new List<RopeSimulation>();
        internal static IReadOnlyList<RopeSimulation> ActiveRopes => activeRopes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => activeRopes.Clear();

        private void OnEnable()
        {
            if (!activeRopes.Contains(this)) activeRopes.Add(this);
        }

        [SerializeField, Min(1), Tooltip("Piece count. Total length is Length x Segment Length.")]
        private int length = 1;
        [SerializeField, HideInInspector] private Transform[] pieces;
        [SerializeField, HideInInspector] private Vector2 restDirection = Vector2.down;
        [SerializeField, Min(0.01f)] private float segmentLength = 0.5f;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField, Min(0f), Tooltip("Individual multiplier of world gravity for this rope's segments and rider tension calculation. Independent of a connected body's Gravity Scale.")]
        private float gravityScale = 1f;
        private float GravityAcceleration => (physicsSettings != null
            ? physicsSettings.gravityAcceleration : PhysicsSettings.DefaultGravityAcceleration) * Mathf.Max(0f, gravityScale);
        [SerializeField, Min(0f), Tooltip("Distance below full rope length at which the balloon and attached body both consider this rope taut, in world units.")]
        private float tautTolerance = 0.01f;
        public float TautTolerance => Mathf.Clamp(tautTolerance, 0f, Length);
        private int SimulationSubsteps => Mathf.Max(1, physicsSettings != null
            ? physicsSettings.ropeSimulationSubsteps : PhysicsSettings.DefaultRopeSimulationSubsteps);
        private int ConstraintIterations => Mathf.Max(1, physicsSettings != null
            ? physicsSettings.ropeConstraintIterations : PhysicsSettings.DefaultRopeConstraintIterations);
        [SerializeField, Min(0f)] private float damping = 0.35f;
        [SerializeField] private RopeSwingConstraint swingLimits = new RopeSwingConstraint();
        [SerializeField] private bool canBeCut = true;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private BalloonLift balloonAnchor;
        private RopeAttachment load;
        private bool retainCrateSide;
        private RopeChain chain;
        private PlayerRopeInteraction rider;
        private float gripDistance = -1f;
        private float drive;
        public int PieceCount => pieces != null ? pieces.Length : 0;
        public int ConfiguredPieceCount => Mathf.Max(1, length);
        public Transform Piece(int index) => pieces[index];
        public RopeChain Chain => chain;
        public RopeSwingConstraint SwingLimits => swingLimits;
        public float Length => chain != null ? chain.Length : (pieces != null ? pieces.Length * segmentLength : 0f);
        public bool Available => isActiveAndEnabled && chain != null && rider == null && ClimbableEndSegment > ClimbableFirstSegment;
        public bool CanBeCut => canBeCut;
        public bool ShowGizmos => showGizmos;
        public bool HasBeenCut { get; private set; }
        public BalloonLift BalloonAnchor => balloonAnchor;
        public RopeAttachment Load => load;
        public bool HasIntactLoadConnection => load != null && load.Attached && !HasBeenCut
            && chain != null && chain.ConnectedSegmentCount == chain.SegmentCount;
        public bool KeepCrateSideAfterCut => retainCrateSide || (!HasBeenCut && load != null && load.Floats
            && load.AttachmentPoint.y > transform.position.y + 0.01f);
        public int ClimbableFirstSegment => chain != null && retainCrateSide
            ? chain.SegmentCount - chain.EndpointConnectedSegmentCount : 0;
        public int ClimbableEndSegment => chain == null ? 0 : retainCrateSide ? chain.SegmentCount : chain.ConnectedSegmentCount;
        public float ClimbableStart => chain != null ? ClimbableFirstSegment * chain.SegmentLength : 0f;
        public float ClimbableLength => chain != null ? ClimbableEndSegment * chain.SegmentLength : 0f;
        public float MinimumGripDistance => retainCrateSide ? ClimbableStart : Mathf.Min(0.3f, ClimbableLength);
        public event Action<RopeSimulation> Cut;
        private bool AnchorSupportsRider => rider != null && rider.isActiveAndEnabled && chain != null
            && gripDistance > 0f && gripDistance <= chain.ConnectedLength;
        private bool LoadSupportsRider => rider != null && rider.isActiveAndEnabled && chain != null
            && load != null && load.HasRopeConnection && chain.EndpointConnectedSegmentCount > 0
            && gripDistance >= (chain.SegmentCount - chain.EndpointConnectedSegmentCount) * chain.SegmentLength;
        public float AnchorRiderWeight => AnchorSupportsRider ? rider.WeightKg
            * (LoadSupportsRider ? 1f - Mathf.Clamp01(gripDistance / chain.Length) : 1f) : 0f;

        public float LoadRiderWeight(RopeAttachment attachment)
        {
            if (load != attachment || !LoadSupportsRider) return 0f;
            return rider.WeightKg * (AnchorSupportsRider ? Mathf.Clamp01(gripDistance / chain.Length) : 1f);
        }

        public Vector2 AnchorPull(Vector2 anchorVelocity)
        {
            if (rider == null || !rider.isActiveAndEnabled || chain == null
                || gripDistance <= 0f || gripDistance > chain.ConnectedLength)
                return Vector2.zero;
            return RiderPull(transform.position, anchorVelocity)
                * (LoadSupportsRider ? 1f - Mathf.Clamp01(gripDistance / chain.Length) : 1f);
        }

        public Vector2 LoadPull(RopeAttachment attachment, Vector2 endpointVelocity)
        {
            if (load != attachment || attachment == null || !attachment.HasRopeConnection
                || rider == null || !rider.isActiveAndEnabled || chain == null
                || chain.EndpointConnectedSegmentCount == 0
                || gripDistance < (chain.SegmentCount - chain.EndpointConnectedSegmentCount) * chain.SegmentLength)
                return Vector2.zero;
            return RiderPull(attachment.AttachmentPoint, endpointVelocity)
                * (AnchorSupportsRider ? Mathf.Clamp01(gripDistance / chain.Length) : 1f);
        }

        private Vector2 RiderPull(Vector2 attachmentPoint, Vector2 attachmentVelocity)
        {
            Vector2 radial = chain.Point(gripDistance) - attachmentPoint;
            float distance = radial.magnitude;
            if (distance < 0.01f) return Vector2.zero;
            Vector2 direction = radial / distance;
            Vector2 relativeVelocity = chain.Velocity(gripDistance) - attachmentVelocity;
            Vector2 tangentVelocity = relativeVelocity - direction * Vector2.Dot(relativeVelocity, direction);
            float gravity = GravityAcceleration;
            // Pendulum tension transfers both the hanging weight and the swing's centripetal load.
            float tension = Mathf.Max(0f, -gravity * direction.y) + tangentVelocity.sqrMagnitude / distance;
            return direction * (Mathf.Min(tension, 120f) * rider.WeightKg);
        }

        public void SetPieces(Transform[] value)
        {
            pieces = value;
            AlignPieces();
        }

        public void AlignPieces()
        {
            Vector2 direction = restDirection.sqrMagnitude > 0.0001f ? restDirection.normalized : Vector2.down;
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f);
            for (int i = 0; i < PieceCount; i++)
            {
                Transform piece = pieces[i];
                if (piece == null) continue;
                piece.localPosition = direction * ((i + 0.5f) * segmentLength);
                piece.localRotation = rotation;
                piece.name = i == 0 ? "Rope Piece" : "Rope Piece (" + i + ")";
                SpriteRenderer visual = piece.GetComponent<SpriteRenderer>();
                if (visual == null || visual.sprite == null) continue;
                float spriteHeight = visual.sprite.bounds.size.y;
                if (spriteHeight > 0.0001f)
                    piece.localScale = new Vector3(piece.localScale.x, segmentLength / spriteHeight, piece.localScale.z);
            }
        }

        private void Awake()
        {
            if (pieces == null || pieces.Length == 0) { enabled = false; return; }
            chain = new RopeChain(transform.position, pieces.Length, segmentLength);
            if (load != null) chain.InitializeEndpoint(transform.position, load.AttachmentPoint);
            UpdatePieces();
        }

        private void FixedUpdate()
        {
            float gravity = GravityAcceleration;
            chain.Step(transform.position, Time.fixedDeltaTime, gravity, damping, gripDistance, drive,
                ConstraintIterations, SimulationSubsteps,
                swingLimits: swingLimits, endpoint: load != null && load.HasRopeConnection ? load.AttachmentPoint : (Vector2?)null);
            UpdatePieces();
        }

        public bool BindLoad(RopeAttachment attachment)
        {
            if (!isActiveAndEnabled || HasBeenCut || (load != null && load != attachment)) return false;
            load = attachment;
            chain?.InitializeEndpoint(transform.position, load.AttachmentPoint);
            return true;
        }

        public void ReleaseLoad(RopeAttachment attachment)
        {
            if (load != attachment) return;
            chain?.ReleaseEndpoint(Time.fixedDeltaTime, SimulationSubsteps);
            load = null;
        }

        public bool TryCutSegment(int segment)
        {
            if (!isActiveAndEnabled || !canBeCut || chain == null || !chain.IsSegmentIntact(segment)) return false;
            bool keepCrate = KeepCrateSideAfterCut;
            if (!chain.CutSegment(segment)) return false;
            int remainingPieces = chain.RemoveShortFragments(2);
            retainCrateSide = keepCrate;
            HasBeenCut = true;
            rider?.RetainGripAfterCut(this);
            Cut?.Invoke(this);
            UpdatePieces();
            if (remainingPieces == 0) enabled = false;
            return true;
        }

        public bool Attach(PlayerRopeInteraction player, float distance, Vector2 incomingVelocity)
        {
            if (!Available) return false;
            rider = player;
            gripDistance = distance;
            chain.AddVelocity(distance, incomingVelocity * 0.4f, Time.fixedDeltaTime, SimulationSubsteps);
            return true;
        }

        public void SetDrive(PlayerRopeInteraction player, float distance, float acceleration)
        {
            if (rider != player) return;
            gripDistance = distance;
            drive = acceleration;
        }

        public void Release(PlayerRopeInteraction player)
        {
            if (rider != player) return;
            rider = null;
            gripDistance = -1f;
            drive = 0f;
        }

        private void UpdatePieces()
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] == null) continue;
                pieces[i].gameObject.SetActive(chain.IsSegmentIntact(i));
                if (!chain.IsSegmentIntact(i)) continue;
                Vector2 delta = chain[i + 1] - chain[i];
                Vector2 center = (chain[i + 1] + chain[i]) * 0.5f;
                pieces[i].SetPositionAndRotation(new Vector3(center.x, center.y, transform.position.z),
                    Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 90f));
            }
        }

        private void OnDisable()
        {
            activeRopes.Remove(this);
            if (load != null) load.Detach();
            rider = null;
            drive = 0f;
            gripDistance = -1f;
        }
        private void OnValidate()
        {
            length = Mathf.Max(1, length);
            segmentLength = Mathf.Max(0.01f, segmentLength);
            if (swingLimits == null) swingLimits = new RopeSwingConstraint();
            swingLimits.Sanitize();
        }
    }
}
