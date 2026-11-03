# Phase 3 Step 2: API authorization inventory

Baseline: `origin/design-system` at `344218d` (merged dormant RBAC foundations).
**Preparation only. No entry below is activated or secured by this change.**
The companion [integration and rollout contract](api-authorization-integration.md)
defines trusted resource resolution, prerequisites, HTTP semantics and rollback.

## Coverage and notation

There are **81 explicit API/controller endpoints**: 80 controller actions and
`GET /api/health`. Development OpenAPI is entry E082, bringing the explicit
HTTP inventory to **82**. S001 separately covers static-file delivery, not a
controller action. Routing is case-insensitive; lowercase `officials`, `users`
and `boats` below expand `[controller]`. Crew routes really lack `/api`.
No extra route is inferred from a service method or a `Created` Location URL.

All authenticated entries are **activation-held** by C0. **53 entries have
additional scope blockers**: E005, E010-E014, E025-E069, E080-E081.
Of these, 21 have no reliable event boundary at all (E010-E014, E025-E032,
E054-E055, E066-E069, E080-E081); 31 need audited child/aggregate adapters
(E033-E053, E056-E065); E005 needs a filtered event collection boundary.
An event ID in a URL or SQL filter is not a deployed RBAC resource resolver.
The existing PostgreSQL resolver supports only verified Events/EventRoles roots
and the four fixed global keys; it cannot yet resolve these child resources.

Each row specifies method, exact route template, required permission/action,
scope, allowed roles, data exposure, existing behavior and its activation gate.
The gate table below is normative for **every** endpoint carrying that code.

| Code | Meaning |
|---|---|
| A | Active global System Administrator, loaded from current authorization storage, not a profile role |
| O / R | Active Organizer / Race Officer **in the resolved event** |
| T* / W* | Technical Staff / On-Water Personnel, only explicitly account-assigned resources; never an entire collection |
| S | Separate active, purpose-labelled, expiring sensitive grant for each relevant field class and event, **also for A**; event membership or A is additionally required |
| G | Fixed server-owned global key, no event ID; administrator-only |
| E | Existing Events row loaded by its own ID, not merely a route assertion |
| C | Canonical child row -> persisted EventId -> existing Events row; route/body event IDs must agree with this chain |
| U | No reliable event resolution today: **SCOPE-BLOCKED**; adding a query/body event ID is not a fix |
| L | Current shared-password cookie + fallback authenticated policy only; no individual identity, event membership, permissions or sensitive-field authorization |
| ANON | Explicit AllowAnonymous today; no RBAC decision |
| PI / MI / PD | Personal/contact/emergency data / medical data / private document content or identifying metadata/URLs |
| OP | Operational names, schedules, positions, channels, free-text notes; minimize and prohibit sensitive content |

`Permission (Kind.Action)` uses the existing enums. `+` means **all**, not any.
For sensitive writes that return a full DTO, both manage and read grants are
needed. PD grants apply to protected file metadata as well as file bytes.
Role lists for operational fields are the foundation's proposed candidates only;
they do not authorize the current mixed-sensitive DTOs. `A pending split`
means no non-administrator admission; A alone still cannot bypass S or U.
Global inventory reads conservatively use `ManageGlobalInventory` because
there is no narrower global inventory-read permission in the foundation.

## Endpoint inventory

