# EventHelm V2 – szervizmodul telepítése

A ZIP tartalma az EventhelmV2 projekt gyökeréhez viszonyított mappaszerkezetet követi.

1. Készíts biztonsági másolatot a projektedről.
2. A ZIP-ben lévő `backend`, `frontend` és `database` mappák tartalmát másold az `EventhelmV2` gyökérmappájába, a meglévő fájlok felülírását engedélyezve. A ZIP kizárólag a felsorolt új/módosított forrásfájlokat tartalmazza.
3. A `database/2026-10-25_boat_defects.sql` fájlt **külön** futtasd a megfelelő Railway PostgreSQL-adatbázison, előzetes mentés és a kapcsolat ellenőrzése után. A fájl nem tartalmaz adatvesztést okozó DROP/DELETE utasítást. A ZIP bemásolása önmagában nem futtatja le.
4. Ellenőrizd helyben a frontend buildet és a backend buildet, majd telepítsd a megfelelő Git-ágra. A változtatásokat ne `git add .` paranccsal add hozzá, mert a projektben titkos fájlok/mentések is lehetnek.
5. Az alkalmazásban nyiss meg egy hajót, válaszd a Karbantartás fület, és próbálj ki egy hibabejelentést, majd állapotváltást. Frissítsd az oldalt a mentés ellenőrzéséhez.

**Fontos:** A feltöltött projektben `.env`, Git-metaadatok és adatbázismentések is voltak. Ezeket a csomag nem tartalmazza. Ha az eredeti ZIP-et másokkal megosztottad, érdemes a benne szereplő titkokat cserélni.

**Korlátok:** A felület a meglévő API-hozzáférési szabályokat örökli; nincs külön jogosultságkezelés a hibákhoz. Nincs PDF, QR-kód vagy fotófeltöltés. A bejelentési idő automatikusan kerül rögzítésre. A szolgáltatás használatához működő API és adatbázis szükséges; offline beküldés nincs.
