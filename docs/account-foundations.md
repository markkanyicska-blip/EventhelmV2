# Individual account foundations (Phase 2B, Step 1)

This is dormant infrastructure, not individual login. Shared-password routes,
cookie settings, authentication registration and the Phase 1 permission proposal
are unchanged. No account endpoints, RBAC, production service registration,
database repository or automatic migration runner are introduced.

## Model and lifecycle

`Account` has its own UUID and a unique `UserId` foreign key to the existing
`Users.Id`. Personnel records and assignments retain their IDs and meaning.
`Users.Role` remains descriptive and never grants privileges. No email-based
identity matching, account creation on import, or existing-record backfill occurs.

`AccountService` depends only on `IAccountRepository`,
`IPasswordHasher<Account>` and `TimeProvider`. The repository contract requires
atomic unique-user creation and optimistic version checks on updates. A future
adapter must map status names to PostgreSQL text explicitly and increment the
version using an update guarded by the expected version.

- Creation requires an existing User and produces Pending without a password.
- Activation requires Pending plus an active linked User and sets the hash.
- Pending or Active can become Suspended. Suspended cannot activate or verify.
- Verification requires Active, a password hash and an active linked User.
  Invalid credentials return false; repository failures and concurrent rehash
  conflicts propagate rather than being reported as valid credentials.
- There is no reactivation, password reset, session creation or email verification
  in this step. Activation is an internal primitive, not proof of mailbox ownership.

`AccountPasswordHasher` uses ASP.NET Core Identity's versioned V3 PBKDF2 format
(HMAC-SHA512, random per-password salt), explicitly configured to 220,000
iterations. The shared framework supplies these maintained primitives; no new
package is needed. Successful verification upgrades hashes when Identity requests
a rehash. Benchmark the work factor on deployment hardware before integration.
New passwords must be nonblank and 15-1024 UTF-16 code units; spaces and Unicode
are accepted without trimming. A compromised-password blocklist, throttling and
other public-login controls remain prerequisites, not implemented guarantees.

The password hash is excluded from System.Text.Json serialization and the record's
`ToString()`. It must still never be logged through structured destructuring,
placed in an audit payload or returned as an account DTO. Services must not be
exposed directly as endpoints.

## Prepared migration: do not run as part of this step

`database/2026-11-02_account_foundations.sql` creates only `Accounts`, with
referential integrity, one account per User, lifecycle checks and concurrency
metadata. It inserts nothing and changes no existing table or record.

Before any future authorized application:

1. Verify the deployed `Users` schema has a UUID primary key named `Id`; do not
   assume checked-in initialization scripts describe the deployed database.
2. Verify no conflicting `Accounts` table exists. This migration intentionally
   fails rather than silently accepting an incompatible existing schema.
3. Rehearse on disposable PostgreSQL with synthetic Users and verify constraints,
   lock behavior, preservation of existing data, and transaction rollback.
4. Take and verify a restorable backup; arrange approved migration credentials
   separately from runtime credentials and record the applied migration.
5. Apply as a reviewed deployment operation, never during application startup.
   The transaction has a five-second lock timeout; failures require investigation,
   not an automatic permissive fallback.

An uncommitted failed migration rolls back transactionally. After commit, prefer
leaving the unused additive table in place when rolling back application code.
Only if it is confirmed empty and unused may an operator approve dropping
`Accounts`; no destructive down script is supplied. Populated accounts require
backup and a separately reviewed recovery plan, never automatic deletion.

## Future integration gates

Implement a PostgreSQL repository with disposable-database integration tests
before registering these services. Define verified login identifiers separately
from editable profile contact data. Add invitations/reset tokens, purpose-bound
single-use consumption, security stamps, server-revocable sessions, MFA,
antiforgery, audit redaction, throttling and transactional race protections.
Credential verification here is a point-in-time check, not an authorization or
session guarantee; future session issuance must revalidate current account state.

Approve and implement the Phase 1 resource-level permission matrix before
admitting restricted accounts. Preserve assignment IDs, explicitly approve any
account-to-Official links, close public document access, and resolve global Race
Control ownership. Never promote legacy shared-password tickets into accounts.

## Isolated validation

Account tests construct the service directly with synthetic Users, an in-memory
repository and a fixed clock. They do not load configuration, user secrets,
environment settings, the application entry point, JSON stores or a database.
Existing shared-password tests retain their isolated TestServer harness.

From the repository root:

```sh
dotnet test backend/EventHelm.Api.Tests/EventHelm.Api.Tests.csproj --configuration Release
dotnet build backend/EventHelm.Api/EventHelm.Api.csproj --configuration Release
cd frontend/eventhelm-admin
npm test
npm run build
```