| ID | Method | Endpoint | Required permission (resource/action) | Required scope | Allowed roles after gates | Exposure | Existing authorization | Activation prerequisites |
|---|---|---|---|---|---|---|---|---|
| E001 | `POST` | `/api/access/login` | Credential validation; not RBAC | No event | Anonymous credential exchange | Shared credential, cookie | ANON | AUTH; replace, never reinterpret legacy identity |
| E002 | `POST` | `/api/access/logout` | Valid individual session; revoke current session | Account/session, no event | Authenticated account, self only | Session cookie | L | AUTH; server-side revocation and CSRF |
| E003 | `GET` | `/api/access/status` | Minimal authentication status; not a role source | Session, no event | Anonymous | Authentication boolean | ANON | AUTH; individual validation, no role/token leakage |
| E004 | `GET` | `/api/health` | Authentication only; keep minimal liveness response | No event | Any valid individual session | Service/version/time | L | C0; any public probe exception needs separate review |
| E005 | `GET` | `/api/events` | ManageSystem (System.Manage) until filtered; then ReadEvent (Event.Read) per row | G System now; E per visible row later | A pending filtered list; O/R per event later | OP, leadership names/notes | L; unfiltered global list | SCOPE-BLOCKED: LIST |
| E006 | `GET` | `/api/events/{id:guid}` | ReadEvent (Event.Read) | E | A/O/R | OP, leadership/notes | L; lookup by ID | C0 + EV; projection review |
| E007 | `POST` | `/api/events` | CreateEvent (Event.Create) | G EventCreation | A only | OP; newly generated event ID | L | C0 + EV; no auto-role grant from creation |
| E008 | `PUT` | `/api/events/{id:guid}` | ManageEvent (Event.Manage) | E | A/O | OP; full event body/response | L; update by route ID | C0 + EV; body ID cannot move scope |
| E009 | `DELETE` | `/api/events/{id:guid}` | ManageEvent (Event.Manage) | E | A/O | Event/related-data destruction | L | C0 + EV; dependency and grant lifecycle review |
| E010 | `GET` | `/api/officials` | ReadOfficials (Officials.Read) + ReadPersonalInformation (PersonalInformation.Read) + ReadMedicalInformation (MedicalInformation.Read) + ReadPrivateDocuments (Documents.Read, private) | U global personnel; future E per projected row | A pending split + S; O/R operational-only later | PI/MI/PD, all officials | L; global full DTOs | SCOPE-BLOCKED: PERS + LIST + FILE |
| E011 | `GET` | `/api/officials/{id:guid}` | ReadOfficials + ReadPersonalInformation + ReadMedicalInformation + ReadPrivateDocuments (same Read actions as E010) | U; official may belong to multiple events | A pending split + S; O/R operational-only later | PI/MI/PD | L; global lookup | SCOPE-BLOCKED: PERS + FILE |
| E012 | `POST` | `/api/officials` | ManageOfficials (Officials.Manage) + ManagePersonalInformation (PersonalInformation.Manage) + ManageMedicalInformation (MedicalInformation.Manage) + ManagePrivateDocuments (Documents.Manage, private); returned fields also require E010 reads | U global create | A pending split + S | PI/MI/PD input and response | L | SCOPE-BLOCKED: PERS + FILE; global creation workflow |
| E013 | `PUT` | `/api/officials/{id:guid}` | E012 manage permissions + E010 reads | U global mutation affects other events | A pending split + S | PI/MI/PD input and response | L | SCOPE-BLOCKED: PERS + FILE; protect shared record |
| E014 | `DELETE` | `/api/officials/{id:guid}` | ManageOfficials (Officials.Manage) + ManagePersonalInformation (PersonalInformation.Manage) + ManageMedicalInformation (MedicalInformation.Manage) + ManagePrivateDocuments (Documents.Manage, private) | U global destructive mutation | A pending split + S | Deletes PI/MI/document references | L | SCOPE-BLOCKED: PERS + FILE; all-event dependency/retention review |
| E015 | `GET` | `/api/users` | ManageSystem (System.Manage) | G System, global user administration | A only | Names, email, phone, legacy Role, IsActive | L | C0 + ADMIN; minimized administrative projection |
| E016 | `GET` | `/api/users/{id:guid}` | ManageSystem (System.Manage) | G System; verify target user separately | A only | Account-administration contact data | L | C0 + ADMIN; no self-service implicit exception |
| E017 | `POST` | `/api/users` | ManageSystem (System.Manage) | G System | A only | Contact data, editable Role/IsActive | L | C0 + ADMIN; never provision authority from Role |
| E018 | `PUT` | `/api/users/{id:guid}` | ManageSystem (System.Manage) | G System; persisted target user | A only | Contact data, Role/IsActive; account eligibility | L | C0 + ADMIN; revocation/version and last-admin protection |
| E019 | `DELETE` | `/api/users/{id:guid}` | ManageSystem (System.Manage) | G System; persisted target user | A only | Account/user deletion and dependencies | L | C0 + ADMIN; session/grant lifecycle and last-admin protection |
| E020 | `GET` | `/api/boats` | ManageGlobalInventory (GlobalInventory.Manage) | G GlobalInventory | A only | Global boat inventory/notes | L | C0 + INV; event users need separate filtered event projection |
| E021 | `GET` | `/api/boats/{id:guid}` | ManageGlobalInventory (GlobalInventory.Manage) | G GlobalInventory; persisted global boat | A only | Global registration/equipment/notes | L | C0 + INV; never treat boat ID as event ID |
| E022 | `POST` | `/api/boats` | ManageGlobalInventory (GlobalInventory.Manage) | G GlobalInventory | A only | Global equipment input/response | L | C0 + INV |
| E023 | `PUT` | `/api/boats/{id:guid}` | ManageGlobalInventory (GlobalInventory.Manage) | G GlobalInventory; persisted global boat | A only | Global equipment mutation | L | C0 + INV; changes affect every event using boat |
| E024 | `DELETE` | `/api/boats/{id:guid}` | ManageGlobalInventory (GlobalInventory.Manage) | G GlobalInventory; persisted global boat | A only | Global equipment deletion | L | C0 + INV; dependency checks |
| E025 | `GET` | `/api/boats/{boatId:guid}/defects` | ManageGlobalInventory (GlobalInventory.Manage), interim boundary; future event ReadBoats (Boats.Read) | U defects -> global BoatId, not event | A pending event split; A/O/R/T*/W* later | Reporter name, defect text/priority/status | L; BoatId filter only | SCOPE-BLOCKED: DEF |
| E026 | `POST` | `/api/boats/{boatId:guid}/defects` | ManageGlobalInventory (GlobalInventory.Manage), interim boundary; future ReportDefect (Boats.Report) | U; no event provenance on new defect | A pending event split; A/O/R/T*/W* later | Client ReportedBy, free text | L; verifies global boat exists | SCOPE-BLOCKED: DEF; derive reporter account server-side |
| E027 | `PATCH` | `/api/boats/{boatId:guid}/defects/{defectId:guid}/status` | ManageGlobalInventory (GlobalInventory.Manage), interim boundary; future ResolveDefect (Boats.Resolve) | U defect -> BoatId; must also match route boat | A pending event split; A/T* later | Safety-critical status | L; SQL matches boat and defect | SCOPE-BLOCKED: DEF; no organizer/race-officer resolve permission |
| E028 | `GET` | `/boats/{boatId:guid}/crew` | ReadAssignments (Assignments.Read) + ReadPersonalInformation (PersonalInformation.Read); interim ManageSystem (System.Manage) boundary | U global BoatCrewAssignments | A pending split + S; A/O/R/T*/W* minimized later | Crew names, contact, photo URL, dates/notes | L; outside /api but fallback applies | SCOPE-BLOCKED: CREW + PERS |
| E029 | `GET` | `/boats/{boatId:guid}/crew/{id:guid}` | E028 permissions/actions | U crew -> global boat/official | A pending split + S | Crew PI | L; post-read boat equality check | SCOPE-BLOCKED: CREW + PERS |
| E030 | `POST` | `/boats/{boatId:guid}/crew` | ManageAssignments (Assignments.Manage) + E028 reads on response; interim ManageSystem (System.Manage) | U global crew creation | A pending split + S; A/O/R minimized later | OfficialId, dates, captain, returned PI | L | SCOPE-BLOCKED: CREW + PERS |
| E031 | `PUT` | `/boats/{boatId:guid}/crew/{id:guid}` | E030 permissions/actions | U; resolve persisted crew BEFORE update | A pending split + S; A/O/R minimized later | Captain/crew mutation, returned PI | L; boat mismatch checked AFTER mutation | SCOPE-BLOCKED: CREW + PERS; move relationship check before side effects |
| E032 | `DELETE` | `/boats/{boatId:guid}/crew/{id:guid}` | ManageAssignments (Assignments.Manage); interim ManageSystem (System.Manage) | U global crew | A pending split; A/O/R later | Crew/captain removal | L; pre-read boat check, no event | SCOPE-BLOCKED: CREW |
| E033 | `GET` | `/api/events/{eventId:guid}/areas` | ReadEvent (Event.Read) + verified area projection | E + C EventAreas collection | A/O/R | Positions, area descriptions | L; EventId SQL filter | SCOPE-BLOCKED: AREA + LIST |
| E034 | `GET` | `/api/events/{eventId:guid}/areas/{areaId:guid}` | ReadEvent (Event.Read) + verified area ownership | C EventAreas -> E | A/O/R | Positions/descriptions | L; event/area SQL filter | SCOPE-BLOCKED: AREA |
| E035 | `POST` | `/api/events/{eventId:guid}/areas` | PlanEvent (Event.Plan) | E; create in verified event | A/O/R | Operational area input/response | L; checks event existence | SCOPE-BLOCKED: AREA; audited create boundary |
| E036 | `PUT` | `/api/events/{eventId:guid}/areas/{areaId:guid}` | PlanEvent (Event.Plan) + area ownership | C EventAreas -> E | A/O/R | Position/area mutation | L; event/area SQL filter | SCOPE-BLOCKED: AREA |
| E037 | `DELETE` | `/api/events/{eventId:guid}/areas/{areaId:guid}` | PlanEvent (Event.Plan) + area ownership | C EventAreas -> E | A/O/R | Area deletion | L; event/area SQL filter | SCOPE-BLOCKED: AREA; dependent boat/assignment review |
| E038 | `GET` | `/api/events/{eventId:guid}/boats` | ReadBoats (Boats.Read) | E + C EventBoats collection | A/O/R; T*/W* only filtered assigned subset | Boat/unit, channels, registration, notes | L; event filter, joined global boat | SCOPE-BLOCKED: EBOAT + LIST |
| E039 | `GET` | `/api/events/{eventId:guid}/boats/{eventBoatId:guid}` | ReadBoats (Boats.Read) | C EventBoats -> E | A/O/R/T*/W* | Equipment/channels/notes | L; event/eventBoat filter | SCOPE-BLOCKED: EBOAT |
| E040 | `POST` | `/api/events/{eventId:guid}/boats` | AllocateBoats (Boats.Allocate) | E; validate boat/area/task/shift references | A/O/R; T* only explicit equipment allocation authority | Equipment allocation | L; reference checks, no account relationship | SCOPE-BLOCKED: EBOAT |
| E041 | `PUT` | `/api/events/{eventId:guid}/boats/{eventBoatId:guid}` | AllocateBoats (Boats.Allocate), NOT UpdateOwnUnit for full DTO | C EventBoats -> E + same-event references | A/O/R/T* | Whole allocation, status, channel, notes | L | SCOPE-BLOCKED: EBOAT; narrow own-unit endpoint must be separate |
| E042 | `DELETE` | `/api/events/{eventId:guid}/boats/{eventBoatId:guid}` | AllocateBoats (Boats.Allocate) | C EventBoats -> E | A/O/R/T* | Removes event allocation | L; event/eventBoat filter | SCOPE-BLOCKED: EBOAT; assigned equipment/dependency checks |
| E043 | `GET` | `/api/events/{eventId:guid}/assignments` | ReadAssignments (Assignments.Read) | E + C EventAssignments collection | A/O/R; T*/W* only assigned subset | Personnel IDs, schedules, actual times, notes | L; event/time filters | SCOPE-BLOCKED: ASSIGN + LIST |
| E044 | `GET` | `/api/events/{eventId:guid}/assignments/{assignmentId:guid}` | ReadAssignments (Assignments.Read) | C EventAssignments -> E | A/O/R/T*/W* | Schedule/notes/workload | L; event/assignment filter | SCOPE-BLOCKED: ASSIGN |
| E045 | `POST` | `/api/events/{eventId:guid}/assignments` | ManageAssignments (Assignments.Manage) | E; validate every referenced resource in event | A/O/R | Personnel/schedule mutation | L; EventOfficials and linked-resource validation | SCOPE-BLOCKED: ASSIGN |
| E046 | `PUT` | `/api/events/{eventId:guid}/assignments/{assignmentId:guid}` | ManageAssignments (Assignments.Manage) | C EventAssignments -> E; old and proposed references | A/O/R | Full assignment mutation | L; event/assignment filter | SCOPE-BLOCKED: ASSIGN |
| E047 | `DELETE` | `/api/events/{eventId:guid}/assignments/{assignmentId:guid}` | ManageAssignments (Assignments.Manage) | C EventAssignments -> E | A/O/R | Assignment deletion | L | SCOPE-BLOCKED: ASSIGN |
| E048 | `POST` | `/api/events/{eventId:guid}/assignments/validate` | ManageAssignments (Assignments.Manage), not anonymous validation | E + C candidate references/excluded assignment | A/O/R | Availability, conflicts and workload inference | L; validation queries | SCOPE-BLOCKED: ASSIGN; authorize before validation queries |
| E049 | `GET` | `/api/events/{eventId:guid}/official-assignments` | ReadAssignments (Assignments.Read) + ReadPersonalInformation (PersonalInformation.Read) for current DTO | E + C EventOfficialAssignments -> E | A/O/R + S; T*/W* only minimized own subset later | Names, phone/email, radio/boat/notes | L; event filter, global official join | SCOPE-BLOCKED: OASSIGN + PERS + LIST |
| E050 | `GET` | `/api/events/{eventId:guid}/official-assignments/{assignmentId:guid}` | E049 permissions/actions | C EventOfficialAssignments -> E | A/O/R + S; T*/W* minimized later | PI, OP | L; event/assignment filter | SCOPE-BLOCKED: OASSIGN + PERS |
| E051 | `POST` | `/api/events/{eventId:guid}/official-assignments` | ManageAssignments (Assignments.Manage) + E049 reads for returned DTO | E + C referenced official/boat | A/O/R + S for response | PI in response, role labels/notes | L; validates global references | SCOPE-BLOCKED: OASSIGN + PERS |
| E052 | `PUT` | `/api/events/{eventId:guid}/official-assignments/{assignmentId:guid}` | E051 permissions/actions | C EventOfficialAssignments -> E; old/new references | A/O/R + S for response | PI, OP mutation | L | SCOPE-BLOCKED: OASSIGN + PERS |
| E053 | `DELETE` | `/api/events/{eventId:guid}/official-assignments/{assignmentId:guid}` | ManageAssignments (Assignments.Manage) | C EventOfficialAssignments -> E | A/O/R | Operational assignment deletion | L; event/assignment filter | SCOPE-BLOCKED: OASSIGN |
| E054 | `POST` | `/api/events/{eventId:guid}/assignment-import/preview` | ManageAssignments (Assignments.Manage) + ReadPersonalInformation (PersonalInformation.Read); interim ManageSystem (System.Manage) | E for target, U for global matching/existence queries | A pending split + S | Spreadsheet PI, global existence inference | L; event exists, global email/phone/name matching | SCOPE-BLOCKED: IMPORT + PERS |
| E055 | `POST` | `/api/events/{eventId:guid}/assignment-import/commit` | ManageAssignments (Assignments.Manage) + ManageOfficials (Officials.Manage) + ManagePersonalInformation (PersonalInformation.Manage) + ManageGlobalInventory (GlobalInventory.Manage) | E target + U global shared official/boat side effects | A only pending split + S | PI writes, global boats/officials, assignment replacement | L; transaction, global upserts/matching | SCOPE-BLOCKED: IMPORT + PERS + INV |
| E056 | `GET` | `/api/events/{eventId:guid}/tasks` | ReadTasks (Tasks.Read) | E + C EventTasks collection | A/O/R; T*/W* assigned subset only | Requirements, qualifications, notes | L; event filter | SCOPE-BLOCKED: TASK + LIST |
| E057 | `GET` | `/api/events/{eventId:guid}/tasks/{taskId:guid}` | ReadTasks (Tasks.Read) | C EventTasks -> E | A/O/R/T*/W* | Task requirements/notes | L; event/task filter | SCOPE-BLOCKED: TASK |
| E058 | `POST` | `/api/events/{eventId:guid}/tasks` | ManageTasks (Tasks.Manage) | E; trusted task creation boundary | A/O/R | Task input/response | L; event existence check | SCOPE-BLOCKED: TASK |
| E059 | `PUT` | `/api/events/{eventId:guid}/tasks/{taskId:guid}` | ManageTasks (Tasks.Manage), NOT UpdateOwnTask | C EventTasks -> E | A/O/R | Full task definition | L; event/task filter | SCOPE-BLOCKED: TASK; separate own-progress DTO/endpoint required |
| E060 | `DELETE` | `/api/events/{eventId:guid}/tasks/{taskId:guid}` | ManageTasks (Tasks.Manage) | C EventTasks -> E | A/O/R | Task deletion | L | SCOPE-BLOCKED: TASK |
| E061 | `GET` | `/api/events/{eventId:guid}/timeline` | ReadAssignments (Assignments.Read) + ReadBoats (Boats.Read) + ReadTasks (Tasks.Read) + ReadEvent (Event.Read) for area projection | E + every C contributing row | A/O/R; limited-role projection requires separate boundary | Names, movements, schedules, notes | L; event/time-filtered joins | SCOPE-BLOCKED: TIMELINE + LIST |
| E062 | `GET` | `/api/events/{eventId:guid}/timeline/officials` | ReadAssignments (Assignments.Read) | E + C EventAssignments + verified officials | A/O/R; T*/W* own subset only | Personnel names/schedules/notes | L; event/time filter | SCOPE-BLOCKED: TIMELINE + LIST |
| E063 | `GET` | `/api/events/{eventId:guid}/timeline/officials/{officialId:guid}` | ReadAssignments (Assignments.Read) | C event assignment/official relationship -> E | A/O/R/T*/W* | Individual movements/workload | L; event/official filter | SCOPE-BLOCKED: TIMELINE; global OfficialId alone insufficient |
| E064 | `GET` | `/api/events/{eventId:guid}/timeline/boats` | ReadAssignments (Assignments.Read) + ReadBoats (Boats.Read) | E + C assignments/EventBoats | A/O/R; T*/W* assigned subset only | Units, personnel, location/schedule | L; event filter | SCOPE-BLOCKED: TIMELINE + LIST |
| E065 | `GET` | `/api/events/{eventId:guid}/timeline/boats/{eventBoatId:guid}` | ReadAssignments (Assignments.Read) + ReadBoats (Boats.Read) | C EventBoats and contributing assignments -> E | A/O/R/T*/W* | Unit movements/personnel/notes | L; event/eventBoat filter | SCOPE-BLOCKED: TIMELINE |
| E066 | `GET` | `/api/events/{eventId}/official-radio-assignments` | ReadAssignments (Assignments.Read) + ReadBoats (Boats.Read) for equipment allocation projection | U JSON event string, no authoritative event/official validation | A pending boundary; A/O/R/T*/W* filtered later | Personnel IDs, radios, labels, notes | L; JSON string equality only | SCOPE-BLOCKED: RADIO + LIST |
| E067 | `POST` | `/api/events/{eventId}/official-radio-assignments` | ManageAssignments (Assignments.Manage) + AllocateBoats (Boats.Allocate), equipment semantics | U JSON event/official strings | A pending boundary; A/O/R later | Radio allocations and notes | L; equipment validation, no event/official existence proof | SCOPE-BLOCKED: RADIO |
| E068 | `PUT` | `/api/events/{eventId}/official-radio-assignments/{assignmentId}` | E067 permissions/actions | U JSON child/event strings; verify old/new relationships | A pending boundary; A/O/R later | Radio allocation mutation | L; JSON event/assignment equality | SCOPE-BLOCKED: RADIO |
| E069 | `DELETE` | `/api/events/{eventId}/official-radio-assignments/{assignmentId}` | E067 permissions/actions | U JSON child/event strings | A pending boundary; A/O/R later | Allocation removal | L | SCOPE-BLOCKED: RADIO |
| E070 | `GET` | `/api/radios` | ManageGlobalInventory (GlobalInventory.Manage) | G GlobalInventory | A only | Global radio serial/inventory/notes | L; global JSON | C0 + INV; no implied event membership |
| E071 | `POST` | `/api/radios` | ManageGlobalInventory (GlobalInventory.Manage) | G GlobalInventory | A only | Global equipment creation | L | C0 + INV |
| E072 | `PUT` | `/api/radios/{id}` | ManageGlobalInventory (GlobalInventory.Manage) | G GlobalInventory; persisted JSON radio ID | A only | Global equipment mutation | L | C0 + INV; string ID is not a ResourceKey UUID without validated mapping |
| E073 | `GET` | `/api/race-control` | ReadRaceControl (RaceControl.Read) | G GlobalRaceControl | A only, never event R | Entire global state/messages/positions | L; global JSON singleton | C0 + RC |
| E074 | `PUT` | `/api/race-control/status` | OperateRaceControl (RaceControl.Operate) | G GlobalRaceControl | A only | Global status, returns whole state | L | C0 + RC |
| E075 | `PUT` | `/api/race-control/flag` | OperateRaceControl (RaceControl.Operate) | G GlobalRaceControl | A only | Global flags/state | L | C0 + RC |
| E076 | `PUT` | `/api/race-control/weather` | OperateRaceControl (RaceControl.Operate) | G GlobalRaceControl | A only | Global weather/state | L | C0 + RC |
| E077 | `PUT` | `/api/race-control/radio` | OperateRaceControl (RaceControl.Operate), NOT event SendCommunications | G GlobalRaceControl | A only | LastMessage/call signs/global radio/state | L | C0 + RC + COM |
| E078 | `PUT` | `/api/race-control/units/{id}` | OperateRaceControl (RaceControl.Operate), NOT UpdateOwnUnit | G GlobalRaceControl; persisted unit string ID | A only | Unit locations/status, whole global state | L | C0 + RC; no account-to-unit link |
| E079 | `POST` | `/api/race-control/timeline` | OperateRaceControl (RaceControl.Operate), NOT event SendCommunications | G GlobalRaceControl | A only | Free-text system messages, whole state | L | C0 + RC + COM |
| E080 | `POST` | `/api/uploads/official-document` | ManagePrivateDocuments (Documents.Manage, private); read grant if response exposes protected metadata | U no owner/event/classification; public static destination | A pending private storage + S, never role alone | Uploaded licenses/qualifications/medical/other bytes, original name/URL | L; extension/MIME/size only | SCOPE-BLOCKED: FILE |
| E081 | `DELETE` | `/api/uploads/official-document` | ManagePrivateDocuments (Documents.Manage, private) | U client URL -> filename only, no owner/event metadata | A pending private storage + S | Arbitrary matching official file removal | L; prefix/basename/file existence only | SCOPE-BLOCKED: FILE; opaque metadata ID instead of URL |
| E082 | `GET` | `/openapi/{documentName}.json` | ManageSystem (System.Manage), or disable outside controlled development | G System, no event | A only if exposed | Full API schema, examples/shape | L via fallback; Development only | C0 + ADMIN; do not make public incidentally |

