-- Preparation only. Do not execute in Phase 2B Step 4 or at application startup.
-- Requires the account foundations migration; does not alter existing records.
BEGIN;
SET LOCAL lock_timeout = '5s';

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

COMMIT;
