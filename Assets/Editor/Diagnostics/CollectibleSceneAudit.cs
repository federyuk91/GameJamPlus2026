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
                    string id = collectible.CollectionId ?? string.Empty;
                    report.AppendLine("  ID=" + id + " global=" + GlobalObjectId.GetGlobalObjectIdSlow(collectible));
                    if (!ids.ContainsKey(id)) ids[id] = 0;
                    ids[id]++;
                    SerializedObject data = new SerializedObject(collectible);
                    SerializedProperty calls = data.FindProperty("onCollected.m_PersistentCalls.m_Calls");
                    for (int call = 0; call < calls.arraySize; call++)
                    {
                        SerializedProperty entry = calls.GetArrayElementAtIndex(call);
                        report.AppendLine("  EVENT " + entry.FindPropertyRelative("m_MethodName").stringValue + " target=" + entry.FindPropertyRelative("m_Target").objectReferenceValue + " state=" + entry.FindPropertyRelative("m_CallState").intValue);
                    }
                }
                foreach (CollectionGoalController controller in root.GetComponentsInChildren<CollectionGoalController>(true))
                {
                    SerializedObject settings = new SerializedObject(controller);
                    SerializedProperty rules = settings.FindProperty("rules");
                    report.AppendLine("GOALS " + PathOf(controller.transform) + " rules=" + rules.arraySize);
                }
            }
            foreach (KeyValuePair<string, int> entry in ids)
                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value > 1) report.AppendLine("INVALID ID '" + entry.Key + "' instances=" + entry.Value);
        }
        File.WriteAllText("Temp/CollectibleSceneAudit.txt", report.ToString());
    }

    private static string PathOf(Transform item) => item.parent != null ? PathOf(item.parent) + "/" + item.name : item.name;
}
