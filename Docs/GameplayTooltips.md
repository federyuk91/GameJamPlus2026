# Gameplay tooltip text

Edit Assets/Settings/GameplayTooltipSettings.asset in the Inspector and expand Texts. Every field has an Inspector tooltip describing when that message is displayed. The asset is assigned on Gameplay HUD.prefab and the standalone HUD in Parallax Test; nested HUD instances inherit it.

This catalog contains the twelve interaction/spawn descriptions rendered by GameplayHudController. Keyboard/gamepad icons and bindings still come from InputDevicePrompts, and availability/priority still comes from GameplayInteractionAvailability. Camera status messages and control-page descriptions remain separate.

Create another catalog with Create > Game > UI > Gameplay Tooltip Settings and assign it to GameplayHudController > Tooltip Settings if a HUD needs different text. Older HUD copies without an assigned catalog use the same defaults declared in GameplayTooltipText, with no resource lookup or scene search. Changed strings appear when the hint panel next refreshes.

## Player preference

Options in both the main menu and pause menu include Hide tooltip, disabled by default. GameUISettings stores the preference in PlayerPrefs Settings.HideTooltip and notifies the HUD to refresh. It hides interaction suggestions, including their key icons, without changing interaction availability or inputs. The mandatory spawn-start prompt remains visible.
