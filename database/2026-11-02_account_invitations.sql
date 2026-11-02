-- Preparation only. Never run on application startup.
-- Requires 2026-11-02_account_foundations.sql; see docs/account-foundations.md.
BEGIN;
SET LOCAL lock_timeout = '5s';

CREATE TABLE "AccountInvitations"
(
    "Id" uuid PRIMARY KEY,
    "AccountId" uuid NOT NULL REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    "TokenHash" text NOT NULL UNIQUE,
    "CreatedAt" timestamptz NOT NULL,
    "ExpiresAt" timestamptz NOT NULL,
    "RedeemedAt" timestamptz NULL,
    "RevokedAt" timestamptz NULL,
    CONSTRAINT "CK_AccountInvitations_Hash" CHECK ("TokenHash" ~ '^[0-9A-F]{64}$'),
    CONSTRAINT "CK_AccountInvitations_Expiration" CHECK (
        "ExpiresAt" > "CreatedAt" AND "ExpiresAt" <= "CreatedAt" + interval '7 days'),
    CONSTRAINT "CK_AccountInvitations_Terminal" CHECK (
        NOT ("RedeemedAt" IS NOT NULL AND "RevokedAt" IS NOT NULL)
        AND ("RedeemedAt" IS NULL OR
            ("RedeemedAt" >= "CreatedAt" AND "RedeemedAt" < "ExpiresAt"))
        AND ("RevokedAt" IS NULL OR "RevokedAt" >= "CreatedAt")
    )
);
CREATE INDEX "IX_AccountInvitations_AccountId" ON "AccountInvitations" ("AccountId");

CREATE TABLE "AccountAuditEvents"
(
    "Id" uuid PRIMARY KEY,
    "AccountId" uuid NOT NULL REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    "InvitationId" uuid NULL REFERENCES "AccountInvitations" ("Id") ON DELETE RESTRICT,
    "EventType" text NOT NULL CHECK ("EventType" IN (
        'AccountCreated', 'InvitationIssued', 'InvitationRevoked', 'InvitationRedeemed')),
    "OccurredAt" timestamptz NOT NULL,
    CONSTRAINT "CK_AccountAuditEvents_Invitation" CHECK (
        ("EventType" = 'AccountCreated') = ("InvitationId" IS NULL))
);
CREATE INDEX "IX_AccountAuditEvents_AccountId" ON "AccountAuditEvents" ("AccountId");

COMMIT;
