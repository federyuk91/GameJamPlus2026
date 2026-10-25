# Door

Assets/Prefabs/Props/Door.prefab is based on the Door placed in Universo_10.
The original sprite is Closed Sprite; Full TileSet_155 is Open Sprite.
Door starts closed by default. Starts Open can change its initial state.

Connect a button's On Pressed event to Door.Open, and On Release to Door.Close.
Both are public parameterless UnityEvent callbacks. SetOpen(bool) is also
available for events that assign an explicit state. Toggle() switches between
open and closed based on the current state. Opening disables the assigned
blocking collider; closing restores it. Original collider geometry is preserved.

Tools/Gameplay/Create Door Prefab from Universe 10 creates and connects the
prefab from the existing scene object. It preserves an open scene's unsaved
changes: if the scene was already dirty, the conversion is left dirty for saving
in the editor. Otherwise the converted scene is saved automatically.
