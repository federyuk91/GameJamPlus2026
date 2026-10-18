using System.Collections;
using UnityEngine;

namespace GameJamPlus.Gameplay
{
    [DefaultExecutionOrder(250), DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D), typeof(TestEndEvent))]
    public sealed class MouseTrap : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D contactArea;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite[] closingFrames;
        [SerializeField] private TestEndEvent testEnd;
        [SerializeField, Min(0.01f)] private float frameDuration = 0.08f;
        [SerializeField, Min(0f)] private float closedHoldDuration = 0.25f;
        [SerializeField] private bool showGizmos = true;
        private bool triggered;
        private bool completed;
        private Coroutine animationRoutine;

        private void Awake()
        {
            if (contactArea == null) contactArea = GetComponent<BoxCollider2D>();
            if (testEnd == null) testEnd = GetComponent<TestEndEvent>();
            contactArea.isTrigger = true;
            if (visual != null && closingFrames != null && closingFrames.Length > 0) visual.sprite = closingFrames[0];
        }

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (triggered || manager == null || manager.State != GameManager.LoopState.Playing
                || !contactArea.enabled || testEnd == null) return;
            if (playerCollider == null) playerCollider = manager.PlayerCollider;
            if (playerCollider == null || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy) return;
            Physics2D.SyncTransforms();
            ColliderDistance2D contact = contactArea.Distance(playerCollider);
            if (!contact.isValid || contact.distance > 0f || !testEnd.BeginAnimatedEnd()) return;
            triggered = true;
            animationRoutine = StartCoroutine(PlayClosingAnimation());
        }

        private IEnumerator PlayClosingAnimation()
        {
            foreach (Sprite frame in closingFrames ?? System.Array.Empty<Sprite>())
            {
                if (frame == null) continue;
                if (visual != null) visual.sprite = frame;
                yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, frameDuration));
            }
            if (closedHoldDuration > 0f) yield return new WaitForSecondsRealtime(closedHoldDuration);
            completed = true;
            animationRoutine = null;
            testEnd.CompleteAnimatedEnd();
        }

        private void OnDisable()
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            // A despawned trap must not leave the game paused while awaiting animation completion.
            if (triggered && !completed && testEnd != null)
            {
                completed = true;
                testEnd.CompleteAnimatedEnd();
            }
        }

        private void Reset()
        {
            contactArea = GetComponent<BoxCollider2D>();
            testEnd = GetComponent<TestEndEvent>();
            contactArea.isTrigger = true;
        }

        private void OnValidate()
        {
            if (contactArea != null) contactArea.isTrigger = true;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || contactArea == null) return;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = contactArea.transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.5f);
            Gizmos.DrawWireCube(contactArea.offset, contactArea.size);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
