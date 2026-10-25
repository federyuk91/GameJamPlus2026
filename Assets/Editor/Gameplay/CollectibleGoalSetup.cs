using System.IO;
using GameJamPlus.Gameplay;
using MousePlatformer;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CollectibleGoalSetup
{
    static CollectibleGoalSetup() => EditorApplication.delayCall += ProcessRequest;

    private static void ProcessRequest()
    {
        const string request = "Temp/RepairCollectibleGoals.request";
        if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(request);
        Configure();
    }

    [MenuItem("Tools/Gameplay/Repair Collectible Goal Prefabs")]
    private static void Configure()
    {
        const string path = "Assets/Prefabs/GameLoop/Universe Rig.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            GameManager manager = root.GetComponentInChildren<GameManager>(true);
            if (manager == null) return;
            CollectionGoalController controller = manager.GetComponent<CollectionGoalController>();
            if (controller == null) controller = manager.gameObject.AddComponent<CollectionGoalController>();
            SerializedObject settings = new SerializedObject(controller);
            settings.FindProperty("gameManager").objectReferenceValue = manager;
            SerializedProperty rules = settings.FindProperty("rules");
            if (rules.arraySize == 0)
            {
                rules.arraySize = 2;
                ConfigureRule(rules.GetArrayElementAtIndex(0), CollectibleType.Cheese, 10,
                    CollectionGoalMatch.Threshold, CollectionGoalTiming.Collected, StageEndCase.CheeseGoal);
                ConfigureRule(rules.GetArrayElementAtIndex(1), CollectibleType.Photo, 3,
                    CollectionGoalMatch.EveryMultiple, CollectionGoalTiming.PhotoClosed, StageEndCase.PhotoGoal);
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject owner = new SerializedObject(manager);
            owner.FindProperty("collectionGoals").objectReferenceValue = controller;
            owner.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        const string photoPath = "Assets/Prefabs/Collectibles/Photo Collectible.prefab";
        root = PrefabUtility.LoadPrefabContents(photoPath);
        try
        {
            RepairPhoto(root.GetComponent<CollectiblePhoto>());
            PrefabUtility.SaveAsPrefabAsset(root, photoPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        File.WriteAllText("Temp/CollectibleGoalSetup-report.txt", "Central collection goals wired in Universe Rig. Existing rules preserved; defaults supplied only for an empty list. Photo references and Play event repaired.");
    }

    private static void ConfigureRule(SerializedProperty rule, CollectibleType type, int count,
        CollectionGoalMatch match, CollectionGoalTiming timing, StageEndCase ending)
    {
        rule.FindPropertyRelative("enabled").boolValue = true;
        rule.FindPropertyRelative("type").enumValueIndex = (int)type;
        rule.FindPropertyRelative("requiredCount").intValue = count;
        rule.FindPropertyRelative("useSessionTotal").boolValue = true;
        rule.FindPropertyRelative("match").enumValueIndex = (int)match;
        rule.FindPropertyRelative("timing").enumValueIndex = (int)timing;
        rule.FindPropertyRelative("ending").enumValueIndex = (int)ending;
    }

    public static int RepairPhoto(CollectiblePhoto photo)
    {
        if (photo == null) return 0;
        Collectible collectible = photo.GetComponent<Collectible>();
        if (collectible == null) return 0;
        int changed = 0;
        SerializedObject settings = new SerializedObject(photo);
        SerializedProperty owner = settings.FindProperty("collectible");
        if (owner.objectReferenceValue != collectible)
        {
            owner.objectReferenceValue = collectible;
            if (settings.ApplyModifiedProperties()) { changed++; EditorUtility.SetDirty(photo); }
        }
        SerializedObject pickup = new SerializedObject(collectible);
        SerializedProperty type = pickup.FindProperty("type");
        if (type.enumValueIndex != (int)CollectibleType.Photo) { type.enumValueIndex = (int)CollectibleType.Photo; changed++; }
        SerializedProperty calls = pickup.FindProperty("onCollected.m_PersistentCalls.m_Calls");
        bool hasPlay = false;
        for (int index = 0; index < calls.arraySize; index++)
        {
            SerializedProperty call = calls.GetArrayElementAtIndex(index);
            if (call.FindPropertyRelative("m_Target").objectReferenceValue != photo || call.FindPropertyRelative("m_MethodName").stringValue != "Play") continue;
            hasPlay = true;
            if (call.FindPropertyRelative("m_CallState").intValue == 0) { call.FindPropertyRelative("m_CallState").intValue = 2; changed++; }
        }
        if (!hasPlay)
        {
            calls.InsertArrayElementAtIndex(calls.arraySize);
            SerializedProperty call = calls.GetArrayElementAtIndex(calls.arraySize - 1);
            call.FindPropertyRelative("m_Target").objectReferenceValue = photo;
            call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = typeof(CollectiblePhoto).AssemblyQualifiedName;
            call.FindPropertyRelative("m_MethodName").stringValue = "Play";
            call.FindPropertyRelative("m_Mode").intValue = 1;
            call.FindPropertyRelative("m_CallState").intValue = 2;
            changed++;
        }
        if (pickup.ApplyModifiedProperties()) EditorUtility.SetDirty(collectible);
        if (changed > 0 && PrefabUtility.IsPartOfPrefabInstance(photo))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(photo);
            PrefabUtility.RecordPrefabInstancePropertyModifications(collectible);
        }
        return changed;
    }
}
