# Phase 3 Step 1: dormant event-scoped RBAC

## Status and boundaries

This is a preparation-only foundation on `design-system`, following Phase 2B
individual sessions. No runtime registration, middleware, policy, controller,
endpoint, frontend, shared-password behavior or existing record is changed.
**This does not secure today's application or its existing static uploads.**
Do not admit restricted users until the integration prerequisites below are met.

The five roles are System Administrator (`GlobalRole.SystemAdministrator`),
Event Organizer (`EventRole.EventOrganizer`), Race Officer, Technical Staff and
On-Water Personnel. The global role and four event roles use different types
and different tables. Their numeric values are independent namespaces.
`Users.Role`, `Officials.Role`, imported roles, committee labels, profile data
and client role claims are never authorization inputs.

## Capability matrix

This refines the proposal in [Phase 1](authentication-phase-1.md), but does not
approve or enforce it. Each event column requires an active individual account,
an active linked user, a valid individual session and a current role in **that
event**. Roles combine within one event only. Blank/unknown/revoked/expired/
not-yet-effective roles grant nothing.

| Resource / action | System Administrator | Event Organizer | Race Officer | Technical Staff | On-Water Personnel |
|---|---|---|---|---|---|
| Account administration / global system management | Global | No | No | No | No |
| Create event / appoint Organizers | Global | No | No | No | No |
| Read event overview | Global override | Event | Event | No broad overview | No broad overview |
| Manage event setup | Global override | Event | No | No | No |
| Plan event operations / areas | Global override | Event | Event | No | No |
| Read operational officials / manage officials | Global override | Both in event | Read in event | No | No |
| Read event boats | Global override | Event | Event | Assigned equipment | Own unit |
| Allocate event boats / radios | Global override | Event | Event | Assigned equipment only | No |
| Report defects / incidents | Global override | Event | Event | Assigned equipment | Own unit |
| Resolve defects / equipment condition | Global override | No | No | Assigned equipment | No |
| Update unit status | Global override | Event | Event | Own assignment | Own unit |
| Read assignments / shifts / tasks | Global override | Event | Event | Own assignment | Own assignment |
| Manage assignments / crews / shifts / tasks | Global override | Event | Event | No | No |
| Update task progress | Global override | Event | Event | Own assignment | Own assignment |
| Read / send operational communications | Global override | Event | Event | Assigned channel/context | Assigned channel/context |
| Read / manage operational documents | Global override | Both in event | Read in event | No | No |
| Read / operate event-scoped Race Control (future) | Global override | Requires Race Officer too | Event | No | No |
| Read / operate current global Race Control | Global only | No | No | No | No |
| Global boat / radio inventory | Global only | No | No | No | No |
| Grant/revoke non-organizer event roles | Global override | Event; not self | No | No | No |
| Personal / medical information; private documents | Explicit grant | Explicit grant | Explicit grant | Explicit grant, not by role | Explicit grant, not by role |

General officials reads mean a deliberately minimized operational projection,
not contact details, emergency contacts, medical fields or documents. Global
account administration is not a permission to read those sensitive fields.
Own-profile self-service remains a separate future boundary, not a bypass for
event personnel records.

`RolePermissions.For(resource, action)` is an explicit allowlist. Unknown
resource/action pairs deny. Sensitive read and manage permissions are separate;
a manage grant does not imply read. A private-document grant is separate from
operational document access. All grants are event-specific, purpose-labelled
and time-limited, including for global administrators. A sensitive grant alone
does not establish event membership: an event role or global administrator is
also required. Limited roles can receive a separate sensitive grant only through
a future privileged provisioning workflow, never by assigning an operational role.

## Decision flow and trust boundary

`EventAuthorizationService` takes an opaque individual session token, a typed
resource key and a typed action. It accepts no claims principal, free-text role,
caller-supplied event scope or caller-supplied assignment context.

1. `AccountSessionService` validates the individual session against its
   persistent account/security versions, revocation and expiry. A shared-password
   cookie is not an individual token and cannot produce an account identity.
2. `PostgresAuthorizationStore` reads account/user eligibility and grants in a
   fresh repeatable-read transaction. It joins event grants to existing events.
   The store exposes no grant/revoke operations. Ineligible accounts deny even
   if a session was valid immediately before the snapshot.
3. A trusted `IAuthorizationResourceResolver` resolves existence, canonical kind,
   event ownership, sensitivity and account assignment from authoritative
   server-side data. Missing, mismatched or unscoped context denies, even for an
   administrator. The caller cannot supply this result.
4. The service checks the explicit resource/action permission, current event
   roles or global role, assignment restrictions and sensitive grants.
   Administrators cannot bypass missing scope, unsafe document storage or
   sensitive permission requirements. Event role grant preflights also require
   an existing eligible target account, loaded server-side.

The provided PostgreSQL resolver supports existing event roots, event-role
management roots and fixed global system/inventory/event-creation/Race Control
keys only. It never opens the Race Control JSON file. All child resources,
documents and private fields **deny by default** until audited adapters exist.
The matrix is fully represented in the policy and tested with synthetic trusted
resolvers; it is not a claim that operational resource adapters already exist.
Do not manufacture child scope from a route parameter or copy a global official/
boat record into an event context without checking a real relationship.

`AuthorizationResource` is an internal server-side contract, not an HTTP request
DTO. An adapter must prevent aliases from reclassifying a medical/private
resource as an ordinary official/document. Collection queries must be filtered
server-side and their projections independently authorized; permission on a
parent does not automatically authorize all children or fields. Assignment
restrictions require an explicit account-to-resource relationship, never a
matching name, email or operational label. No such links are inferred here.

