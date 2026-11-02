# EventHelm PostgreSQL inicializálás

Másold az `init.sql` fájlt ide:

`C:\Users\USER\Desktop\EventhelmV2\database\init.sql`

Majd futtasd a saját adatbázisneveddel:

```powershell
psql -U postgres -d eventhelm -f C:\Users\USER\Desktop\EventhelmV2\database\init.sql
```

A felhasználónevet és az adatbázisnevet az
`backend\EventHelm.Api\appsettings.json` fájl
`DefaultConnection` értékéhez igazítsd.

pgAdminban is futtatható a Query Toolból.

A script létrehozza:

- `"Users"`
- `"Events"`
- `"Officials"`
- `"Boats"`

és néhány mintaadatot is beszúr.

## Dormant account migrations (Phase 2B)

`2026-11-02_account_foundations.sql` and
`2026-11-02_account_invitations.sql` are preparation-only additive migrations,
in that order. Do not execute them against an existing database for this phase
or add them to application startup. Step 2 validated them only in a fresh
disposable PostgreSQL container using synthetic data. Step 3 executes no migration
scripts; its tests bootstrap a separate test-only schema fixture inside a new
disposable container.

`2026-11-02_account_sessions.sql` is the separate preparation-only Step 4 script
for explicitly bound login identities, revocable sessions and persistent login
attempt limits. It requires account foundations, inserts/backfills nothing and
must remain unexecuted in this phase. Tests use only the test schema snapshot,
never these migration scripts.

See [account foundations](../docs/account-foundations.md) for schema review,
invitation security, audit guarantees and future deployment prerequisites.

## Dormant RBAC migration (Phase 3 Step 1)

`2026-11-02_rbac_foundations.sql` is an additive preparation-only script for
separate global administrator assignments, event-scoped account roles and
purpose-specific sensitive permission grants. It requires account foundations
and the existing `Events` table. It seeds/backfills nothing.
**Do not execute it in this phase or wire it into application startup.**
Tests use a separate test-only schema snapshot in disposable PostgreSQL
containers, never migration scripts or an existing database.

See [RBAC foundations](../docs/rbac-foundations.md) for role capabilities,
storage IDs, trust boundaries, revocation behavior and rollout prerequisites.
