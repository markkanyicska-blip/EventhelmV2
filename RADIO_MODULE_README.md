# EventHelm rádió-hozzárendelési modul

A ZIP teljes tartalmát másold ide, és engedélyezd a felülírást:

`C:\Users\USER\Desktop\EventhelmV2`

Ne hozz létre újabb `EventhelmV2\EventhelmV2` vagy `src\src` mappát.

## Ellenőrzés

Backend:

```powershell
cd C:\Users\USER\Desktop\EventhelmV2\backend\EventHelm.Api
dotnet build
dotnet run
```

Frontend, külön PowerShellben:

```powershell
cd C:\Users\USER\Desktop\EventhelmV2\frontend\eventhelm-admin
npm run build
npm run dev
```

Új menüpont: `Rádiókiosztás`.

Az adatok automatikusan itt jönnek létre:

`backend\EventHelm.Api\App_Data\radio-assignments.json`
