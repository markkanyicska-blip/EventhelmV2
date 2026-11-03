# EventHelm assignment import fix

1. Futtasd a `database/2026-07-18_assignment_import_schema_alignment.sql` fájlt az `eventhelm` adatbázison.
2. Alkalmazd az `EventAssignmentImportService.patch` módosítást.
3. Ellenőrizd a Program.cs-ben:

```csharp
builder.Services.AddScoped<IEventAssignmentImportService, EventAssignmentImportService>();
```

4. Futtasd:

```powershell
dotnet build
```
