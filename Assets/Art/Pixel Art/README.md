# Pixel Art

Le texture in questa cartella e nelle sue sottocartelle vengono importate automaticamente come sprite con filtro Point (no filter), 32 pixel per unità, mipmap disabilitate e dimensioni originali non ridimensionate a potenze di due.

- Immagini quadrate fino a 64 × 64 pixel inclusi: Sprite Mode Single.
- Immagini non quadrate oppure con larghezza o altezza superiore a 64 pixel: Sprite Mode Multiple.

Le regole vengono applicate a ogni importazione, reimportazione e spostamento di immagini o cartelle in questa gerarchia. Gli sprite sheet vengono impostati su Multiple; il ritaglio dei frame rimane gestibile nello Sprite Editor, senza imporre una griglia o sostituire i ritagli esistenti.

L'importatore si trova in `Assets/Editor/PixelArtTexturePostprocessor.cs`.
