BEGIN;

CREATE TABLE IF NOT EXISTS "EventOfficialAssignments"
(
    "Id" uuid PRIMARY KEY,
    "EventId" uuid NOT NULL,
    "OfficialId" uuid NOT NULL,
    "Role" text NOT NULL,
    "TrunkRadioNumber" varchar(3) NULL,
    "VhfRadioNumber" varchar(4) NULL,
    "BoatId" uuid NULL,
    "AttendanceStatus" text NOT NULL DEFAULT 'Planned',
    "Notes" text NULL,
    "CreatedAt" timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "FK_EventOfficialAssignments_Events"
        FOREIGN KEY ("EventId")
        REFERENCES "Events" ("Id")
        ON DELETE CASCADE,

    CONSTRAINT "FK_EventOfficialAssignments_Officials"
        FOREIGN KEY ("OfficialId")
        REFERENCES "Officials" ("Id")
        ON DELETE RESTRICT,

    CONSTRAINT "FK_EventOfficialAssignments_Boats"
        FOREIGN KEY ("BoatId")
        REFERENCES "Boats" ("Id")
        ON DELETE SET NULL,

    CONSTRAINT "CK_EventOfficialAssignments_Role"
        CHECK (length(trim("Role")) > 0),

    CONSTRAINT "CK_EventOfficialAssignments_Attendance"
        CHECK (
            "AttendanceStatus" IN
            ('Planned', 'Present', 'Absent', 'Standby')
        ),

    CONSTRAINT "CK_EventOfficialAssignments_Trunk"
        CHECK (
            "TrunkRadioNumber" IS NULL
            OR "TrunkRadioNumber" ~ '^[0-9]{3}$'
        ),

    CONSTRAINT "CK_EventOfficialAssignments_Vhf"
        CHECK (
            "VhfRadioNumber" IS NULL
            OR "VhfRadioNumber" ~ '^[0-9]{4}$'
        )
);

CREATE UNIQUE INDEX IF NOT EXISTS
    "UX_EventOfficialAssignments_Event_Official"
ON "EventOfficialAssignments"
    ("EventId", "OfficialId");

CREATE UNIQUE INDEX IF NOT EXISTS
    "UX_EventOfficialAssignments_Event_Trunk"
ON "EventOfficialAssignments"
    ("EventId", "TrunkRadioNumber")
WHERE "TrunkRadioNumber" IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS
    "UX_EventOfficialAssignments_Event_Vhf"
ON "EventOfficialAssignments"
    ("EventId", "VhfRadioNumber")
WHERE "VhfRadioNumber" IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS
    "UX_EventOfficialAssignments_Event_Boat"
ON "EventOfficialAssignments"
    ("EventId", "BoatId")
WHERE "BoatId" IS NOT NULL;

CREATE INDEX IF NOT EXISTS
    "IX_EventOfficialAssignments_Event"
ON "EventOfficialAssignments"
    ("EventId");

CREATE INDEX IF NOT EXISTS
    "IX_EventOfficialAssignments_Attendance"
ON "EventOfficialAssignments"
    ("AttendanceStatus");

COMMIT;
