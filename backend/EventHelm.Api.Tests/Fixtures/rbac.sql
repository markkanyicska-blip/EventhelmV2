-- Test-only snapshot, not a deployment migration. Used only in a fresh container.
CREATE TABLE "Events" ("Id" uuid PRIMARY KEY);

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

-- Only for testing trusted scope resolution without accessing operational tables.
CREATE TABLE "SyntheticAuthorizationResources" (
    "Id" uuid PRIMARY KEY,
    "Kind" integer NOT NULL,
    "EventId" uuid NOT NULL REFERENCES "Events" ("Id"),
    "AssignedAccountId" uuid NULL REFERENCES "Accounts" ("Id")
);
