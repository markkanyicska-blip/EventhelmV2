BEGIN;

CREATE TABLE IF NOT EXISTS "Users" (
    "Id" uuid PRIMARY KEY,
    "Name" text NOT NULL,
    "Email" text NOT NULL,
    "Phone" text NULL,
    "Role" text NOT NULL DEFAULT 'User',
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Users_Email"
ON "Users" (lower("Email"));

CREATE TABLE IF NOT EXISTS "Events" (
    "Id" uuid PRIMARY KEY,
    "Name" text NOT NULL,
    "Status" text NOT NULL DEFAULT 'Draft'
);

CREATE TABLE IF NOT EXISTS "Officials" (
    "Id" uuid PRIMARY KEY,
    "Name" text NOT NULL,
    "Phone" text NULL,
    "Email" text NULL,
    "Role" text NOT NULL DEFAULT 'Rendező',
    "Status" text NOT NULL DEFAULT 'Available',
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS "Boats" (
    "Id" uuid PRIMARY KEY,
    "Name" text NOT NULL,
    "RegistrationNumber" text NULL,
    "CallSign" text NOT NULL,
    "Status" text NOT NULL DEFAULT 'Active',
    "Notes" text NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Boats_CallSign"
ON "Boats" (lower("CallSign"));

INSERT INTO "Users" (
    "Id","Name","Email","Phone","Role","IsActive","CreatedAt"
) VALUES (
    '11111111-1111-1111-1111-111111111111',
    'Admin User',
    'admin@eventhelm.local',
    NULL,
    'Admin',
    true,
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Events" ("Id","Name","Status")
VALUES
('22222222-2222-2222-2222-222222222222','Balaton Race 07','Active'),
('22222222-2222-2222-2222-222222222223','Őszi Regatta','Draft')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Officials" (
    "Id","Name","Phone","Email","Role","Status","CreatedAt"
) VALUES
(
    '33333333-3333-3333-3333-333333333331',
    'Kovács Péter',
    '+36 30 111 2233',
    'kovacs.peter@eventhelm.local',
    'Versenyvezető',
    'Active',
    CURRENT_TIMESTAMP
),
(
    '33333333-3333-3333-3333-333333333332',
    'Nagy Anna',
    '+36 30 444 5566',
    'nagy.anna@eventhelm.local',
    'Pályafelelős',
    'Available',
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Boats" (
    "Id","Name","RegistrationNumber","CallSign","Status","Notes","CreatedAt"
) VALUES
(
    '44444444-4444-4444-4444-444444444441',
    'Alpha',
    'HUN-RC-01',
    'RC-01',
    'Active',
    'Race Committee egység',
    CURRENT_TIMESTAMP
),
(
    '44444444-4444-4444-4444-444444444442',
    'Bravo',
    'HUN-RC-02',
    'RC-02',
    'Active',
    'Pályajelző egység',
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Id") DO NOTHING;

COMMIT;
