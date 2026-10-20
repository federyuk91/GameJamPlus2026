using System.Collections.Generic;
using UnityEngine;
using GameJamPlus.UI;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class PlayerOxygenBubbles : MonoBehaviour
    {
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private MouseFacing facing;
        [SerializeField] private SpriteRenderer playerVisual;
        [SerializeField] private Sprite[] bubbleSprites;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip bubbleClip;
        [SerializeField, Range(0f, 1f)] private float bubbleVolume = 0.6f;
        [SerializeField, Min(0.01f)] private float emissionInterval = 0.12f;
        [SerializeField, Min(0.01f)] private float riseSpeed = 1.5f;
        [SerializeField, Min(0f)] private float wobbleWidth = 0.04f;
        [SerializeField, Range(0f, 1f)] private float mouthHeight = 0.75f;
        [SerializeField, Min(1)] private int pixelsPerUnit = 32;
        private readonly List<Bubble> bubbles = new List<Bubble>();
        private GameObject effectRoot;
        private int remainingEmissions;
        private int spriteIndex;
        private float emissionRemaining;
        private bool burstSoundPlayed;

        private sealed class Bubble
        {
            public SpriteRenderer Visual;
            public Vector2 Origin;
            public float Age;
        }

        private void Awake()
        {
            if (playerCollider == null) playerCollider = GetComponent<Collider2D>();
            if (facing == null) facing = GetComponent<MouseFacing>();
            effectRoot = new GameObject("Oxygen Bubbles FX");
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 0f;
            }
        }

        private void OnEnable()
        {
            GameAudioSettings.EffectsVolumeChanged += ApplyEffectsVolume;
            ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
        }

        private void ApplyEffectsVolume(float effectsVolume)
        {
            if (audioSource != null) audioSource.volume = bubbleVolume * effectsVolume;
        }

        public void PlayBurst()
        {
            if (!isActiveAndEnabled || playerCollider == null || bubbleSprites == null || bubbleSprites.Length == 0) return;
            remainingEmissions = 3;
            spriteIndex = 0;
            emissionRemaining = 0f;
            burstSoundPlayed = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (remainingEmissions > 0)
            {
                emissionRemaining -= dt;
                if (emissionRemaining <= 0f)
                {
                    EmitBubble();
                    remainingEmissions--;
                    emissionRemaining = Mathf.Max(0.01f, emissionInterval);
                }
            }
            foreach (Bubble bubble in bubbles)
            {
                if (!bubble.Visual.enabled) continue;
                bubble.Age += dt;
                Vector2 position = bubble.Origin + new Vector2(
                    Mathf.Sin(bubble.Age * 5f) * wobbleWidth, bubble.Age * riseSpeed);
                // Water containment follows the unrounded position so pixel snapping cannot stall surface detection.
                if (!WaterVolume.ContainsPoint(position))
                {
                    bubble.Visual.enabled = false;
                    continue;
                }
                float ppu = Mathf.Max(1, pixelsPerUnit);
                bubble.Visual.transform.position = new Vector3(Mathf.Round(position.x * ppu) / ppu,
                    Mathf.Round(position.y * ppu) / ppu, bubble.Visual.transform.position.z);
            }
        }

        private void EmitBubble()
        {
            Bounds bounds = playerCollider.bounds;
            bool right = facing == null || facing.FacingRight;
            Vector2 mouth = new Vector2(right ? bounds.max.x : bounds.min.x,
                Mathf.Lerp(bounds.min.y, bounds.max.y, mouthHeight));
            Sprite sprite = bubbleSprites[spriteIndex++ % bubbleSprites.Length];
            if (sprite == null || !WaterVolume.ContainsPoint(mouth)) return;
            if (!burstSoundPlayed)
            {
                burstSoundPlayed = true;
                if (audioSource != null && bubbleClip != null)
                {
                    ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
                    audioSource.clip = bubbleClip;
                    audioSource.Play();
                }
            }
            Bubble available = null;
            foreach (Bubble candidate in bubbles)
            {
                if (candidate.Visual.enabled) continue;
                available = candidate;
                break;
            }
            if (available == null)
            {
                GameObject bubbleObject = new GameObject("Oxygen Bubble");
                bubbleObject.transform.SetParent(effectRoot.transform, false);
                SpriteRenderer visual = bubbleObject.AddComponent<SpriteRenderer>();
                if (playerVisual != null)
                {
                    visual.sharedMaterial = playerVisual.sharedMaterial;
                    visual.sortingLayerID = playerVisual.sortingLayerID;
                    visual.sortingOrder = playerVisual.sortingOrder + 2;
                }
                available = new Bubble { Visual = visual };
                bubbles.Add(available);
            }
            available.Origin = mouth;
            available.Age = 0f;
            available.Visual.sprite = sprite;
            available.Visual.enabled = true;
            available.Visual.transform.position = new Vector3(mouth.x, mouth.y,
                playerVisual != null ? playerVisual.transform.position.z : transform.position.z);
        }

        private void OnDisable()
        {
            GameAudioSettings.EffectsVolumeChanged -= ApplyEffectsVolume;
            if (audioSource != null) audioSource.Stop();
            remainingEmissions = 0;
            foreach (Bubble bubble in bubbles)
                if (bubble.Visual != null) bubble.Visual.enabled = false;
        }

        private void OnDestroy()
        {
            if (effectRoot != null) Destroy(effectRoot);
        }
    }
}
