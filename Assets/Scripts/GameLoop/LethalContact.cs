using UnityEngine;

namespace GameJamPlus.Gameplay
{
    [DefaultExecutionOrder(250), DisallowMultipleComponent]
    public sealed class LethalContact : MonoBehaviour
    {
        [SerializeField] private Collider2D contactArea;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private TestEndEvent testEnd;
        private bool triggered;

        private void FixedUpdate()
        {
            if (triggered || testEnd == null || contactArea == null || playerCollider == null
                || !contactArea.enabled || !playerCollider.enabled
                || !playerCollider.gameObject.activeInHierarchy) return;

            // Both actors move via Transform, without Rigidbody2D collision callbacks.
            Physics2D.SyncTransforms();
            ColliderDistance2D contact = contactArea.Distance(playerCollider);
            if (!contact.isValid || contact.distance > 0f) return;
            triggered = true;
            testEnd.EndTest();
        }

        private void Reset()
        {
            contactArea = GetComponent<Collider2D>();
            testEnd = GetComponent<TestEndEvent>();
            if (contactArea != null) contactArea.isTrigger = true;
        }

        private void OnValidate()
        {
            if (contactArea != null) contactArea.isTrigger = true;
        }
    }
}
