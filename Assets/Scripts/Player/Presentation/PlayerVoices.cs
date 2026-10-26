using System.Collections;
using GameJamPlus.Gameplay;
using GameJamPlus.UI;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class PlayerVoices : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private CollectibleCollector collector;
        [SerializeField] private PlayerRopeBite ropeBite;
        [SerializeField] private PlayerRopeInteraction ropeInteraction;
        [SerializeField] private PlayerCrateInteraction crateInteraction;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerDigging digging;
        [SerializeField] private PlayerLedgeClimb ledgeClimb;
        [SerializeField] private GameplayHudController hud;
        [SerializeField] private AudioClip cheeseFirstClip;
        [SerializeField] private AudioClip cheeseSecondClip;
        [SerializeField] private AudioClip ropeCutClip;
        [SerializeField] private AudioClip sufferingClip;
        [SerializeField] private AudioClip crateStartClip;
        [SerializeField] private AudioClip diggingClip;
        [SerializeField] private AudioClip climbUpClip;
        [SerializeField] private AudioClip photoClip;
        [SerializeField] private AudioClip ropeJumpClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.75f;
        [SerializeField, Min(0f)] private float cheeseSequenceGap = 0.04f;
        [SerializeField, Min(5f)] private float diggingVoiceMinInterval = 5f;
        [SerializeField, Min(5f)] private float diggingVoiceMaxInterval = 9f;
        private Coroutine cheeseRoutine;
        private float diggingVoiceRemaining;
        private bool photoSubscribed;

        private void Awake()
        {
            if (collector == null) collector = GetComponent<CollectibleCollector>();
            if (ropeBite == null) ropeBite = GetComponent<PlayerRopeBite>();
            if (ropeInteraction == null) ropeInteraction = GetComponent<PlayerRopeInteraction>();
            if (crateInteraction == null) crateInteraction = GetComponent<PlayerCrateInteraction>();
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (digging == null) digging = GetComponent<PlayerDigging>();
            if (ledgeClimb == null) ledgeClimb = GetComponent<PlayerLedgeClimb>();
            ScheduleDiggingVoice();
            if (source == null) return;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            if (collector != null) collector.Collected += OnCollected;
            if (ropeBite != null) ropeBite.RopeCut += OnRopeCut;
            if (ropeInteraction != null) ropeInteraction.JumpedFromRope += OnRopeJumped;
            if (crateInteraction != null) crateInteraction.InteractionStarted += OnCrateStarted;
            if (stats != null) stats.SatietyLost += OnNotchLost;
            if (ledgeClimb != null) ledgeClimb.ClimbStarted += OnClimbStarted;
            GameAudioSettings.EffectsVolumeChanged += ApplyEffectsVolume;
            ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
            SubscribePhoto();
        }

        private void Start() => SubscribePhoto();

        private void SubscribePhoto()
        {
            if (photoSubscribed) return;
            if (hud == null && GameManager.Instance != null) hud = GameManager.Instance.Hud;
            if (hud == null) return;
            hud.PhotoShown += OnPhotoShown;
            photoSubscribed = true;
        }

        private void ApplyEffectsVolume(float effectsVolume)
        {
            if (source != null) source.volume = effectsVolume;
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f || digging == null || !digging.isActiveAndEnabled || !digging.IsDigging
                || source == null || diggingClip == null) return;
            // Count active digging time without resetting the cooldown when digging stops.
            diggingVoiceRemaining -= Time.deltaTime;
            if (diggingVoiceRemaining > 0f || source.isPlaying || cheeseRoutine != null) return;
            Play(diggingClip);
            ScheduleDiggingVoice();
        }

        private void ScheduleDiggingVoice()
        {
            float minimum = Mathf.Max(5f, diggingVoiceMinInterval);
            diggingVoiceRemaining = Random.Range(minimum, Mathf.Max(minimum, diggingVoiceMaxInterval));
        }

        private void OnCollected(Collectible collectible)
        {
            CollectibleType type = collectible.Type;
            if (type != CollectibleType.Cheese || source == null) return;
            if (cheeseRoutine != null) StopCoroutine(cheeseRoutine);
            cheeseRoutine = StartCoroutine(PlayCheeseSequence());
        }

        private IEnumerator PlayCheeseSequence()
        {
            Play(cheeseFirstClip);
            // Real-time spacing keeps the two clips separate even if a UI pauses gameplay.
            float delay = (cheeseFirstClip != null ? cheeseFirstClip.length : 0f) + Mathf.Max(0f, cheeseSequenceGap);
            yield return new WaitForSecondsRealtime(delay);
            Play(cheeseSecondClip);
            cheeseRoutine = null;
        }

        private void OnRopeCut() => Play(ropeCutClip);
        private void OnCrateStarted() => Play(crateStartClip);
        private void OnNotchLost() => Play(sufferingClip);
        private void OnClimbStarted() => Play(climbUpClip);
        private void OnPhotoShown() => Play(photoClip);
        private void OnRopeJumped() => Play(ropeJumpClip);

        private void Play(AudioClip clip)
        {
            if (isActiveAndEnabled && source != null && clip != null) source.PlayOneShot(clip, volume);
        }

        private void OnDisable()
        {
            if (collector != null) collector.Collected -= OnCollected;
            if (ropeBite != null) ropeBite.RopeCut -= OnRopeCut;
            if (ropeInteraction != null) ropeInteraction.JumpedFromRope -= OnRopeJumped;
            if (crateInteraction != null) crateInteraction.InteractionStarted -= OnCrateStarted;
            if (stats != null) stats.SatietyLost -= OnNotchLost;
            if (ledgeClimb != null) ledgeClimb.ClimbStarted -= OnClimbStarted;
            GameAudioSettings.EffectsVolumeChanged -= ApplyEffectsVolume;
            if (photoSubscribed && hud != null) hud.PhotoShown -= OnPhotoShown;
            photoSubscribed = false;
            if (cheeseRoutine != null) StopCoroutine(cheeseRoutine);
            cheeseRoutine = null;
            if (source != null) source.Stop();
        }
    }
}
