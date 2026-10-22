using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameJamPlus.Gameplay
{
    [DisallowMultipleComponent, RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class HandReleaseIntro : MonoBehaviour
    {
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int ReleaseState = Animator.StringToHash("Base Layer.Release");
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField, Tooltip("Hand position above the selected player spawn, in world units.")]
        private Vector2 spawnOffset = Vector2.up;
        [SerializeField, Tooltip("Replace this placeholder with the final release animation. The controller is overridden only for this instance.")]
        private AnimationClip releaseAnimation;
        [SerializeField, Min(0f), Tooltip("Time after input before the real player is activated. An animation event can also call ReleasePlayer earlier.")]
        private float releaseDelay;
        [SerializeField, Min(0.01f)] private float exitDuration = 0.3f;
        [SerializeField, Min(0f)] private float exitDistance = 4f;
        private InputAction releaseInput;
        private Transform player;
        private Vector3 spawnPosition;
        private Action released;
        private Color originalColor;
        private AnimatorOverrideController runtimeController;
        private RuntimeAnimatorController originalController;
        private bool waiting;
        private bool playerReleased;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (visual == null) visual = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (releaseInput == null)
            {
                releaseInput = new InputAction("Release Mouse", InputActionType.Button);
                releaseInput.AddBinding("<Keyboard>/anyKey");
                releaseInput.AddBinding("<Mouse>/leftButton");
                releaseInput.AddBinding("<Mouse>/rightButton");
                string[] buttons = { "buttonSouth", "buttonEast", "buttonNorth", "buttonWest", "start", "select",
                    "leftShoulder", "rightShoulder", "leftTrigger", "rightTrigger", "leftStickPress", "rightStickPress",
                    "dpad/up", "dpad/down", "dpad/left", "dpad/right" };
                foreach (string button in buttons) releaseInput.AddBinding("<Gamepad>/" + button);
                releaseInput.performed += OnReleaseInput;
            }
            releaseInput.Enable();
        }

        public bool Begin(Transform subject, Action onReleased)
        {
            if (subject == null || animator == null || visual == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogError("Assign the hand Animator, SpriteRenderer and controller before starting the intro.", this);
                return false;
            }
            player = subject;
            spawnPosition = player.position;
            released = onReleased;
            originalColor = visual.color;
            // Frames already include the suspended mouse. Keep the real player fully inactive.
            player.gameObject.SetActive(false);
            if (player.IsChildOf(transform)) player.SetParent(transform.parent, true);
            transform.position = spawnPosition + (Vector3)spawnOffset;
            player.position = spawnPosition;
            Physics2D.SyncTransforms();
            originalController = animator.runtimeAnimatorController;
            if (releaseAnimation != null)
            {
                runtimeController = new AnimatorOverrideController(originalController);
                runtimeController["HandRelease_Release"] = releaseAnimation;
                animator.runtimeAnimatorController = runtimeController;
            }
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Play(IdleState, 0, 0f);
            waiting = true;
            return true;
        }

        private void OnReleaseInput(InputAction.CallbackContext context)
        {
            if (!waiting) return;
            waiting = false;
            releaseInput.Disable();
            StartCoroutine(ReleaseSequence());
        }

        private IEnumerator ReleaseSequence()
        {
            animator.Play(ReleaseState, 0, 0f);
            float elapsed = 0f;
            // Do not pass the initiating key to gameplay or dialogue in the same input update.
            yield return null;
            while (!playerReleased && elapsed < releaseDelay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            ReleasePlayer();
            float remaining = releaseAnimation != null ? Mathf.Max(0f, releaseAnimation.length - elapsed) : 0f;
            if (remaining > 0f) yield return new WaitForSecondsRealtime(remaining);
            Vector3 origin = transform.position;
            elapsed = 0f;
            while (elapsed < exitDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / exitDuration);
                transform.position = origin + Vector3.up * (exitDistance * Mathf.SmoothStep(0f, 1f, progress));
                Color color = originalColor;
                color.a *= 1f - progress;
                visual.color = color;
                yield return null;
            }
            gameObject.SetActive(false);
        }

        public void ReleasePlayer()
        {
            if (waiting || playerReleased || player == null) return;
            playerReleased = true;
            player.position = spawnPosition;
            Physics2D.SyncTransforms();
            player.gameObject.SetActive(true);
            if (player.TryGetComponent(out MousePlatformer.PlayerInputReader input))
                input.SetGameplayActionsLocked(false);
            Action callback = released;
            released = null;
            callback?.Invoke();
        }

        private void OnDisable()
        {
            releaseInput?.Disable();
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            if (releaseInput != null)
            {
                releaseInput.performed -= OnReleaseInput;
                releaseInput.Dispose();
            }
            if (runtimeController != null)
            {
                if (animator != null) animator.runtimeAnimatorController = originalController;
                Destroy(runtimeController);
            }
        }
    }
}
