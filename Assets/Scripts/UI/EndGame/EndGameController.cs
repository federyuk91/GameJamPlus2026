using System.Collections;
using GameJamPlus.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class EndGameController : MonoBehaviour
    {
        private enum ScreenState { OpeningReport, Report, ClosingReport, Ending, Inactive }

        [SerializeField] private UIDocument document;
        [SerializeField] private Texture2D noDeathsEndingImage;
        [SerializeField] private Texture2D partialLossEndingImage;
        [SerializeField] private Texture2D allSubjectsLostEndingImage;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField, Range(0, 10)] private int previewDeceasedSubjects = 5;

        private VisualElement root;
        private VisualElement monitor;
        private VisualElement reportContent;
        private VisualElement glow;
        private VisualElement endingView;
        private Button continueButton;
        private Button menuButton;
        private ScreenState state = ScreenState.Inactive;
        private int deceasedSubjects;
        private int recoveredSubjects;

        private void OnEnable()
        {
            if (document == null)
            {
                Debug.LogError("Assegna UIDocument a EndGameController.", this);
                return;
            }
            root = document.rootVisualElement;
            monitor = root.Q("final-report-monitor");
            reportContent = root.Q("final-report-content");
            glow = root.Q("report-power-glow");
            endingView = root.Q("ending-view");
            continueButton = root.Q<Button>("report-continue");
            menuButton = root.Q<Button>("return-to-menu");
            continueButton.clicked += Continue;
            menuButton.clicked += ReturnToMenu;
            endingView.AddToClassList("hidden");
            monitor.style.display = DisplayStyle.Flex;
            BindReport();
            StartCoroutine(OpenReport());
        }

        private void BindReport()
        {
            bool hasSessionResults = GameSession.CompletedTests > 0;
            int total = hasSessionResults ? GameSession.TotalTests : 10;
            int completed = hasSessionResults ? GameSession.CompletedTests : 10;
            int used = hasSessionResults ? GameSession.SubjectsUsed : 10;
            deceasedSubjects = hasSessionResults ? GameSession.DeceasedSubjects : previewDeceasedSubjects;
            recoveredSubjects = hasSessionResults ? GameSession.RecoveredSubjects : used - deceasedSubjects;
            root.Q<Label>("completed-tests").text = $"{completed}/{total}";
            root.Q<Label>("subjects-used").text = used.ToString();
            root.Q<Label>("subjects-deceased").text = deceasedSubjects.ToString();
            root.Q<Label>("subjects-recovered").text = recoveredSubjects.ToString();
            int successPercent = total > 0 ? Mathf.RoundToInt(100f * completed / total) : 0;
            root.Q<Label>("experiment-outcome").text = $"ESITO: {successPercent}% SUCCESSO";
        }

        private IEnumerator OpenReport()
        {
            state = ScreenState.OpeningReport;
            continueButton.SetEnabled(false);
            reportContent.style.opacity = 0f;
            glow.style.opacity = 1f;
            SetMonitorScale(0.005f, 0.008f);
            yield return AnimateMonitor(0.005f, 1f, 0.008f, 0.008f, 0.12f);
            yield return AnimateMonitor(1f, 1f, 0.008f, 1f, 0.2f);
            float elapsed = 0f;
            while (elapsed < 0.14f)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / 0.14f);
                glow.style.opacity = 1f - progress;
                reportContent.style.opacity = progress;
                yield return null;
            }
            glow.style.opacity = 0f;
            reportContent.style.opacity = 1f;
            state = ScreenState.Report;
            continueButton.SetEnabled(true);
            continueButton.Focus();
        }

        public void Continue()
        {
            if (state != ScreenState.Report) return;
            state = ScreenState.ClosingReport;
            continueButton.SetEnabled(false);
            StartCoroutine(CloseReportAndShowEnding());
        }

        private IEnumerator CloseReportAndShowEnding()
        {
            reportContent.style.opacity = 0f;
            glow.style.opacity = 1f;
            yield return AnimateMonitor(1f, 1f, 1f, 0.008f, 0.16f);
            yield return AnimateMonitor(1f, 0.005f, 0.008f, 0.008f, 0.12f);
            monitor.style.display = DisplayStyle.None;
            ShowEnding();
        }

        private void ShowEnding()
        {
            Texture2D image;
            string title;
            switch (EndGameOutcomeSelector.Select(deceasedSubjects))
            {
                case EndGameOutcome.NoDeaths:
                    image = noDeathsEndingImage;
                    title = "NESSUN SOGGETTO PERDUTO";
                    break;
                case EndGameOutcome.PartialLoss:
                    image = partialLossEndingImage;
                    title = "IL COSTO DELL'ESPERIMENTO";
                    break;
                default:
                    image = allSubjectsLostEndingImage;
                    title = "NESSUN SOPRAVVISSUTO";
                    break;
            }
            Image endingImage = root.Q<Image>("ending-image");
            endingImage.image = image;
            endingImage.scaleMode = ScaleMode.ScaleToFit;
            root.Q<Label>("ending-title").text = title;
            root.Q<Label>("ending-description").text =
                $"Soggetti deceduti: {deceasedSubjects} · Soggetti recuperati: {recoveredSubjects}";
            endingView.RemoveFromClassList("hidden");
            state = ScreenState.Ending;
            menuButton.Focus();
        }

        private void ReturnToMenu()
        {
            if (state != ScreenState.Ending) return;
            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError("Includi la scena del menu nei Build Profiles: " + mainMenuSceneName, this);
                return;
            }
            state = ScreenState.Inactive;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private IEnumerator AnimateMonitor(float fromX, float toX, float fromY, float toY, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetMonitorScale(Mathf.Lerp(fromX, toX, progress), Mathf.Lerp(fromY, toY, progress));
                yield return null;
            }
            SetMonitorScale(toX, toY);
        }

        private void SetMonitorScale(float x, float y)
        {
            monitor.style.scale = new Scale(new Vector3(x, y, 1f));
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (continueButton != null) continueButton.clicked -= Continue;
            if (menuButton != null) menuButton.clicked -= ReturnToMenu;
            state = ScreenState.Inactive;
        }
    }
}
