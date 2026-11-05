using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MaskedSpriteLensSetup
{
    [MenuItem("CONTEXT/SpriteRenderer/Configure Masked Lens")]
    private static void Configure(MenuCommand command)
    {
        SpriteRenderer renderer = (SpriteRenderer)command.context;
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MaskedSpriteLens.mat");
        if (material == null) { Debug.LogError("MaskedSpriteLens.mat is missing.", renderer); return; }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure masked sprite lens");
        MagnifyingLens2D lens = renderer.GetComponent<MagnifyingLens2D>();
        if (lens == null) lens = Undo.AddComponent<MagnifyingLens2D>(renderer.gameObject);
        SerializedObject data = new SerializedObject(lens);
        data.FindProperty("lensRenderer").objectReferenceValue = renderer;
        data.FindProperty("magnifyingMaterial").objectReferenceValue = material;
        data.FindProperty("maskSource").enumValueIndex = (int)MagnifyingLens2D.EffectMaskSource.SpriteSecondaryTexture;
        data.FindProperty("maskChannel").enumValueIndex = (int)MagnifyingLens2D.EffectMaskChannel.Red;
        data.FindProperty("maskStrength").floatValue = 1f;
        data.FindProperty("lensCenterUV").vector2Value = Vector2.one * 0.5f;
        data.FindProperty("lensRadiusUV").vector2Value = Vector2.one * 0.5f;
        data.ApplyModifiedProperties();
        Undo.RecordObject(renderer, "Assign masked sprite lens material");
        Undo.RecordObject(lens, "Store original sprite material");
        lens.ApplySettings();
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(lens);
        EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
        Undo.CollapseUndoOperations(group);
        SceneView.RepaintAll();
    }

    [MenuItem("CONTEXT/SpriteRenderer/Configure Masked Lens", true)]
    private static bool CanConfigure(MenuCommand command)
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode && command.context is SpriteRenderer renderer
            && renderer.gameObject.scene.IsValid();
    }
}
