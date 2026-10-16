using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhysicsSettings)), CanEditMultipleObjects]
public sealed class PhysicsSettingsEditor : Editor
{
    [MenuItem("Tools/Mouse Platformer/Impostazioni fisica")]
    private static void SelectSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<PhysicsSettings>("Assets/Settings/PhysicsSettings.asset");
        if (settings == null)
        {
            Debug.LogError("PhysicsSettings.asset non trovato in Assets/Settings.");
            return;
        }
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        EditorGUILayout.HelpBox("1 unità = 32 pixel. Velocità in unità/s; accelerazioni in unità/s².", MessageType.Info);

        Section("Player — movimento");
        Field("player.speed", "Velocità orizzontale (unità/s)");
        Field("player.groundAcceleration", "Accelerazione e frenata a terra (unità/s²)");
        Field("player.airAcceleration", "Accelerazione in aria (unità/s²)");
        Field("player.coyoteTime", "Coyote time (s)");
        Field("player.jumpBuffer", "Buffer del salto (s)");
        Field("player.launchControlTime", "Controllo ridotto dopo il distacco (s)");
        Field("player.launchControlMultiplier", "Moltiplicatore controllo dopo il distacco");
        Section("Player — risalita automatica dei bordi");
        Field("ledgeClimb.enabled", "Abilitata");
        Field("ledgeClimb.maxHeight", "Altezza massima (unità)", "Dislivello massimo fra i piedi e la superficie del bordo.");
        Field("ledgeClimb.forwardReach", "Distanza sonda oltre il bordo (unità)");
        Field("ledgeClimb.climbSpeed", "Velocità risalita (unità/s)");
        Field("ledgeClimb.retryDelay", "Pausa tra tentativi (s)");

        Section("Acqua — movimento orizzontale");
        Field("water.speed", "Velocità orizzontale (unità/s)", "Velocità massima a destra e sinistra nell'acqua.");
        Field("water.acceleration", "Accelerazione (unità/s²)", "Quanto rapidamente raggiunge la velocità richiesta.");
        Field("water.strokeAcceleration", "Accelerazione dopo Jump (unità/s²)", "Risposta laterale dopo la bracciata, anche appena fuori dall'acqua.");
        Field("water.strokeControlTime", "Durata controllo dopo Jump (s)");
        Field("water.braking", "Frenata (unità/s²)", "Quanto rapidamente si ferma quando rilasci la direzione.");
        Section("Acqua — affondamento e risalita");
        Field("water.gravity", "Gravità in acqua (unità/s²)");
        Field("water.maxSinkSpeed", "Caduta massima (unità/s)");
        Field("water.strokeSpeed", "Spinta con Jump (unità/s)");
        Field("water.verticalDrag", "Resistenza verticale (1/s)");

        Section("Player — gravità e salto");
        Field("gravity.playerGravity", "Gravità (unità/s²)");
        Field("gravity.fallGravityMultiplier", "Moltiplicatore in discesa");
        Field("gravity.playerTerminalSpeed", "Caduta massima (unità/s)");
        Field("gravity.jumpHeight", "Jump Height (unità)", "Altezza del salto completo sopra il punto di partenza. Il rilascio anticipato di Jump riduce l'altezza.");
        Field("gravity.jumpReleaseMultiplier", "Moltiplicatore al rilascio di Jump");
        Section("Casse e corda");
        Field("gravity.crateGravity", "Gravità casse (unità/s²)");
        Field("gravity.crateTerminalSpeed", "Caduta massima casse (unità/s)");
        Field("gravity.ropeGravity", "Gravità corda (unità/s²)");
        Field("gravity.ropeJumpUpSpeed", "Salto dalla corda: verticale (unità/s)");
        Field("gravity.ropeJumpSwingBoost", "Salto dalla corda: slancio (unità/s)");
        Field("interactions.crateMoveSpeed", "Velocità spinta/tiro casse (unità/s)");
        Field("interactions.crateGrabReach", "Portata presa casse (unità)");
        Field("interactions.ropeClimbSpeed", "Arrampicata sulla corda (unità/s)");
        Field("interactions.ropeSwingAcceleration", "Accelerazione oscillazione (unità/s²)");
        Field("interactions.ropeGrabRadius", "Portata presa corda (unità)");
        Field("interactions.ropeRegrabDelay", "Ritardo riaggancio corda (s)");
        Section("Scala");
        Field("ladder.climbSpeed", "Arrampicata (unità/s)");
        Field("ladder.jumpHorizontalSpeed", "Salto laterale (unità/s)");
        Field("ladder.jumpUpSpeed", "Salto verticale (unità/s)");
        Field("ladder.grabReach", "Portata aggancio (unità)");
        Field("ladder.regrabDelay", "Ritardo riaggancio (s)");
        serializedObject.ApplyModifiedProperties();
    }

    private static void Section(string label)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }

    private void Field(string path, string label, string tooltip = "")
    {
        SerializedProperty property = serializedObject.FindProperty(path);
        if (property != null) EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
        else EditorGUILayout.HelpBox($"Parametro mancante: {path}", MessageType.Error);
    }
}
