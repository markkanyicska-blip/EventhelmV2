# EventHelm PostgreSQL inicializálás

Másold az `init.sql` fájlt ide:

`C:\Users\USER\Desktop\EventhelmV2\database\init.sql`

Majd futtasd a saját adatbázisneveddel:

```powershell
psql -U postgres -d eventhelm -f C:\Users\USER\Desktop\EventhelmV2\database\init.sql
```

A felhasználónevet és az adatbázisnevet az
`backend\EventHelm.Api\appsettings.json` fájl
`DefaultConnection` értékéhez igazítsd.

pgAdminban is futtatható a Query Toolból.

A script létrehozza:

- `"Users"`
- `"Events"`
- `"Officials"`
- `"Boats"`

és néhány mintaadatot is beszúr.
