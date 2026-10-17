# Gerarchia di LoopScene

La scena e organizzata in quattro gruppi principali:

- Systems: Game Manager (con i suoi Spawn Point), Main Camera, Global Light 2D, Gameplay Music e Gameplay HUD.
- Level: griglia delle stanze, terreno scavabile e Water Volume (prima chiamato Square).
- Actors: Player e Piranha.
- Interactables: Crates and Tethers, Ropes and Ladders, Dropping Platforms e Collectibles.

I gruppi aggiunti hanno posizione zero, rotazione identita e scala unitaria. Le posizioni degli oggetti, i dati delle tilemap, gli override dei prefab e i riferimenti dei componenti restano invariati. Le strutture esistenti delle stanze e degli esempi di casse/acqua/corde sono mantenute. I nuovi gruppi servono soltanto a organizzare la Hierarchy.

Per nuovi oggetti, usare il sottogruppo corrispondente. Gli Spawn Point che partecipano alla scelta iniziale devono comunque essere assegnati alla lista del Game Manager: la sola collocazione nella gerarchia non li registra.
