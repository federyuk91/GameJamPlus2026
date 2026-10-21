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
        [SerializeField, Tooltip("Each activation allows one press. After pressing, the button locks until ActivateButton is called again.")]
        private bool singleUse;
        [SerializeField, Tooltip("Starts pressed and unavailable until ActivateButton is called. Activation preserves Single Use and On Pressed settings.")]
        private bool startsLocked;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private UnityEvent onPressed = new UnityEvent();
        [Header("Spawn")]
        [SerializeField, Tooltip("Objects available to Spawn(int index). List indices start at zero.")]
        private List<GameObject> spawnPrefabs = new List<GameObject>();
        [SerializeField, Tooltip("Spawn position relative to this button. Move the Spawn Point handle in the Scene view to place it.")]
        private Vector3 spawnOffset = Vector3.up;
        [SerializeField, Tooltip("Off: each call repeats the supplied index. On: the supplied index starts a sequence that advances after each successful spawn and stops after the last entry. Passing a different starting index starts a new sequence.")]
        private bool advanceSpawnIndex;
        private int sequenceStartIndex = -1;
        private int nextSpawnIndex = -1;
        private Coroutine animationRoutine;
        private bool busy;
        private bool used;
        private bool initialized;
        private bool locked;
        private float visualBottom;
        public bool IsLocked => initialized ? locked : startsLocked;
        public bool CanPress => isActiveAndEnabled && !IsLocked && !busy && (!singleUse || !used)
            && interactionArea != null && interactionArea.enabled;
        public Vector2 InteractionPoint => visual != null ? visual.bounds.center : transform.position;
        public bool ShowGizmos => showGizmos;
        public Vector3 SpawnPosition => transform.TransformPoint(spawnOffset);
        public void SetSpawnPosition(Vector3 worldPosition) => spawnOffset = transform.InverseTransformPoint(worldPosition);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => activeButtons.Clear();

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            locked = startsLocked;
            if (interactionArea == null) interactionArea = GetComponent<BoxCollider2D>();
            interactionArea.isTrigger = true;
            if (visual != null)
                visualBottom = visual.transform.localPosition.y + (visual.sprite != null ? visual.sprite.bounds.min.y * visual.transform.localScale.y : 0f);
            SetSprite(locked ? pressedSprite : releasedSprite);
        }

        public void ActivateButton()
        {
            Initialize();
            if (!locked && !(singleUse && used)) return;
            // A new activation must not be overwritten by an unfinished press animation.
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            busy = false;
            locked = false;
            used = false;
            SetSprite(releasedSprite);
        }

        public void LockButton()
        {
            Initialize();
            locked = true;
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            busy = false;
            SetSprite(pressedSprite);
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
            if (singleUse) locked = true;
            busy = true;
            animationRoutine = StartCoroutine(AnimatePress());
            onPressed.Invoke();
            return true;
        }

        public void Spawn(int index)
        {
            if (index < 0 || index >= spawnPrefabs.Count)
            {
                Debug.LogWarning($"Spawn index {index} is outside this button's list.", this);
                return;
            }

            if (!advanceSpawnIndex || sequenceStartIndex != index)
            {
                sequenceStartIndex = index;
                nextSpawnIndex = index;
            }
            if (nextSpawnIndex >= spawnPrefabs.Count) return;

            GameObject prefab = spawnPrefabs[nextSpawnIndex];
            if (prefab == null)
            {
                Debug.LogWarning($"No object assigned at spawn index {nextSpawnIndex}.", this);
                return;
            }

            Instantiate(prefab, SpawnPosition, prefab.transform.rotation);
            if (advanceSpawnIndex) nextSpawnIndex++;
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
            SetSprite(IsLocked || (singleUse && used) ? pressedSprite : releasedSprite);
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

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            Color previousColor = Gizmos.color;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, SpawnPosition);
            Gizmos.DrawWireSphere(SpawnPosition, 0.12f);
            Gizmos.color = previousColor;
        }
    }
}
