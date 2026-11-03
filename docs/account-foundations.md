# Individual account foundations (Phase 2B, Steps 1-4)

This is dormant infrastructure, not individual login. Shared-password routes,
cookie settings, authentication registration and the Phase 1 permission proposal
are unchanged. No account endpoints, RBAC, production service registration,
individual login endpoints or automatic migration runner are introduced.

## Model and lifecycle

`Account` has its own UUID and a unique `UserId` foreign key to the existing
`Users.Id`. Personnel records and assignments retain their IDs and meaning.
`Users.Role` remains descriptive and never grants privileges. No email-based
identity matching, account creation on import, or existing-record backfill occurs.

`AccountService` depends only on `IAccountRepository`,
`IPasswordHasher<Account>` and `TimeProvider`. `AccountRepository` implements
this contract using Dapper/Npgsql. It receives a caller-owned `NpgsqlDataSource`;
it never reads configuration, environment connection strings or user secrets.
Status names map explicitly to PostgreSQL text. Creation enforces unique UserId;
updates atomically compare the expected version and increment it in SQL.
UserId and CreatedAt cannot be changed through updates. Cancellation and database
errors propagate; unexpected database failures are never treated as conflicts.

- Creation requires an existing User and produces Pending without a password.
- Activation requires Pending plus an active linked User and sets the hash.
- Pending or Active can become Suspended. Suspended cannot activate or verify.
- Verification requires Active, a password hash and an active linked User.
  Invalid credentials return false; repository failures and concurrent rehash
  conflicts propagate rather than being reported as valid credentials.
- There is no reactivation, password reset, session creation or email verification
  in this step. Direct activation remains an internal primitive, not proof of
  mailbox ownership; it must not be exposed as an alternative to invitation redemption.

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

## Invitations and transactional audit

`AccountInvitationService` is constructed directly, never registered. Issuance
requires an existing Pending account and active linked User. Lifetimes must be
positive and at most seven days. The caller chooses the lifetime; there is no
transport, public URL builder, automatic email matching or email delivery.

Tokens contain 32 cryptographically random bytes (256 bits), encoded as 64
uppercase hexadecimal characters. Only the SHA-256 hash of that exact,
purpose-specific token is stored in `AccountInvitations`. High-entropy random
tokens do not require a slow password KDF. Tokens are never normalized; malformed
input throws without accessing the database. The plaintext is returned once in
`IssuedAccountInvitation.Token` for a future trusted delivery adapter. It and
the persisted hash are excluded from JSON and `ToString()`. Neither exclusion
protects arbitrary structured destructuring: never log these objects or put
tokens in audit events, telemetry or exception messages.

Redemption validates the password with the same Step 1 policy and hashes it
with `AccountPasswordHasher`. `IAccountInvitationRepository` requires an atomic
transaction that rechecks Pending status, active User, expected account version,
token hash, expiration, and absence of both redemption and revocation. The
repository locks account/user rows before consuming a token, then activates the
account and appends the audit event in that transaction. Concurrent attempts,
including different invitations for one account, can activate only once.
Conflicts roll back token consumption. Expiration is exclusive (`now >= ExpiresAt`
fails); SQL also checks PostgreSQL `clock_timestamp()` after acquiring locks so
stale application time cannot extend a token's lifetime. Keep clocks synchronized.
The injected clock enables deterministic tests but is not caller-controlled input.

Revocation uses the same account-first locking order and commits its audit event
with the state change. A redeemed or revoked token cannot be revoked again.
Expired unused invitations may still be explicitly revoked. There is no renewal
or reuse: issue a fresh token while the account is still Pending. Multiple unused
invitations may coexist, but after one activates the account all others are
unusable. Failed redemption creates no session and changes no credentials.

`AccountAuditEvent` and `IAccountAuditLog` provide a secret-free, durable audit
foundation. Account creation, issuance, redemption and revocation append only
event/account/invitation UUIDs, event type and UTC timestamp to `AccountAuditEvents`.
Audit write failure rolls back the corresponding mutation; the read interface
is intended for a future authorized consumer. No mutable free-text payload is
accepted. Failed attempts, delivery, actor attribution, retention, external audit
export and tamper-resistant storage remain future integration work; this is not
a complete operational security monitoring system. Current services have no
authenticated individual actor to attribute.

## Invitation acceptance and initial password setup (Step 3)

`AccountInvitationService.RedeemAsync(token, password, cancellationToken)` is the
internal acceptance entry point. Step 3 reuses and strengthens Step 2's atomic
redemption workflow rather than adding a second activation path or service.
Construct it directly with `AccountRepository`, `AccountPasswordHasher` and a
trusted `TimeProvider`. Do not register it or call it from an endpoint yet.

