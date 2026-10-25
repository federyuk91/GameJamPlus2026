using System.Collections;
using UnityEngine;
using GameJamPlus.UI;
using MousePlatformer;

namespace GameJamPlus.Gameplay
{
    [DefaultExecutionOrder(250), DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class MouseTrap : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D contactArea;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField, Min(1)] private int damage = 3;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite[] closingFrames;
        [SerializeField, Tooltip("Conclusion requested from the central StageEndManager. None disables this ending.")]
        private StageEndCase ending = StageEndCase.MouseTrap;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip goreClip;
        [SerializeField, Range(0f, 1f)] private float goreVolume = 0.8f;
        [SerializeField, Min(0.01f)] private float frameDuration = 0.08f;
        [SerializeField, Min(0f)] private float closedHoldDuration = 0.25f;
        [SerializeField] private bool showGizmos = true;
        private bool triggered;
        private bool completed;
        private bool endingStage;
        private Coroutine animationRoutine;
        private StageEndRequest animationRequest;

        private void Awake()
        {
            if (contactArea == null) contactArea = GetComponent<BoxCollider2D>();

            contactArea.isTrigger = true;
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 0f;
            }
            if (visual != null && closingFrames != null && closingFrames.Length > 0) visual.sprite = closingFrames[0];
        }

        private void OnEnable()
        {
            GameAudioSettings.EffectsVolumeChanged += ApplyEffectsVolume;
            ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
        }

        private void ApplyEffectsVolume(float effectsVolume)
        {
            if (audioSource != null) audioSource.volume = goreVolume * effectsVolume;
        }

        private void PlayGoreSound()
        {
            if (audioSource == null || goreClip == null) return;
            ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
            audioSource.clip = goreClip;
            audioSource.Play();
        }

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (triggered || manager == null || manager.State != GameManager.LoopState.Playing
                || !contactArea.enabled || ending == StageEndCase.None) return;
            if (playerCollider == null) playerCollider = manager.PlayerCollider;
            if (playerCollider == null || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy) return;
            if (playerHealth == null) playerCollider.TryGetComponent(out playerHealth);
            if (playerHealth == null || playerHealth.IsDead) return;
            Physics2D.SyncTransforms();
            ColliderDistance2D contact = contactArea.Distance(playerCollider);
            if (!contact.isValid || contact.distance > 0f) return;
            triggered = true;
            contactArea.enabled = false;
            GameSession.RecordObservation(TestObservation.LittleMechanic);
            playerHealth.ApplyDamage(Mathf.Max(1, damage));
            endingStage = playerHealth.IsDead && StageEndManager.TryBeginAnimated(ending, this, out animationRequest);
            PlayGoreSound();
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
            if (endingStage) animationRequest.Complete();
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            GameAudioSettings.EffectsVolumeChanged -= ApplyEffectsVolume;
            if (audioSource != null) audioSource.Stop();
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            // A despawned trap must not leave the game paused while awaiting animation completion.
            if (endingStage && !completed)
            {
                completed = true;
                animationRequest.Complete();
            }
        }

        private void Reset()
        {
            contactArea = GetComponent<BoxCollider2D>();

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
