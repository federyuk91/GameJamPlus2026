# Uscita dal tunnel al Goal

Il prefab Goal abilita Use Exit Image nel suo TestEndEvent. Al contatto il gioco
si ferma come nella sequenza precedente: Exit1.png copre lo schermo, poi zooma
progressivamente verso il centro. Un'apertura procedurale bianca si espande
con un alone caldo, usando la stessa crescita del tunnel senza immagine.
L'esposizione globale aumenta solo leggermente, preservando le pareti.
Una sfumatura finale porta anche i pixel neri al bianco completo, prima della comunicazione dello
scienziato e dei risultati. Gli altri eventi di fine stage conservano la loro
presentazione, salvo attivare esplicitamente Use Exit Image.

Nel prefab Gameplay HUD, la sezione Exit Tunnel permette di configurare:

- Image e Shader: Exit1 e GameJamPlus/ExitTunnel, già assegnati.
- Duration: 4 secondi, durata predefinita per l'uscita con immagine.
- Final Zoom: 5 per impostazione iniziale.
- Exposure Stops: 1; luminosità globale fino a 2 volte quella iniziale.
- Whiteout Start: 0.88; punto della sequenza da cui sfuma al bianco puro.
- Light Strength: 1.25; intensità dell'alone caldo attorno all'apertura.
  Il bianco completo viene raggiunto al 95% della sequenza.

La luce si espande in coordinate schermo, mantenendo una forma circolare
indipendente dal ritaglio e dallo zoom della texture. Il centro arriva al bianco
prima delle pareti, che rimangono leggibili fino al passaggio dell'alone.

Le eventuali parti trasparenti dell'immagine rivelano luce bianca. Non occorre
modificare l'alpha per ottenere il bagliore: lo shader lo genera autonomamente.

Transition Duration Override nel TestEndEvent può sostituire questa durata per
un singolo evento. Il Goal usa -1 e quindi segue Duration della HUD. Per gli
altri eventi senza immagine il valore predefinito è Game Manager / Transition
Duration, attualmente 1.5 secondi. I nomi serializzati dei precedenti campi Flash
Duration e Flash Duration Override sono preservati, così gli override esistenti
restano validi. L'immagine mantiene le proporzioni coprendo tutto lo schermo, con
ritaglio centrale dove necessario, comprese le bande della camera pixel perfect.
Il rendering usa il tempo non scalato e continua mentre il gameplay è fermo.
Materiale e RenderTexture sono temporanei e vengono rilasciati con la HUD.
