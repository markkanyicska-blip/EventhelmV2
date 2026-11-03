# Phase 3 Step 2: dormant API integration and safe rollout

## Safety boundary

This adds server-only preparation components and isolated tests, not enforcement.
No controllers, routes, services already in use, middleware, runtime DI,
authentication configuration, frontend, schemas, migrations or records change.
Individual login and RBAC remain disabled. The legacy fallback policy still
requires only a shared-password identity for business endpoints. Public static
uploads remain a **pre-existing exposure**, not a safe document boundary.
Do not admit restricted users on the strength of this preparation.

The [82-entry inventory](api-authorization-inventory.md) is authoritative for
endpoint coverage, actions, role candidates, scope blockers and endpoint gates.
It covers all 18 controllers, the health mapping, Development OpenAPI and the
separate static-file surface. Inspected service families include event CRUD,
global officials/users/boats, boat crews/defects, event areas/allocations/tasks,
both assignment models, spreadsheet import, timeline aggregation, JSON radios
and global Race Control. Account/invitation/session services and RBAC stores/
resolver are dormant foundations, not extra endpoints.

## Prepared components and contract

`Authorization/Integration/ApiAuthorizationRequirement` is server-owned
operation metadata: canonical resource key, action, required scope and expected
event ID. **Never bind it, or AuthorizationResource, from HTTP input.**
An endpoint's action/permission list must be fixed by reviewed server code;
clients cannot omit a sensitive check, choose `Read` for a write, select Global
scope, choose the System key or classify their own document as operational.

`ApiAuthorizationService.AuthorizeAllAsync`:

1. Validates only an opaque individual session through AccountSessionService.
   It does not read HttpContext, claims, frontend roles, Users.Role or legacy cookies.
2. Rejects null/empty requirements, invalid IDs/actions, unknown scope and
   event boundaries without nonempty expected event IDs. Global boundaries
   accept only the four fixed server-owned keys, with no event ID.
3. Wraps the trusted resolver for each check. It requires exact canonical key,
   expected scope and equality between **stored** EventId and the expected event.
   It never fills in a missing EventId from a route/body, and never retries a
   failed child check as a parent/global check. The underlying RBAC service
   resolves each requirement once; there is no separate check/use lookup here.
4. Delegates permissions, current account eligibility, current roles, sensitive
   grants and assigned-account restrictions to the existing EventAuthorizationService.
   Session and authorization state are reread, not cached.
5. Requires **all** checks to pass. A full mixed-sensitive response needs
   operational + PI + MI + private-document checks, not just ReadOfficials.
   Failure short-circuits and carries a typed reason; storage/resolver errors
   and cancellation propagate, never become a successful empty response.

Global and event requirements can coexist for a reviewed compound operation
(for example an import), but each needs its own authoritative resource. This
does not make the current import safe: its global personnel side effects still
lack a sensitive event boundary. GrantEventRole is intentionally not exposed
by this adapter (there is no target-account/role contract); use a separately
reviewed provisioning workflow with transaction-time validation.

Illustrative **server-side only**, not a registration or route change:

```csharp
var decision = await preparation.AuthorizeAllAsync(individualToken,
[
    new(taskKey, ResourceAction.Read, ResourceScope.Event, routeEventId),
    new(medicalKey, ResourceAction.Read, ResourceScope.Event, routeEventId)
], cancellationToken);
```

`taskKey` and `medicalKey` require audited resolvers; routeEventId is only an
assertion to compare against persisted ownership. A matching assertion is not
proof that the caller has membership/permission, and access to a task never
implies access to medical information. No new child resolver is supplied here:
with the shipped PostgreSQL resolver these checks remain denied.

`AuthorizationHttpStatus.DenialStatusCode` maps a decision without touching
an HTTP response: unauthenticated -> 401, forbidden/unknown -> 403, allowed ->
null (not a success response). Callers must inspect the decision before any
business service call. Typed reasons are internal audit inputs, not public
resource-existence disclosures. Aggregate AdministratorOverride indicates at
least one override; future audit integration must record each resource/action
and sensitive access, not rely solely on this aggregate reason.

## Authoritative event resolution

The inventory gate table specifies every resource family. These invariants
apply to all future adapters:

- Load canonical resource by its persisted ID/type, then load its stored event
  relationship and verify Events exists. Compare every route, query and body
  event assertion to that result. UUID syntax, a nested URL, SQL EventId filter
  or a frontend-selected event is not authorization.
- Existing roots use Events.Id; EventRoles roots verify the same Events row.
  New resources authorize a verified event collection/parent and every linked
  resource before creation; never fabricate a resolvable child ID. Lists and
  aggregates authorize the root plus filtered children/fields. This adapter
  alone neither scopes queries nor redacts responses.
