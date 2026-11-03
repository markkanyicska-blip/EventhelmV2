BEGIN;

ALTER TABLE "EventOfficialAssignments"
    ADD COLUMN IF NOT EXISTS "GroupName" text,
    ADD COLUMN IF NOT EXISTS "SubgroupName" text,
    ADD COLUMN IF NOT EXISTS "Task" text,
    ADD COLUMN IF NOT EXISTS "AssignmentOrder" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "Source" text,
    ADD COLUMN IF NOT EXISTS "ImportKey" character varying(64),
    ADD COLUMN IF NOT EXISTS "ImportBatchId" uuid,
    ADD COLUMN IF NOT EXISTS "SourceRowNumber" integer;

CREATE TABLE IF NOT EXISTS "EventAssignmentImportBatches"
(
    "Id" uuid PRIMARY KEY,
    "EventId" uuid NOT NULL,
    "FileName" text NOT NULL,
    "ReplacePreviousImport" boolean NOT NULL DEFAULT false,
    "Status" character varying(30) NOT NULL DEFAULT 'Processing',
    "ImportedRows" integer NOT NULL DEFAULT 0,
    "SkippedRows" integer NOT NULL DEFAULT 0,
    "CreatedOfficials" integer NOT NULL DEFAULT 0,
    "UpdatedOfficials" integer NOT NULL DEFAULT 0,
    "CreatedBoats" integer NOT NULL DEFAULT 0,
    "CreatedAssignments" integer NOT NULL DEFAULT 0,
    "UpdatedAssignments" integer NOT NULL DEFAULT 0,
    "RemovedAssignments" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CompletedAt" timestamp without time zone NULL
);

ALTER TABLE "EventAssignmentImportBatches"
    ADD COLUMN IF NOT EXISTS "ReplacePreviousImport" boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS "Status" character varying(30) NOT NULL DEFAULT 'Processing',
    ADD COLUMN IF NOT EXISTS "ImportedRows" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "SkippedRows" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "CreatedOfficials" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "UpdatedOfficials" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "CreatedBoats" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "CreatedAssignments" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "UpdatedAssignments" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "RemovedAssignments" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ADD COLUMN IF NOT EXISTS "CompletedAt" timestamp without time zone NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'FK_EventAssignmentImportBatches_Events_EventId'
    ) THEN
        ALTER TABLE "EventAssignmentImportBatches"
            ADD CONSTRAINT "FK_EventAssignmentImportBatches_Events_EventId"
            FOREIGN KEY ("EventId")
            REFERENCES "Events" ("Id")
            ON DELETE CASCADE;
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'FK_EventOfficialAssignments_ImportBatch'
    ) THEN
        ALTER TABLE "EventOfficialAssignments"
            ADD CONSTRAINT "FK_EventOfficialAssignments_ImportBatch"
            FOREIGN KEY ("ImportBatchId")
            REFERENCES "EventAssignmentImportBatches" ("Id")
            ON DELETE SET NULL;
    END IF;
END
$$;

CREATE INDEX IF NOT EXISTS
    "IX_EventOfficialAssignments_EventId_GroupName"
ON "EventOfficialAssignments" ("EventId", "GroupName");

CREATE INDEX IF NOT EXISTS
    "IX_EventAssignmentImportBatches_EventId"
ON "EventAssignmentImportBatches" ("EventId");

DROP INDEX IF EXISTS "UX_EventOfficialAssignments_EventId_ImportKey";

CREATE UNIQUE INDEX
    "UX_EventOfficialAssignments_EventId_ImportKey"
ON "EventOfficialAssignments" ("EventId", "ImportKey");

COMMIT;
