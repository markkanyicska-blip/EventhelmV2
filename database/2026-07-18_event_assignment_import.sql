BEGIN;

ALTER TABLE "EventOfficialAssignments"
    ADD COLUMN IF NOT EXISTS "GroupName" text,
    ADD COLUMN IF NOT EXISTS "SubgroupName" text,
    ADD COLUMN IF NOT EXISTS "Task" text,
    ADD COLUMN IF NOT EXISTS "AssignmentOrder" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "BoatId" uuid,
    ADD COLUMN IF NOT EXISTS "Source" text,
    ADD COLUMN IF NOT EXISTS "ImportKey" text,
    ADD COLUMN IF NOT EXISTS "ImportBatchId" uuid,
    ADD COLUMN IF NOT EXISTS "SourceRowNumber" integer,
    ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamptz NOT NULL DEFAULT now();

-- A rendező egy eseményen több külön beosztást is kaphat.
DROP INDEX IF EXISTS "UX_EventOfficialAssignments_EventId_OfficialId";

CREATE TABLE IF NOT EXISTS "EventAssignmentImportBatches"
(
    "Id" uuid PRIMARY KEY,
    "EventId" uuid NOT NULL REFERENCES "Events"("Id") ON DELETE CASCADE,
    "FileName" text NOT NULL,
    "ReplacePreviousImport" boolean NOT NULL DEFAULT false,
    "Status" text NOT NULL DEFAULT 'Processing',
    "ImportedRows" integer NOT NULL DEFAULT 0,
    "SkippedRows" integer NOT NULL DEFAULT 0,
    "CreatedOfficials" integer NOT NULL DEFAULT 0,
    "UpdatedOfficials" integer NOT NULL DEFAULT 0,
    "CreatedBoats" integer NOT NULL DEFAULT 0,
    "CreatedAssignments" integer NOT NULL DEFAULT 0,
    "UpdatedAssignments" integer NOT NULL DEFAULT 0,
    "RemovedAssignments" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CompletedAt" timestamptz
);

ALTER TABLE "EventAssignmentImportBatches"
    ADD COLUMN IF NOT EXISTS "ReplacePreviousImport" boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS "Status" text NOT NULL DEFAULT 'Processing',
    ADD COLUMN IF NOT EXISTS "SkippedRows" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "CreatedAssignments" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "UpdatedAssignments" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "RemovedAssignments" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "CompletedAt" timestamptz;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'FK_EventOfficialAssignments_Boats_BoatId'
    ) THEN
        ALTER TABLE "EventOfficialAssignments"
            ADD CONSTRAINT "FK_EventOfficialAssignments_Boats_BoatId"
            FOREIGN KEY ("BoatId") REFERENCES "Boats"("Id")
            ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'FK_EventOfficialAssignments_ImportBatches_ImportBatchId'
    ) THEN
        ALTER TABLE "EventOfficialAssignments"
            ADD CONSTRAINT "FK_EventOfficialAssignments_ImportBatches_ImportBatchId"
            FOREIGN KEY ("ImportBatchId") REFERENCES "EventAssignmentImportBatches"("Id")
            ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS "IX_EventOfficialAssignments_EventId"
    ON "EventOfficialAssignments" ("EventId");

CREATE INDEX IF NOT EXISTS "IX_EventOfficialAssignments_OfficialId"
    ON "EventOfficialAssignments" ("OfficialId");

CREATE INDEX IF NOT EXISTS "IX_EventOfficialAssignments_BoatId"
    ON "EventOfficialAssignments" ("BoatId");

CREATE INDEX IF NOT EXISTS "IX_EventOfficialAssignments_ImportBatchId"
    ON "EventOfficialAssignments" ("ImportBatchId");

-- Csak az importált, ImportKey-vel rendelkező sorokra vonatkozik.
DROP INDEX IF EXISTS "UX_EventOfficialAssignments_EventId_ImportKey";

CREATE UNIQUE INDEX IF NOT EXISTS "UX_EventOfficialAssignments_EventId_ImportKey"
    ON "EventOfficialAssignments" ("EventId", "ImportKey")
    WHERE "ImportKey" IS NOT NULL;

CREATE INDEX IF NOT EXISTS "IX_EventAssignmentImportBatches_EventId_CreatedAt"
    ON "EventAssignmentImportBatches" ("EventId", "CreatedAt" DESC);

COMMIT;
