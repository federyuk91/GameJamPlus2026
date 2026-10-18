using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhysicsSettings)), CanEditMultipleObjects]
public sealed class PhysicsSettingsEditor : Editor
{
    [MenuItem("Tools/Mouse Platformer/Impostazioni fisica")]
    private static void SelectSettings()
    {
        PhysicsSettings settings = AssetDatabase.LoadAssetAtPath<PhysicsSettings>("Assets/Settings/PhysicsSettings.asset");
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

        Section("Interaction Areas");
        Field("interactions.crateGrabReach", "Distanza laterale massima fra il collider del topo e la cassa per spingere o tirare. Nei gizmo sono le fasce azzurre ai lati della cassa.");
        Field("interactions.ropeGrabRadius", "Distanza massima fra la mano del topo e la corda per agganciarsi. Nei gizmo è il contorno azzurro lungo la corda.");
        EditorGUILayout.HelpBox("Morso: seleziona Player → Player Rope Bite → Reach.\nCollezionabili: seleziona l'oggetto → Collectible → Pickup Area, poi modifica Size/Radius e Offset di quel collider.\nShow Gizmos sui singoli oggetti controlla solo la visualizzazione delle aree.", MessageType.Info);

        Section("Player — Movement");
        Field("player.speed");
        Field("player.groundAcceleration");
        Field("player.airAcceleration");
        Field("player.maxStepHeight", "Supera automaticamente piccoli dislivelli a terra. 0 disabilita; 0.15 unità corrispondono a 4.8 pixel.");
        Field("player.coyoteTime");
        Field("player.jumpBuffer");
        Field("player.launchControlTime");
        Field("player.launchControlMultiplier");
        Section("Player — Footsteps");
        Field("player.runFootstepsPerSecond", "0 disabilita i passi durante la corsa.");
        Field("player.walkFootstepsPerSecond", "Predisposta per la futura camminata. 0 disabilita i passi.");
        Section("Player — Slopes and Sliding");
        Field("player.slopeSlideAngle", "Sotto questa soglia il topo resta stabile. Il valore effettivo non supera il limite di salita.");
        Field("player.maxSlopeAngle", "Oltre questo angolo la salita è bloccata. Fra le due soglie puoi contrastare la scivolata con il movimento.");
        Field("player.slideOnSteepSlopes");
        Field("player.slopeSlideAcceleration", "Applicata lungo la superficie in proporzione alla pendenza.");
        Field("player.slopeSlideMaxSpeed");
        Section("Ledge Climb");
        Field("ledgeClimb.enabled");
        Field("ledgeClimb.maxHeight", "Dislivello massimo fra i piedi e il bordo. A terra gli ostacoli sopra l'altezza dei gradini standard vengono risaliti con la direzione.");
        Field("ledgeClimb.forwardReach");
        Field("ledgeClimb.hangHeightFraction", "Frazione dell'altezza del collider misurata dai piedi: 0.85 posiziona la presa vicino alla parte superiore del corpo.");
        Field("ledgeClimb.grabVerticalReach", "Distanza massima fra il bordo e l'altezza della presa durante un salto.");
        Field("ledgeClimb.climbSpeed");
        Field("ledgeClimb.retryDelay");

        Section("Water — Horizontal Movement");
        Field("water.speed", "Velocità massima a destra e sinistra nell'acqua.");
        Field("water.bottomWalkSpeedMultiplier", "Rispetto alla velocità normale del Player: 0.5 = metà velocità. Usato quando è immerso e appoggiato su una superficie percorribile.");
        Field("water.acceleration", "Quanto rapidamente raggiunge la velocità richiesta.");
        Field("water.strokeAcceleration", "Risposta laterale dopo la bracciata, anche appena fuori dall'acqua.");
        Field("water.strokeControlTime");
        Field("water.braking", "Quanto rapidamente si ferma quando rilasci la direzione.");
        Section("Water — Vertical Movement");
        Field("water.gravity");
        Field("water.maxSinkSpeed");
        Field("water.strokeSpeed");
        Field("water.verticalDrag");

        Section("Gravity — Player and Jump");
        Field("gravity.playerGravity");
        Field("gravity.fallGravityMultiplier");
        Field("gravity.playerTerminalSpeed");
        Field("gravity.jumpHeight", "Altezza del salto completo sopra il punto di partenza. Il rilascio anticipato di Jump riduce l'altezza.");
        Field("gravity.jumpReleaseMultiplier");
        Section("Crates and Ropes");
        Field("gravity.crateGravity");
        Field("gravity.crateTerminalSpeed");
        Field("gravity.ropeGravity");
        Field("gravity.ropeJumpUpSpeed");
        Field("gravity.ropeJumpSwingBoost");
        Field("interactions.crateMoveSpeed");
        Field("interactions.ropeClimbSpeed");
        Field("interactions.ropeSwingAcceleration");
        Field("interactions.ropeRegrabDelay");
        Section("Ladder");
        Field("ladder.climbSpeed");
        Field("ladder.jumpHorizontalSpeed");
        Field("ladder.jumpUpSpeed");
        Field("ladder.grabReach");
        Field("ladder.regrabDelay");
        Section("Digging — Shared Bite Input");
        Field("digging.enabled");
        Field("digging.speed");
        Field("digging.radius", "Il raggio effettivo non scende sotto quello necessario a contenere il collider del topo.");
        Field("digging.reach");
        Field("digging.excavatedAlpha", "0 = trasparente; 254 = quasi opaco. Non modifica le collisioni: il tunnel resta aperto.");
        Field("digging.excavatedTint", "La componente alpha del colore non viene usata; regola Alpha post scavo.");
        serializedObject.ApplyModifiedProperties();
    }

    private static void Section(string label)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }

    private void Field(string path, string tooltip = "")
    {
        SerializedProperty property = serializedObject.FindProperty(path);
        if (property != null) EditorGUILayout.PropertyField(property, new GUIContent(ObjectNames.NicifyVariableName(property.name), string.IsNullOrEmpty(tooltip) ? path : path + "\n" + tooltip));
        else EditorGUILayout.HelpBox($"Parametro mancante: {path}", MessageType.Error);
    }
}
