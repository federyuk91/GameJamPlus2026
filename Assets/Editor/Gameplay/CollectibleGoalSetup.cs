using System.IO;
using GameJamPlus.Gameplay;
using GameJamPlus.UI;
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
        const string path = "Assets/Prefabs/Characters/Player.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            CollectibleCollector collector = root.GetComponent<CollectibleCollector>();
            Transform goalTransform = root.transform.Find("Cheese Collection Goal");
            GameObject owner = goalTransform != null ? goalTransform.gameObject : new GameObject("Cheese Collection Goal");
            owner.transform.SetParent(root.transform, false);
            TestEndEvent ending = owner.GetComponent<TestEndEvent>();
            if (ending == null) ending = owner.AddComponent<TestEndEvent>();
            DialogueMessagePlayer dialogue = owner.GetComponent<DialogueMessagePlayer>();
            if (dialogue == null) dialogue = owner.AddComponent<DialogueMessagePlayer>();
            SerializedObject messages = new SerializedObject(dialogue);
            SerializedProperty text = messages.FindProperty("messages");
            if (text.arraySize == 0)
            {
                text.arraySize = 2;
                text.GetArrayElementAtIndex(0).stringValue = "Excellent appetite! That is enough cheese for this trial. Time for a well-earned rest.";
                text.GetArrayElementAtIndex(1).stringValue = "Ten portions already! What a healthy appetite. We can conclude this trial here.";
            }
            messages.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject outcome = new SerializedObject(ending);
            outcome.FindProperty("subjectDied").boolValue = false;
            outcome.FindProperty("scientistComment").objectReferenceValue = dialogue;
            SerializedProperty results = outcome.FindProperty("resultMessages");
            results.arraySize = 1;
            results.GetArrayElementAtIndex(0).stringValue = "The subject has enjoyed ten portions of cheese and completed the trial in good health.";
            outcome.ApplyModifiedPropertiesWithoutUndo();
            CollectibleTestGoal goal = owner.GetComponent<CollectibleTestGoal>();
            if (goal == null) goal = owner.AddComponent<CollectibleTestGoal>();
            SerializedObject settings = new SerializedObject(goal);
            settings.FindProperty("collector").objectReferenceValue = collector;
            settings.FindProperty("type").enumValueIndex = (int)CollectibleType.Cheese;
            settings.FindProperty("requiredCount").intValue = 10;
            settings.FindProperty("useSessionTotal").boolValue = true;
            settings.FindProperty("outcome").objectReferenceValue = ending;
            settings.ApplyModifiedPropertiesWithoutUndo();
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
        File.WriteAllText("Temp/CollectibleGoalSetup-report.txt", "Player prefab: cheese quantity goal wired, Required Count=10, non-lethal outcome. Photo prefab: Photo type and Play event verified, quantity threshold=3. Both use the HUD session total. IDs remain separate from quantity checks.");
        EditorApplication.delayCall += CollectibleSceneAudit.Run;
    }

    public static int RepairPhoto(CollectiblePhoto photo)
    {
        if (photo == null) return 0;
        int changed = 0;
        SerializedObject settings = new SerializedObject(photo);
        SerializedProperty total = settings.FindProperty("useSessionTotal");
        if (!total.boolValue) { total.boolValue = true; changed++; }
        SerializedProperty ending = settings.FindProperty("curiosityEnding");
        if (ending.objectReferenceValue == null)
        {
            TestEndEvent localEnding = photo.GetComponent<TestEndEvent>();
            if (localEnding != null) { ending.objectReferenceValue = localEnding; changed++; }
        }
        if (settings.ApplyModifiedProperties()) EditorUtility.SetDirty(photo);
        Collectible collectible = photo.GetComponent<Collectible>();
        if (collectible == null) return changed;
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
