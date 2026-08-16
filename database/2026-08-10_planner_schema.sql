BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS "EventAreas"
(
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "EventId" uuid NOT NULL REFERENCES "Events"("Id") ON DELETE CASCADE,
    "Name" text NOT NULL,
    "Code" text NULL,
    "Notes" text NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_EventAreas_EventId_Name"
ON "EventAreas" ("EventId","Name");

CREATE TABLE IF NOT EXISTS "EventTasks"
(
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "EventId" uuid NOT NULL REFERENCES "Events"("Id") ON DELETE CASCADE,
    "Name" text NOT NULL,
    "Code" text NULL,
    "Description" text NULL,
    "RequiredOfficials" integer NOT NULL DEFAULT 1 CHECK ("RequiredOfficials" >= 0),
    "RequiredBoats" integer NOT NULL DEFAULT 0 CHECK ("RequiredBoats" >= 0),
    "RequiredQualification" text NULL,
    "Priority" text NOT NULL DEFAULT 'Normal',
    "Notes" text NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_EventTasks_EventId_Name"
ON "EventTasks" ("EventId","Name");

CREATE TABLE IF NOT EXISTS "EventBoats"
(
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "EventId" uuid NOT NULL REFERENCES "Events"("Id") ON DELETE CASCADE,
    "BoatId" uuid NOT NULL REFERENCES "Boats"("Id") ON DELETE RESTRICT,
    "DisplayName" text NULL,
    "Code" text NULL,
    "Status" text NOT NULL DEFAULT 'Planned',
    "Notes" text NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_EventBoats_EventId_BoatId"
ON "EventBoats" ("EventId","BoatId");

CREATE TABLE IF NOT EXISTS "EventOfficials"
(
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "EventId" uuid NOT NULL REFERENCES "Events"("Id") ON DELETE CASCADE,
    "OfficialId" uuid NOT NULL REFERENCES "Officials"("Id") ON DELETE RESTRICT,
    "Role" text NULL,
    "AttendanceStatus" text NOT NULL DEFAULT 'Planned',
    "Notes" text NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_EventOfficials_EventId_OfficialId"
ON "EventOfficials" ("EventId","OfficialId");

CREATE TABLE IF NOT EXISTS "EventShifts"
(
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "EventId" uuid NOT NULL REFERENCES "Events"("Id") ON DELETE CASCADE,
    "Name" text NOT NULL,
    "StartAt" timestamptz NOT NULL,
    "EndAt" timestamptz NOT NULL,
    "Location" text NULL,
    "Notes" text NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT "CK_EventShifts_TimeRange" CHECK ("EndAt" > "StartAt")
);

CREATE INDEX IF NOT EXISTS "IX_EventShifts_EventId_StartAt"
ON "EventShifts" ("EventId","StartAt");

CREATE TABLE IF NOT EXISTS "EventBoatCrew"
(
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "EventId" uuid NOT NULL REFERENCES "Events"("Id") ON DELETE CASCADE,
    "EventBoatId" uuid NOT NULL REFERENCES "EventBoats"("Id") ON DELETE CASCADE,
    "OfficialId" uuid NOT NULL REFERENCES "Officials"("Id") ON DELETE RESTRICT,
    "Role" text NOT NULL DEFAULT 'Rendező',
    "IsCaptain" boolean NOT NULL DEFAULT false,
    "FromDate" timestamptz NULL,
    "ToDate" timestamptz NULL,
    "Notes" text NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT "CK_EventBoatCrew_TimeRange"
        CHECK ("FromDate" IS NULL OR "ToDate" IS NULL OR "ToDate" > "FromDate")
);

CREATE INDEX IF NOT EXISTS "IX_EventBoatCrew_EventBoatId"
ON "EventBoatCrew" ("EventBoatId");

CREATE INDEX IF NOT EXISTS "IX_EventBoatCrew_OfficialId"
ON "EventBoatCrew" ("OfficialId");

CREATE TABLE IF NOT EXISTS "EventAssignments"
(
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "EventId" uuid NOT NULL REFERENCES "Events"("Id") ON DELETE CASCADE,
    "OfficialId" uuid NOT NULL REFERENCES "Officials"("Id") ON DELETE RESTRICT,
    "EventTaskId" uuid NULL REFERENCES "EventTasks"("Id") ON DELETE SET NULL,
    "EventBoatId" uuid NULL REFERENCES "EventBoats"("Id") ON DELETE SET NULL,
    "EventAreaId" uuid NULL REFERENCES "EventAreas"("Id") ON DELETE SET NULL,
    "AssignmentGroupId" uuid NULL,
    "Title" text NOT NULL,
    "Role" text NULL,
    "StartAt" timestamptz NOT NULL,
    "EndAt" timestamptz NOT NULL,
    "Status" text NOT NULL DEFAULT 'Planned',
    "Notes" text NULL,
    "IsLocked" boolean NOT NULL DEFAULT false,
    "ActualStartAt" timestamptz NULL,
    "ActualEndAt" timestamptz NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT "CK_EventAssignments_TimeRange" CHECK ("EndAt" > "StartAt"),
    CONSTRAINT "CK_EventAssignments_ActualTimeRange"
        CHECK ("ActualStartAt" IS NULL OR "ActualEndAt" IS NULL OR "ActualEndAt" > "ActualStartAt")
);

CREATE INDEX IF NOT EXISTS "IX_EventAssignments_EventId"
ON "EventAssignments" ("EventId");

CREATE INDEX IF NOT EXISTS "IX_EventAssignments_OfficialId"
ON "EventAssignments" ("OfficialId");

CREATE INDEX IF NOT EXISTS "IX_EventAssignments_EventTaskId"
ON "EventAssignments" ("EventTaskId");

CREATE INDEX IF NOT EXISTS "IX_EventAssignments_EventBoatId"
ON "EventAssignments" ("EventBoatId");

CREATE INDEX IF NOT EXISTS "IX_EventAssignments_EventAreaId"
ON "EventAssignments" ("EventAreaId");

CREATE INDEX IF NOT EXISTS "IX_EventAssignments_AssignmentGroupId"
ON "EventAssignments" ("AssignmentGroupId");

CREATE INDEX IF NOT EXISTS "IX_EventAssignments_EventId_StartAt_EndAt"
ON "EventAssignments" ("EventId","StartAt","EndAt");

CREATE INDEX IF NOT EXISTS "IX_EventAssignments_OfficialId_StartAt_EndAt"
ON "EventAssignments" ("OfficialId","StartAt","EndAt");

COMMIT;
