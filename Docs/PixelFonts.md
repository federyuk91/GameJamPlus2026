# Additional pixel fonts

Imported on 23 October 2026 into the existing `Assets/Art/UI/Gameplay/Fonts` folder. No new folders were created. Existing fonts and UI assignments were preserved.

| Font | Imported files | Source and usage terms |
| --- | --- | --- |
| m5x7 | `m5x7.ttf` | [Daniel Linssen's page](https://managore.itch.io/m5x7) lists CC0. Free use, including commercial projects; attribution appreciated. |
| m6x11 | `m6x11.ttf` | [Daniel Linssen's page](https://managore.itch.io/m6x11) permits free use with attribution. Credit the author when using the font. No standard license is named on the page; it is not treated as CC0. |
| Pixelify Sans | Regular, Medium, SemiBold and Bold static TTFs | [Author's repository](https://github.com/eifetx/Pixelify-Sans), SIL Open Font License 1.1. Commercial embedding is permitted subject to the OFL conditions. |

Static Pixelify Sans files were downloaded directly from the author's `fonts/ttf` directory without modification. Static files avoid requiring variable-font weight support. m6x11plus was not imported because the requested font was m6x11.

## Notices and distribution

The font folder contains `m5x7-License.txt`, `m6x11-Permission.txt`, the original `PixelifySans-OFL.txt`, and `FontCredits.txt`. The existing `OFL.txt` belongs to Silkscreen and was not replaced.

When using m6x11, include: **m6x11 font by Daniel Linssen — https://managore.itch.io/m6x11**.

For Pixelify Sans, retain its copyright notice and full OFL with distributed copies. Do not sell the font by itself. Redistributed font software remains under OFL; the game's own code and artwork do not become OFL. Follow the original license for modifications and any reserved-name conditions. Merely storing notices in Assets does not guarantee they are exposed in a built player: include readable notices in the release package or a suitable acknowledgements view before shipping with these fonts.

## Using the assets

The TTFs are used directly through UI Toolkit USS references; no TextCore/TMP font atlas was generated.

- m5x7: communication and avatar terminals (including SYSTEM hints), per-test result notebook, EndGame report/ending, and EndGameCredits (including the final thank-you message).
- m6x11: MainMenu and its options/credits, pause menu, and shared controls/binding pages.
- Other HUD labels retain their existing font. The MainMenu thanks-logo row is four units shorter to reserve space for the font attribution.

Communication text is now 14px (previously 10px), with SYSTEM hint descriptions at 13px (previously 9px). The expanded scientist ending message is 22px (previously 16px). EndGame and final-credit font sizes are six units larger than their previous values; the font-attribution row has six additional units of height. Menu and unrelated HUD text sizes are unchanged.

The main menu credits and final credits now acknowledge Daniel Linssen for both fonts, covering the required attribution for m6x11. FontCredits.txt remains alongside the imported files.

Font rules explicitly cover each selected section and its descendants, and clear `-unity-font-definition` to `none`. UI Toolkit gives an inherited TextCore font asset precedence over the legacy `-unity-font` property; setting only the latter does not reliably replace the theme font. Controls created dynamically under the selected sections inherit the same rules.

The author recommends sizes 16, 32, 48, etc. for both m5x7 and m6x11. Check readability, pixel alignment, line height and required language glyphs in the actual UI before switching fonts.

Downloaded font headers and embedded copyright/license records were inspected. No build, compilation or Play Mode checks were run.
