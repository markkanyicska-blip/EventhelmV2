# Authentication regression baseline and proposed permissions

## Phase 1 scope

Individual accounts and RBAC are not enabled. Existing shared-password login,
cookie settings, fallback authentication and application functionality remain
unchanged. Authentication registration is shared between production and tests
to avoid a separate, drifting copy of those settings.

Run from the repository root:

```sh
dotnet test backend/EventHelm.Api.Tests/EventHelm.Api.Tests.csproj --configuration Release
cd frontend/eventhelm-admin
npm ci
npm test
```

GitHub Actions runs these tests and existing builds on pull requests targeting
`design-system` and pushes to that branch.

## Isolation and coverage

Backend tests construct an in-memory TestServer, not the application entry
point or a default host. They load only an in-memory synthetic password,
production authentication registration and controller assembly. Data Protection
keys are ephemeral; cookie expiry uses an advancing test clock. User reads use
an in-memory fake; writes throw. No database connection, real application
service, static-file middleware, application JSON store, appsettings file,
environment-based configuration or user-secrets provider is registered.
The server does not listen on a network port.

Coverage includes anonymous/authenticated status, valid/invalid login, missing
password configuration, protected API rejection before controller activation,
logout cookie deletion, expired/malformed cookies and 403 without redirects or
session loss. The admin-only test endpoint exists only in the harness.

Frontend tests use jsdom, mocked page components, synthetic Axios adapters and
mocked fetch. Vitest disables dotenv loading and pins the API URL to `/api`.
Default transports reject unexpected requests rather than contacting a server.
Tests exercise the real client interceptors and App session handling, including
401 expiry, 403 without logout, status failure and successful logout.

These are focused authentication regressions, not end-to-end coverage of the
full production startup, deployment, database, operational pages or static
uploads. Logout currently clears the browser cookie; it does not revoke a
copied ticket server-side. This phase makes no new revocation guarantee.

## Initial permission matrix (proposal, not enforced)

All event permissions require active membership in that event. Multiple event
roles may be combined. An unknown role, expired membership or absent membership
grants nothing. System Administrator is global; other roles are event-scoped.
System administrators have an explicit audited override for event operations.

| Action | System Administrator | Organizer | Race Officer | Technical Staff | On-Water Personnel |
|---|---|---|---|---|---|
| Invite/suspend accounts; revoke sessions | Global | No | No | No | No |
| Grant/revoke System Administrator | Global; preserve last active admin | No | No | No | No |
| Create events; appoint/remove Organizers | Global | No | No | No | No |
| View event operational information | Global | Assigned event | Assigned event | Necessary event subset | Necessary event subset |
| Edit event setup, areas and tasks | Global | Assigned event | Operational planning only | No | No |
| Manage schedules, crews and personnel assignments | Global | Assigned event | Assigned event | No | No |
| Grant/revoke non-organizer event roles | Global | Assigned event; no self-escalation | No | No | No |
| Operate race status, flags, weather and command timeline | Global | Requires Race Officer role | Assigned event | No | No |
| Manage global boat/radio inventory | Global | No | No | No | No |
| Allocate event boats and radios | Global | Assigned event | Assigned event | Assigned equipment only | No |
| Report defects/incidents | Global | Assigned event | Assigned event | Assigned equipment | Own assigned unit |
| Resolve defects / edit equipment condition | Global | No | No | Assigned equipment | No |
| Update unit status / own task progress | Global | Assigned event | Assigned event | Own assignment | Own assignment |
| View or edit all user accounts | Global | No | No | No | No |
| View own account / operational assignments | Own account | Own account | Own account | Own account | Own account |
| Read sensitive personnel fields or private documents | Separate purpose-specific permission | Separate permission | Separate permission | No by role alone | No by role alone |

Membership does not grant access to unrelated events or all personnel contact
details. Medical information, emergency contacts and private documents require
separate purpose-specific authorization, including for system administrators.
Account/profile self-service must never accept privilege fields.

Operational labels in `Users.Role`, `Officials.Role`, imported assignments or
event committee names must not automatically become authorization grants.
Creating or importing an assignment must not create an account or grant access.
Event organizers may assign existing accounts; only system administrators
invite new accounts in the initial proposal.

## Prerequisites for later enforcement

- Introduce individual identities, explicit account-to-person links and event
  memberships without replacing existing IDs or assignments.
- Check resource ownership and event membership server-side on every endpoint,
  nested resource, list/search, import and download.
- Add event ownership to global JSON-backed Race Control before allowing
  event-scoped access; until then restrict it to global administrators at RBAC
  rollout. Shared global assets need explicit allocation rules.
- Replace public static document delivery with authorized downloads before
  claiming document privacy.
- Keep HTTP 401 (not authenticated) distinct from 403 (not permitted).
- Complete endpoint coverage before admitting restricted users. Shared-password
  sessions must not gain administrator rights or bypass the new permission
  model during a production pilot.

The matrix requires product approval before enforcement. Future phases need
permission-combination, cross-event access, privilege-escalation, invitation,
session-revocation and migration-preservation tests against disposable stores.
No database changes or migrations are included in Phase 1.

Phase 3 Step 1 now provides a [dormant typed RBAC foundation](rbac-foundations.md)
with the refined capability matrix, explicit sensitive grants, server-resolved
scope boundaries and isolated tests. It does not activate this proposal or
change existing shared-password behavior.
