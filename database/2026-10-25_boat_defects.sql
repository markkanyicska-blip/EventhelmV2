CREATE TABLE IF NOT EXISTS "BoatDefects" (
    "Id" uuid PRIMARY KEY,
    "BoatId" uuid NOT NULL REFERENCES "Boats"("Id"),
    "ReportedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ReportedBy" text NOT NULL,
    "Description" text NOT NULL,
    "Priority" text NOT NULL DEFAULT 'Normal',
    "Status" text NOT NULL DEFAULT 'Reported',
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "CK_BoatDefects_Priority" CHECK ("Priority" IN ('Low', 'Normal', 'High', 'Critical')),
    CONSTRAINT "CK_BoatDefects_Status" CHECK ("Status" IN ('Reported', 'InProgress', 'Resolved'))
);
CREATE INDEX IF NOT EXISTS "IX_BoatDefects_BoatId" ON "BoatDefects" ("BoatId");