## Static files and absent surfaces

| ID | Method | Endpoint/surface | Required boundary | Current behavior | Blocker |
|---|---|---|---|---|---|
| S001 | GET/HEAD | `/uploads/officials/{fileName}` and any other protected content under `wwwroot` | ReadPrivateDocuments (Documents.Read, private) + S on verified metadata/owner/event; authenticated authorized streaming only | Public static files run before authentication/authorization; no endpoint RBAC evaluation | **BLOCKED even for admin rollout**: FILE; prohibit new sensitive uploads, relocate protected content in separately approved work; no static fallback or bearer URLs |

There are **no standalone shift, communications, document-download, account,
invitation, individual-session-management, RBAC-provisioning or administrative role endpoints**.
`EventShifts` is referenced by event boats; availability/rules/workload tables
are queried by assignment validation. `OfficialService.GetProfileAsync` exists
but is not routed; do not invent `/api/officials/{id}/profile` in the count.
The two JSON services expose communications only through Race Control radio/
timeline fields and radio allocation notes; event boat allocations carry channels.
A future endpoint needs a new inventory row and independent authorization tests
before activation.

## Activation gates (in addition to C0)

| Gate | Exact prerequisites / resource-resolution contract |
|---|---|
| C0 | Approved matrix and data classification; separate individual session authentication scheme/transport; current session/account/user eligibility; current RBAC grants; audited operation definitions and authoritative resolvers; deny unknown endpoints/actions; CSRF for cookie mutations, rate limiting, generic errors, persistent security auditing, transactional rechecks for writes; independent negative tests and explicit per-endpoint activation approval. None is supplied by a legacy shared-password login. |
| AUTH | Implement/review individual credential endpoints separately, session rotation/revocation, secure HttpOnly cookie transport, CSRF, rate limits and generic failures. Never convert shared tickets or `Users.Role` into accounts/permissions. Retire shared-password business access before admitting restricted users. |
| EV | Existing resolver can verify an Events root. Still verify allowed fields, dependencies, transaction-time eligibility/scope, root existence and mismatched body IDs before read/write. Event creation uses only fixed EventCreation key. |
| LIST | Filter SQL/JSON by authorized event and, for T*/W*, persisted account assignments **before** pagination/count/search/aggregation; authorize each field class and contributing resource. Never call global GetAll then filter in the browser or infer collection access from one child. |
| PERS | Separate minimized event-operational DTOs from global personnel and PI/MI/PD DTOs. Establish explicit official-to-event and account-to-person links; a person can appear in several events. Global mutations are not delegated through one event. Require distinct sensitive read/manage grants for each event/class, including returned fields. A global administrator is not a global sensitive grant. No contact/name matching as identity. |
| ADMIN | Global System key + current global administrator grant; validate target existence and allowed fields. Users.Role remains non-authoritative. Separate audited account/role provisioning, bootstrap and last-active-admin protection; eligibility changes revoke/version sessions correctly. No self-service bypass on these routes. |
| INV | GlobalInventory fixed key + A; load requested global resource from its actual store for existence. Global boat/radio edits remain global-admin only. Event users get only verified event allocations and minimized equipment projections; do not derive an event from global BoatId or radio ID. |
| DEF | Model defect provenance/visibility explicitly. BoatDefects only stores global BoatId and one boat can serve many events; EventBoats existence in one event cannot authorize all that boat's defects. Persist defect-to-event relationship/validated event allocation, check defect/boat/event before mutations, derive reporting account server-side, preserve global maintenance separately. |
| CREW | Model event-specific crew/boat/official/account relationships; current BoatCrewAssignments has no EventId. Dates/name labels do not establish scope. Resolve record and compare route boat **before** update, including captain-removal side effects. Add PI projection/grants separately. |
| AREA | Verify persisted EventAreas.EventId and referenced Events before use. There is no Areas resource enum: use verified Event.Read/Event.Plan on the resolved parent **plus** an independently audited child-ownership check, not ResourceKey(Event, areaId). Creates need verified parent; deletes need reference review. |
| EBOAT | Resolver loads EventBoats by its canonical ID then Events, checks route equality, global boat and same-event area/task/shift references. Existing joins on optional IDs must be audited for malformed cross-event data; write validation alone is insufficient. Explicit account-to-equipment link for T*/W*, no name/email inference. Creates authorize a verified allocation collection, not a fictional child. |
| ASSIGN | Resolve EventAssignments.EventId, EventOfficials membership, task/area/EventBoat and any assignment group/excluded assignment in the same event. Define account-to-assignment links for limited roles. Validation is sensitive operational querying and needs permission before any conflict/workload/availability lookup. |
| OASSIGN | Resolve EventOfficialAssignments by ID, then its event and official/boat references; existing global existence checks are not event relationships. Distinguish this resource family from EventAssignments (both are UUIDs under Assignments semantics); audited typed identity/discriminator required to avoid aliasing. Returned OfficialPhone/OfficialEmail need separate PI read or removal. Role labels never grant RBAC. |
| IMPORT | Preview leaks global matching/existence; commit writes global Officials/Boats as well as event assignments and can replace prior imports. Keep both administrator-only until scope-safe matching, explicit per-side-effect permissions and PI grants, staging ownership/expiry, server revalidation of committed rows, transaction-bound authorization and audits exist. Never grant roles from spreadsheet labels. |
| TASK | Resolve EventTasks.EventId, event existence, account-to-task assignment and safe notes projection. Entire PUT edits task definitions, so UpdateOwnTask must not authorize it. A future progress-only endpoint needs a narrow DTO and independent boundary. |
| TIMELINE | Resolve and filter every contributing EventAssignments/EventBoats/official/task/area row; ensure all joins stay in event, redact notes and verify requested official/eventBoat membership and limited-role assignment. An authorized parent event is not permission to read all aggregates. |
| RADIO | JSON accepts arbitrary event/official strings. Validate canonical UUIDs against existing Events and official-in-event links; resolve stored assignment independently of claimed event, verify selected global radio and persisted account/equipment relationships. Use validated typed identity mapping (not hashes or parsing arbitrary labels). Address cross-event equipment allocation conflicts and authoritative store consistency before delegation. |
| RC | Preserve fixed GlobalRaceControl key and A for all seven routes. Whole-state responses include other units/messages/locations. Event RaceOfficer membership, query event IDs and UpdateOwnUnit are insufficient. Any future event Race Control requires separate state partitioning, typed resolvers, projections and new endpoint approval; never reclassify this singleton as event-scoped. |
| COM | No independent message/channel permission boundary today. Global Race Control messages stay under RC. Future event messages need persisted event/channel/account relationships and ReadCommunications (Communications.Read) / SendCommunications (Communications.Operate); no client-selected channel grants access. |
| FILE | Persist opaque document ID, owner, canonical event relationship, classification, private storage key and lifecycle; resolve those server-side, never client URL/path/event metadata. Move protected bytes outside static roots, deny old direct paths at application/proxy/CDN and invalidate cached public copies via an approved plan. Add authorized upload/download/delete/metadata flow, malware/content validation, path protections, retention and auditing; refuse public/unknown storage even for A. Existing filename/MIME checks and random names are not access control. |

## Routes unavailable to non-administrators during future admission

Before any non-administrator is admitted, the deployment must close **all**
unactivated business endpoints, including aliases, direct static paths and routes
outside `/api`. This PR does not introduce that runtime gate.

Permanently global-admin-only under today's model: E007 (event creation),
E015-E024 (users/global boats), E070-E079 (global radios/all Race Control),
and E082 if exposed. E005 stays admin-only until LIST. E010-E014, E025-E032,
E054-E055, E066-E069 and E080-E081 stay unavailable to non-administrators
until their unresolved boundaries are redesigned and independently verified.
Sensitive officials/files additionally remain blocked even for A unless every
classification/scope/storage/grant requirement is satisfied. S001 must not be a
delivery mechanism for protected content for **any** role.
All remaining event endpoints E006/E008-E009/E033-E053/E056-E065 remain
unavailable to non-administrators until C0 and their listed gates pass.
Health/authentication exceptions do not grant any business permission.
