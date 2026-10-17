# Organizzazione grafica

## Pixel Art

Le immagini di gameplay si trovano in `Assets/Art/Pixel Art`, dove l'importatore applica filtro Point, 32 pixel per unità, mipmap disabilitate e nessun ridimensionamento a potenze di due.

| Cartella | Contenuto |
| --- | --- |
| `Pixel Art/Characters/Mouse` | `Mouse_Idle_Sheet.png` e `Mouse_Run_Sheet.png`, con i ritagli e i riferimenti alle animazioni conservati. |
| `Pixel Art/Props` | `Crate.png`, `PressureButton_Sheet.png` e `WinchDrum_Sheet.png`. |
| `Pixel Art/Prototypes` | `Mouse_Prototype.png`, il precedente sprite provvisorio del player. |

I fogli di animazione terminano con `_Sheet`; i singoli frame hanno nomi come `Mouse_Run_0`, `PressureButton_0` e `WinchDrum_0`. Le immagini quadrate fino a 64×64 inclusi usano Sprite Mode Single; le altre usano Multiple. I ritagli dei fogli si modificano nello Sprite Editor.

## Palette e interfaccia

- `Palettes/GamePalette.png`: palette cromatica condivisa, usata come riferimento per il menu.
- `UI/Branding/GameJamPlus_Icon.png`: icona dell'applicazione, collegata ai Player Settings.
- `UI/MainMenu`: sfondi, branding, icone e illustrazioni del menu, già suddivisi per funzione.

Palette e grafica dell'interfaccia conservano le proprie impostazioni di importazione e rimangono fuori dalla cartella Pixel Art.

Le animazioni del topo restano in `Assets/Animations/Mouse`. Script, scene e settings mantengono i propri percorsi.
