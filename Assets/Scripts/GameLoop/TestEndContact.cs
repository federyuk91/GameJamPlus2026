using UnityEngine;

namespace GameJamPlus.Gameplay
{
    [DefaultExecutionOrder(250), DisallowMultipleComponent]
    public sealed class TestEndContact : MonoBehaviour
    {
        [SerializeField] private Collider2D contactArea;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private TestEndEvent outcome;
        private bool triggered;

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            // Prefab assets cannot reference the player in a scene.
            if (playerCollider == null && manager != null) playerCollider = manager.PlayerCollider;
            if (triggered || manager == null || manager.State != GameManager.LoopState.Playing
                || outcome == null || contactArea == null || playerCollider == null
                || !contactArea.enabled || !contactArea.gameObject.activeInHierarchy
                || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy) return;

            // Simulated bodies move their Transforms without Rigidbody2D trigger callbacks.
            Physics2D.SyncTransforms();
            ColliderDistance2D contact = contactArea.Distance(playerCollider);
            if (!contact.isValid || contact.distance > 0f) return;

            triggered = true;
            outcome.EndTest();
        }

        private void Reset()
        {
            contactArea = GetComponent<Collider2D>();
            outcome = GetComponent<TestEndEvent>();
            if (contactArea != null) contactArea.isTrigger = true;
        }

        private void OnValidate()
        {
            if (contactArea != null) contactArea.isTrigger = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (contactArea == null) return;
            Color previousColor = Gizmos.color;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(contactArea.bounds.center, contactArea.bounds.size);
            Gizmos.color = previousColor;
        }
    }
}
