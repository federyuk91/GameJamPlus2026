using UnityEngine;
using UnityEngine.Events;

namespace GameJamPlus.Gameplay
{
    [DefaultExecutionOrder(250), DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D activationArea;
        [SerializeField, Tooltip("Empty means This. Other scenes must be enabled in Build Profiles.")]
        private string targetScenePath;
        [SerializeField, Tooltip("Player pivot position relative to the checkpoint. Used only with This.")]
        private Vector2 respawnOffset = Vector2.right * 2f;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private UnityEvent onActivated = new UnityEvent();
        private bool playerInside;

        public bool UsesLocalRespawn => string.IsNullOrEmpty(targetScenePath);
        public bool ShowGizmos => showGizmos;
        public Vector3 RespawnPosition => transform.TransformPoint(respawnOffset);

        private void Awake()
        {
            if (activationArea == null) activationArea = GetComponent<BoxCollider2D>();
            activationArea.isTrigger = true;
        }

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.LoopState.Playing) return;
            Collider2D player = manager.PlayerCollider;
            if (activationArea == null || !activationArea.enabled || player == null
                || !player.enabled || !player.gameObject.activeInHierarchy) return;

            // Transform-based player movement does not produce Rigidbody2D trigger callbacks.
            Physics2D.SyncTransforms();
            ColliderDistance2D distance = activationArea.Distance(player);
            bool inside = distance.isValid && distance.distance <= 0f;
            if (inside && !playerInside) Activate();
            playerInside = inside;
        }

        public void Activate()
        {
            GameManager manager = GameManager.Instance;
            if (!isActiveAndEnabled || manager == null || manager.State != GameManager.LoopState.Playing) return;
            string scenePath = UsesLocalRespawn ? gameObject.scene.path : targetScenePath;
            if (string.IsNullOrWhiteSpace(scenePath)
                || !Application.CanStreamedLevelBeLoaded(scenePath))
            {
                Debug.LogError("Checkpoint destination must be a saved scene enabled in Build Profiles: " + scenePath, this);
                return;
            }
            GameSession.SetCheckpoint(gameObject.scene.path, scenePath, UsesLocalRespawn, RespawnPosition);
            onActivated.Invoke();
        }

        public void SetRespawnPosition(Vector3 position) => respawnOffset = transform.InverseTransformPoint(position);

        private void OnDisable() => playerInside = false;

        private void Reset()
        {
            activationArea = GetComponent<BoxCollider2D>();
            activationArea.isTrigger = true;
        }

        private void OnValidate()
        {
            if (activationArea != null) activationArea.isTrigger = true;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            Color previous = Gizmos.color;
            if (activationArea != null)
            {
                Matrix4x4 previousMatrix = Gizmos.matrix;
                Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.65f);
                Gizmos.matrix = activationArea.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(activationArea.offset, activationArea.size);
                Gizmos.matrix = previousMatrix;
            }
            if (UsesLocalRespawn)
            {
                Vector3 position = RespawnPosition;
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, position);
                Gizmos.DrawWireSphere(position, 0.15f);
                Gizmos.DrawLine(position + Vector3.left * 0.25f, position + Vector3.right * 0.25f);
                Gizmos.DrawLine(position + Vector3.down * 0.25f, position + Vector3.up * 0.25f);
            }
            Gizmos.color = previous;
        }
    }
}