The workflow:

1. Honor cancellation and enforce the existing password policy. Validate the
   exact token format, hash it and load only by that hash.
2. Reject unknown, expired, revoked, consumed or not-yet-created invitations.
   Require a Pending account without a password and an active linked User before
   performing the expensive Identity password hash.
3. Generate the salted ASP.NET Core Identity hash in memory. A hashing failure
   or unusable hash aborts without writing anything. Honor cancellation again and
   recheck the application clock after hashing, since hashing can span expiry.
4. Call the existing transactional repository primitive. Its authoritative
   revalidation under locks is still required: preflight checks do not reserve a
   token or guarantee eligibility. In the same transaction, consume the token,
   write the initial password hash, activate and increment the account version,
   and append `InvitationRedeemed`. Commit before returning the account.

Hash computation intentionally precedes the transaction to avoid holding locks
during CPU-intensive work; persistence of the hash is atomic with activation,
consumption and audit. If a concurrent request revokes the token, suspends the
account, deactivates the User, or changes the account version after hashing, the
transaction rejects acceptance. If the account update or audit append fails,
all writes roll back, including token consumption; the unused token can be
retried if it remains eligible and unexpired. No intermediate password or Active
account is persisted by the service.

The existing `InvitationRedeemed` event describes the combined initial password
setup and activation, not a separate unaudited credential operation. Replays and
losing concurrent attempts cannot replace the winning password or append a
second redemption event. The returned Account has its password hash redacted
from JSON and string output; it is not an authentication ticket or session.
Database/hash failures and cancellation propagate, while unavailable invitations
use a common lifecycle error. Do not retry automatically: a connection loss or
cancellation during commit can make the commit outcome unknown to the caller.
No new schema, migration, email transport, credential verification endpoint or
login behavior is introduced by Step 3.

## Dormant individual authentication and sessions (Step 4)

`IndividualAuthenticationService` verifies credentials but does not authenticate
HTTP requests. It is not registered, invoked by `AccessController`, or connected
to cookies or claims. Construct it directly with `IAccountRepository`,
`IAccountSessionRepository`, the existing `AccountPasswordHasher`, and a trusted
clock. Reuse an instance rather than constructing one per attempt: construction
precomputes a synthetic dummy Identity hash for unknown/ineligible identities.
No custom password cryptography is used.

### Login identity and generic rejection

The login email lives in the separate `AccountSecurity` table, not `Users.Email`.
`BindLoginEmailAsync` is a trusted, explicit provisioning primitive: it binds
one unique normalized email once and fails on duplicate/rebinding. It is not
called by imports, profile edits, account creation or invitation acceptance.
An authorized future provisioning workflow must verify the intended login
identifier and ownership before binding; this primitive does not send email or
claim verification. There is deliberately no backfill or legacy profile fallback.

`AccountLoginEmail.Normalize` trims outer whitespace, accepts ASCII mail addresses
without display names, rejects invalid/oversized/non-ASCII input, and lowercases
the entire identifier invariantly. This intentionally defines case-insensitive
local parts and does not apply provider-specific alias or dot rules. International
email identifiers require a separately reviewed normalization policy.

`AuthenticateAsync` returns null for every credential/eligibility/lockout rejection,
including invalid input, unknown email, bad password, missing binding, Pending or
Suspended account, inactive User, and concurrency conflicts. No per-account failure
reason is returned. Unknown/ineligible identities perform dummy Identity password
verification after the attempt gate; this reduces hash-work timing differences,
but is not a guarantee of identical end-to-end timing. A future endpoint must use
the same generic status/body for all these null results and never log inputs.
Database errors/cancellation propagate rather than masquerading as credentials
or successful sessions; an integration must fail closed on infrastructure errors.

Successful verification upgrades older Identity hashes through the existing
optimistic account update, advancing `Accounts.Version`. Session creation then
revalidates Active status, active User, exact account version/hash, captured
security version and lockout state under database locks. A password verified
before suspension, credential change or logout-all cannot establish a valid
old-generation session afterward.

### Persistent brute-force foundations

`AccountLoginAttempts` stores only a SHA-256 digest of the normalized identifier,
window start, counters and lockout timestamp. An atomic upsert reserves at most
10 attempts per identifier in a fixed 15-minute window, for unknown and known
identifiers alike. Five failed verifications lock that identifier for 15 minutes.
Counters/lockout survive process restarts and are shared between instances.
Successful attempts do not reset the window or failure counter; this deliberately
prevents successful logins from bypassing the budget. Expired windows reset upon
the next admitted attempt; lockout is checked again when issuing a session.
Use synchronized, trusted clocks across instances; timestamps are never supplied
by an unauthenticated caller.