- Scope must be unambiguous for that operation. Global Officials, Boats,
  BoatCrewAssignments and BoatDefects have no exclusive event owner. Do not
  choose the first event, use the caller's membership as the scope, or infer
  visibility of global records from any EventBoats/EventOfficials membership.
  Redesign relationships/projections or keep global administration separate.
- Resolve canonical classification, not just ownership. A medical/private
  resource cannot be relabelled Officials/Documents-operational. Distinguish
  the two assignment tables and other same-kind UUID families using audited
  typed identity/discriminators before they share a resolver namespace.
- AssignedAccountId requires an explicit persisted account/person/assignment
  relationship. User/official names, emails, import role strings, captain
  flags, Users.Role and client claims never prove account identity or authority.
- Verify child/parent and old/new foreign-key relationships **before** reads or
  writes, including validation, imports, captain removal and file deletion.
  For mutations reauthorize/revalidate in the business transaction/locking
  boundary. Preparation decisions are point-in-time preflights, not locks.
  Fresh individual-session/eligibility and grant checks are necessary to
  prevent scope-change/revocation TOCTOU; do not treat multiple checks as an
  atomic transaction.
- Resolve private documents through persisted owner/event/classification/storage
  metadata and opaque IDs. Never accept URL/path/storage/IsPrivateDocument from
  a request. No secret-bearing URL, public static path or random filename is an
  authorization mechanism. PublicStatic/unknown document storage must deny.

The only global sentinels are System, EventCreation, GlobalInventory and
GlobalRaceControl. They intentionally have no event ID and require current
global administrator authority. Current Race Control remains global regardless
of a caller's event role. Shifts need verified EventShifts.EventId and account
assignment before new endpoints; there are none today. Communications need
event/channel/account relationships before event permissions can be used.

## Authentication replacement and HTTP semantics

Today's `EventHelm.Access` ticket identifies a shared gate user only.
AccessGateMiddleware exists but is not registered; the actual current boundary
is SharedPasswordAuthentication's fallback policy and two AllowAnonymous actions.
The fallback covers crew routes outside `/api`, health and Development OpenAPI.
UseStaticFiles precedes authentication and is not protected by that fallback.

Future individual authentication needs a distinct reviewed scheme/cookie and
credential endpoints backed by IndividualAuthenticationService and
AccountSessionService, with opaque token transport, secure HttpOnly cookies,
CSRF defenses, bounded lifetime/rotation, rate limits and persistent revocation.
No automatic conversion of shared-password identities, Users.Role, official
roles or frontend claims is allowed. Account eligibility is linked to an
active user and current account/security versions. Provision global/event/
sensitive grants only through explicit audited administration.

| Situation | Future API result | Session/client behavior |
|---|---|---|
| Missing, malformed, unknown, expired or revoked individual session | 401, generic authentication-required response; no login redirect | Clear invalid individual credentials and request login; never accept shared cookie instead |
| Inactive/suspended account or inactive linked user detected during persistent session validation | 401 (repository rejects identity) | No business action |
| Session initially valid but eligibility lost/missing in fresh authorization snapshot | 403, generic forbidden response | Deny; no cached identity/permission fallback |
| Valid identity, wrong event, revoked/missing permission or sensitive grant, unsafe storage, missing/ambiguous resource scope | 403, generic forbidden response | Preserve otherwise valid session; do not turn permission denial into logout |
| Unknown/unregistered operation or invalid server authorization context | Deny; authenticated caller gets 403, unauthenticated caller 401 | Record internal reason; no fallback to parent/global permission |
| Storage/resolver/audit infrastructure failure | Fail closed; reviewed generic 5xx/503 handler, never allow, 200, empty list or cached success | Log operational error without tokens/content; no business action |
| Authorized request for subsequently absent business resource | Normal reviewed 404 after authorization | Do not reveal resource existence through different denial bodies |

Do not expose AuthorizationReason, document existence, account state details or
private fields in denial bodies. 400 is for ordinary request validation after
the identity/access boundary, not a way to skip authorization. Consistent
non-enumerating 403 behavior is required for unresolved scopes. Authentication
and health exceptions are explicit reviewed allowlist entries, not a bypass for
any other endpoint.

## Incremental rollout and rollback

**No rollout flag, DI registration or runtime activation is added in this PR.**
All phases below require separate reviewed changes and independent verification.
Migration review/provisioning are prerequisites, not instructions to execute
scripts now. No existing database or secret is accessed by this preparation.

1. **Approve and isolate:** approve the inventory/matrix/classifications and
   retained admin-only routes, build threat/abuse tests from every row, review
   schema/backups/restore procedures offline. Inventory aliases, proxy/CDN/static
   delivery, search/count/import/export and deployment environments too.
   Do not enable restricted accounts while any unactivated route remains
   reachable. Explicitly protect or disable every unactivated business route.
