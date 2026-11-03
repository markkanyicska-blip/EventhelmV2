BEGIN;

ALTER TABLE "Officials"
    ADD COLUMN IF NOT EXISTS "Club" text NULL,
    ADD COLUMN IF NOT EXISTS "ShirtSize" text NULL,
    ADD COLUMN IF NOT EXISTS "ProfileImageUrl" text NULL,
    ADD COLUMN IF NOT EXISTS "Qualification" text NULL,
    ADD COLUMN IF NOT EXISTS "QualificationValidUntil" date NULL,
    ADD COLUMN IF NOT EXISTS "QualificationDocumentUrl" text NULL,
    ADD COLUMN IF NOT EXISTS "BoatLicenseType" text NULL,
    ADD COLUMN IF NOT EXISTS "BoatLicenseValidUntil" date NULL,
    ADD COLUMN IF NOT EXISTS "BoatLicenseDocumentUrl" text NULL,
    ADD COLUMN IF NOT EXISTS "FoodAllergies" text NULL,
    ADD COLUMN IF NOT EXISTS "DrugAllergies" text NULL,
    ADD COLUMN IF NOT EXISTS "HealthNotes" text NULL,
    ADD COLUMN IF NOT EXISTS "EmergencyContactName" text NULL,
    ADD COLUMN IF NOT EXISTS "EmergencyContactPhone" text NULL,
    ADD COLUMN IF NOT EXISTS "EmergencyContactRelation" text NULL,
    ADD COLUMN IF NOT EXISTS "Languages" text NULL,
    ADD COLUMN IF NOT EXISTS "OtherDocumentUrl" text NULL;

CREATE INDEX IF NOT EXISTS "IX_Officials_Club"
    ON "Officials" ("Club");

CREATE INDEX IF NOT EXISTS "IX_Officials_BoatLicenseValidUntil"
    ON "Officials" ("BoatLicenseValidUntil");

COMMIT;