These are foundations, not sufficient public-endpoint abuse protection. Before
exposure add global and trusted client/network rate limits before database/KDF
work, bounded state cleanup, monitoring, recovery policy and defenses against
identifier spraying and intentional account lockout. Identifier hashes are
pseudonymous, not anonymized; emails are guessable. No background cleanup or
runtime limiter registration is enabled here.

### Session contract, expiry and revocation

Successful authentication returns `IssuedAccountSession` containing only account
UUID, expiry and an in-memory bearer token. Tokens are 32 CSPRNG bytes encoded as
64 uppercase hex characters. Only the SHA-256 hash is stored in `AccountSessions`;
the plaintext is omitted from JSON and string output. Never destructure/log the
result. No password hashes, profile roles or administrator flags are returned.
Invitation and session hashes are stored in separate purpose-specific tables.

Sessions have an absolute, non-sliding eight-hour lifetime. `AccountSessionService`
validates token format and consults PostgreSQL on every validation, checking:
unused revocation state, exclusive expiry (`now >= ExpiresAt` rejects), creation
time, current account/User eligibility, account version and security version.
An independent database `clock_timestamp()` expiry check prevents a stale
application clock from extending validity. There is no positive validation cache,
offline fallback or authentication when storage is unavailable. Malformed/unknown
tokens return null; valid-format tokens encounter and propagate storage failures.
Validation is point-in-time; already running operations need their own transaction
and authorization checks, not a promise that later revocation rewinds a request.

`RevokeAsync` is idempotent and revokes one token. `RevokeAllAsync(accountId)`
atomically increments `AccountSecurity.SecurityVersion` and revokes all current
sessions. Account-first locks serialize it with session issuance; the captured
generation prevents an in-flight verified login from recreating a valid session
across logout-all. Revocation failure rolls back both the generation increment
and token updates. Fresh logins capture the new generation.

`Accounts.Version` doubles as a conservative credential/security version:
all account mutations through `IAccountRepository.TryUpdateAsync` increment it.
Suspension, password replacement, rehash and explicit version updates therefore
invalidate existing sessions without needing to enumerate them. Future password
change/reset code MUST use that version-guarded update (or a reviewed equivalent);
never write a password or reactivate an account without advancing the version.
`AccountSecurity.SecurityVersion` provides additional explicit logout-all
invalidation. User inactivity blocks validation while inactive; permanent session
invalidation on profile deactivation also requires an explicit logout-all.

### Future HTTP integration requirements

Preserve HttpOnly cookie authentication: a future separately approved integration
should put the bearer only in a Secure, HttpOnly cookie with reviewed SameSite,
path, lifetime, antiforgery and CSRF controls, and validate it server-side on every
request. Never return the bearer as a JSON API field or store it in localStorage.
No cookie handler or browser changes are made here. Establish a distinct
individual identity scheme and explicit authorization policy; never reinterpret
legacy shared-password cookies as individual identities or administrator grants.
`Users.Role` remains descriptive and is not included in session validation.
Actor-attributed login/revocation audit, security monitoring, recovery and retention
remain integration gates; existing invitation audit events are unchanged.

## Prepared migrations: do not apply to an existing database in this step

`database/2026-11-02_account_foundations.sql` creates only `Accounts`, with
referential integrity, one account per User, lifecycle checks and concurrency
metadata. It inserts nothing and changes no existing table or record.
Review found no required correction to that Step 1 script, which is unchanged.
The new, separate `database/2026-11-02_account_invitations.sql` requires it and
adds only `AccountInvitations`, `AccountAuditEvents`, constraints and indexes.
Hashes are unique and format-constrained; expiry bounds and mutually exclusive
terminal states are enforced by PostgreSQL. No backfill or existing-record
updates are included. Step 2 validated both scripts in a newly created disposable
test container. Step 3 neither changes nor executes migration scripts; tests
bootstrap a fresh container with the test-only schema snapshot described below.
Step 4 adds the separate, unexecuted
`database/2026-11-02_account_sessions.sql`, creating only `AccountSecurity`,
`AccountSessions`, `AccountLoginAttempts` and related constraints/indexes.
It alters no existing table and populates no login identifiers or sessions.
Apply it only after future deployment approval and the account foundations
prerequisite, never as part of this phase or application startup.

Before any future authorized application:

1. Verify the deployed `Users` schema has a UUID primary key named `Id`; do not
   assume checked-in initialization scripts describe the deployed database.
