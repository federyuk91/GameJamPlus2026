using System;
using System.IO;
using System.Collections.Generic;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class WelcomeWaterSetup
{
    private const string ScenePath = "Assets/Scenes/Levels/1_WelcomeToUniverse.unity";
    private const string MaterialPath = "Assets/Materials/PlatformWater.mat";
    private const string PrefabPath = "Assets/Prefabs/Environment/Water Section.prefab";
    private const string Request = "Library/WelcomeWaterV2.request";
    private const string Report = "Library/WelcomeWater-result.txt";
    private const string RootName = "Communicating Water - Welcome";

    static WelcomeWaterSetup() => EditorApplication.update += ProcessRequest;

    private static void ProcessRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { Configure(); }
        catch (Exception exception)
        {
            File.WriteAllText(Report, exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Mouse Platformer/Water/Configure Welcome Communicating Basins")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        Scene previous = SceneManager.GetActiveScene();
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            GameObject existingSetup = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == RootName) existingSetup = root;
            Tilemap terrain = null;
            WaterVolume source = null;
            float nearest = float.PositiveInfinity;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Tilemap candidate in root.GetComponentsInChildren<Tilemap>(true))
                    if (candidate.name == "Terrain - Solid") terrain = candidate;
                foreach (WaterVolume candidate in root.GetComponentsInChildren<WaterVolume>())
                {
                    float distance = ((Vector2)candidate.Shape.bounds.center - new Vector2(-5f, -3f)).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance;
                    source = candidate;
                }
            }
            if (terrain == null || source == null) throw new InvalidOperationException("Welcome terrain or water volume not found.");
            Physics2D.SyncTransforms();
            Vector2 leftSeed = terrain.CellToWorld(new Vector3Int(-2, 5, 0)) + new Vector3(0.5f, 0.5f);
            Vector2 rightSeed = terrain.CellToWorld(new Vector3Int(5, 5, 0)) + new Vector3(0.5f, 0.5f);
            Rect left = ProbeRoom(scene, terrain, leftSeed);
            Rect right = ProbeRoom(scene, terrain, rightSeed);
            if (right.xMin <= left.xMax) throw new InvalidOperationException("The two rooms overlap: " + left + " / " + right);
            Rect passage = ProbePassage(scene, terrain, left, right);
            if (existingSetup != null)
            {
                bool changed = false;
                foreach (WaterBasin basin in existingSetup.GetComponentsInChildren<WaterBasin>())
                    changed |= TrimCeiling(basin, basin.name == "Left Basin" ? left : right);
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save basin ceiling adjustment.");
                }
                File.WriteAllText(Report, "SUCCESS\nExisting setup preserved; ceiling bounds checked.\nLeft bounds: " + left
                    + "\nRight bounds: " + right + "\nRaised passage: " + passage + "\nScene saved: " + changed);
                return;
            }
            Bounds initialBounds = source.Shape.bounds;
            // Preserve an authored left-only fill; the old rectangle spanning both rooms becomes the demonstration fill.
            bool leftOnly = initialBounds.max.x < right.xMin + 0.1f;
            float initialLeftSurface = leftOnly ? Mathf.Clamp(initialBounds.max.y, left.yMin, left.yMax) : left.yMax - 0.15f;
            float initialRightSurface = right.yMin;
            Material waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (waterMaterial == null) throw new InvalidOperationException("PlatformWater material is missing.");
            if (ShaderUtil.ShaderHasError(waterMaterial.shader)) throw new InvalidOperationException("PlatformWater shader has compilation errors.");
            List<Rect> leftSections = BakeSpace(terrain, left);
            leftSections.AddRange(BakeSpace(terrain, passage));
            List<Rect> rightSections = BakeSpace(terrain, right);
            SpriteRenderer sourceVisual = source.GetComponent<SpriteRenderer>();
            Color tint = sourceVisual != null ? sourceVisual.color : new Color(0f, 1f, 1f, 0.2f);
            int sortingOrder = sourceVisual != null ? sourceVisual.sortingOrder : 0;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configure Welcome communicating water");
            GameObject owner = NewObject(RootName, scene, null);
            WaterBasin first = CreateBasin("Left Basin", scene, owner.transform, left);
            WaterBasin second = CreateBasin("Right Basin", scene, owner.transform, right);
            Undo.SetTransformParent(source.transform, first.transform, "Move existing water into left basin");
            Undo.RecordObject(source.gameObject, "Name left water");
            source.name = "Water - Left Room";
            if (sourceVisual != null)
            {
                Undo.RecordObject(sourceVisual, "Assign platform water shader");
                sourceVisual.sharedMaterial = waterMaterial;
            }
            WaterBasin.Section[] firstSections = CreateSections(first, leftSections, source, scene, tint, sortingOrder, waterMaterial);
            WaterBasin.Section[] secondSections = CreateSections(second, rightSections, null, scene, tint, sortingOrder, waterMaterial);
            Undo.RecordObject(first, "Configure left basin");
            first.Configure(firstSections, initialLeftSurface);
            Undo.RecordObject(second, "Configure right basin");
            second.Configure(secondSections, initialRightSurface);
            GameObject connectionOwner = NewObject("Connection - Raised Passage", scene, owner.transform);
            connectionOwner.transform.position = new Vector3(passage.center.x, passage.yMin, 0f);
            WaterConnection connection = Undo.AddComponent<WaterConnection>(connectionOwner);
            connection.Configure(first, second, passage.yMin);
            foreach (WaterBasin.Section section in firstSections) RecordWater(section.water);
            foreach (WaterBasin.Section section in secondSections) RecordWater(section.water);
            first.ResetWater();
            second.ResetWater();
            foreach (WaterBasin.Section section in firstSections) RecordOverrides(section.water);
            foreach (WaterBasin.Section section in secondSections) RecordOverrides(section.water);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Welcome could not be saved.");
            File.WriteAllText(Report, "SUCCESS\nScene saved: " + ScenePath
                + "\nLeft bounds: " + left + "\nRight bounds: " + right + "\nRaised passage: " + passage
                + "\nGeometry sections (1/32-unit sampling): " + firstSections.Length + " / " + secondSections.Length
                + "\nInitial surfaces: " + initialLeftSurface + " / " + initialRightSurface
                + "\nThe separate pool and other gameplay objects were preserved.\nAir is vented; no pressure compression or object displacement simulation.");
            Debug.Log("Welcome communicating water configured. " + Report);
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    private static bool TrimCeiling(WaterBasin basin, Rect room)
    {
        SerializedObject data = new SerializedObject(basin);
        SerializedProperty sections = data.FindProperty("sections");
        bool changed = false;
        for (int index = sections.arraySize - 1; index >= 0; index--)
        {
            SerializedProperty section = sections.GetArrayElementAtIndex(index);
            Rect bounds = section.FindPropertyRelative("bounds").rectValue;
            float worldCenterX = basin.transform.position.x + bounds.center.x;
            if (worldCenterX < room.xMin || worldCenterX > room.xMax) continue;
            float maximum = room.yMax - basin.transform.position.y;
            if (bounds.yMax <= maximum + 0.0001f) continue;
            WaterVolume water = section.FindPropertyRelative("water").objectReferenceValue as WaterVolume;
            if (bounds.yMin >= maximum)
            {
                if (water != null) Undo.DestroyObjectImmediate(water.gameObject);
                sections.DeleteArrayElementAtIndex(index);
            }
            else
            {
                bounds.yMax = maximum;
                section.FindPropertyRelative("bounds").rectValue = bounds;
                if (water != null) RecordWater(water);
            }
            changed = true;
        }
        if (!changed) return false;
        data.ApplyModifiedProperties();
        basin.ResetWater();
        return true;
    }

    private static void RecordWater(WaterVolume volume)
    {
        Undo.RecordObject(volume.transform, "Resize water fill");
        Undo.RecordObject(volume.Shape, "Resize water trigger");
        SpriteRenderer visual = volume.GetComponent<SpriteRenderer>();
        if (visual != null) Undo.RecordObject(visual, "Update water fill");
    }

    private static GameObject NewObject(string name, Scene scene, Transform parent)
    {
        GameObject owner = new GameObject(name);
        SceneManager.MoveGameObjectToScene(owner, scene);
        if (parent != null) owner.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(owner, "Create " + name);
        return owner;
    }

    private static WaterBasin CreateBasin(string name, Scene scene, Transform parent, Rect bounds)
    {
        GameObject owner = NewObject(name, scene, parent);
        owner.transform.position = new Vector3(bounds.center.x, bounds.yMin, 0f);
        return Undo.AddComponent<WaterBasin>(owner);
    }

    private static WaterBasin.Section Section(WaterBasin basin, Rect worldBounds, WaterVolume water)
    {
        Rect local = worldBounds;
        local.position -= (Vector2)basin.transform.position;
        return new WaterBasin.Section { bounds = local, water = water };
    }

    private static WaterVolume CreateWater(string name, Scene scene, Transform parent, Color tint, int sortingOrder, Material material)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject owner = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Undo.RegisterCreatedObjectUndo(owner, "Create " + name);
        owner.name = name;
        owner.transform.SetParent(parent, false);
        SpriteRenderer visual = owner.GetComponent<SpriteRenderer>();
        visual.sharedMaterial = material;
        visual.color = tint;
        visual.sortingOrder = sortingOrder;
        PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
        return owner.GetComponent<WaterVolume>();
    }

    private static void RecordOverrides(WaterVolume water)
    {
        PrefabUtility.RecordPrefabInstancePropertyModifications(water.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(water.Shape);
        SpriteRenderer visual = water.GetComponent<SpriteRenderer>();
        if (visual != null) PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
    }

    private static WaterBasin.Section[] CreateSections(WaterBasin basin, List<Rect> rectangles, WaterVolume existing,
        Scene scene, Color tint, int sortingOrder, Material material)
    {
        WaterBasin.Section[] sections = new WaterBasin.Section[rectangles.Count];
        for (int index = 0; index < rectangles.Count; index++)
        {
            WaterVolume water = index == 0 && existing != null ? existing
                : CreateWater("Water Section " + index, scene, basin.transform, tint, sortingOrder, material);
            sections[index] = Section(basin, rectangles[index], water);
        }
        return sections;
    }

    private static List<Rect> BakeSpace(Tilemap terrain, Rect bounds)
    {
        Collider2D[] solids = terrain.GetComponents<Collider2D>();
        int columns = Mathf.CeilToInt(bounds.width * 32f);
        int rows = Mathf.CeilToInt(bounds.height * 32f);
        float width = bounds.width / columns;
        float height = bounds.height / rows;
        List<Rect> rectangles = new List<Rect>();
        Dictionary<Vector2Int, int> previous = new Dictionary<Vector2Int, int>();
        for (int row = 0; row < rows; row++)
        {
            Dictionary<Vector2Int, int> current = new Dictionary<Vector2Int, int>();
            int runStart = -1;
            for (int column = 0; column <= columns; column++)
            {
                bool empty = column < columns;
                Vector2 sample = new Vector2(bounds.xMin + (column + 0.5f) * width, bounds.yMin + (row + 0.5f) * height);
                if (empty)
                    foreach (Collider2D solid in solids)
                        if (solid.enabled && !solid.isTrigger && solid.OverlapPoint(sample)) { empty = false; break; }
                if (empty && runStart < 0) runStart = column;
                if (empty || runStart < 0) continue;
                Vector2Int run = new Vector2Int(runStart, column);
                if (previous.TryGetValue(run, out int index))
                {
                    Rect extended = rectangles[index];
                    extended.yMax = bounds.yMin + (row + 1) * height;
                    rectangles[index] = extended;
                    current.Add(run, index);
                }
                else
                {
                    current.Add(run, rectangles.Count);
                    rectangles.Add(new Rect(bounds.xMin + runStart * width, bounds.yMin + row * height,
                        (column - runStart) * width, height));
                }
                runStart = -1;
            }
            previous = current;
        }
        if (rectangles.Count == 0) throw new InvalidOperationException("No empty basin space in " + bounds);
        return rectangles;
    }

    private static bool IsTerrain(Collider2D shape, Scene scene, Tilemap terrain)
        => shape != null && shape.gameObject.scene == scene && shape.gameObject == terrain.gameObject && !shape.isTrigger;

    private static Vector2 Probe(Scene scene, Tilemap terrain, Vector2 seed, Vector2 direction)
    {
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(seed, direction, 40f))
            if (IsTerrain(hit.collider, scene, terrain) && hit.distance > 0.001f) return hit.point;
        throw new InvalidOperationException("No room wall from " + seed + " towards " + direction);
    }

    private static Rect ProbeRoom(Scene scene, Tilemap terrain, Vector2 seed)
    {
        float left = Probe(scene, terrain, seed, Vector2.left).x;
        float right = Probe(scene, terrain, seed, Vector2.right).x;
        float bottom = Probe(scene, terrain, seed, Vector2.down).y;
        List<float> ceilings = new List<float>(16);
        for (int sample = 0; sample < 16; sample++)
        {
            Vector2 start = new Vector2(Mathf.Lerp(left, right, (sample + 0.5f) / 16f), seed.y);
            foreach (RaycastHit2D hit in Physics2D.RaycastAll(start, Vector2.up, 40f))
                if (IsTerrain(hit.collider, scene, terrain) && hit.distance > 0.001f)
                {
                    ceilings.Add(hit.point.y);
                    break;
                }
        }
        if (ceilings.Count == 0) throw new InvalidOperationException("No roof band above " + seed);
        ceilings.Sort();
        // A grate can let isolated upward rays reach a different room above; use the dominant lower roof band.
        float top = ceilings[ceilings.Count / 3];
        Rect bounds = Rect.MinMaxRect(left, bottom, right, top);
        if (bounds.width < 1f || bounds.height < 1f)
            throw new InvalidOperationException("Unexpected room geometry at " + seed + ": " + bounds);
        return bounds;
    }

    private static bool PassageIsOpen(Scene scene, Tilemap terrain, float left, float right, float y)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(new Vector2(left - 0.05f, y), new Vector2(right + 0.05f, y)))
            if (IsTerrain(hit.collider, scene, terrain)) return false;
        return true;
    }

    private static Rect ProbePassage(Scene scene, Tilemap terrain, Rect left, Rect right)
    {
        const float step = 1f / 64f;
        float bottom = float.NaN;
        float top = float.NaN;
        float start = Mathf.Max(left.yMin, right.yMin) + step;
        float end = Mathf.Min(left.yMax, right.yMax) - step;
        for (float y = start; y < end; y += step)
        {
            bool open = PassageIsOpen(scene, terrain, left.xMax, right.xMin, y);
            if (open && float.IsNaN(bottom)) bottom = y;
            if (!open && !float.IsNaN(bottom)) { top = y - step; break; }
        }
        if (float.IsNaN(top) && !float.IsNaN(bottom)) top = end;
        if (float.IsNaN(bottom) || top - bottom < 0.2f)
            throw new InvalidOperationException("No open raised passage between " + left + " and " + right);
        return Rect.MinMaxRect(left.xMax, bottom, right.xMin, top);
    }
}
