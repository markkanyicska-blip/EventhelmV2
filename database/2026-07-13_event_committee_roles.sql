BEGIN;

ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "JuryChair" text NULL,
    ADD COLUMN IF NOT EXISTS "TechnicalCommitteeChair" text NULL;

COMMIT;
