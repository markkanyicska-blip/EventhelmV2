-- Preparation only. Apply manually only after the prerequisites in
-- docs/account-foundations.md have been reviewed.
BEGIN;
SET LOCAL lock_timeout = '5s';

CREATE TABLE "Accounts"
(
    "Id" uuid PRIMARY KEY,
    "UserId" uuid NOT NULL,
    "Status" text NOT NULL DEFAULT 'Pending',
    "PasswordHash" text NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Version" bigint NOT NULL DEFAULT 0,
    CONSTRAINT "FK_Accounts_Users" FOREIGN KEY ("UserId")
        REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "UQ_Accounts_UserId" UNIQUE ("UserId"),
    CONSTRAINT "CK_Accounts_Status"
        CHECK ("Status" IN ('Pending', 'Active', 'Suspended')),
    CONSTRAINT "CK_Accounts_PasswordState" CHECK (
        ("Status" <> 'Active' OR
            ("PasswordHash" IS NOT NULL AND length("PasswordHash") > 0))
        AND ("Status" <> 'Pending' OR "PasswordHash" IS NULL)
    ),
    CONSTRAINT "CK_Accounts_Version" CHECK ("Version" >= 0),
    CONSTRAINT "CK_Accounts_Timestamps" CHECK ("UpdatedAt" >= "CreatedAt")
);

COMMIT;
