-- Preparation only. DO NOT EXECUTE in Phase 3 Step 1 or at application startup.
-- Requires account foundations and existing Events. No grants, seeds or backfills.
BEGIN;
SET LOCAL lock_timeout = '5s';

-- Global roles are intentionally separate: an event role can never encode an administrator.
CREATE TABLE "GlobalRoleAssignments" (
    "Id" uuid PRIMARY KEY,
    "AccountId" uuid NOT NULL REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    "Role" integer NOT NULL CHECK ("Role" = 1),
    "GrantedByAccountId" uuid NOT NULL REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    "GrantedAt" timestamptz NOT NULL,
    "ExpiresAt" timestamptz NULL,
    "RevokedAt" timestamptz NULL,
    CHECK ("ExpiresAt" IS NULL OR "ExpiresAt" > "GrantedAt")
);
CREATE INDEX "IX_GlobalRoleAssignments_AccountId" ON "GlobalRoleAssignments" ("AccountId");

-- EventRole: 1 Organizer, 2 Race Officer, 3 Technical Staff, 4 On-Water Personnel.
CREATE TABLE "EventRoleAssignments" (
    "Id" uuid PRIMARY KEY,
    "AccountId" uuid NOT NULL REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    "EventId" uuid NOT NULL REFERENCES "Events" ("Id") ON DELETE RESTRICT,
    "Role" integer NOT NULL CHECK ("Role" IN (1, 2, 3, 4)),
    "GrantedByAccountId" uuid NOT NULL REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    "GrantedAt" timestamptz NOT NULL,
    "ExpiresAt" timestamptz NULL,
    "RevokedAt" timestamptz NULL,
    CHECK ("ExpiresAt" IS NULL OR "ExpiresAt" > "GrantedAt")
);
CREATE INDEX "IX_EventRoleAssignments_AccountEvent" ON "EventRoleAssignments" ("AccountId", "EventId");

-- Permission enum values are a versioned storage contract, not arbitrary role claims.
-- 101/102 personal read/manage; 103/104 medical read/manage; 105/106 private document read/manage.
CREATE TABLE "SensitivePermissionGrants" (
    "Id" uuid PRIMARY KEY,
    "AccountId" uuid NOT NULL REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    "EventId" uuid NOT NULL REFERENCES "Events" ("Id") ON DELETE RESTRICT,
    "Permission" integer NOT NULL CHECK ("Permission" IN (101, 102, 103, 104, 105, 106)),
    "Purpose" text NOT NULL CHECK (length(btrim("Purpose")) BETWEEN 1 AND 500),
    "GrantedByAccountId" uuid NOT NULL REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    "GrantedAt" timestamptz NOT NULL,
    "ExpiresAt" timestamptz NOT NULL CHECK ("ExpiresAt" > "GrantedAt"),
    "RevokedAt" timestamptz NULL
);
CREATE INDEX "IX_SensitivePermissionGrants_AccountEvent"
    ON "SensitivePermissionGrants" ("AccountId", "EventId");

COMMIT;
