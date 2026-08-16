BEGIN;
ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "OrganizingClub" text NULL,
    ADD COLUMN IF NOT EXISTS "HostingClub" text NULL,
    ADD COLUMN IF NOT EXISTS "StartDate" timestamp NULL,
    ADD COLUMN IF NOT EXISTS "EndDate" timestamp NULL,
    ADD COLUMN IF NOT EXISTS "Location" text NULL,
    ADD COLUMN IF NOT EXISTS "WaterArea" text NULL,
    ADD COLUMN IF NOT EXISTS "RaceOfficer" text NULL,
    ADD COLUMN IF NOT EXISTS "RaceOffice" text NULL,
    ADD COLUMN IF NOT EXISTS "VhfChannel" text NULL,
    ADD COLUMN IF NOT EXISTS "TrunkGroup" text NULL,
    ADD COLUMN IF NOT EXISTS "Notes" text NULL;
CREATE INDEX IF NOT EXISTS "IX_Events_StartDate" ON "Events" ("StartDate");
COMMIT;
