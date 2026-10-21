using System.Collections.Generic;
using System.IO;
using System.Text;
using MousePlatformer;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CollectibleSceneAudit
{
    static CollectibleSceneAudit() => EditorApplication.delayCall += Run;

    [MenuItem("Tools/Gameplay/Audit Collectible Scene Setup")]
    public static void Run()
    {
        StringBuilder report = new StringBuilder();
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            if (!scene.isLoaded) continue;
            report.AppendLine("SCENE " + scene.path);
            Dictionary<string, int> ids = new Dictionary<string, int>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Collectible collectible in root.GetComponentsInChildren<Collectible>(true))
                {
                    report.AppendLine("COLLECTIBLE " + PathOf(collectible.transform) + " type=" + collectible.Type + " active=" + collectible.gameObject.activeInHierarchy);
                    CollectibleRemains remains = collectible.GetComponent<CollectibleRemains>();
                    CollectiblePhoto photo = collectible.GetComponent<CollectiblePhoto>();
                    Object persistence = photo != null ? (Object)photo : remains;
                    if (persistence != null)
                    {
                        SerializedObject settings = new SerializedObject(persistence);
                        string id = settings.FindProperty("collectionId").stringValue;
                        report.AppendLine("  ID=" + id + " global=" + GlobalObjectId.GetGlobalObjectIdSlow(persistence));
                        if (!ids.ContainsKey(id)) ids[id] = 0;
                        ids[id]++;
                        if (photo != null)
                            report.AppendLine("  photoThreshold=" + settings.FindProperty("photosToEndTest").intValue + " ending=" + settings.FindProperty("curiosityEnding").objectReferenceValue);
                    }
                    SerializedObject data = new SerializedObject(collectible);
                    SerializedProperty calls = data.FindProperty("onCollected.m_PersistentCalls.m_Calls");
                    for (int call = 0; call < calls.arraySize; call++)
                    {
                        SerializedProperty entry = calls.GetArrayElementAtIndex(call);
                        report.AppendLine("  EVENT " + entry.FindPropertyRelative("m_MethodName").stringValue + " target=" + entry.FindPropertyRelative("m_Target").objectReferenceValue + " state=" + entry.FindPropertyRelative("m_CallState").intValue);
                    }
                }
                foreach (CollectibleTestGoal goal in root.GetComponentsInChildren<CollectibleTestGoal>(true))
                {
                    SerializedObject settings = new SerializedObject(goal);
                    report.AppendLine("GOAL " + PathOf(goal.transform) + " count=" + settings.FindProperty("requiredCount").intValue + " collector=" + settings.FindProperty("collector").objectReferenceValue + " outcome=" + settings.FindProperty("outcome").objectReferenceValue);
                }
            }
            foreach (KeyValuePair<string, int> entry in ids)
                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value > 1) report.AppendLine("INVALID ID '" + entry.Key + "' instances=" + entry.Value);
        }
        File.WriteAllText("Temp/CollectibleSceneAudit.txt", report.ToString());
    }

    private static string PathOf(Transform item) => item.parent != null ? PathOf(item.parent) + "/" + item.name : item.name;
}
