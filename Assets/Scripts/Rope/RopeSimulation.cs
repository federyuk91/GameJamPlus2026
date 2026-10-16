using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(-50), DisallowMultipleComponent]
    public sealed class RopeSimulation : MonoBehaviour
    {
        [SerializeField] private Transform[] pieces;
        [InspectorName("Segment Length (units)")]
        [SerializeField, Min(0.01f)] private float segmentLength = 0.5f;
        [SerializeField] private PhysicsSettings physicsSettings;
        [InspectorName("Damping (1/s)")]
        [SerializeField, Min(0f)] private float damping = 0.35f;
        [SerializeField] private RopeSwingConstraint swingLimits = new RopeSwingConstraint();
        private RopeChain chain;
        private PlayerRopeInteraction rider;
        private float gripDistance = -1f;
        private float drive;
        public RopeChain Chain => chain;
        public RopeSwingConstraint SwingLimits => swingLimits;
        public float Length => chain != null ? chain.Length : (pieces != null ? pieces.Length * segmentLength : 0f);
        public bool Available => isActiveAndEnabled && chain != null && rider == null;

        private void Awake()
        {
            if (pieces == null || pieces.Length == 0) { enabled = false; return; }
            chain = new RopeChain(transform.position, pieces.Length, segmentLength);
            UpdatePieces();
        }

        private void FixedUpdate()
        {
            float gravity = (physicsSettings != null ? physicsSettings.gravity : PhysicsSettings.DefaultGravity).ropeGravity;
            chain.Step(transform.position, Time.fixedDeltaTime, gravity, damping, gripDistance, drive,
                swingLimits: swingLimits);
            UpdatePieces();
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
                Vector2 delta = chain[i + 1] - chain[i];
                Vector2 center = (chain[i + 1] + chain[i]) * 0.5f;
                pieces[i].SetPositionAndRotation(new Vector3(center.x, center.y, transform.position.z),
                    Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 90f));
            }
        }

        private void OnDisable() { rider = null; drive = 0f; gripDistance = -1f; }
        private void OnValidate()
        {
            if (swingLimits == null) swingLimits = new RopeSwingConstraint();
            swingLimits.Sanitize();
        }
    }
}