2. **Identity/admin boundary first:** separately implement individual authentication,
   audited bootstrap/provisioning, last-admin protection, CSRF and revocation.
   Independently verify individual session + persistent global administrator
   checks on E015-E024/E070-E079 and E082 if exposed, with target/resource
   validation and endpoint-specific gates. These are never event-role routes.
   Retire or fence shared-password business access with no compatibility fallback.
   Block S001 and unresolved sensitive flows before restricted admission.
3. **Event roots:** activate E006 (read), then E007 (global create), E008/E009
   (manage), each only after C0+EV, generic 401/403, same/cross-event, deleted
   root, forged body ID, revoked account/grant, and transaction-time tests.
   E005 is independent: keep admin-only until server-filtered collection tests
   cover every row, counts/pagination and unauthorized events.
4. **Event operations, individually:** implement and verify AREA for E033-E037,
   EBOAT for E038-E042, ASSIGN for E043-E048, TASK for E056-E060, OASSIGN+PERS
   for E049-E053 and TIMELINE for E061-E065. Each row's verb/side effects and
   response fields must be tested separately; GET success never approves PUT
   or DELETE. Establish explicit limited-role account assignments and query
   filters before admitting T/W; no full-task or full-boat update through
   own-progress/own-unit permissions.
5. **Unresolved/shared resources:** keep E010-E014, E025-E032, E054-E055 and
   E066-E069 inaccessible to non-admins until PERS/DEF/CREW/IMPORT/RADIO redesigns
   satisfy every row. Explicitly test malicious child IDs, spoofed route/body
   events, multievent global records, aliases and validation side channels.
   Import preview and commit require separate approvals; neither creates roles.
6. **Protected documents/sensitive fields last:** FILE+PERS before E080/E081,
   any protected metadata or sensitive official projection. Verify private
   storage, direct old URL denial (including cache/proxy), authorized streaming,
   metadata ownership/classification, no path traversal, explicit grants for A,
   expiry/revocation, content controls and security audit durability. Do not
   upload new sensitive bytes to public paths during a transition.
7. **Controlled activation:** use future explicit per-endpoint server-owned
   allowlists/kill switches, not an opt-out authorization attribute. Require
   successful negative tests and independent review for each inventory row;
   maintain default deny for unknown endpoints. Canary with synthetic data,
   compare expected decisions in an isolated environment, monitor generic
   denial/error rates and audit events without credentials/PI/MI. Shadow
   decisions alone must never expose an unprotected endpoint to restricted users.

For **every activated endpoint**, required evidence is: valid individual account
and allowed role success; missing/legacy/revoked session 401; missing permission,
cross-event and forged parent/child IDs 403; missing/ambiguous scope deny even
for admin; sensitive grants/storage checks; no side effects on denial; current
revocation and transaction-time checks; filtered response/count/pagination; CSRF;
safe audit/error handling. Endpoint activation records should link that evidence
to its inventory ID, gate implementation, approved configuration and owner.

**Rollback:** turn off the affected endpoint/cohort via a deny/maintenance gate,
revoke exposed sessions/grants as appropriate and retain audit evidence. Do not
fall back to shared-password authorization, frontend roles, anonymous delivery,
public static documents or cached positive decisions. If identity integration
fails, close business access; if returning to an older binary, fence the service
at ingress for administrators/maintenance and independently verify that restricted
users and old document URLs remain unreachable. Do not roll back schema/records
automatically. Restore compatibility only through a separately approved plan;
record affected endpoint IDs and incident evidence before reactivation.

## Isolated verification

New component tests use synthetic sessions/stores/resources only. Inventory
coverage tests reflect controller metadata without starting Program or loading
configuration; the Markdown inventory is copied as a test-only asset. Existing
backend persistence tests use newly created disposable PostgreSQL containers
and test-only schema fixtures, never an existing database or deployment
migration. No operational JSON store or existing uploaded file is opened.

```sh
dotnet test backend/EventHelm.Api.Tests/EventHelm.Api.Tests.csproj --configuration Release
dotnet build backend/EventHelm.Api/EventHelm.Api.csproj --configuration Release
dotnet format analyzers backend/EventHelm.Api.Tests/EventHelm.Api.Tests.csproj --no-restore --verify-no-changes --include backend/EventHelm.Api/Authorization/Integration backend/EventHelm.Api.Tests/ApiAuthorizationIntegrationTests.cs backend/EventHelm.Api.Tests/ApiAuthorizationInventoryTests.cs
cd frontend/eventhelm-admin
npm test
npm run build
npm run lint
```

The frontend is unchanged. Record pre-existing lint failures separately rather
than fixing unrelated frontend files. Review the complete diff against
`origin/design-system`; confirm no runtime wiring, protected branches, secrets,
database/migration files or frontend files changed.
