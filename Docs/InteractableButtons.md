# Interactable Button

Trascina Assets/Prefabs/Interactable Button.prefab nella scena. Il pivot è alla base del pulsante. La grafica usa i tre frame di PressureButton_Sheet, mantenendo allineata la base durante la pressione.

Il topo deve essere dentro Interaction Area e avere una linea libera verso il pulsante. Premi F sulla tastiera o il pulsante ovest del gamepad (X Xbox / quadrato PlayStation). Non viene premuto automaticamente passando sopra.

In Interactable Button → On Pressed aggiungi le chiamate UnityEvent desiderate. L'evento viene invocato una volta per pressione valida. Animation Step Duration controlla la breve animazione; Single Use impedisce nuove pressioni e lascia il pulsante premuto. Di default è riutilizzabile dopo l'animazione.

Box Collider 2D → Size/Offset regola l'area di prossimità, mostrata in azzurro quando Show Gizmos è attivo. Il collider è un trigger e non blocca il movimento. Non occorrono Rigidbody2D, tag o liste manuali di pulsanti.

PlayerButtonInteraction è collegato nel prefab Player e nella GameplayInteractionAvailability di LoopScene. Se un raccoglibile e un pulsante condividono lo stesso input, viene scelto il più vicino; a parità di distanza mantiene priorità il raccoglibile. L'HUD mostra Press button e il comando corretto. La selezione rispetta le priorità già esistenti di casse, morso e scavo.

Da verificare in Play Mode: On Pressed assegnato, ostacolo tra topo e pulsante, più pulsanti vicini, raccoglibile vicino, uso singolo e riutilizzo.
