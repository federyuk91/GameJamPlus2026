using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SceneBackupRecovery
{
    private const string Request = "Recovery/SceneRecovery_20261020/convert.request";
    private const string Candidate = "Assets/Scenes/Recovered/1_WelcomeToUniverse_Recovered.unity";

    static SceneBackupRecovery() => EditorApplication.delayCall += ConvertBackup;

    private static void ConvertBackup()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        Scene active = SceneManager.GetActiveScene();
        Scene recovered = default;
        try
        {
            recovered = EditorSceneManager.OpenScene(Candidate, OpenSceneMode.Additive);
            bool saved = EditorSceneManager.SaveScene(recovered, Candidate);
            File.WriteAllText("Recovery/SceneRecovery_20261020/conversion-result.txt", saved ? "Converted backup successfully" : "Save failed");
        }
        catch (Exception exception)
        {
            File.WriteAllText("Recovery/SceneRecovery_20261020/conversion-result.txt", exception.ToString());
            Debug.LogException(exception);
        }
        finally
        {
            if (recovered.IsValid() && recovered.isLoaded) EditorSceneManager.CloseScene(recovered, true);
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        }
    }
}