Document decisions require `DocumentStorage.Private`. Public-static and unknown
storage deny even for administrators; private documents additionally require a
specific sensitive grant. No file delivery, path generation or upload behavior
is added. Existing `/uploads/officials/` delivery is a known rollout blocker:
new sensitive content must never be placed there. Move protected documents
outside `wwwroot` and implement authenticated, authorized streaming/downloads,
including metadata, content-type and path traversal protections, before rollout.

Outcomes distinguish `Unauthenticated` (future HTTP 401) from `Forbidden`
(future HTTP 403). Denials carry typed reasons; do not convert infrastructure
errors to an allow or a successful empty response. Storage/resolver/cancellation
errors propagate. Future HTTP handling should return generic denial messages,
without disclosing whether a sensitive resource exists.

Administrator overrides have the explicit `AdministratorOverride` reason.
Future integration must persist a security audit of overrides, sensitive
access, role changes and denials without tokens, medical content or personal
details. The dormant service does not silently claim to persist such an audit.

## Storage, revocation and provisioning

Prepared script: `database/2026-11-02_rbac_foundations.sql`. **Do not execute it
in this phase**, against an existing database or at application startup.
It requires the account foundations and an existing `Events` table and adds only:

| Table | Boundary |
|---|---|
| `GlobalRoleAssignments` | Only global System Administrator; account and granting account FKs |
| `EventRoleAssignments` | Four event roles; account, event and granting account FKs |
| `SensitivePermissionGrants` | Six explicit sensitive permissions; account/event/grantor FKs, purpose, mandatory expiry |

Every assignment has an ID, granting account, effective timestamp, optional
expiry and revocation timestamp (sensitive grants require expiry). Database
checks reject unknown role/permission IDs, invalid lifetimes and orphaned
references. Restrictive FKs prevent deletion from silently erasing grants.
There are no seeds, implicit administrators, record updates or data backfills.
Sensitive permission IDs 101-106 are a stable storage contract; do not renumber.
Purpose text must describe a non-sensitive operational purpose, not medical or
personal content. Grant rows are controlled by trusted provisioning, not clients.
Foreign keys provide referential integrity, **not authority to grant roles**.

No positive or negative authorization decisions or grant snapshots are cached.
Every decision revalidates the session and rereads current state. Revocation,
membership change, grant expiry and account suspension therefore take effect on
the next decision, without forcing a login or mutating account password versions.
If caching is added later, use a transactionally incremented authorization
version for every global/event/sensitive grant mutation, validate it on every
request, bound entries by grant/session expiry and invalidate across instances.
Never use cookie role claims or a TTL alone as the revocation mechanism.

Decisions are point-in-time checks, not locks held across a subsequent business
write. Future sensitive mutations must authorize and write within an appropriate
transaction/locking boundary (and recheck session/security versions as needed)
to prevent TOCTOU with concurrent revocation, scope changes or suspension.

## Required before any enforcement

- Obtain product approval of the refined matrix and least-privilege projections.
- Wire individual authentication separately; never let a shared-password
  identity assume a role. Keep 401 distinct from 403 and preserve frontend sessions
  on 403. No compatibility fallback may grant access after RBAC rejects.
- Implement audited provisioning with a distinct global-admin workflow, an
  explicit out-of-band bootstrap, and transactional last-active-admin protection.
  Organizers may manage only non-organizer roles in their event, never their
  own roles. Validate target account IDs/state and scope server-side, reauthorize
  the actor at mutation time, and reject privilege fields in profile/import DTOs.
  The current grant decision is a preflight, not a provisioning API.
- Add explicit account/person/assignment links and audited resource resolvers
  for officials, boats, crews, shifts, tasks, communications and documents.
  Validate nested IDs, event existence, relationships and minimal field sets.
  Do not auto-grant roles from creating/importing operational assignments.
- Event-scope the JSON-backed Race Control and other shared state before using
  event Race Officer permissions. The shipped resolver recognizes only the
  global Race Control singleton, which is administrator-only.
- Replace public document delivery as described above; cover list/search,
  exports, imports, uploads, downloads and direct static paths.
- Integrate persistent security auditing, anti-forgery controls, rate limits,
  error handling, migration review/backups and full endpoint coverage before DI
  registration or enabling policies. Database privileges must restrict who can
  write authorization tables independently of this application policy.

## Isolated verification

`AuthorizationTests` uses synthetic sessions and trusted in-memory resources.
`AuthorizationPersistenceTests` uses the real individual authentication/session
and authorization stores against a newly created disposable PostgreSQL container.
No external database connection string or production host is accepted.
`Fixtures/accounts.sql` and `Fixtures/rbac.sql` are test-only schema snapshots;
no migration script is loaded or executed. Keep their constraints aligned
with prepared scripts during review.

Coverage includes cross-event/account denial, free-text-role non-authority,
role combinations, organizer self-escalation, global-role separation, global
Race Control, limited assignment scope, missing/forged scope, inactive accounts,
revoked sessions/roles/grants, time boundaries, sensitive permissions including
administrator denial, unsafe document storage, fresh decisions, database
constraints and infrastructure failures. Tests never start `Program`, load
appsettings, open operational JSON files or serve real uploads.

```sh
dotnet test backend/EventHelm.Api.Tests/EventHelm.Api.Tests.csproj --configuration Release
cd frontend/eventhelm-admin
npm test
npm run build
```
