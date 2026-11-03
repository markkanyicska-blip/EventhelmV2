-- EventHelm: official qualification fields
-- Futtasd egyszer a PostgreSQL adatbázison.

ALTER TABLE "Officials"
    ADD COLUMN IF NOT EXISTS "OrganizerQualification" text NULL,
    ADD COLUMN IF NOT EXISTS "JudgeQualification" text NULL,
    ADD COLUMN IF NOT EXISTS "MeasurerQualification" text NULL,
    ADD COLUMN IF NOT EXISTS "IsCompetitionAdministrator" boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS "IsResultScorer" boolean NOT NULL DEFAULT false;

-- Régi rendezői minősítés átvitele.
UPDATE "Officials"
SET "OrganizerQualification" = "Qualification"
WHERE "OrganizerQualification" IS NULL
  AND "Qualification" IS NOT NULL;

-- A régi oszlopot biztonsági okból egyelőre megtartjuk.
-- Később törölhető:
-- ALTER TABLE "Officials" DROP COLUMN "Qualification";
