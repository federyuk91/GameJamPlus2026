# PlayerStats

`PlayerStats` sul prefab Player riunisce lo stato vitale della singola cavia:

- Vita: cinque cuori, danni e ripristino della vita conservata in GameSession.
- Sazietà: consumo ordinario e consumo specifico della ruota.
- Veleno: il formaggio verde riempie la sazietà, infligge il danno iniziale e applica i cinque consumi periodici. Il formaggio normale cura il veleno.
- Elettricità: accumulo e decadimento delle cariche, soglia letale e paralisi durante la scarica.
- Ossigeno: immersione del punto di respirazione, consumo, recupero e avviso di ossigeno basso.

I parametri restano sul player, con tooltip nell'Inspector. I flag Satiety Enabled, Oxygen Enabled ed Electricity Enabled permettono di disattivare un sistema nelle scene che lo richiedono. BalloonAscent mantiene disattivati sazietà e ossigeno.

La HUD legge un solo riferimento Stats e riceve eventi distinti per risorse, danni e consumi. Voci, bolle, animazioni e flash rimangono nei componenti di presentazione. Trappole e piranha applicano i propri effetti attraverso PlayerStats; StageEndManager continua a gestire gli esiti e i relativi messaggi.

La migrazione conserva valori, callback On Low Oxygen e riferimenti in prefab e scene operative. PlayerHealth, PlayerSatiety, PlayerElectricity e PlayerOxygen sono stati rimossi dagli asset operativi; i relativi script rimangono temporaneamente come classi obsolete vuote in attesa dell'approvazione per eliminarli insieme ai loro .meta.

Non sono stati eseguiti test o Play Mode. In Unity verificare consumo e recupero dell'ossigeno, avviso di ossigeno basso, veleno e cura, paralisi, danni e aggiornamento della HUD.
