# Collectible progress

GameSession conserva i totali di Cheese, GreenCheese, Photo e Knowledge tra gli universi della stessa partita. Il reload di LoopScene ricrea il Player ma non azzera i contatori. Nuova partita e l'avvio di una nuova sessione Play Mode li riportano a zero; non vengono salvati su disco.

| Slot HUD | Tipo | Valore enum |
| --- | --- | --- |
| Primary | Cheese | 0 |
| Secondary | GreenCheese | 1 |
| Tertiary | Knowledge | 3 |
| Quaternary | Photo | 2 |

I valori enum già salvati restano invariati. Le foto conservano lo slot precedentemente assegnato; lo slot libero diventa Ricerca/Conoscenza.

CollectibleCollector.GetCount legge i totali della sessione. La raccolta incrementa il totale prima degli eventi del raccoglibile, quindi formaggio verde e foto vengono registrati prima del fine test. Il conteggio non è un inventario di oggetti unici: i raccoglibili della scena continuano a ricomparire al reload.

Per assegnare conoscenza si può impostare Collectible → Type = Knowledge, ottenendo un punto per raccolta, oppure chiamare CollectibleCollector.AddKnowledge(int amount) da un UnityEvent. Non vengono assegnati automaticamente punti conoscenza alle altre azioni.

La lampadina è un'icona provvisoria disegnata dalla UI quando Gameplay HUD → Counter Icons → Element 2 è vuoto. Assegnando uno sprite a quel campo la si sostituisce. Gli Initial Counter Values dimostrativi sono stati rimossi: tutti i contatori partono dai totali di GameSession.

Da verificare in Play Mode: raccolta normale, raccolta verde con fine test, foto con fine test, reload di LoopScene e Nuova partita dal menu.
