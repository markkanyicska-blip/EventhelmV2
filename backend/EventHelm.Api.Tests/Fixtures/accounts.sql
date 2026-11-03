-- Test-only schema snapshot for a newly created disposable PostgreSQL container.
-- Not a migration or deployment input. Keep constraints aligned with the prepared
-- account migrations without loading or executing those scripts.
CREATE TABLE "Users" (
    "Id" uuid PRIMARY KEY,
    "Name" text NOT NULL,
    "Email" text NOT NULL UNIQUE,
    "Phone" text NULL,
    "Role" text NOT NULL DEFAULT 'User',
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "Accounts" (
    "Id" uuid PRIMARY KEY,
    "UserId" uuid NOT NULL REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    "Status" text NOT NULL DEFAULT 'Pending',
    "PasswordHash" text NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Version" bigint NOT NULL DEFAULT 0,
    UNIQUE ("UserId"),
    CHECK ("Status" IN ('Pending', 'Active', 'Suspended')),
    CHECK (
        ("Status" <> 'Active' OR ("PasswordHash" IS NOT NULL AND length("PasswordHash") > 0))
        AND ("Status" <> 'Pending' OR "PasswordHash" IS NULL)
    ),
    CHECK ("Version" >= 0),
    CHECK ("UpdatedAt" >= "CreatedAt")
);

CREATE TABLE "AccountInvitations" (
    "Id" uuid PRIMARY KEY,
    "AccountId" uuid NOT NULL REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    "TokenHash" text NOT NULL UNIQUE,
    "CreatedAt" timestamptz NOT NULL,
    "ExpiresAt" timestamptz NOT NULL,
    "RedeemedAt" timestamptz NULL,
    "RevokedAt" timestamptz NULL,
    CHECK ("TokenHash" ~ '^[0-9A-F]{64}$'),
    CHECK ("ExpiresAt" > "CreatedAt" AND "ExpiresAt" <= "CreatedAt" + interval '7 days'),
    CHECK (
        NOT ("RedeemedAt" IS NOT NULL AND "RevokedAt" IS NOT NULL)
        AND ("RedeemedAt" IS NULL OR ("RedeemedAt" >= "CreatedAt" AND "RedeemedAt" < "ExpiresAt"))
        AND ("RevokedAt" IS NULL OR "RevokedAt" >= "CreatedAt")
    )
);
CREATE INDEX "IX_AccountInvitations_AccountId" ON "AccountInvitations" ("AccountId");

CREATE TABLE "AccountAuditEvents" (
    "Id" uuid PRIMARY KEY,
    "AccountId" uuid NOT NULL REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    "InvitationId" uuid NULL REFERENCES "AccountInvitations" ("Id") ON DELETE RESTRICT,
    "EventType" text NOT NULL CHECK ("EventType" IN (
        'AccountCreated', 'InvitationIssued', 'InvitationRevoked', 'InvitationRedeemed')),
    "OccurredAt" timestamptz NOT NULL,
    CHECK (("EventType" = 'AccountCreated') = ("InvitationId" IS NULL))
);
CREATE INDEX "IX_AccountAuditEvents_AccountId" ON "AccountAuditEvents" ("AccountId");

CREATE TABLE "AccountSecurity" (
    "AccountId" uuid PRIMARY KEY REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    "LoginEmail" text NOT NULL UNIQUE,
    "SecurityVersion" bigint NOT NULL DEFAULT 0 CHECK ("SecurityVersion" >= 0),
    CHECK (length("LoginEmail") BETWEEN 3 AND 254 AND "LoginEmail" = lower(btrim("LoginEmail")))
);
CREATE TABLE "AccountSessions" (
    "TokenHash" text PRIMARY KEY CHECK ("TokenHash" ~ '^[0-9A-F]{64}$'),
    "AccountId" uuid NOT NULL REFERENCES "AccountSecurity" ("AccountId") ON DELETE RESTRICT,
    "AccountVersion" bigint NOT NULL CHECK ("AccountVersion" >= 0),
    "SecurityVersion" bigint NOT NULL CHECK ("SecurityVersion" >= 0),
    "CreatedAt" timestamptz NOT NULL,
    "ExpiresAt" timestamptz NOT NULL,
    "RevokedAt" timestamptz NULL,
    CHECK ("ExpiresAt" > "CreatedAt" AND "ExpiresAt" <= "CreatedAt" + interval '8 hours'),
    CHECK ("RevokedAt" IS NULL OR "RevokedAt" >= "CreatedAt")
);
CREATE INDEX "IX_AccountSessions_AccountId" ON "AccountSessions" ("AccountId");
CREATE INDEX "IX_AccountSessions_ExpiresAt" ON "AccountSessions" ("ExpiresAt");
CREATE TABLE "AccountLoginAttempts" (
    "IdentifierHash" text PRIMARY KEY CHECK ("IdentifierHash" ~ '^[0-9A-F]{64}$'),
    "WindowStartedAt" timestamptz NOT NULL,
    "Attempts" integer NOT NULL CHECK ("Attempts" BETWEEN 1 AND 10),
    "Failures" integer NOT NULL CHECK ("Failures" >= 0),
    "LockedUntil" timestamptz NULL
);
