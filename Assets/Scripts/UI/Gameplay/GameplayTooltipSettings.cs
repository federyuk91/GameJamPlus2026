using System;
using UnityEngine;

namespace GameJamPlus.UI
{
    [CreateAssetMenu(menuName = "Game/UI/Gameplay Tooltip Settings")]
    public sealed class GameplayTooltipSettings : ScriptableObject
    {
        [SerializeField] private GameplayTooltipText texts = new GameplayTooltipText();
        public GameplayTooltipText Texts => texts;
    }

    [Serializable]
    public sealed class GameplayTooltipText
    {
        [Tooltip("Mostrato durante la presentazione iniziale dello spawn, prima che il giocatore avvii il controllo del topo.")]
        [TextArea(1, 3)] public string start = "Press a button to start";

        [Tooltip("Mostrato quando una cassa raggiungibile può essere spinta o tirata. Il comando viene accompagnato dalle icone direzionali.")]
        [TextArea(1, 3)] public string moveCrate = "Hold to push / pull";
        [Tooltip("Mostrato vicino a una Snake Box disponibile, per nascondersi al suo interno.")]
        [TextArea(1, 3)] public string enterSnakeBox = "Hide in box";
        [Tooltip("Mostrato dentro una Snake Box. Anche il salto fa uscire, senza saltare.")]
        [TextArea(1, 3)] public string exitSnakeBox = "Leave box";

        [Tooltip("Mostrato mentre il topo è appeso a un bordo. Indica il comando per tentare la salita; non garantisce che lo spazio sopra sia libero.")]
        [TextArea(1, 3)] public string climbUp = "Climb up";

        [Tooltip("Mostrato quando il bersaglio commestibile raggiungibile è un formaggio normale non ancora consumato.")]
        [TextArea(1, 3)] public string eatCheese = "Eat cheese";

        [Tooltip("Mostrato quando il bersaglio commestibile raggiungibile è un formaggio avvelenato non ancora consumato.")]
        [TextArea(1, 3)] public string eatGreenCheese = "Eat green cheese";

        [Tooltip("Mostrato quando una foto raccoglibile è selezionata come interazione delle zampe anteriori.")]
        [TextArea(1, 3)] public string collectPhoto = "Collect photo";

        [Tooltip("Mostrato quando è selezionato un oggetto raccoglibile diverso da foto e formaggi.")]
        [TextArea(1, 3)] public string collectItem = "Collect";

        [Tooltip("Mostrato quando un pulsante raggiungibile e disponibile è selezionato per l'attivazione manuale. Non riguarda la pressione automatica tramite peso.")]
        [TextArea(1, 3)] public string activateButton = "Activate";

        [Tooltip("Mostrato quando una ruota utilizzabile è selezionata e il topo non la sta ancora usando.")]
        [TextArea(1, 3)] public string useWheel = "Use wheel";

        [Tooltip("Mostrato mentre il topo sta usando una ruota, per indicare il comando che interrompe l'utilizzo.")]
        [TextArea(1, 3)] public string leaveWheel = "Leave wheel";

        [Tooltip("Mostrato quando il topo può mordere una corda raggiungibile con il comando Testa.")]
        [TextArea(1, 3)] public string gnaw = "Hold to gnaw";

        [Tooltip("Mostrato quando le Mani possono iniziare o continuare lo scavo. Le altre interazioni delle mani hanno precedenza prima di iniziare. Include le icone direzionali.")]
        [TextArea(1, 3)] public string dig = "Hold to dig";
    }
}
