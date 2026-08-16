# EventHelm teljes frontend csomag

Ez a csomag közvetlenül bemásolható a
`frontend/eventhelm-admin` mappába.

## Mit tartalmaz?

- teljes `src` mappa;
- új routing és layout;
- Sidebar és Topbar;
- Dashboard és Race Control panel;
- Events, Boats, Officials és Users CRUD-oldalak;
- közös UI-komponensek;
- központi Axios API-kliens;
- Health API;
- `.env.development`;
- grafit–arany EventHelm dizájn;
- Lucide ikonok, emoji nélkül.

## Beillesztés

1. Készíts Git commitot vagy biztonsági másolatot.
2. A ZIP teljes tartalmát másold ide:

   `C:\Users\USER\Desktop\EventhelmV2\frontend\eventhelm-admin`

3. Engedélyezd a fájlok felülírását.
4. A frontend mappában futtasd:

```powershell
npm install
npm run build
npm run dev
```

## Backend

A csomag alapértelmezetten ezt használja:

`/api`

Ha a backend más porton fut, módosítsd:

`.env.development`

példa:

```env
VITE_API_URL=http://localhost:5105/api
```

A `.env.development` módosítása után indítsd újra a Vite-ot.
