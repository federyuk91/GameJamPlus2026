using System;
using GameJamPlus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

namespace GameJamPlus.Editor
{
    public static class MainMenuValidation
    {
        private static double nextCheck;
        private static int stage;
        private static Gamepad gamepad;
        private static VisualElement root;

        // Run with -batchmode -executeMethod GameJamPlus.Editor.MainMenuValidation.Run.
        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
                UIDocument document = UnityEngine.Object.FindFirstObjectByType<UIDocument>();
                Require(document != null && document.visualTreeAsset != null, "UIDocument/UXML reference");
                Require(document.GetComponent<MainMenuController>() != null, "Menu controller");
                InputSystemUIInputModule input = UnityEngine.Object.FindFirstObjectByType<InputSystemUIInputModule>();
                Require(input != null && input.move.action != null && input.submit.action != null && input.cancel.action != null, "UI input actions");
                TemplateContainer tree = document.visualTreeAsset.CloneTree();
                foreach (string name in new[] { "new-game", "options", "credits", "quit", "options-back", "credits-back", "quit-back", "quit-confirm" })
                    Require(tree.Q<Button>(name) != null, "Button " + name);

                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.update += CheckPlayMode;
                nextCheck = EditorApplication.timeSinceStartup + 3;
                EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void CheckPlayMode()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextCheck)
                return;

            nextCheck = EditorApplication.timeSinceStartup + 0.15;
            try
            {
                switch (stage++)
                {
                    case 0:
                        root = UnityEngine.Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
                        Require(root.focusController.focusedElement == root.Q<Button>("new-game"), "Initial focus");
                        gamepad = InputSystem.AddDevice<Gamepad>();
                        Press(GamepadButton.DpadDown);
                        break;
                    case 1:
                        Require(root.focusController.focusedElement == root.Q<Button>("options"), "Gamepad D-pad navigation");
                        Release();
                        break;
                    case 2:
                        Press(GamepadButton.South);
                        break;
                    case 3:
                        Require(!root.Q("options-panel").ClassListContains("hidden"), "Gamepad submit opens options");
                        Release();
                        break;
                    case 4:
                        Press(GamepadButton.East);
                        break;
                    case 5:
                        Require(root.Q("modal").ClassListContains("hidden"), "Gamepad cancel closes modal");
                        Require(root.focusController.focusedElement == root.Q<Button>("options"), "Restore focus");
                        Release();
                        break;
                    case 6:
                        root.Q<Button>("credits").Focus();
                        using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
                        {
                            submit.target = root.Q<Button>("credits");
                            root.Q<Button>("credits").SendEvent(submit);
                        }
                        Require(!root.Q("credits-panel").ClassListContains("hidden"), "Credits panel");
                        Debug.Log("MAIN MENU VALIDATION PASSED: scene, UXML, input, initial focus, gamepad navigation/submit/cancel, focus restoration, credits.");
                        InputSystem.RemoveDevice(gamepad);
                        EditorApplication.update -= CheckPlayMode;
                        EditorApplication.Exit(0);
                        break;
                }
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void Press(GamepadButton button)
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(button));
        }

        private static void Release()
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
        }

        private static void Require(bool condition, string description)
        {
            if (!condition)
                throw new InvalidOperationException("Main menu validation failed: " + description);
        }

        private static void Fail(Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.update -= CheckPlayMode;
            EditorApplication.Exit(1);
        }
    }
}
