using System.Collections;
using MousePlatformer;
using UnityEngine;
using GameJamPlus.UI;

namespace GameJamPlus.Gameplay
{
    [DefaultExecutionOrder(250), DisallowMultipleComponent]
    [RequireComponent(typeof(PropsMotion), typeof(TestEndEvent), typeof(AudioSource))]
    public sealed class FallingCrushHazard : MonoBehaviour
    {
        [SerializeField] private PropsMotion motion;
        [SerializeField] private TestEndEvent testEnd;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip impactClip;
        [SerializeField, Range(0f, 1f), Tooltip("Crushing impact volume before Effects and Master volume settings.")]
        private float impactVolume = 0.8f;
        [SerializeField, Min(0f), Tooltip("Minimum downward crate speed in world units per second that can crush the player. Ignores resting contact and small settling movements.")]
        private float minimumFallSpeed = 0.5f;
        [SerializeField, Min(0f), Tooltip("Extra world-space head contact tolerance, added to the crate collision skin.")]
        private float contactTolerance = 0.02f;
        [SerializeField, Min(0f), Tooltip("Seconds continuously inside or outside water before disabling or enabling crushing damage. Returning to the previous medium cancels the pending change.")]
        private float waterTransitionDelay = 2f;
        private bool waterProtectionActive;
        private float waterTransitionElapsed;
        private Bounds previousBounds;
        private float previousFallSpeed;
        private bool triggered;
        private bool completed;
        private Coroutine animationRoutine;

        private void Awake()
        {
            if (motion == null) motion = GetComponent<PropsMotion>();
            if (testEnd == null) testEnd = GetComponent<TestEndEvent>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            previousBounds = motion.Body.Shape.bounds;
            waterProtectionActive = WaterVolume.SubmergedFraction(previousBounds) > 0f;
            waterTransitionElapsed = 0f;
            previousFallSpeed = 0f;
            triggered = false;
            completed = false;
            GameAudioSettings.EffectsVolumeChanged += ApplyEffectsVolume;
            ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
        }

        private void ApplyEffectsVolume(float volume) => audioSource.volume = impactVolume * volume;

        private void FixedUpdate()
        {
            Bounds currentBounds = motion.Body.Shape.bounds;
            // Check the final bounds: the prop may have entered water during this physics step.
            bool inWater = WaterVolume.SubmergedFraction(currentBounds) > 0f;
            UpdateWaterProtection(inWater);
            float fallSpeed = Mathf.Max(0f, -motion.FrameVelocity.y);
            GameManager manager = GameManager.Instance;
            if (!triggered && !waterProtectionActive && motion.isActiveAndEnabled && motion.Body.Shape.enabled
                && manager != null && manager.State == GameManager.LoopState.Playing
                && Mathf.Max(previousFallSpeed, fallSpeed) >= minimumFallSpeed
                && HitsHead(previousBounds, currentBounds, manager.PlayerCollider)
                && testEnd.BeginAnimatedEnd())
            {
                triggered = true;
                if (impactClip != null)
                {
                    ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
                    audioSource.PlayOneShot(impactClip);
                }
                manager.Hud.PlayDamageFlash();
                animationRoutine = StartCoroutine(PlayCrush(manager.PlayerCrushVisual));
            }
            previousBounds = currentBounds;
            previousFallSpeed = waterProtectionActive ? 0f : fallSpeed;
        }

        private void UpdateWaterProtection(bool inWater)
        {
            if (inWater == waterProtectionActive)
            {
                waterTransitionElapsed = 0f;
                return;
            }
            waterTransitionElapsed += Time.fixedDeltaTime;
            if (waterTransitionElapsed < waterTransitionDelay) return;
            waterProtectionActive = inWater;
            waterTransitionElapsed = 0f;
        }

        private IEnumerator PlayCrush(PlayerCrushVisual crushVisual)
        {
            if (crushVisual != null) yield return crushVisual.Play(FollowCrushedHead);
            else Debug.LogError("Assign Player Crush Visual on the Game Manager and the Player prefab to animate crushing.", this);
            completed = true;
            animationRoutine = null;
            testEnd.CompleteAnimatedEnd();
        }

        private void FollowCrushedHead(float headHeight)
        {
            float descent = motion.Body.Shape.bounds.min.y - headHeight - motion.Body.Skin;
            if (descent <= 0f) return;
            // The game is paused during death, so follow the collapsing collider with a solid cast.
            Vector2 travel = motion.Body.AllowedDisplacement(Vector2.down * descent);
            transform.position += (Vector3)travel;
            Physics2D.SyncTransforms();
        }

        private bool HitsHead(Bounds before, Bounds after, Collider2D player)
        {
            if (player == null || !player.enabled || !player.gameObject.activeInHierarchy) return false;
            float descent = before.min.y - after.min.y;
            if (descent <= 0.00001f) return false;
            Bounds head = player.bounds;
            float tolerance = motion.Body.Skin + contactTolerance;
            if (before.min.y < head.max.y - tolerance || after.min.y > head.max.y + tolerance) return false;

            // Check the crate's swept position at head height, including casts stopping at the skin gap.
            float impact = Mathf.Clamp01((before.min.y - head.max.y) / descent);
            float left = Mathf.Lerp(before.min.x, after.min.x, impact);
            float right = Mathf.Lerp(before.max.x, after.max.x, impact);
            return Mathf.Min(right, head.max.x) - Mathf.Max(left, head.min.x) > 0.01f;
        }

        private void OnDisable()
        {
            GameAudioSettings.EffectsVolumeChanged -= ApplyEffectsVolume;
            if (audioSource != null) audioSource.Stop();
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            if (triggered && !completed && testEnd != null)
            {
                completed = true;
                testEnd.CompleteAnimatedEnd();
            }
        }
    }
}
