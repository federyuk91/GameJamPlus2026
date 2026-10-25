using UnityEngine;

namespace GameJamPlus.Gameplay
{
    [DefaultExecutionOrder(250), DisallowMultipleComponent]
    public sealed class LethalContact : MonoBehaviour
    {
        [SerializeField] private Collider2D contactArea;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField, Tooltip("Conclusion requested from the central StageEndManager. None disables this ending.")]
        private StageEndCase ending = StageEndCase.Fall;
        [SerializeField] private bool showGizmos;
        private bool triggered;

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.LoopState.Playing) return;
            if (playerCollider == null) playerCollider = manager.PlayerCollider;
            if (triggered || ending == StageEndCase.None || contactArea == null || playerCollider == null
                || !contactArea.enabled || !playerCollider.enabled
                || !playerCollider.gameObject.activeInHierarchy) return;

            // Both actors move via Transform, without Rigidbody2D collision callbacks.
            Physics2D.SyncTransforms();
            ColliderDistance2D contact = contactArea.Distance(playerCollider);
            if (!contact.isValid || contact.distance > 0f) return;
            triggered = true;
            StageEndManager.Request(ending, this);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || contactArea == null) return;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            if (contactArea is BoxCollider2D box)
            {
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.06f);
                Gizmos.DrawCube(box.offset, box.size);
                Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.5f);
                Gizmos.DrawWireCube(box.offset, box.size);
            }
            else
            {
                Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.5f);
                Gizmos.DrawWireCube(contactArea.bounds.center, contactArea.bounds.size);
            }
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        private void Reset()
        {
            contactArea = GetComponent<Collider2D>();

            if (contactArea != null) contactArea.isTrigger = true;
        }

        private void OnValidate()
        {
            if (contactArea != null) contactArea.isTrigger = true;
        }
    }
}
