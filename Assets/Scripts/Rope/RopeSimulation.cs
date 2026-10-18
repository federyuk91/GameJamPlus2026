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

        [SerializeField, Min(1), Tooltip("Numero di pezzi. La lunghezza totale e Length x Segment Length.")]
        private int length = 1;
        [SerializeField, HideInInspector] private Transform[] pieces;
        [SerializeField, HideInInspector] private Vector2 restDirection = Vector2.down;
        [SerializeField, Min(0.01f)] private float segmentLength = 0.5f;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField, Min(0f)] private float damping = 0.35f;
        [SerializeField] private RopeSwingConstraint swingLimits = new RopeSwingConstraint();
        [SerializeField] private bool canBeCut = true;
        [SerializeField] private bool showGizmos = true;
        private CrateRopeAttachment load;
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
        public bool Available => isActiveAndEnabled && chain != null && rider == null && chain.ConnectedSegmentCount > 0;
        public bool CanBeCut => canBeCut;
        public bool ShowGizmos => showGizmos;
        public bool HasBeenCut { get; private set; }
        public float ClimbableLength => chain != null ? chain.ConnectedLength : 0f;
        public event Action<RopeSimulation> Cut;

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
            float gravity = (physicsSettings != null ? physicsSettings.gravity : PhysicsSettings.DefaultGravity).ropeGravity;
            chain.Step(transform.position, Time.fixedDeltaTime, gravity, damping, gripDistance, drive,
                swingLimits: swingLimits, endpoint: load != null && load.Attached ? load.AttachmentPoint : (Vector2?)null);
            UpdatePieces();
        }

        public bool BindLoad(CrateRopeAttachment attachment)
        {
            if (!isActiveAndEnabled || HasBeenCut || (load != null && load != attachment)) return false;
            load = attachment;
            chain?.InitializeEndpoint(transform.position, load.AttachmentPoint);
            return true;
        }

        public void ReleaseLoad(CrateRopeAttachment attachment)
        {
            if (load != attachment) return;
            chain?.ReleaseEndpoint(Time.fixedDeltaTime);
            load = null;
        }

        public bool TryCutSegment(int segment)
        {
            if (!isActiveAndEnabled || !canBeCut || chain == null || !chain.CutSegment(segment)) return false;
            HasBeenCut = true;
            rider?.RetainGripAfterCut(this);
            Cut?.Invoke(this);
            UpdatePieces();
            return true;
        }

        public bool Attach(PlayerRopeInteraction player, float distance, Vector2 incomingVelocity)
        {
            if (!Available) return false;
            rider = player;
            gripDistance = distance;
            chain.AddVelocity(distance, incomingVelocity * 0.4f, Time.fixedDeltaTime);
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
