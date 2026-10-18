# Loop HUD visual assets

Source: user attachment `C:/Users/feder/AppData/Local/Temp/codex-clipboard-d16df1c8-8e60-4a3a-809f-1af39e2362f5.png`.

Built-in imagegen editing generated two separate blank derivatives of the reference. They are reconstructions, not identical pixel crops. Transparent margins were trimmed and exports normalized with nearest-neighbor resampling to the logical panel dimensions. The original generated files remain under Codex generated_images.

## Prompt set

1. **Communication terminal background:** Extract/recreate only the wide bottom-right terminal casing. Very dark navy empty display, thin warm cream/gold clipped-corner double border, pale cyan outline and subtle CRT scanlines. Transparent outside corners. Remove all text, avatar, portrait frame, indicator, button and icons. No gameplay. Wide 5:1 composition for a 300 × 60 panel.
2. **Timer frame:** Extract/recreate only the top-left timer frame. Dark navy empty interior, thin cream/gold bevelled outline, short stacked amber bars at both ends. Remove PERMANENZA and numbers. Transparent exterior, no other elements, shadow or glow. Suitable for a 70 × 18 logical panel.

## Files

- `Assets/Art/UI/Gameplay/Frames/CommunicationTerminal_Background.png`: 300 × 60, empty terminal background; also used for the resource dashboard.
- `Assets/Art/UI/Gameplay/Frames/StayTimer_Frame.png`: 70 × 18, empty timer frame.
- `Assets/Art/UI/Gameplay/Fonts/Silkscreen-Regular.ttf`: pixel typeface from the [official Google Fonts repository](https://github.com/google/fonts/tree/main/ofl/silkscreen); original OFL license included alongside the font.

Frame texture import uses Point filtering, no mipmaps, alpha transparency and no compression. Text, portrait, device icons, satiety notches and collected values remain live Unity UI elements, with their existing game bindings. Existing item icons and input icons are reused.

## Layout

Base 640 × 360: upper band 20, visible game area 260, lower HUD band 80. Timer at upper left; resource dashboard at lower left; terminal at lower right. Both lower panels are 60 pixels high and inset 10 from the bottom. Viewport masking and integer scaling use the existing PixelPerfectViewport. Other scenes retain their old symmetric borders unless Bottom Border Pixels is explicitly configured.

No automated tests or Play Mode were run. Manual Unity review remains with the user.
