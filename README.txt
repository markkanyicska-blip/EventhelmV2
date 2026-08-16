EventHelm Official módosítás

1. Másold az OfficialService.cs fájlt ide:
   EventHelm.Api/Services/OfficialService.cs

2. Futtasd le egyszer PostgreSQL-en:
   official_qualifications_migration.sql

3. Ellenőrizd az Official.cs új mezőit:
   OrganizerQualification
   JudgeQualification
   MeasurerQualification
   IsCompetitionAdministrator
   IsResultScorer

4. Indítsd újra a backendet, majd teszteld a létrehozást és módosítást.

Megjegyzés:
A QualificationValidUntil és QualificationDocumentUrl a jelenlegi modellben
a rendezői minősítéshez tartozik.
