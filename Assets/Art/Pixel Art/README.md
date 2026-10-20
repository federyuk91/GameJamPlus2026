# Pixel Art

Textures in this folder and its subfolders are automatically imported as sprites with Point filtering, 32 pixels per unit, no mipmaps and original dimensions without power-of-two resizing.

- Square images up to and including 64×64 pixels: Sprite Mode Single.
- Non-square images or images wider/taller than 64 pixels: Sprite Mode Multiple.

Rules apply on import, reimport and image/folder moves within this hierarchy. Sprite sheets use Multiple; frame slicing remains editable in Sprite Editor without imposing a grid or replacing existing slices.

Importer: `Assets/Editor/Importing/PixelArtTexturePostprocessor.cs`.
