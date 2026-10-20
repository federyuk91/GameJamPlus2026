using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class InteractableButton : MonoBehaviour
    {
        private static readonly List<InteractableButton> activeButtons = new List<InteractableButton>();
        internal static IReadOnlyList<InteractableButton> ActiveButtons => activeButtons;
        [SerializeField] private BoxCollider2D interactionArea;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite releasedSprite;
        [SerializeField] private Sprite halfwaySprite;
        [SerializeField] private Sprite pressedSprite;
        [SerializeField, Min(0.01f)] private float animationStepDuration = 0.08f;
        [SerializeField] private bool singleUse;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private UnityEvent onPressed = new UnityEvent();
        private Coroutine animationRoutine;
        private bool busy;
        private bool used;
        private float visualBottom;
        public bool CanPress => isActiveAndEnabled && !busy && (!singleUse || !used)
            && interactionArea != null && interactionArea.enabled;
        public Vector2 InteractionPoint => visual != null ? visual.bounds.center : transform.position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => activeButtons.Clear();

        private void Awake()
        {
            if (interactionArea == null) interactionArea = GetComponent<BoxCollider2D>();
            interactionArea.isTrigger = true;
            if (visual != null)
                visualBottom = visual.transform.localPosition.y + (visual.sprite != null ? visual.sprite.bounds.min.y * visual.transform.localScale.y : 0f);
            SetSprite(releasedSprite);
        }

        private void OnEnable()
        {
            if (!activeButtons.Contains(this)) activeButtons.Add(this);
        }

        public bool Contains(Collider2D playerCollider)
        {
            if (!CanPress || playerCollider == null || !playerCollider.enabled) return false;
            ColliderDistance2D distance = interactionArea.Distance(playerCollider);
            return distance.isValid && distance.distance <= 0f;
        }

        public bool TryPress()
        {
            if (!CanPress) return false;
            used = true;
            busy = true;
            animationRoutine = StartCoroutine(AnimatePress());
            onPressed.Invoke();
            return true;
        }

        private IEnumerator AnimatePress()
        {
            SetSprite(halfwaySprite);
            yield return new WaitForSeconds(animationStepDuration);
            SetSprite(pressedSprite);
            yield return new WaitForSeconds(animationStepDuration);
            if (!singleUse)
            {
                SetSprite(halfwaySprite);
                yield return new WaitForSeconds(animationStepDuration);
                SetSprite(releasedSprite);
            }
            busy = false;
            animationRoutine = null;
        }

        private void SetSprite(Sprite sprite)
        {
            if (visual == null || sprite == null) return;
            visual.sprite = sprite;
            Vector3 position = visual.transform.localPosition;
            position.y = visualBottom - sprite.bounds.min.y * visual.transform.localScale.y;
            visual.transform.localPosition = position;
        }

        private void OnDisable()
        {
            activeButtons.Remove(this);
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            busy = false;
            SetSprite(singleUse && used ? pressedSprite : releasedSprite);
        }

        private void Reset()
        {
            interactionArea = GetComponent<BoxCollider2D>();
            interactionArea.isTrigger = true;
        }

        private void OnValidate()
        {
            if (interactionArea != null) interactionArea.isTrigger = true;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || interactionArea == null) return;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = interactionArea.transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.6f);
            Gizmos.DrawWireCube(interactionArea.offset, interactionArea.size);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
