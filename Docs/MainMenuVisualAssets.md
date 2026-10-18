# Main menu visual assets — 18 October 2026

Reference supplied by the user: `C:/Users/feder/Desktop/Immagine ChatGPT 18 ott 2026, 10_41_06.png`.

Mode: built-in `image_gen`, reference-image editing, three separate outputs. The reference was used for visual identity; outputs are generated derivatives rather than exact pixel crops. The title retains transparency. Menu chrome and all interface text are authored in UXML/USS, including the English tagline **A home for every mouse**.

## Prompt set

- **Title logo:** Extract/recreate only the UNIVERSE-10 title logo from the supplied reference. Preserve the tall condensed navy lettering, subtle aged print grain, thin brick-red orbital swoosh and red dot. Transparent background. No welcome heading, tagline, illustration, buttons or other text. Wide composition with the complete orbit visible.
- **Welcome room illustration:** Extract/recreate the right-hand mouse-in-laboratory illustration as a standalone rectangular image. Preserve the friendly cream mouse, enclosure, muted sage/navy/cream palette and vintage printed-paper grain. Remove monitor frame, header, status footer and surrounding UI. Change the guest placard to exactly GUEST 010. No Italian text or additional captions.
- **Paper background:** Create a seamless-looking full-bleed cream aged-paper texture matching the supplied menu reference. Remove every title, illustration, button, border and text. Fine warm paper grain with subtle variations; quiet enough to support legible UI text.

## Saved assets

All paths below are relative to `Assets/Art/UI/MainMenu`:

| Asset | Purpose |
| --- | --- |
| `Branding/Universe10_TitleLogo.png` | Transparent title and orbit |
| `Illustrations/WelcomeRoom_Mouse.png` | Right-hand artwork |
| `Backgrounds/WelcomeProgram_Paper.png` | Paper background and dialog surfaces |

Textures use bilinear filtering, no mipmaps and no texture compression for the illustrated menu. These settings apply only to these menu assets, not to gameplay pixel art.

## Typeface

Cardo Regular and Bold from the [official Google Fonts repository](https://github.com/google/fonts/tree/main/ofl/cardo), distributed under the SIL Open Font License. The original license is included at `Assets/Art/UI/MainMenu/Fonts/OFL.txt`.

## Unity review

No Play Mode or automated tests were run, following the project rule. Review the MainMenu scene at 640 × 360, controller focus, Options/Credits/quit dialogs, and scaling on the target display. The existing 20 px black bars and controller event wiring are retained.
