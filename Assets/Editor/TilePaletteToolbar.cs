using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.Toolbars;
using UnityEngine;

public static class TilePaletteToolbar
{
    [MainToolbarElement("Mouse Platformer/Tile Palette",
        defaultDockPosition = MainToolbarDockPosition.Left, defaultDockIndex = 101)]
    public static MainToolbarElement CreateButton()
    {
        MainToolbarContent content = new MainToolbarContent("Tile Palette", "Open or close Tile Palette");
        return new MainToolbarButton(content, TogglePalette);
    }

    private static void TogglePalette()
    {
        // Query only on click so closing or docking the window elsewhere stays supported.
        GridPaintPaletteWindow[] windows = Resources.FindObjectsOfTypeAll<GridPaintPaletteWindow>();
        if (windows.Length == 0)
        {
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            return;
        }
        foreach (GridPaintPaletteWindow window in windows)
            window.Close();
    }
}