2. Verify no conflicting account, invitation, audit or session/security/attempt
   tables exist. The migrations intentionally fail rather than silently accepting
   an incompatible existing schema. Apply foundations before dependent scripts.
3. Rehearse on disposable PostgreSQL with synthetic Users and verify constraints,
   lock behavior, preservation of existing data, and transaction rollback.
4. Take and verify a restorable backup; arrange approved migration credentials
   separately from runtime credentials and record the applied migration.
5. Apply as a reviewed deployment operation, never during application startup.
   The transaction has a five-second lock timeout; failures require investigation,
   not an automatic permissive fallback.

An uncommitted failed migration rolls back transactionally. After commit, prefer
leaving the unused additive tables in place when rolling back application code.
Only if they are confirmed empty and unused may an operator approve dropping
them in reverse dependency order; no destructive down script is supplied.
Populated accounts require backup and a separately reviewed recovery plan, never
automatic deletion.

## Future integration gates

Do not register these services until a separately approved integration step.
Define verified login identifiers separately from editable profile contact data.
Add reset tokens with a separate purpose/store, security stamps, server-revocable
sessions, MFA, antiforgery, delivery redaction, authorized actor attribution,
throttling and abuse monitoring before exposing invitation or account endpoints.
Future runtime database permissions should prohibit audit updates/deletes and
use a separate, reviewed migration identity. The current application is not
configured to access any of the new tables.
Credential verification here is a point-in-time check, not an authorization or
session guarantee; future session issuance must revalidate current account state.

Approve and implement the Phase 1 resource-level permission matrix before
admitting restricted accounts. Preserve assignment IDs, explicitly approve any
account-to-Official links, close public document access, and resolve global Race
Control ownership. Never promote legacy shared-password tickets into accounts.

## Isolated validation

Step 1 tests still construct the service directly with synthetic Users, an
in-memory repository and a fixed clock. `AccountPersistenceTests` additionally
use Testcontainers to create a fresh `postgres:17-alpine` container with generated
test-only credentials. Step 3 changes the shared fixture to bootstrap
`backend/EventHelm.Api.Tests/Fixtures/accounts.sql`, a test-only schema snapshot
with synthetic Users and the account/invitation/audit constraints. It does not
load or execute files under `database/`; those remain prepared deployment inputs,
not part of this phase's test execution. Keep the fixture constraints aligned
with the prepared schema when future schema changes are authorized.
There is no external database connection-string option or fallback; unavailable
Docker fails the tests rather than silently skipping persistence coverage.
The container and data source are disposed after testing.

Tests cover SQL constraints and mapping, account uniqueness/version conflicts,
hash-only persistence and redaction, lifetime bounds, expiration at the exact
boundary, the independent database-clock check, revocation/replay, account/user
eligibility, cancellation, concurrent same-token and sibling-token redemption,
redemption racing revocation, and rollback of each mutation when audit fails.
`InvitationAcceptanceTests` additionally verifies exact password boundaries and
Unicode/space preservation with the real Identity hasher, rejects ineligible
requests before hashing, injects hashing failure and cancellation, crosses expiry
during hashing, changes eligibility after hashing but before persistence, and
forces an account-write failure after token consumption to verify full rollback.
Deterministic gates bring both concurrent requests to the transaction boundary
after hashing; only one token is consumed, one redemption event is written, and
only the winner's password is stored. Each test class owns a separate disposable
container, so injected schema failures cannot affect another class.
`IndividualAuthenticationTests` uses the same synthetic snapshot extended with
session tables, never executing migration scripts. It covers explicit identity
binding, generic failures, no profile fallback, password rehash, persisted
attempt budgets and lockout, hash-only tokens/redaction, exclusive and database
expiry, single/concurrent revocation, logout-all racing issuance, revocation
rollback, account/security-version/password/status/User invalidation, storage
failure and cancellation. The lookup also runs with a single-connection pool
to ensure it does not hold a second connection while loading the Account.
They do not load appsettings, user secrets, the application entry point or JSON
stores. Existing shared-password tests retain their isolated TestServer harness.

From the repository root:

```sh
dotnet test backend/EventHelm.Api.Tests/EventHelm.Api.Tests.csproj --configuration Release
dotnet build backend/EventHelm.Api/EventHelm.Api.csproj --configuration Release
cd frontend/eventhelm-admin
npm test
npm run build
```

Docker must be available for the backend suite (also true on the CI Ubuntu runner).
Scoped C# formatting and analyzers can be checked with `dotnet format` on each
backend project separately, using `--include` for that project's changed C# paths
and `--verify-no-changes --no-restore`.
Frontend lint remains `npm run lint`; Steps 2-4 do not change frontend source.
