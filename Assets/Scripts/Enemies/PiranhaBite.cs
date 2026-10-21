using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(250), DisallowMultipleComponent]
    public sealed class PiranhaBite : MonoBehaviour
    {
        [SerializeField] private Collider2D contactArea;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private PlayerOxygen playerOxygen;
        [SerializeField, Min(0.1f), Tooltip("Minimum seconds between bites while this piranha remains in contact with the player. Each bite consumes one oxygen notch.")]
        private float biteInterval = 1f;
        [SerializeField] private bool showGizmos;
        private double nextBiteTime;

        private void Awake()
        {
            if (contactArea == null) contactArea = GetComponent<Collider2D>();
        }

        private void OnEnable() => nextBiteTime = 0d;

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.LoopState.Playing
                || Time.timeAsDouble < nextBiteTime) return;
            if (playerCollider == null) playerCollider = manager.PlayerCollider;
            if (playerOxygen == null && playerCollider != null)
                playerCollider.TryGetComponent(out playerOxygen);
            if (contactArea == null || !contactArea.enabled || playerCollider == null
                || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy
                || playerOxygen == null) return;

            // Both actors use custom Transform movement, without Rigidbody2D contact callbacks.
            Physics2D.SyncTransforms();
            ColliderDistance2D contact = contactArea.Distance(playerCollider);
            if (contact.isValid && contact.distance <= 0f && playerOxygen.ConsumeNotch())
                nextBiteTime = Time.timeAsDouble + Mathf.Max(0.1f, biteInterval);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || contactArea == null) return;
            Color previous = Gizmos.color;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(contactArea.bounds.center, contactArea.bounds.size);
            Gizmos.color = previous;
        }
    }
}
