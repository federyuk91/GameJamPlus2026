# Controls UI authoring

This refactor is scoped to the controls screen. Other project UI has not been migrated.

## Editable views

Open these assets directly in Unity UI Builder (outside Play Mode):

- `Assets/UI Toolkit/Gameplay/BodyControlsOverview.uxml`
- `Assets/UI Toolkit/Gameplay/BodyControlsHead.uxml`
- `Assets/UI Toolkit/Gameplay/BodyControlsFrontPaws.uxml`
- `Assets/UI Toolkit/Gameplay/BodyControlsHindPaws.uxml`
- `Assets/UI Toolkit/Gameplay/BodyControlsTail.uxml`
- `Assets/UI Toolkit/Gameplay/BodyControlsBindingDialog.uxml`

Each body page owns its sprite, diagram, indicators and description. No script replaces its image or static description at runtime. Both ControlsScene and the gameplay pause screen instantiate these same templates.

`BodyControls.uxml` contains the shared shell and page references. `ControlsScene.uxml` adds the standalone scene footer. `BodyControls.uss` provides common styles.

For changes limited to one page, use inline styles or its root class, for example `.body-page-FrontPaws`. Editing a common selector changes every page using that class.

## Elements and data

Preserve the names of bound elements. Layout, styling, sprite references, static descriptions, tab labels and dialog copy can be edited in the views.

- `body-image`: sprite background, position and size.
- `body-callout-1`: front paws key indicator. It is an InputPromptElement. Preview Path / Preview Label define its UI Builder preview; its Binding attribute selects the gameplay action.
- `body-anchor-1`: endpoint of the front paws connector, positioned relative to the sprite. Connectors render in Editor and runtime through the presentation-only BodyControlsConnectors element.
- `body-description`: authored text, retained at runtime.
- The Overview text uses `{move}`, `{camera}`, `{cameraLock}`, `{zoom}`, `{pause}`, `{advance}` placeholders.
- Dialog title templates use `{part}` and `{button}`. The presenter replaces tokens; it does not replace the authored wording.

## Responsibilities

`BodyControlsView` coordinates page navigation and rebinding. `BodyControlsPageView` and `BodyControlsBindingView` bind state/data to authored UI. `PlayerControlBindings` persists and applies input assignments. BodyControlsSettings only references the shared layout; sprite assignments now belong to the individual page views.

Existing hover, click/Confirm, directional navigation and Yes/No/Cancel behavior remain in the presenter. ControlsScene and the pause menu keep their own outer navigation.

## New Game overview

The New Game Controls scene now instantiates only BodyControlsOverview.uxml and offers Continue. It does not create the rebinding presenter, tabs, reset button, cancellation action or main-menu button. Continue loads the destination selected by MainMenuController. Gameplay pause still uses the full editable BodyControls.uxml shell and the same page templates.

## Kenney input prompts

`Assets/Resources/InputPrompts.asset` is the shared catalog. Entries map a controller family and Input System control name to an existing Kenney PNG texture. PNGs are used whole, avoiding dependency on the automatic sprite slices of individual icons. No source artwork is modified.

`InputPromptElement` is an atomic UXML widget. Its `binding` attribute selects mouth, front, hind, tail, camera, cameraLock, zoomIn, zoomOut, confirm, cancel, listenCancel, advance, pause, directions, horizontal or vertical. The presenter resolves these against the active device and current PlayerControlBindings. `preview-path`, `preview-label` and `preview-family` allow authoring previews in UI Builder. InputPrompts.uss controls glyph sizes, cream tint and fallback labels.

Keyboard, Xbox, PlayStation and Nintendo Switch have icon mappings. Unknown gamepad layouts use Generic, with N/W/S/E position labels for face buttons. Missing keyboard glyphs use a blank outlined key with the actual character/name overlaid. The resolver uses printed keyboard letters rather than physical QWERTY paths where available. Icons update with both device changes and rebinding.

The shared input widget is used in body callouts, camera combinations, navigation hints, rebinding confirmation and gameplay hints. Gameplay hints use short actions and separate icons for directional combinations. Dialogue and stage-result advance controls use the same widget. New Game remains an Overview-only screen with Continue as its only choice.

## Overview scanner

Overview contains a `body-scanner` child of `body-image`. The authored BodyControlsScanner element uses Scan Sprite for an alpha-preserving inverted-color scan band over the normal background sprite; it does not modify the source PNG, anchors or input prompts. Other body pages do not contain the effect.

Edit the element in UI Builder to change Scan Enabled, Scan Duration (seconds), Pause Duration (seconds), Band Width (fraction of sprite height), Scan Angle (degrees), Pixel Offset (source pixels), Line Width and Line Color. The normal image is authored on body-image; Scan Sprite is assigned on body-scanner. Pixel Offset fades both overlay opacity and color inversion from full strength at the scan edge to zero at its outer edge. Its layout comes from `.body-scanner` in BodyControls.uss. The effect renders in both Editor panels and runtime using real time, including while gameplay is paused. Hidden pages skip rendering, and detaching the element releases its temporary material and render texture.

The shader lives in `Assets/Resources/BodyControlsScanner.shader`, ensuring availability in player builds. It samples the selected sprite rectangle rather than the full sheet. ControlsScene and gameplay pause use the same Overview view.
