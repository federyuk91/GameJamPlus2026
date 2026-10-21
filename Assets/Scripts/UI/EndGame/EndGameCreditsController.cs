using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument), typeof(AudioSource))]
    public sealed class EndGameCreditsController : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private AudioSource typingAudio;
        [SerializeField] private AudioClip keyPressClip;
        [SerializeField] private AudioSource ambienceAudio;
        [SerializeField] private AudioClip ambienceClip;
        [SerializeField] private AudioSource televisionAudio;
        [SerializeField] private AudioClip switchOffClip;
        [SerializeField, Min(0f), Tooltip("Seconds showing the unlit screen before the CRT begins opening.")]
        private float startupDelay = 2f;
        [SerializeField, Min(0f), Tooltip("Seconds after the logo fade-in before automatic CRT shutdown. Main Menu skips this wait.")]
        private float shutdownDelay = 5f;
        [SerializeField, Min(0.01f), Tooltip("Seconds for the horizontal light line to expand across the screen.")]
        private float lineOpeningDuration = 0.45f;
        [SerializeField, Min(0.01f), Tooltip("Seconds for the CRT aperture to expand from a thin line to full screen.")]
        private float screenOpeningDuration = 0.85f;
        [SerializeField, Range(0f, 1f), Tooltip("Background piano level before the shared Music and Master volume settings.")]
        private float ambienceVolume = 0.8f;
        [SerializeField, Range(0f, 1f), Tooltip("Television shutdown level before the shared Effects and Master volume settings.")]
        private float switchOffVolume = 0.6f;
        [SerializeField, Min(1f), Tooltip("Characters revealed per second, with one keystroke sound per printed character.")]
        private float charactersPerSecond = 30f;
        [SerializeField, Min(0f), Tooltip("Seconds fading in the Special Thanks logos after the text is revealed.")]
        private float logoFadeDuration = 1.2f;
        [SerializeField, Min(0f), Tooltip("Seconds fading in the central Unity logo after the monitor shuts down.")]
        private float unityLogoFadeDuration = 1.5f;
        [SerializeField, Min(0f), Tooltip("Seconds holding the Unity logo and completed thank-you message before returning to Main Menu.")]
        private float unityLogoHoldDuration = 1.5f;
        [SerializeField, Min(0f), Tooltip("Seconds after the Unity logo finishes appearing before the thank-you message begins fading in.")]
        private float thankYouDelay = 0.5f;
        [SerializeField, Min(0f), Tooltip("Seconds fading in the thank-you message below the Unity logo.")]
        private float thankYouFadeDuration = 1f;
        [SerializeField, Min(0.05f), Tooltip("Seconds between visible and hidden states of the terminal underscore cursor.")]
        private float cursorBlinkInterval = 0.5f;
        [SerializeField, Range(0f, 1f), Tooltip("Keystroke volume before the shared Effects and Master volume settings.")]
        private float typingVolume = 0.25f;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private readonly List<Label> labels = new List<Label>();
        private readonly List<string> texts = new List<string>();
        private VisualElement monitor;
        private VisualElement content;
        private VisualElement glow;
        private VisualElement powerLed;
        private VisualElement logos;
        private VisualElement unityLogo;
        private Label thankYouMessage;
        private Label transmissionCursor;
        private Button menuButton;
        private InputAction advance;
        private InputAction back;
        private bool opening;
        private bool complete;
        private bool revealRequested;
        private bool leaving;
        private int revealedFrame = -1;

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (typingAudio == null) typingAudio = GetComponent<AudioSource>();
            typingAudio.playOnAwake = false;
            typingAudio.loop = false;
            typingAudio.spatialBlend = 0f;
            if (ambienceAudio != null)
            {
                ambienceAudio.playOnAwake = false;
                ambienceAudio.loop = false;
                ambienceAudio.spatialBlend = 0f;
            }
            if (televisionAudio != null)
            {
                televisionAudio.playOnAwake = false;
                televisionAudio.loop = false;
                televisionAudio.spatialBlend = 0f;
            }
            advance = new InputAction("Advance final screen", InputActionType.Button);
            advance.AddBinding("<Keyboard>/space");
            advance.AddBinding("<Keyboard>/enter");
            advance.AddBinding("<Gamepad>/buttonSouth");
            back = new InputAction("Back from final screen", InputActionType.Button);
            back.AddBinding("<Keyboard>/escape");
            back.AddBinding("<Gamepad>/buttonEast");
            advance.performed += OnAdvance;
            back.performed += OnAdvance;
        }

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            monitor = root.Q("final-monitor");
            content = root.Q("monitor-content");
            glow = root.Q("power-glow");
            powerLed = root.Q("power-led");
            powerLed.RemoveFromClassList("is-powered");
            logos = root.Q("thanks-logos");
            unityLogo = root.Q("unity-logo");
            thankYouMessage = root.Q<Label>("thank-you-message");
            thankYouMessage.style.opacity = 0f;
            transmissionCursor = root.Q<Label>("transmission-cursor");
            transmissionCursor.style.opacity = 1f;
            menuButton = root.Q<Button>("main-menu");
            labels.Clear();
            texts.Clear();
            root.Query<Label>(className: "typed").ForEach(label =>
            {
                labels.Add(label);
                texts.Add(label.text);
                label.text = string.Empty;
            });
            opening = true;
            complete = revealRequested = leaving = false;
            content.style.opacity = 0f;
            monitor.style.scale = new Scale(new Vector3(0.004f, 0.006f, 1f));
            monitor.style.opacity = 0f;
            glow.style.opacity = 0f;
            logos.style.visibility = Visibility.Hidden;
            unityLogo.style.visibility = Visibility.Hidden;
            logos.style.opacity = 0f;
            unityLogo.style.opacity = 0f;
            menuButton.style.visibility = Visibility.Hidden;
            menuButton.clicked += ReturnToMenu;
            GameAudioSettings.EffectsVolumeChanged += ApplyVolume;
            GameAudioSettings.MusicVolumeChanged += ApplyMusicVolume;
            ApplyVolume(GameAudioSettings.EffectsVolume);
            ApplyMusicVolume(GameAudioSettings.MusicVolume);
            advance.Enable();
            back.Enable();
            StartCoroutine(OpenAndType());
        }

        private void ApplyVolume(float volume)
        {
            typingAudio.volume = typingVolume * volume;
            if (televisionAudio != null) televisionAudio.volume = switchOffVolume * volume;
        }

        private void ApplyMusicVolume(float volume)
        {
            if (ambienceAudio != null) ambienceAudio.volume = ambienceVolume * volume;
        }

        private void PlayAmbience()
        {
            if (ambienceAudio == null || ambienceClip == null) return;
            ApplyMusicVolume(GameAudioSettings.MusicVolume);
            ambienceAudio.clip = ambienceClip;
            ambienceAudio.Play();
        }

        private void OnAdvance(InputAction.CallbackContext context)
        {
            if (opening) { revealRequested = true; return; }
            if (!complete) RevealAll();
            else ReturnToMenu();
        }

        private IEnumerator OpenAndType()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, startupDelay));
            if (televisionAudio != null && switchOffClip != null) televisionAudio.PlayOneShot(switchOffClip);
            monitor.style.opacity = 1f;
            powerLed.AddToClassList("is-powered");
            glow.style.opacity = 0.55f;
            yield return AnimateMonitor(0.004f, 1f, 0.006f, 0.006f, lineOpeningDuration);
            yield return AnimateMonitor(1f, 1f, 0.006f, 1f, screenOpeningDuration);
            PlayAmbience();
            float fade = 0f;
            while (fade < 0.4f)
            {
                fade += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(fade / 0.4f);
                glow.style.opacity = 0.55f * (1f - progress);
                content.style.opacity = progress;
                yield return null;
            }
            glow.style.opacity = 0f;
            content.style.opacity = 1f;
            opening = false;
            if (revealRequested) { RevealAll(); yield break; }

            for (int row = 0; row < labels.Count && !complete; row++)
            {
                string text = texts[row];
                for (int character = 0; character < text.Length && !complete; character++)
                {
                    labels[row].text = text.Substring(0, character + 1);
                    if (!char.IsControl(text[character]) && keyPressClip != null)
                        typingAudio.PlayOneShot(keyPressClip);
                    yield return new WaitForSecondsRealtime(1f / Mathf.Max(1f, charactersPerSecond));
                }
            }
            if (!complete) RevealAll();
        }

        private void RevealAll()
        {
            complete = true;
            revealedFrame = Time.frameCount;
            for (int index = 0; index < labels.Count; index++) labels[index].text = texts[index];
            StartCoroutine(BlinkTransmissionCursor());
            logos.style.visibility = Visibility.Visible;
            StartCoroutine(FadeLogosIn());
            menuButton.style.visibility = Visibility.Visible;
            menuButton.Focus();
        }

        private IEnumerator BlinkTransmissionCursor()
        {
            bool visible = true;
            while (!leaving)
            {
                transmissionCursor.style.opacity = visible ? 1f : 0f;
                yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, cursorBlinkInterval));
                visible = !visible;
            }
        }

        private IEnumerator FadeLogosIn()
        {
            float elapsed = 0f;
            while (elapsed < logoFadeDuration)
            {
                float opacity = Mathf.SmoothStep(0f, 1f, elapsed / logoFadeDuration);
                logos.style.opacity = opacity;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            logos.style.opacity = 1f;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, shutdownDelay));
            if (!leaving) ReturnToMenu();
        }

        public void ReturnToMenu()
        {
            if (!complete || leaving || Time.frameCount == revealedFrame) return;
            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError("Include the menu scene in Build Profiles: " + mainMenuSceneName, this);
                return;
            }
            leaving = true;
            StartCoroutine(CloseAndReturn());
        }

        private IEnumerator CloseAndReturn()
        {
            menuButton.SetEnabled(false);
            powerLed.RemoveFromClassList("is-powered");
            if (televisionAudio != null && switchOffClip != null) televisionAudio.PlayOneShot(switchOffClip);
            content.style.opacity = 0f;
            glow.style.opacity = 0.55f;
            yield return AnimateMonitor(1f, 1f, 1f, 0.006f, 0.35f);
            yield return AnimateMonitor(1f, 0.004f, 0.006f, 0.006f, 0.25f);
            monitor.style.opacity = 0f;
            while (televisionAudio != null && televisionAudio.isPlaying) yield return null;
            unityLogo.style.visibility = Visibility.Visible;
            float elapsed = 0f;
            while (elapsed < unityLogoFadeDuration)
            {
                unityLogo.style.opacity = Mathf.SmoothStep(0f, 1f, elapsed / unityLogoFadeDuration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            unityLogo.style.opacity = 1f;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, thankYouDelay));
            elapsed = 0f;
            while (elapsed < thankYouFadeDuration)
            {
                thankYouMessage.style.opacity = Mathf.SmoothStep(0f, 1f, elapsed / thankYouFadeDuration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            thankYouMessage.style.opacity = 1f;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, unityLogoHoldDuration));
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private IEnumerator AnimateMonitor(float fromX, float toX, float fromY, float toY, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                monitor.style.scale = new Scale(new Vector3(Mathf.Lerp(fromX, toX, progress),
                    Mathf.Lerp(fromY, toY, progress), 1f));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            monitor.style.scale = new Scale(new Vector3(toX, toY, 1f));
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            advance.Disable();
            back.Disable();
            GameAudioSettings.EffectsVolumeChanged -= ApplyVolume;
            GameAudioSettings.MusicVolumeChanged -= ApplyMusicVolume;
            if (menuButton != null) menuButton.clicked -= ReturnToMenu;
            typingAudio.Stop();
            if (ambienceAudio != null) ambienceAudio.Stop();
            if (televisionAudio != null) televisionAudio.Stop();
            for (int index = 0; index < labels.Count; index++) labels[index].text = texts[index];
        }

        private void OnDestroy()
        {
            advance.performed -= OnAdvance;
            back.performed -= OnAdvance;
            advance.Dispose();
            back.Dispose();
        }
    }
}
