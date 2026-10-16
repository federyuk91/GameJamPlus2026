# Regole del progetto

Questo file contiene le regole generiche del progetto. Quando l'utente dice "aggiungi regola", aggiorna questo file con la regola indicata, mantenendo le altre istruzioni.

## Unity e C#

L'utente è uno sviluppatore Unity. Quando lavori su codice C# per Unity, comportati come un esperto di dominio Unity/C#.

### Stile

- Usa sempre tipi espliciti; evita `var`, salvo casi banali e altamente leggibili.
- Scrivi codice chiaro, modulare e mantenibile.
- Inserisci commenti brevi solo quando aggiungono valore: logica complessa, condizioni con più booleani, workaround, assunzioni o vincoli di Unity.
- Non commentare variabili, metodi o proprietà con nomi già autoesplicativi.

### Regole Unity

- Preferisci reference assegnate da Inspector tramite `[SerializeField]` o public rispetto a `GetComponent`, `Find`, `FindObjectOfType` o ricerche runtime.
- Usa `GetComponent` solo quando è locale, necessario e non ripetuto inutilmente.
- Quando è possibile usa eventi, callback o state machine.
- Mantieni separata la logica di dominio dalla UI e dai componenti MonoBehaviour quando possibile.

### Architettura

- Preferisci composizione rispetto a ereditarietà rigida.
- Usa interfacce quando aiutano a disaccoppiare sistemi.
- Applica SOLID con pragmatismo, senza creare astrazioni inutili.
- Usa pattern noti quando adatti al problema: State, Strategy, Observer/Event Bus, Command, Factory.
- Prima di refactor ampi, proponi un piano sintetico e segnala i rischi.

## Test

- Non testare quello che puoi far testare a me.

## Output

- Quando modifichi codice, spiega brevemente cosa hai cambiato e perché.
- Evidenzia eventuali breaking change, assunzioni o punti da testare in Unity.
