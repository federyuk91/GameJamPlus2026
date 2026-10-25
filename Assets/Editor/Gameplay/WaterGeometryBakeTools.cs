using System;
using System.Collections.Generic;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class WaterGeometryBakeTools
{
    private sealed class RoomCore
    {
        public readonly List<int> cells = new List<int>();
        public int seed;
        public Rect limit;
    }

    [MenuItem("Tools/Mouse Platformer/Water/Create Baker from Selected Tilemaps")]
    public static void CreateFromSelection()
    {
        List<Tilemap> maps = new List<Tilemap>();
        foreach (GameObject selected in Selection.gameObjects)
            foreach (Tilemap map in selected.GetComponentsInChildren<Tilemap>(true))
                if (HasSolidCollider(map) && !maps.Contains(map)) maps.Add(map);
        if (maps.Count == 0) throw new InvalidOperationException("Select one or more Tilemaps with solid colliders, or their Grid.");
        GameObject owner = new GameObject("Water Geometry Baker");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, maps[0].gameObject.scene);
        Undo.RegisterCreatedObjectUndo(owner, "Create Water Geometry Baker");
        WaterGeometryBaker baker = Undo.AddComponent<WaterGeometryBaker>(owner);
        baker.Configure(maps.ToArray(), AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Water Section.prefab"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlatformWater.mat"), Array.Empty<WaterBasin>());
        Selection.activeGameObject = owner;
        EditorSceneManager.MarkSceneDirty(owner.scene);
    }

    public static void PreviewBasin(WaterBasin basin)
    {
        if (Application.isPlaying) { basin.ResetWater(); return; }
        WaterVolume[] volumes = basin.GetComponentsInChildren<WaterVolume>(true);
        foreach (WaterVolume water in volumes)
        {
            Undo.RecordObject(water.transform, "Update basin water preview");
            Undo.RecordObject(water.Shape, "Update basin water preview");
            SpriteRenderer visual = water.GetComponent<SpriteRenderer>();
            if (visual != null) Undo.RecordObject(visual, "Update basin water preview");
        }
        basin.ResetWater();
        foreach (WaterVolume water in volumes)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(water.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(water.Shape);
            SpriteRenderer visual = water.GetComponent<SpriteRenderer>();
            if (visual != null) PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
        }
        SceneView.RepaintAll();
    }

    public static void RefreshWaterPreviews(WaterGeometryBaker baker)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Refresh baked water previews");
        HashSet<WaterBasin> processed = new HashSet<WaterBasin>();
        foreach (WaterBasin basin in baker.Basins)
            if (basin != null && processed.Add(basin))
            {
                Undo.RecordObject(basin, "Update water visual overlap");
                basin.SetVisualOverlapOffset(baker.VisualOverlapOffset);
                PrefabUtility.RecordPrefabInstancePropertyModifications(basin);
                PreviewBasin(basin);
            }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(baker.gameObject.scene);
    }

    public static void CenterBasinPivots(WaterGeometryBaker baker)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Center water basin pivots");
        HashSet<WaterBasin> processed = new HashSet<WaterBasin>();
        foreach (WaterBasin basin in baker.Basins)
        {
            if (basin == null || !processed.Add(basin)) continue;
            Vector2 center = basin.GeometryBounds.center;
            MoveBasinPivot(basin, new Vector3(center.x, center.y, basin.transform.position.z));
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(baker.gameObject.scene);
        SceneView.RepaintAll();
    }

    private static void MoveBasinPivot(WaterBasin basin, Vector3 center)
    {
        Vector3 delta = center - basin.transform.position;
        if (delta.sqrMagnitude < 0.00000001f) return;
        Undo.RecordObject(basin, "Preserve basin coordinates");
        Undo.RecordObject(basin.transform, "Center basin pivot");
        Transform[] children = new Transform[basin.transform.childCount];
        Vector3[] positions = new Vector3[children.Length];
        for (int index = 0; index < children.Length; index++)
        {
            children[index] = basin.transform.GetChild(index);
            positions[index] = children[index].position;
            Undo.RecordObject(children[index], "Preserve water child position");
        }
        SerializedObject data = new SerializedObject(basin);
        SerializedProperty sections = data.FindProperty("sections");
        for (int index = 0; index < sections.arraySize; index++)
        {
            SerializedProperty boundsProperty = sections.GetArrayElementAtIndex(index).FindPropertyRelative("bounds");
            Rect bounds = boundsProperty.rectValue;
            bounds.position -= (Vector2)delta;
            boundsProperty.rectValue = bounds;
        }
        data.FindProperty("initialSurfaceHeight").floatValue -= delta.y;
        data.ApplyModifiedProperties();
        basin.transform.position = center;
        for (int index = 0; index < children.Length; index++)
        {
            children[index].position = positions[index];
            PrefabUtility.RecordPrefabInstancePropertyModifications(children[index]);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(basin);
        PrefabUtility.RecordPrefabInstancePropertyModifications(basin.transform);
    }

    public static bool HasSolidCollider(Tilemap map)
    {
        foreach (Collider2D shape in map.GetComponents<Collider2D>())
            if (shape.enabled && !shape.isTrigger) return true;
        return false;
    }

    public static void Bake(WaterGeometryBaker baker, bool generateBasins = true)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (Quaternion.Angle(baker.transform.rotation, Quaternion.identity) > 0.01f
            || (baker.transform.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
            throw new InvalidOperationException("Keep the Water Geometry Baker unrotated and at world scale 1.");
        List<Collider2D> solids = new List<Collider2D>();
        List<Tilemap> maps = new List<Tilemap>();
        List<Dictionary<Vector3Int, bool>> tilePresence = new List<Dictionary<Vector3Int, bool>>();
        Bounds combined = new Bounds();
        bool first = true;
        foreach (Tilemap map in baker.SourceTilemaps)
        {
            if (map == null) continue;
            if (map.gameObject.scene != baker.gameObject.scene) throw new InvalidOperationException("Source Tilemaps must belong to the baker's scene.");
            if (!maps.Contains(map))
            {
                maps.Add(map);
                tilePresence.Add(new Dictionary<Vector3Int, bool>());
            }
            foreach (Collider2D shape in map.GetComponents<Collider2D>())
                if (shape.enabled && !shape.isTrigger && !solids.Contains(shape)) solids.Add(shape);
            Bounds local = map.localBounds;
            Bounds world = new Bounds(map.transform.TransformPoint(local.center), Vector3.zero);
            for (int corner = 0; corner < 8; corner++)
                world.Encapsulate(map.transform.TransformPoint(local.center + Vector3.Scale(local.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1))));
            if (first) { combined = world; first = false; }
            else combined.Encapsulate(world);
        }
        if (solids.Count == 0 || first) throw new InvalidOperationException("Assign at least one Tilemap with enabled non-trigger colliders.");
        Physics2D.SyncTransforms();
        Rect area = baker.UseCustomBounds ? baker.CustomBounds
            : Rect.MinMaxRect(combined.min.x, combined.min.y, combined.max.x, combined.max.y);
        if (area.width <= 0f || area.height <= 0f) throw new InvalidOperationException("Bake bounds must have positive dimensions.");
        int width = Mathf.CeilToInt(area.width * baker.PixelsPerUnit);
        int height = Mathf.CeilToInt(area.height * baker.PixelsPerUnit);
        if ((long)width * height > 4000000) throw new InvalidOperationException("Bake exceeds four million samples. Reduce Pixels Per Unit or enable Custom Bounds.");
        float dx = area.width / width;
        float dy = area.height / height;
        int count = width * height;
        bool[] blocked = new bool[count];
        byte[] packed = new byte[(count + 7) / 8];
        try
        {
            for (int y = 0; y < height; y++)
            {
                if (y % 16 == 0 && EditorUtility.DisplayCancelableProgressBar("Bake Water Geometry", "Sampling selected Tilemap colliders", (float)y / height))
                    throw new OperationCanceledException("Water bake cancelled; previous bake preserved.");
                for (int x = 0; x < width; x++)
                {
                    Vector2 sample = new Vector2(area.xMin + (x + 0.5f) * dx, area.yMin + (y + 0.5f) * dy);
                    int index = y * width + x;
                    bool hasTile = false;
                    for (int mapIndex = 0; mapIndex < maps.Count; mapIndex++)
                    {
                        Tilemap map = maps[mapIndex];
                        Vector3Int cell = map.WorldToCell(new Vector3(sample.x, sample.y, map.transform.position.z));
                        Dictionary<Vector3Int, bool> presence = tilePresence[mapIndex];
                        if (!presence.TryGetValue(cell, out bool painted))
                        {
                            painted = map.HasTile(cell);
                            presence.Add(cell, painted);
                        }
                        if (!painted) continue;
                        hasTile = true;
                        break;
                    }
                    // Empty cells limit basin geometry but are not physical obstacles for falling water.
                    blocked[index] = !hasTile;
                    foreach (Collider2D shape in solids)
                        if (shape.OverlapPoint(sample))
                        {
                            blocked[index] = true;
                            packed[index >> 3] |= (byte)(1 << (index & 7));
                            break;
                        }
                }
            }
            List<RoomCore> cores = generateBasins
                ? FindCores(blocked, width, height, area, baker.RoomSeparationRadius, baker.PassageReach) : new List<RoomCore>();
            int[] owners = generateBasins ? GrowRooms(cores, blocked, width, height, area) : Array.Empty<int>();
            if (generateBasins) IncludeEnclosedPassages(owners, blocked, width, height);
            List<Rect>[] regions = generateBasins ? CompactRegions(owners, cores.Count, width, height, area) : Array.Empty<List<Rect>>();
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Bake selected Tilemaps for water");
            try
            {
                Undo.RecordObject(baker, "Store baked water geometry");
                if (baker.WaterVolumePrefab == null || AssetDatabase.GetAssetPath(baker.WaterVolumePrefab) == "Assets/Prefabs/Environment/Water Volume.prefab")
                {
                    SerializedObject settings = new SerializedObject(baker);
                    settings.FindProperty("waterVolumePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Water Section.prefab");
                    settings.ApplyModifiedProperties();
                }
                baker.SetBake(area, width, height, packed);
                if (generateBasins) ConfigureBasins(baker, cores, owners, regions, width, height, area);
                EditorUtility.SetDirty(baker);
                EditorSceneManager.MarkSceneDirty(baker.gameObject.scene);
                Undo.CollapseUndoOperations(undo);
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
            Debug.Log("Water bake complete: " + baker.SourceTilemaps.Count + " Tilemaps, " + width + " × " + height
                + " samples, " + cores.Count + " room cores. Save the scene to keep the bake.", baker);
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    private static int Neighbor(int index, int direction, int width, int height)
    {
        int x = index % width;
        int y = index / width;
        if (direction == 0) return x > 0 ? index - 1 : -1;
        if (direction == 1) return x + 1 < width ? index + 1 : -1;
        if (direction == 2) return y > 0 ? index - width : -1;
        return y + 1 < height ? index + width : -1;
    }

    private static Vector2 Point(int index, int width, int height, Rect area)
        => new Vector2(area.xMin + (index % width + 0.5f) * area.width / width,
            area.yMin + (index / width + 0.5f) * area.height / height);

    private static bool HasWall(bool[] blocked, int index, int step, int width, int height)
    {
        int x = index % width;
        int y = index / width;
        while (x >= 0 && x < width && y >= 0 && y < height)
        {
            if (blocked[y * width + x]) return true;
            if (step == -1) x--; else if (step == 1) x++; else y--;
        }
        return false;
    }

    private static List<RoomCore> FindCores(bool[] blocked, int width, int height, Rect area, float radius, float reach)
    {
        int[] distance = new int[blocked.Length];
        Queue<int> queue = new Queue<int>();
        for (int index = 0; index < blocked.Length; index++)
        {
            int x = index % width;
            int y = index / width;
            distance[index] = int.MaxValue;
            if (!blocked[index] && x != 0 && x != width - 1 && y != 0 && y != height - 1) continue;
            distance[index] = 0;
            queue.Enqueue(index);
        }
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            for (int direction = 0; direction < 4; direction++)
            {
                int next = Neighbor(current, direction, width, height);
                if (next < 0 || distance[next] <= distance[current] + 1) continue;
                distance[next] = distance[current] + 1;
                queue.Enqueue(next);
            }
        }
        int threshold = Mathf.Max(1, Mathf.CeilToInt(radius * Mathf.Max(width / area.width, height / area.height)));
        bool[] visited = new bool[blocked.Length];
        List<RoomCore> cores = new List<RoomCore>();
        for (int index = 0; index < blocked.Length; index++)
        {
            if (visited[index] || blocked[index] || distance[index] < threshold) continue;
            RoomCore core = new RoomCore { seed = index };
            Vector2 minimum = Point(index, width, height, area);
            Vector2 maximum = minimum;
            visited[index] = true;
            queue.Enqueue(index);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                core.cells.Add(current);
                Vector2 point = Point(current, width, height, area);
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
                if (distance[current] > distance[core.seed]) core.seed = current;
                for (int direction = 0; direction < 4; direction++)
                {
                    int next = Neighbor(current, direction, width, height);
                    if (next < 0 || visited[next] || blocked[next] || distance[next] < threshold) continue;
                    visited[next] = true;
                    queue.Enqueue(next);
                }
            }
            if (!HasWall(blocked, core.seed, -1, width, height) || !HasWall(blocked, core.seed, 1, width, height)
                || !HasWall(blocked, core.seed, -width, width, height)) continue;
            core.limit = Rect.MinMaxRect(minimum.x - radius - reach, minimum.y - radius,
                maximum.x + radius + reach, maximum.y + radius);
            cores.Add(core);
        }
        return cores;
    }

    private static int[] GrowRooms(List<RoomCore> cores, bool[] blocked, int width, int height, Rect area)
    {
        int[] owners = new int[blocked.Length];
        Array.Fill(owners, -1);
        Queue<int> queue = new Queue<int>();
        for (int room = 0; room < cores.Count; room++)
            foreach (int cell in cores[room].cells) { owners[cell] = room; queue.Enqueue(cell); }
        while (queue.Count > 0)
        {
            int cell = queue.Dequeue();
            int room = owners[cell];
            for (int direction = 0; direction < 4; direction++)
            {
                int next = Neighbor(cell, direction, width, height);
                if (next < 0 || blocked[next] || owners[next] >= 0 || !cores[room].limit.Contains(Point(next, width, height, area))) continue;
                owners[next] = room;
                queue.Enqueue(next);
            }
        }
        return owners;
    }

    private static void IncludeEnclosedPassages(int[] owners, bool[] blocked, int width, int height)
    {
        bool[] visited = new bool[owners.Length];
        Queue<int> queue = new Queue<int>();
        List<int> component = new List<int>();
        for (int start = 0; start < owners.Length; start++)
        {
            if (visited[start] || blocked[start] || owners[start] >= 0) continue;
            component.Clear();
            bool outside = false;
            visited[start] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                component.Add(cell);
                int x = cell % width;
                int y = cell / width;
                outside |= x == 0 || y == 0 || x == width - 1 || y == height - 1;
                for (int direction = 0; direction < 4; direction++)
                {
                    int next = Neighbor(cell, direction, width, height);
                    if (next < 0 || visited[next] || blocked[next] || owners[next] >= 0) continue;
                    visited[next] = true;
                    queue.Enqueue(next);
                }
            }
            // Recover enclosed vertical/angled pipes beyond room-core bounds, without filling the exterior sky.
            if (outside) continue;
            foreach (int cell in component)
                for (int direction = 0; direction < 4; direction++)
                {
                    int next = Neighbor(cell, direction, width, height);
                    if (next < 0 || owners[next] < 0 || visited[next]) continue;
                    owners[cell] = owners[next];
                    queue.Enqueue(cell);
                    break;
                }
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                for (int direction = 0; direction < 4; direction++)
                {
                    int next = Neighbor(cell, direction, width, height);
                    if (next < 0 || blocked[next] || owners[next] >= 0) continue;
                    owners[next] = owners[cell];
                    queue.Enqueue(next);
                }
            }
        }
    }

    private static List<Rect>[] CompactRegions(int[] owners, int count, int width, int height, Rect area)
    {
        List<Rect>[] rectangles = new List<Rect>[count];
        for (int index = 0; index < count; index++) rectangles[index] = new List<Rect>();
        Dictionary<Vector3Int, int> previous = new Dictionary<Vector3Int, int>();
        float dx = area.width / width;
        float dy = area.height / height;
        for (int y = 0; y < height; y++)
        {
            Dictionary<Vector3Int, int> current = new Dictionary<Vector3Int, int>();
            for (int x = 0; x < width;)
            {
                int room = owners[y * width + x];
                int start = x++;
                while (x < width && owners[y * width + x] == room) x++;
                if (room < 0) continue;
                Vector3Int run = new Vector3Int(room, start, x);
                if (previous.TryGetValue(run, out int index))
                {
                    Rect rectangle = rectangles[room][index];
                    rectangle.yMax = area.yMin + (y + 1) * dy;
                    rectangles[room][index] = rectangle;
                    current.Add(run, index);
                }
                else
                {
                    current.Add(run, rectangles[room].Count);
                    rectangles[room].Add(new Rect(area.xMin + start * dx, area.yMin + y * dy, (x - start) * dx, dy));
                }
            }
            previous = current;
        }
        return rectangles;
    }

    private static void ConfigureBasins(WaterGeometryBaker baker, List<RoomCore> cores, int[] owners,
        List<Rect>[] regions, int width, int height, Rect area)
    {
        if (cores.Count == 0) return;
        if (baker.WaterVolumePrefab == null || baker.WaterMaterial == null)
            throw new InvalidOperationException("Assign Water Volume Prefab and Water Material before generating basins.");
        Transform root = baker.GeneratedRoot;
        if (root == null)
        {
            GameObject owner = new GameObject("Baked Water Basins");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, baker.gameObject.scene);
            owner.transform.SetParent(baker.transform, false);
            Undo.RegisterCreatedObjectUndo(owner, "Create baked basins");
            root = owner.transform;
        }
        List<WaterBasin> candidates = new List<WaterBasin>();
        foreach (WaterBasin basin in baker.Basins) if (basin != null && !candidates.Contains(basin)) candidates.Add(basin);
        foreach (WaterBasin basin in root.GetComponentsInChildren<WaterBasin>(true)) if (!candidates.Contains(basin)) candidates.Add(basin);
        Dictionary<WaterVolume, Rect> legacyWater = new Dictionary<WaterVolume, Rect>();
        foreach (GameObject owner in baker.gameObject.scene.GetRootGameObjects())
            foreach (WaterVolume water in owner.GetComponentsInChildren<WaterVolume>())
            {
                if (water.GetComponent<WaterUnit>() != null || water.GetComponentInParent<WaterBasin>() != null || !water.Shape.enabled) continue;
                Bounds bounds = water.Shape.bounds;
                legacyWater.Add(water, Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y));
            }
        HashSet<WaterVolume> adopted = new HashSet<WaterVolume>();
        WaterBasin[] mapping = new WaterBasin[cores.Count];
        List<WaterBasin> all = new List<WaterBasin>();
        foreach (WaterBasin basin in candidates) if (!basin.transform.IsChildOf(root)) all.Add(basin);
        Dictionary<WaterBasin, List<Rect>> grouped = new Dictionary<WaterBasin, List<Rect>>();
        for (int room = 0; room < cores.Count; room++)
        {
            Vector2 seed = Point(cores[room].seed, width, height, area);
            WaterBasin basin = candidates.Find(candidate => candidate.ContainsGeometryPoint(seed));
            if (basin == null)
            {
                GameObject owner = new GameObject("Basin " + (room + 1));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, baker.gameObject.scene);
                owner.transform.SetParent(root, false);
                owner.transform.position = seed;
                Undo.RegisterCreatedObjectUndo(owner, "Create automatic water basin");
                basin = Undo.AddComponent<WaterBasin>(owner);
            }
            mapping[room] = basin;
            if (!all.Contains(basin)) all.Add(basin);
            if (!grouped.TryGetValue(basin, out List<Rect> pieces))
            {
                pieces = new List<Rect>();
                grouped.Add(basin, pieces);
            }
            pieces.AddRange(regions[room]);
        }
        foreach (KeyValuePair<WaterBasin, List<Rect>> group in grouped)
        {
            WaterBasin basin = group.Key;
            List<Rect> pieces = group.Value;
            float initial = candidates.Contains(basin) ? basin.SurfaceHeight : float.NegativeInfinity;
            foreach (WaterVolume oldWater in basin.GetComponentsInChildren<WaterVolume>(true))
                if (oldWater.GetComponent<WaterUnit>() == null) Undo.DestroyObjectImmediate(oldWater.gameObject);
            Rect areaBounds = pieces[0];
            foreach (Rect piece in pieces)
                areaBounds = Rect.MinMaxRect(Mathf.Min(areaBounds.xMin, piece.xMin), Mathf.Min(areaBounds.yMin, piece.yMin),
                    Mathf.Max(areaBounds.xMax, piece.xMax), Mathf.Max(areaBounds.yMax, piece.yMax));
            MoveBasinPivot(basin, new Vector3(areaBounds.center.x, areaBounds.center.y, basin.transform.position.z));
            WaterBasin.Section[] sections = new WaterBasin.Section[pieces.Count];
            for (int index = 0; index < sections.Length; index++)
            {
                GameObject owner = (GameObject)PrefabUtility.InstantiatePrefab(baker.WaterVolumePrefab, basin.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(owner, "Create water section");
                owner.transform.SetParent(basin.transform, false);
                owner.name = "Water Section " + index;
                SpriteRenderer visual = owner.GetComponent<SpriteRenderer>();
                visual.sharedMaterial = baker.WaterMaterial;
                Rect local = pieces[index];
                local.position -= (Vector2)basin.transform.position;
                sections[index] = new WaterBasin.Section { bounds = local, water = owner.GetComponent<WaterVolume>() };
            }
            if (float.IsNegativeInfinity(initial))
            {
                initial = float.PositiveInfinity;
                foreach (Rect rectangle in pieces) initial = Mathf.Min(initial, rectangle.yMin);
                double volume = 0d;
                foreach (KeyValuePair<WaterVolume, Rect> legacy in legacyWater)
                    foreach (Rect rectangle in pieces)
                    {
                        float overlap = Mathf.Max(0f, Mathf.Min(rectangle.xMax, legacy.Value.xMax) - Mathf.Max(rectangle.xMin, legacy.Value.xMin))
                            * Mathf.Max(0f, Mathf.Min(rectangle.yMax, legacy.Value.yMax) - Mathf.Max(rectangle.yMin, legacy.Value.yMin));
                        volume += overlap;
                        if (overlap > 0f) adopted.Add(legacy.Key);
                    }
                WaterBasinModel initialModel = new WaterBasinModel(pieces.ToArray(), initial);
                initial = initialModel.SurfaceAtVolume(Math.Min(initialModel.Capacity, volume));
            }
            Undo.RecordObject(basin, "Update basin geometry");
            Undo.RecordObject(basin.gameObject, "Enable baked basin");
            basin.gameObject.SetActive(true);
            basin.Configure(sections, initial);
            basin.SetVisualOverlapOffset(baker.VisualOverlapOffset);
            basin.ResetWater();
            foreach (WaterVolume water in basin.GetComponentsInChildren<WaterVolume>(true))
            {
                if (water.GetComponent<WaterUnit>() != null) continue;
                PrefabUtility.RecordPrefabInstancePropertyModifications(water.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(water.Shape);
                PrefabUtility.RecordPrefabInstancePropertyModifications(water.GetComponent<SpriteRenderer>());
            }
        }
        foreach (WaterBasin candidate in candidates)
            if (candidate.transform.IsChildOf(root) && !all.Contains(candidate))
            {
                Undo.RecordObject(candidate.gameObject, "Disable obsolete basin");
                candidate.gameObject.SetActive(false);
            }
        foreach (WaterVolume legacy in adopted)
        {
            Undo.RecordObject(legacy.gameObject, "Adopt static water into baked basin");
            legacy.gameObject.SetActive(false);
        }
        CreateConnections(baker, root, mapping, owners, width, height, area);
        baker.SetGeneratedBasins(root, all.ToArray());
        RenameConnections(baker);
    }

    public static void RenameConnections(WaterGeometryBaker baker)
    {
        if (Application.isPlaying || !baker.gameObject.scene.IsValid()) return;
        Dictionary<WaterBasin, string> labels = new Dictionary<WaterBasin, string>();
        for (int index = 0; index < baker.Basins.Count; index++)
        {
            WaterBasin basin = baker.Basins[index];
            if (basin == null || labels.ContainsKey(basin)) continue;
            string suffix = basin.name.StartsWith("Basin ", StringComparison.Ordinal) ? basin.name.Substring(6) : string.Empty;
            int number = int.TryParse(suffix, out int parsed) && parsed > 0 ? parsed : index + 1;
            labels.Add(basin, "B" + number);
        }
        foreach (GameObject root in baker.gameObject.scene.GetRootGameObjects())
            foreach (WaterConnection connection in root.GetComponentsInChildren<WaterConnection>(true))
            {
                SerializedObject data = new SerializedObject(connection);
                WaterBasin first = data.FindProperty("first").objectReferenceValue as WaterBasin;
                WaterBasin second = data.FindProperty("second").objectReferenceValue as WaterBasin;
                if (first == null || second == null || !labels.TryGetValue(first, out string firstLabel)
                    || !labels.TryGetValue(second, out string secondLabel)) continue;
                string name = "Link " + firstLabel + "-" + secondLabel;
                if (connection.name == name) continue;
                Undo.RecordObject(connection.gameObject, "Rename water link");
                connection.gameObject.name = name;
                PrefabUtility.RecordPrefabInstancePropertyModifications(connection.gameObject);
                EditorSceneManager.MarkSceneDirty(connection.gameObject.scene);
            }
    }

    private static void CreateConnections(WaterGeometryBaker baker, Transform root, WaterBasin[] mapping,
        int[] owners, int width, int height, Rect area)
    {
        Dictionary<Vector2Int, Vector2> openings = new Dictionary<Vector2Int, Vector2>();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                int first = owners[index];
                if (first < 0) continue;
                for (int axis = 0; axis < 2; axis++)
                {
                    if ((axis == 0 && x + 1 >= width) || (axis == 1 && y + 1 >= height)) continue;
                    int second = owners[index + (axis == 0 ? 1 : width)];
                    if (second < 0 || first == second || mapping[first] == mapping[second]) continue;
                    Vector2Int key = new Vector2Int(Mathf.Min(first, second), Mathf.Max(first, second));
                    Vector2 position = Point(index, width, height, area);
                    position.y = area.yMin + (y + (axis == 0 ? 0 : 1)) * area.height / height;
                    if (!openings.TryGetValue(key, out Vector2 old) || position.y < old.y) openings[key] = position;
                }
            }
        List<WaterConnection> existing = new List<WaterConnection>();
        foreach (GameObject owner in baker.gameObject.scene.GetRootGameObjects()) existing.AddRange(owner.GetComponentsInChildren<WaterConnection>(true));
        HashSet<WaterConnection> valid = new HashSet<WaterConnection>();
        foreach (KeyValuePair<Vector2Int, Vector2> opening in openings)
        {
            WaterBasin first = mapping[opening.Key.x];
            WaterBasin second = mapping[opening.Key.y];
            WaterConnection found = existing.Find(connection =>
            {
                SerializedObject data = new SerializedObject(connection);
                UnityEngine.Object a = data.FindProperty("first").objectReferenceValue;
                UnityEngine.Object b = data.FindProperty("second").objectReferenceValue;
                return (a == first && b == second) || (a == second && b == first);
            });
            if (found != null)
            {
                valid.Add(found);
                if (found.transform.IsChildOf(root))
                {
                    Undo.RecordObject(found.transform, "Update baked passage position");
                    Undo.RecordObject(found, "Update baked passage sill");
                    Undo.RecordObject(found.gameObject, "Enable baked passage");
                    found.transform.position = opening.Value;
                    found.Configure(first, second, opening.Value.y);
                    found.gameObject.SetActive(true);
                }
                continue;
            }
            GameObject owner = new GameObject(first.name + " - " + second.name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, baker.gameObject.scene);
            owner.transform.SetParent(root, false);
            owner.transform.position = opening.Value;
            Undo.RegisterCreatedObjectUndo(owner, "Create hydraulic connection");
            WaterConnection connection = Undo.AddComponent<WaterConnection>(owner);
            connection.Configure(first, second, opening.Value.y);
            valid.Add(connection);
            existing.Add(connection);
        }
        foreach (WaterConnection connection in existing)
            if (connection.transform.IsChildOf(root) && !valid.Contains(connection))
            {
                Undo.RecordObject(connection.gameObject, "Disable obsolete baked passage");
                connection.gameObject.SetActive(false);
            }
    }
}
