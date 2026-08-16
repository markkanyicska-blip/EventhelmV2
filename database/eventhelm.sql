--
-- PostgreSQL database dump
--

\restrict XtekjniANFdJOZ6ScwhxVSZbfgcYiKWt0R5xsmPZUbfP9QrMaCN1OHGwWYYRmQ6

-- Dumped from database version 18.4
-- Dumped by pg_dump version 18.4

-- Started on 2026-07-19 20:00:10

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 2 (class 3079 OID 16478)
-- Name: pgcrypto; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;


--
-- TOC entry 5160 (class 0 OID 0)
-- Dependencies: 2
-- Name: EXTENSION pgcrypto; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION pgcrypto IS 'cryptographic functions';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 224 (class 1259 OID 16437)
-- Name: Assignments; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Assignments" (
    "Id" uuid NOT NULL,
    "OfficialId" uuid NOT NULL,
    "BoatId" uuid,
    "ShiftId" uuid NOT NULL,
    "Role" character varying(120) NOT NULL,
    "TaskDescription" text NOT NULL,
    "Status" character varying(40) DEFAULT 'Planned'::character varying NOT NULL,
    "Priority" character varying(20) DEFAULT 'Normal'::character varying NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."Assignments" OWNER TO postgres;

--
-- TOC entry 222 (class 1259 OID 16412)
-- Name: Boats; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Boats" (
    "Id" uuid NOT NULL,
    "Name" character varying(150) NOT NULL,
    "RegistrationNumber" character varying(80),
    "CallSign" character varying(80),
    "Status" character varying(40) DEFAULT 'Available'::character varying NOT NULL,
    "Notes" text,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL,
    "Type" character varying(100) DEFAULT 'RIB'::character varying NOT NULL,
    "Capacity" integer DEFAULT 0 NOT NULL
);


ALTER TABLE public."Boats" OWNER TO postgres;

--
-- TOC entry 227 (class 1259 OID 16579)
-- Name: EventAssignmentImportBatches; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."EventAssignmentImportBatches" (
    "Id" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "FileName" text NOT NULL,
    "ImportedAt" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "TotalRows" integer DEFAULT 0 NOT NULL,
    "ImportedRows" integer DEFAULT 0 NOT NULL,
    "SkippedRows" integer DEFAULT 0 NOT NULL,
    "ErrorRows" integer DEFAULT 0 NOT NULL,
    "Status" character varying(30) DEFAULT 'Completed'::character varying NOT NULL,
    "ErrorMessage" text,
    "ReplacePreviousImport" boolean DEFAULT false NOT NULL,
    "CreatedOfficials" integer DEFAULT 0 NOT NULL,
    "UpdatedOfficials" integer DEFAULT 0 NOT NULL,
    "CreatedBoats" integer DEFAULT 0 NOT NULL,
    "CreatedAssignments" integer DEFAULT 0 NOT NULL,
    "UpdatedAssignments" integer DEFAULT 0 NOT NULL,
    "RemovedAssignments" integer DEFAULT 0 NOT NULL,
    "CompletedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."EventAssignmentImportBatches" OWNER TO postgres;

--
-- TOC entry 226 (class 1259 OID 16534)
-- Name: EventOfficialAssignments; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."EventOfficialAssignments" (
    "Id" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "OfficialId" uuid NOT NULL,
    "Role" text NOT NULL,
    "TrunkRadioNumber" character varying(3),
    "VhfRadioNumber" character varying(4),
    "BoatId" uuid,
    "AttendanceStatus" text DEFAULT 'Planned'::text NOT NULL,
    "Notes" text,
    "CreatedAt" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "UpdatedAt" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "GroupName" text,
    "TaskName" text,
    "SourceFile" text,
    "ImportBatchId" uuid,
    "SortOrder" integer DEFAULT 0,
    "ImportKey" character varying(64),
    "SubgroupName" text,
    "Task" text,
    "AssignmentOrder" integer DEFAULT 0 NOT NULL,
    "Source" text,
    "SourceRowNumber" integer,
    CONSTRAINT "CK_EventOfficialAssignments_Attendance" CHECK (("AttendanceStatus" = ANY (ARRAY['Planned'::text, 'Present'::text, 'Absent'::text, 'Standby'::text]))),
    CONSTRAINT "CK_EventOfficialAssignments_Role" CHECK ((length(TRIM(BOTH FROM "Role")) > 0)),
    CONSTRAINT "CK_EventOfficialAssignments_Trunk" CHECK ((("TrunkRadioNumber" IS NULL) OR (("TrunkRadioNumber")::text ~ '^[0-9]{3}$'::text))),
    CONSTRAINT "CK_EventOfficialAssignments_Vhf" CHECK ((("VhfRadioNumber" IS NULL) OR (("VhfRadioNumber")::text ~ '^[0-9]{4}$'::text)))
);


ALTER TABLE public."EventOfficialAssignments" OWNER TO postgres;

--
-- TOC entry 220 (class 1259 OID 16389)
-- Name: Events; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Events" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Status" text NOT NULL,
    "OrganizingClub" text,
    "HostingClub" text,
    "StartDate" timestamp without time zone,
    "EndDate" timestamp without time zone,
    "Location" text,
    "WaterArea" text,
    "RaceOfficer" text,
    "RaceOffice" text,
    "VhfChannel" text,
    "TrunkGroup" text,
    "Notes" text,
    "JuryChair" text,
    "TechnicalCommitteeChair" text
);


ALTER TABLE public."Events" OWNER TO postgres;

--
-- TOC entry 221 (class 1259 OID 16399)
-- Name: Officials; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Officials" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Phone" text,
    "Email" text,
    "Role" text NOT NULL,
    "Status" text NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "Club" text,
    "ShirtSize" text,
    "ProfileImageUrl" text,
    "Qualification" text,
    "QualificationValidUntil" date,
    "QualificationDocumentUrl" text,
    "BoatLicenseType" text,
    "BoatLicenseValidUntil" date,
    "BoatLicenseDocumentUrl" text,
    "FoodAllergies" text,
    "DrugAllergies" text,
    "HealthNotes" text,
    "EmergencyContactName" text,
    "EmergencyContactPhone" text,
    "EmergencyContactRelation" text,
    "Languages" text,
    "OtherDocumentUrl" text,
    "OrganizerQualification" text,
    "JudgeQualification" text,
    "MeasurerQualification" text,
    "IsCompetitionAdministrator" boolean DEFAULT false NOT NULL,
    "IsResultScorer" boolean DEFAULT false NOT NULL
);


ALTER TABLE public."Officials" OWNER TO postgres;

--
-- TOC entry 223 (class 1259 OID 16425)
-- Name: Shifts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Shifts" (
    "Id" uuid NOT NULL,
    "Name" character varying(150) NOT NULL,
    "StartAt" timestamp with time zone NOT NULL,
    "EndAt" timestamp with time zone NOT NULL,
    "Location" character varying(200),
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT "CK_Shifts_Time" CHECK (("EndAt" > "StartAt"))
);


ALTER TABLE public."Shifts" OWNER TO postgres;

--
-- TOC entry 225 (class 1259 OID 16518)
-- Name: Users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Users" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Email" text NOT NULL,
    "Phone" text,
    "Role" text NOT NULL,
    "IsActive" boolean DEFAULT true NOT NULL,
    "CreatedAt" timestamp without time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."Users" OWNER TO postgres;

--
-- TOC entry 5151 (class 0 OID 16437)
-- Dependencies: 224
-- Data for Name: Assignments; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Assignments" ("Id", "OfficialId", "BoatId", "ShiftId", "Role", "TaskDescription", "Status", "Priority", "CreatedAt") FROM stdin;
\.


--
-- TOC entry 5149 (class 0 OID 16412)
-- Dependencies: 222
-- Data for Name: Boats; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Boats" ("Id", "Name", "RegistrationNumber", "CallSign", "Status", "Notes", "CreatedAt", "Type", "Capacity") FROM stdin;
1ef9abb5-eaa1-4f8f-a06f-5ce8e50a6387	BVSZK Bravo1		Mentő1	Service		2026-07-11 01:04:39.772115+02	RIB	6
d985aae8-d92f-4163-a6af-74bea981ebcd	Móni		Rajthajó	Active		2026-07-12 14:30:11.63752+02	RIB	0
eb0e7385-78a8-49ec-9db3-fb8287c87796	EZER 1	\N	\N	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.46645+02	RIB	0
fa6c8456-a0a2-4819-ac12-ff58699731b9	AMVK	\N	\N	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.470907+02	RIB	0
0253c2ea-1d60-44f4-945b-d87d52aeca9d	Melipramin	\N	\N	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.516789+02	RIB	0
840c72ef-9a38-4865-a748-3f42937c0191	AMVK Rendező hajó	\N	\N	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.522271+02	RIB	0
38aed340-f4f5-4a3b-9b75-dd9676da5049	Fanyúl	\N	\N	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.526793+02	RIB	0
5978da84-9b3c-44a1-b3a9-b3e1c7cc447c	Sirály	\N	\N	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.534803+02	RIB	0
589c6ced-3cb6-4f59-b3df-acc17d39969f	Aquarius	\N	\N	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.538177+02	RIB	0
e9676911-ccfb-45b1-bea0-c0ae591a3da4	MVSZ Bravo 38177	\N	\N	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.543343+02	RIB	0
1087c2f9-0dfb-40ec-81fb-c9db720b0afc	Saját	\N	\N	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.557837+02	RIB	0
9dc48a4a-bca0-45e3-8cc2-8035657f4a3f	VSR	\N	\N	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.561196+02	RIB	0
9380396b-d318-4eca-ad83-5f3be99414c1	Szövetség	\N	\N	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.564863+02	RIB	0
517dc345-2755-43e6-b205-7c54da7e2214	VVSI Lolivul	\N	\N	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.578786+02	RIB	0
a221b40b-7564-4b81-8547-ea42f0d965f1	BVSZK Bravo 2		H-39883	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.582218+02	RIB	0
ca22d528-a9fc-4145-8a17-1a4ab2d37baf	BVSZK Bravo 1		H-39882	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.5296+02	RIB	0
41dff0bb-7eee-4e23-a47d-2f7688409fba	Flaar		H-	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.545992+02	RIB	0
5fe0756a-9fae-4164-ad41-daf6bb523601	MVA BRAVO		H-35479	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.549692+02	RIB	0
8099991d-16ee-4786-8ee2-e8a30572b632	Lolivul 1		H-40177	Active	Excel beosztás-importtal létrehozva.	2026-07-19 10:57:06.478071+02	RIB	0
\.


--
-- TOC entry 5154 (class 0 OID 16579)
-- Dependencies: 227
-- Data for Name: EventAssignmentImportBatches; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."EventAssignmentImportBatches" ("Id", "EventId", "FileName", "ImportedAt", "TotalRows", "ImportedRows", "SkippedRows", "ErrorRows", "Status", "ErrorMessage", "ReplacePreviousImport", "CreatedOfficials", "UpdatedOfficials", "CreatedBoats", "CreatedAssignments", "UpdatedAssignments", "RemovedAssignments", "CompletedAt", "CreatedAt") FROM stdin;
56aca66e-c4fd-4cc4-8b55-85be181a5f8b	231acc30-a8d5-4141-a4e9-9a4b3759eca5	FSZ_beo.xlsx	2026-07-19 10:57:06.43024	0	67	14	0	Completed	\N	f	46	5	21	67	0	0	2026-07-19 10:57:06.585282+02	2026-07-19 10:57:06.43309+02
0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	231acc30-a8d5-4141-a4e9-9a4b3759eca5	FSZ_beo.xlsx	2026-07-19 10:57:10.292665	0	67	14	0	Completed	\N	t	0	47	0	67	0	67	2026-07-19 10:57:10.412112+02	2026-07-19 10:57:10.294885+02
\.


--
-- TOC entry 5153 (class 0 OID 16534)
-- Dependencies: 226
-- Data for Name: EventOfficialAssignments; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."EventOfficialAssignments" ("Id", "EventId", "OfficialId", "Role", "TrunkRadioNumber", "VhfRadioNumber", "BoatId", "AttendanceStatus", "Notes", "CreatedAt", "UpdatedAt", "GroupName", "TaskName", "SourceFile", "ImportBatchId", "SortOrder", "ImportKey", "SubgroupName", "Task", "AssignmentOrder", "Source", "SourceRowNumber") FROM stdin;
30ef119b-e79e-4520-a571-481aca984e2b	231acc30-a8d5-4141-a4e9-9a4b3759eca5	3c9dcffa-9f0e-4a25-b663-ac5ca06c5b64	Technikai felelős	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.337197	NEVEZÉS, ELŐKÉSZÜLETEK	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	f1cfc2adde68c862e195d5293b54fa02a8068078fa86f4b5054b106735968e37	\N	motoros, eszközök kiosztás	19	xlsx-import	19
17ae781f-48af-469f-8bf3-360e201fad98	231acc30-a8d5-4141-a4e9-9a4b3759eca5	25e96362-8383-4513-9403-6992e6ddce32	Versenyadminisztrátor	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.324286	NEVEZÉS, ELŐKÉSZÜLETEK	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	e08e47d416d1ed1564cdea9aa5f052a9d62653e65b0699b99dfa97014a64f118	\N	VIHAR	12	xlsx-import	12
3bb196ba-b828-40cc-acd8-052401be8788	231acc30-a8d5-4141-a4e9-9a4b3759eca5	78a1c2bd-b099-4d77-b9cd-98dd3de9dfa5	Versenyadminisztrátor	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.325921	NEVEZÉS, ELŐKÉSZÜLETEK	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	02be7a074a31af6d9393faff2591dfce637a6bbd98ab9c737ec02721619099f9	\N	Kékpántlika nevezés + VIHAR	13	xlsx-import	13
290fda41-eb00-4e09-ba4f-a3fb85a01dfc	231acc30-a8d5-4141-a4e9-9a4b3759eca5	e6c84f38-ca0c-4521-ada9-b778242cc1be	Pénzügy	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.32758	NEVEZÉS, ELŐKÉSZÜLETEK	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	927fe05ad0a7ed19bf9cdb964ac4037b51551df00530f546ac4077ec15cb336a	\N	Fizetés	14	xlsx-import	14
f487f210-a512-464d-a710-45db77dcc772	231acc30-a8d5-4141-a4e9-9a4b3759eca5	42871993-896d-443a-a867-4694ccaeea8d	Pénzügy	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.329274	NEVEZÉS, ELŐKÉSZÜLETEK	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	4296813c26fc01df9f1378d098c66374d2c133fbbee7582538c1f62d0a4e7292	\N	Fizetés	15	xlsx-import	15
80e3ed9f-06db-4e6c-8827-3cdcfd639ddb	231acc30-a8d5-4141-a4e9-9a4b3759eca5	bbad0758-d8e7-405a-92d0-890a9155a3f6	Média és Kommunikáció	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.332101	NEVEZÉS, ELŐKÉSZÜLETEK	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	85acec6f3cff206ee0c5eaf386f6d8cfb475949521b8a1af4376a8e60662a3d9	\N	nevezés - GPS, Pálinka, szalag, kioszt	16	xlsx-import	16
5736e9dc-f630-4e9c-ab33-27d81478197d	231acc30-a8d5-4141-a4e9-9a4b3759eca5	e8858806-d4e2-4807-bfd1-7606f7ef01ec	Versenyadminisztrátor	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.333826	NEVEZÉS, ELŐKÉSZÜLETEK	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	3ef43a85098803967870a0760425e6300d2de8cee7535507a93ec2a6efd4f1f3	\N	GPS kiosztás, Szalag	17	xlsx-import	17
ad9face4-5388-49ac-a22c-3883ecb331a8	231acc30-a8d5-4141-a4e9-9a4b3759eca5	33f357f3-717b-48c5-b9ad-d937516d2c84	Versenyadminisztrátor	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.335313	NEVEZÉS, ELŐKÉSZÜLETEK	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	638b5c760dbb7be7806970a9250f236aa4f04e9734bae185ee65441782a29fc7	\N	GPS kiosztás, Szalag	18	xlsx-import	18
b4df3261-689b-4fe2-ac3d-8510a39c27d7	231acc30-a8d5-4141-a4e9-9a4b3759eca5	10bbc774-deb6-4b62-bb09-c51db4a1ebaf	Versenyvezető	\N	\N	d985aae8-d92f-4163-a6af-74bea981ebcd	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.303453	VERSENYVEZETÉS	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	a68ab62bc92b4bc066058f98034ec9330264da0c115a8fa20a440bf51e3b3ff1	\N	\N	3	xlsx-import	3
425b604e-8c74-451e-b53c-39bfcff65aaa	231acc30-a8d5-4141-a4e9-9a4b3759eca5	282ecebb-07b2-4054-88e1-c8129202482a	versenyiroda vezető	\N	\N	d985aae8-d92f-4163-a6af-74bea981ebcd	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.305836	VERSENYVEZETÉS	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	8f69b07dc4ff733d31d5e310c10acf3115db26222fb3a19bcb54be28f64ac593	\N	\N	4	xlsx-import	4
d86af4f4-79dd-4b25-a09c-583cbebda21b	231acc30-a8d5-4141-a4e9-9a4b3759eca5	7fa1cffb-5215-437b-8b02-378f8e539775	Óvási bizottság elnöke	\N	\N	eb0e7385-78a8-49ec-9db3-fb8287c87796	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.307996	VERSENYVEZETÉS	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	3b38caf8f1573a41a0149df3d9f5febd29f24a316e3d2742c1e7b5cd08ad5afd	\N	\N	5	xlsx-import	5
dfa39841-00e2-47d0-bae3-7b9a2ade73b1	231acc30-a8d5-4141-a4e9-9a4b3759eca5	d6ba2893-5a26-4fc9-a21d-539cc3af7966	Óvási bizottság tagja	\N	\N	fa6c8456-a0a2-4819-ac12-ff58699731b9	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.309965	VERSENYVEZETÉS	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	4e8f48d782dc20a2f4ff508cf575648dd36ca2409975742b914209a08e401589	\N	\N	6	xlsx-import	6
6ef1f8ac-1b9c-43a3-8af7-4b376e7795f1	231acc30-a8d5-4141-a4e9-9a4b3759eca5	310059ee-d4c4-4cbf-aed5-7e414f4893d5	Technikai Bizottság elnöke	\N	\N	8099991d-16ee-4786-8ee2-e8a30572b632	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.314	VERSENYVEZETÉS	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	72078edaa1516e8268e636e17349353ab524b79a86a1d359a216477905cc70cb	\N	\N	8	xlsx-import	8
40179a8c-8bc5-4830-a2ce-0fbfac0f39a8	231acc30-a8d5-4141-a4e9-9a4b3759eca5	10bbc774-deb6-4b62-bb09-c51db4a1ebaf	Versenyvezető	\N	\N	d985aae8-d92f-4163-a6af-74bea981ebcd	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.337987	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	54382a51a18db48371adc0c772fc03afafb374d35fc9e462ac4e9ade4458c678	\N	Északi oldal rajt	21	xlsx-import	21
cc03e1f6-da95-45d3-a132-62df6bf11a3a	231acc30-a8d5-4141-a4e9-9a4b3759eca5	282ecebb-07b2-4054-88e1-c8129202482a	Adminisztrátor	\N	\N	d985aae8-d92f-4163-a6af-74bea981ebcd	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.338757	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	05f52ebccc2ff75720807fcdc0a48a56a591f0d46562a63066b3634f7ca064be	\N	VHF rádió - rajt után part, Jegyzőkönyv	22	xlsx-import	22
b8543fc0-8d39-4993-865c-3646101a7664	231acc30-a8d5-4141-a4e9-9a4b3759eca5	25e96362-8383-4513-9403-6992e6ddce32	Adminisztrátor	\N	\N	d985aae8-d92f-4163-a6af-74bea981ebcd	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.339533	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	1315aea976d39eda5c42d5f7f649844472c66172a577974b8783ea6eae07055c	\N	Hangjelek, utána part - rajt után part	23	xlsx-import	23
4c8d879c-e75c-40d5-adc4-5d28b866ba56	231acc30-a8d5-4141-a4e9-9a4b3759eca5	0ebc9b40-d76e-4c08-83e7-5a63a116ba61	Adminisztrátor	\N	\N	d985aae8-d92f-4163-a6af-74bea981ebcd	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.340974	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	6713bdfc465ac497ee9f8399bad969c3305fb2ec4195a22cba11e48687bdd7ce	\N	Célbírói	24	xlsx-import	24
d938e220-196e-46f2-9b98-535ce8cf88b8	231acc30-a8d5-4141-a4e9-9a4b3759eca5	976e0a6a-04db-4d26-a05e-aa7616a8cca2	Adminisztrátor	\N	\N	d985aae8-d92f-4163-a6af-74bea981ebcd	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.342399	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	d0ba3d2e80e0cb278f886d11c68565a74bff6239c7fd6c7f278d400909cc43db	\N	Célbírói	25	xlsx-import	25
0c1ff5ee-8a8a-4a5c-a8d1-3321e2c2e627	231acc30-a8d5-4141-a4e9-9a4b3759eca5	33f357f3-717b-48c5-b9ad-d937516d2c84	Hajóvezető	\N	\N	0253c2ea-1d60-44f4-945b-d87d52aeca9d	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.34347	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	f57b46e662e7ebf4af926e1d4ec8d71a6a75c49c02fd2932c97ea35540a4bc28	\N	Déli oldal célbírói, hangjelek	26	xlsx-import	26
fd313982-6b11-4850-8539-0819a98c89d5	231acc30-a8d5-4141-a4e9-9a4b3759eca5	621930e2-c57f-4254-9635-9b86f70718ec	Adminisztrátor	\N	\N	0253c2ea-1d60-44f4-945b-d87d52aeca9d	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.344178	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	27135451eae64a865f097abd5a65be415c79e8c70f9aaf17bf0a159de663050a	\N	Rádió, jegyzőkönyv rajt utána part	27	xlsx-import	27
f2ccb0af-601e-46d0-a75f-0dcb5debabeb	231acc30-a8d5-4141-a4e9-9a4b3759eca5	78a1c2bd-b099-4d77-b9cd-98dd3de9dfa5	Adminisztrátor	\N	\N	0253c2ea-1d60-44f4-945b-d87d52aeca9d	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.344915	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	880cd85c10efac9500c15ee288540eda9a29e48ae9fb5713482e7839f1d56f6a	\N	célbírói jegyzőkönyv	28	xlsx-import	28
f87eec0f-77d5-4e3a-8547-515b504e0304	231acc30-a8d5-4141-a4e9-9a4b3759eca5	53141a09-9893-4153-ad0c-a9d919d83217	Mentőtiszt	\N	\N	0253c2ea-1d60-44f4-945b-d87d52aeca9d	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.346593	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	2434e092649bcfa217e146f944be6e4ff051ecd5e284f2a8177b11ca00468a94	\N	célbírói jegyzőkönyv	29	xlsx-import	29
389ac7ec-d159-423f-a2b0-2a108b9b52b9	231acc30-a8d5-4141-a4e9-9a4b3759eca5	73648d87-45ba-4905-b34b-8463d602534b	Adminisztrátor	\N	\N	0253c2ea-1d60-44f4-945b-d87d52aeca9d	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.348186	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	cb381ccb4c21a38f87debcc32d5f0aa925012b40d5389b1cec8397a3a334db2a	\N	Látható jelek, célbírói jegyzőkönyv	30	xlsx-import	30
5da59c6b-d587-472d-a7f9-93800c3aca5d	231acc30-a8d5-4141-a4e9-9a4b3759eca5	d6ba2893-5a26-4fc9-a21d-539cc3af7966	Motorosvezető	\N	\N	840c72ef-9a38-4865-a748-3f42937c0191	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.349247	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	a06ebb7cb8afd801b2c8e11037bc457accf6a0c92c467dffbb927dd1193e2b3b	\N	Motorosvezető, Kenesei átfutó jegyzőkönyv	31	xlsx-import	31
fe587c26-3b7c-4b17-941c-920002822cec	231acc30-a8d5-4141-a4e9-9a4b3759eca5	db6f307e-1482-4645-a400-9cbfcd7f5bb0	Adminisztrátor	\N	\N	840c72ef-9a38-4865-a748-3f42937c0191	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.350707	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	b622af9220cefbed28c29477df944559e531d8bdd37fe8af3ea184dd15b188dd	\N	Motorosvezető, Kenesei átfutó jegyzőkönyv	32	xlsx-import	32
d7e67875-3e56-4705-9767-da24349aa35f	231acc30-a8d5-4141-a4e9-9a4b3759eca5	db6f307e-1482-4645-a400-9cbfcd7f5bb0	Pályatűző	\N	\N	fa6c8456-a0a2-4819-ac12-ff58699731b9	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.351436	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	1dfd73e9da2a978819f3ca51cdfd48b6114c6e6ecc51eaa2dab2ba5617c9281a	\N	Kenesei bója, átfutó jegyzőkönyv	34	xlsx-import	34
bd902ed4-720e-447c-a7fd-6f69695d41dc	231acc30-a8d5-4141-a4e9-9a4b3759eca5	cf9de61c-50f4-4850-aefe-6a01a286722a	Óvási bizottság tagja - stand by	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.311932	VERSENYVEZETÉS	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	85621b3462f87509952a4938b7fbb85b5dc1e4ca72595644c1cf026410560bb2	\N	\N	7	xlsx-import	7
d318e9f2-9287-452f-8c56-2d0772119803	231acc30-a8d5-4141-a4e9-9a4b3759eca5	621930e2-c57f-4254-9635-9b86f70718ec	Versenyadminisztrátor	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.320378	NEVEZÉS, ELŐKÉSZÜLETEK	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	a306fbc1b0e586a2d253e56631a150803d59190aa13c3821c2520fa8631e93d4	\N	VIHAR	10	xlsx-import	10
ddcd9622-72c1-41f3-b59b-97b895c36171	231acc30-a8d5-4141-a4e9-9a4b3759eca5	282ecebb-07b2-4054-88e1-c8129202482a	Versenyadminisztrátor	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.322138	NEVEZÉS, ELŐKÉSZÜLETEK	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	a93e9e566cd051f10d7f6b3caa07cf328b36fe5bc2c787634e6653c09a16a68e	\N	VIHAR, iroda vezető	11	xlsx-import	11
0ddfb429-2fcf-4e33-8937-28f1c3eb292f	231acc30-a8d5-4141-a4e9-9a4b3759eca5	db6f307e-1482-4645-a400-9cbfcd7f5bb0	Adminisztrátor	\N	\N	fa6c8456-a0a2-4819-ac12-ff58699731b9	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.352195	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	33f643e82eac17d303c0357a955e762aa14219ae4b7afe0cc38b2bb770d1537f	\N	Kenesei bója, átfutó jegyzőkönyv	35	xlsx-import	35
3102a7c7-8c2b-48b5-9a1c-f97b985603cc	231acc30-a8d5-4141-a4e9-9a4b3759eca5	f938d208-fb18-4fa2-9175-05b3d5bdbd6b	Motorosvezető	\N	\N	38aed340-f4f5-4a3b-9b75-dd9676da5049	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.353892	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	c3e068d1f9a786bb373b369fcedacd03229295286a7ee6b5317f15f5bfc5a17d	\N	Tihanyi pályajel , átfutó jegyzőkönyv írás	36	xlsx-import	36
ac14222f-fba0-4258-9ba0-00f1f262463b	231acc30-a8d5-4141-a4e9-9a4b3759eca5	f938d208-fb18-4fa2-9175-05b3d5bdbd6b	Adminisztrátor	\N	\N	38aed340-f4f5-4a3b-9b75-dd9676da5049	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.354621	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	c1cdfb13740b7196a9a1865469f99d18261c73d6e6aec29777588d7beb43e915	\N	Tihanyi pályajel , átfutó jegyzőkönyv írás	37	xlsx-import	37
faa530cd-c408-4ab6-995a-07c19756bffd	231acc30-a8d5-4141-a4e9-9a4b3759eca5	e2e40ed3-d30c-4983-8b90-7a025f7a4aa0	Motorosvezető	\N	\N	ca22d528-a9fc-4145-8a17-1a4ab2d37baf	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.356353	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	45f9dd5d67883f89525776c533ad6e1bcdebc9b0c32516d3a0418de1f3a23c52	\N	rajtvonal ellenőrzés, Szükség esetén Tihany, GPS összeszedés	39	xlsx-import	39
9e4a38ee-4528-470a-9050-ad68ac141cd1	231acc30-a8d5-4141-a4e9-9a4b3759eca5	f8c62834-e5e6-438f-bed6-7fe0881bf989	Adminisztrátor	\N	\N	ca22d528-a9fc-4145-8a17-1a4ab2d37baf	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.357813	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	53cea46f55fbb6f1bac57e46481c32a4843e2012f95998c120f866fdddb2c510	\N	rajtvonal ellenőrzés, Szükség esetén Tihany, GPS összeszedés	40	xlsx-import	40
a514d815-a760-4161-8917-e3f78cf32ff9	231acc30-a8d5-4141-a4e9-9a4b3759eca5	19027a48-b2c0-44de-bd5c-36714b5e15ad	Adminisztrátor	\N	\N	ca22d528-a9fc-4145-8a17-1a4ab2d37baf	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.35924	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	4061fab84fa9379faeb2ffb501b150736c743a5f1341673443060a6880a4c42f	\N	rajtvonal ellenőrzés, Szükség esetén Tihany, GPS összeszedés	41	xlsx-import	41
5cb94fc9-8637-4d1c-839f-66fb3caad6d9	231acc30-a8d5-4141-a4e9-9a4b3759eca5	45fae019-2ae4-431e-b9cd-cbd64de4f169	Pályatűző	\N	\N	5978da84-9b3c-44a1-b3a9-b3e1c7cc447c	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.361083	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	34be978aa084d9c4735af06fae5a31904a2135938618450570dfde34a9b7e603	\N	Siófoki átfutó, egyből megy nem várja meg a rajtot, 2 db sárgát visz + bója GPS Siófoki átfutó jegyzőkönyv, Siófoki csapatot ők viszik át, hozzák vissza	42	xlsx-import	42
4f74c472-9cc4-47df-8edd-69b09daad945	231acc30-a8d5-4141-a4e9-9a4b3759eca5	284826ff-54a9-40e8-ba69-ec06b0942585	Adminisztrátor	\N	\N	5978da84-9b3c-44a1-b3a9-b3e1c7cc447c	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.362527	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	f49ffe55df631d8bf661fb63b2a9213567a5a61c03eb8922992be090b403c94f	\N	Siófoki átfutó, egyből megy nem várja meg a rajtot, 2 db sárgát visz + bója GPS Siófoki átfutó jegyzőkönyv, Siófoki csapatot ők viszik át, hozzák vissza	43	xlsx-import	43
b261028b-3f3f-4c84-8000-991b7b4003e5	231acc30-a8d5-4141-a4e9-9a4b3759eca5	fbd9aa47-8cc9-4922-8a30-830ad92c8498	Pályatűző	\N	\N	589c6ced-3cb6-4f59-b3df-acc17d39969f	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.364287	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	e0f71be0068d0ea9a43b14767df0685660282786ffddff97570b5c19f0349760	\N	Siófoki bója, átfutó írás	44	xlsx-import	44
dfb1e624-21cd-46e5-bfbf-d6dfc3c91edb	231acc30-a8d5-4141-a4e9-9a4b3759eca5	1ee56ee9-97c0-464b-b8b9-53bb7f93cf89	Adminisztrátor	\N	\N	589c6ced-3cb6-4f59-b3df-acc17d39969f	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.366137	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	a97d339abde7a04682ad32a43800abd8038ec2919ea04ae7dae2aede362b5161	\N	Siófoki bója, átfutó írás	45	xlsx-import	45
1d91dff6-9322-4935-8996-16a184789c40	231acc30-a8d5-4141-a4e9-9a4b3759eca5	7b26c1fe-128d-43d3-a050-bdbd591a2568	Adminisztrátor	\N	\N	589c6ced-3cb6-4f59-b3df-acc17d39969f	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.36764	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	d3ce0a501b75c1f35d23949ce7147991ed3f29ce7fd370600148998816526dc6	\N	Siófoki bója, átfutó írás	46	xlsx-import	46
f91deee6-f30c-4b5f-a376-dc96c82eee27	231acc30-a8d5-4141-a4e9-9a4b3759eca5	1fa5d1a8-413d-40e8-b1c1-dd7a687e66e4	Pályatűző	\N	\N	e9676911-ccfb-45b1-bea0-c0ae591a3da4	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.369442	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	f5cec52c5cf72c50c30514db8be5a0df8c408549a8fceb41447845f4060b6a89	\N	rajtvonal ellenőrzés, Szükség esetén Tihany, GPS összeszedés	47	xlsx-import	47
954f7105-3017-40d1-b4bd-e7293c8f10b4	231acc30-a8d5-4141-a4e9-9a4b3759eca5	a12ce6c2-4068-4f8f-9552-d2bbbf733d09	Adminisztrátor	\N	\N	e9676911-ccfb-45b1-bea0-c0ae591a3da4	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.370933	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	d650494f21ec5d68218b80c4677d287c9b139c92c46ed7b22929dec2015064c4	\N	rajtvonal ellenőrzés, Szükség esetén Tihany, GPS összeszedés	48	xlsx-import	48
b67d2a39-3b64-4b74-aa34-1af7ae292f98	231acc30-a8d5-4141-a4e9-9a4b3759eca5	3c9dcffa-9f0e-4a25-b663-ac5ca06c5b64	Pályatűző	\N	\N	41dff0bb-7eee-4e23-a47d-2f7688409fba	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.371958	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	82d5f8bd396c11408f19c30c6937fc8b33a06e25d7046465084e8439bc083a7d	\N	Rajtvonal ellenőrzés utána katamarán	49	xlsx-import	49
e9354dc6-b1b8-4565-be4c-0be8b85f3d81	231acc30-a8d5-4141-a4e9-9a4b3759eca5	3717e644-b660-439d-ba89-166b95c898c8	Adminisztrátor	\N	\N	41dff0bb-7eee-4e23-a47d-2f7688409fba	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.373404	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	517a9208c2daccb71425b91f32adbd2849ab7b780afc37776e7c8c3c8b1a7f8f	\N	Rajtvonal ellenőrzés utána katamarán	50	xlsx-import	50
34fc86ed-03ca-4737-a268-3b044986c202	231acc30-a8d5-4141-a4e9-9a4b3759eca5	6ed6d768-b5bb-4190-88be-27e5b3f51e80	Motorosvezető	\N	\N	5fe0756a-9fae-4164-ad41-daf6bb523601	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.375022	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	be1b50ca448fc73a748bdd766dff49adc9530d4f60a6918108de8cb42eb6d4ea	\N	rajtvonal ellenőrzés, rajtvonal, GPS összeszedés	51	xlsx-import	51
a9bb6df0-9d58-44e6-90fe-5c86246b1011	231acc30-a8d5-4141-a4e9-9a4b3759eca5	dd3dabc9-fc12-4397-9439-e7747bca46cd	Versenyadminisztrátor	\N	\N	5fe0756a-9fae-4164-ad41-daf6bb523601	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.376416	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	8fd99fa28c147ba4a86cdc6eec52bd60cf7ecd7bab9d00cd2a2fc2e7347788ad	\N	rajtvonal ellenőrzés, rajtvonal, GPS összeszedés	52	xlsx-import	52
28e7ae5e-3a39-4dcf-aed1-12b36605328a	231acc30-a8d5-4141-a4e9-9a4b3759eca5	bf3fdb32-a5f8-4765-9335-127f1d7f0617	Motorosvezető	\N	\N	eb0e7385-78a8-49ec-9db3-fb8287c87796	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.377724	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	2da01090d816124e2f5e3b6fa141d240103e87c0b70c586e3dc77dcfee96e4fc	\N	Ebédek kiosztása, GPS szedés, egyebek	53	xlsx-import	53
131b081e-b71f-400b-a15b-83cfa4386908	231acc30-a8d5-4141-a4e9-9a4b3759eca5	bbad0758-d8e7-405a-92d0-890a9155a3f6	Kommunikáció	\N	\N	eb0e7385-78a8-49ec-9db3-fb8287c87796	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.37906	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	ad04266f2a563f4b1817b178a8748c36f6b624a74a326aab017efe9819d75e7f	\N	Ebédek kiosztása, GPS szedés, egyebek	54	xlsx-import	54
609880d2-0bf7-4ce6-87bc-85d73427ade0	231acc30-a8d5-4141-a4e9-9a4b3759eca5	7fa1cffb-5215-437b-8b02-378f8e539775	Adminisztrátor	\N	\N	eb0e7385-78a8-49ec-9db3-fb8287c87796	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.380384	MOTOROSOK BEOSZTÁSA	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	15e6bea83b1f9d7b4e7138fdbee187954ed545dbd3c254310992f0245304b8cf	\N	Ebédek kiosztása, GPS szedés, egyebek	55	xlsx-import	55
7640d884-3b70-40fe-889d-41f2b1f471e3	231acc30-a8d5-4141-a4e9-9a4b3759eca5	a5cd6992-3f39-4673-bb6f-38544b10563a	Média motorosvezető	\N	\N	1087c2f9-0dfb-40ec-81fb-c9db720b0afc	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.382174	MÉDIA MOTOROS	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	1c8b8e3d65932d7341a99f0bdaafc76ec8246b3ea64b666fa027ab5fd393cb0b	\N	MVSZ Média 1. - Mezőny kísérése + Katamarán verseny	56	xlsx-import	56
eb3bcb2a-b92e-4ae6-b6f4-6843472dd6ae	231acc30-a8d5-4141-a4e9-9a4b3759eca5	25b1d91f-98c7-48e1-928a-486a41cb4fb3	fotós	\N	\N	1087c2f9-0dfb-40ec-81fb-c9db720b0afc	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.383499	MÉDIA MOTOROS	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	f786e9c96b7d6bb4ce3c1cf3e1999c449fe1792308deb6e3a87a269c12996b85	\N	MVSZ Média 1. - Mezőny kísérése + Katamarán verseny	57	xlsx-import	57
c68e3770-ed61-4d54-ab96-19c5762ed750	231acc30-a8d5-4141-a4e9-9a4b3759eca5	964ecb9b-bb20-4991-8eb4-cd95b1c0160c	Média motorosvezető	\N	\N	9dc48a4a-bca0-45e3-8cc2-8035657f4a3f	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.385258	MÉDIA MOTOROS	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	33e7be2bea43d0f1aeeef261427bc2990cc1b660c9d4a8b7a25e38c2c029609e	\N	MVSZ Média 2.	58	xlsx-import	58
cdee9d9a-5a77-47f0-aff2-a739b8154e25	231acc30-a8d5-4141-a4e9-9a4b3759eca5	5f50aa00-c5e7-4639-b92d-99cf41417c57	fotós	\N	\N	9dc48a4a-bca0-45e3-8cc2-8035657f4a3f	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.386643	MÉDIA MOTOROS	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	de855213dacbafc83704839f60b1bf6f71e5c997776356b0334bc52d11d68505	\N	MVSZ Média 2.	59	xlsx-import	59
5dc8bb7d-ca59-493d-a249-ca331197fb4b	231acc30-a8d5-4141-a4e9-9a4b3759eca5	8cf60b5a-af21-4c7d-9b3e-3279f72cf5ed	Média motorosvezető	\N	\N	9380396b-d318-4eca-ad83-5f3be99414c1	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.388277	MÉDIA MOTOROS	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	217f58b44c4dbe1df4422c59ad3fe46889a2a7438b138a33878f76af84e4d681	\N	Kékszalag LIVE - EGÉSZNAP	60	xlsx-import	60
f42f1021-85d0-46df-bff4-10028f50957a	231acc30-a8d5-4141-a4e9-9a4b3759eca5	aeefeca3-189c-432f-bb1c-8e43a862c418	videós	\N	\N	9380396b-d318-4eca-ad83-5f3be99414c1	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.389766	MÉDIA MOTOROS	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	04f49d0bcf8d39666feda2a97985dac3279f49dd78dc4545d0c14b286510164f	\N	Kékszalag LIVE - EGÉSZNAP	61	xlsx-import	61
9545c941-2a85-4bb0-b95a-21d6e64d5e30	231acc30-a8d5-4141-a4e9-9a4b3759eca5	3c9dcffa-9f0e-4a25-b663-ac5ca06c5b64	Motorosvezető	\N	\N	41dff0bb-7eee-4e23-a47d-2f7688409fba	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.396958	KATAMARÁN FUTAM	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	85cfcabb46ea83d317df68cff6155cdfc9455fb706142cf804daf2a26e31e24e	Rajt hajó	\N	82	xlsx-import	82
5390688f-771e-44c1-9e73-064b18e6887a	231acc30-a8d5-4141-a4e9-9a4b3759eca5	7d8c5ead-922d-483c-913c-b34dcbc7f99e	Rajtoltatás	\N	\N	41dff0bb-7eee-4e23-a47d-2f7688409fba	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.399474	KATAMARÁN FUTAM	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	0627da335f620c19438d359e2ccd9b64f235a8e3ea1c8e44afb93f8bc8c48e19	Rajt hajó	\N	83	xlsx-import	83
0a5b32a2-3973-4888-ac47-4b1e4255438d	231acc30-a8d5-4141-a4e9-9a4b3759eca5	3717e644-b660-439d-ba89-166b95c898c8	Látható jelek, hangjelek	\N	\N	41dff0bb-7eee-4e23-a47d-2f7688409fba	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.401746	KATAMARÁN FUTAM	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	ebf289803634f22b2f4a97f2916e9e4ef56c2dd78445fc2702a224b9f0a01a6b	Rajt hajó	\N	84	xlsx-import	84
c65a358a-0341-42da-8b22-e965010d7d23	231acc30-a8d5-4141-a4e9-9a4b3759eca5	6e1b010b-e6e7-4563-872a-ed3796e6cbb0	Motorosvezető	\N	\N	517dc345-2755-43e6-b205-7c54da7e2214	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.404322	KATAMARÁN FUTAM	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	7474d743e633eca3af39a1ff734bf1ef305c6269efa9d0419f095467607e890d	Pályatűzők	\N	86	xlsx-import	86
90abaaee-2bfb-49f8-8a3c-ad2ccf04059b	231acc30-a8d5-4141-a4e9-9a4b3759eca5	4582ccbf-bf8b-48a8-b239-138f0d02f62d	Adminisztrátor	\N	\N	517dc345-2755-43e6-b205-7c54da7e2214	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.406581	KATAMARÁN FUTAM	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	94166a73a0b657e6f4f652a516479a17ddf047fd4281c0b32a8ae8910a3b8249	Pályatűzők	\N	87	xlsx-import	87
b11d0c0d-b8f6-4882-8d81-c4efe825796a	231acc30-a8d5-4141-a4e9-9a4b3759eca5	5e42dffd-4d3b-4024-93e7-c65138962f9e	Motorosvezető	\N	\N	a221b40b-7564-4b81-8547-ea42f0d965f1	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.409164	KATAMARÁN FUTAM	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	8eaa225868015cfe137782b281850a71810b02719fcb919e5495f467c83520a0	Célhajó	\N	89	xlsx-import	89
add948fd-cf50-456a-83db-36a97997d1c5	231acc30-a8d5-4141-a4e9-9a4b3759eca5	a07784d1-00df-42e7-916a-200ce109831e	Adminisztrátor	\N	\N	a221b40b-7564-4b81-8547-ea42f0d965f1	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.411222	KATAMARÁN FUTAM	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	8fcfbf65a55409046f6d26df15bd47aad8b5fd8bc96f724f79ab641d50083fb1	Célhajó	\N	90	xlsx-import	90
0560056b-344a-4bee-a94f-386f3030a53f	231acc30-a8d5-4141-a4e9-9a4b3759eca5	25e96362-8383-4513-9403-6992e6ddce32	Eredményszámító	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.390865	EREDMÉNYSZÁMÍTÁS PART	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	7159bb2953c37b7a027d55e9e6c6e0e3b14c0289e27a990968fa96477c6de4d1	\N	eredmények, díjkiosztó forgatókönyv	73	xlsx-import	73
e6db67d6-8ace-45fa-a3e5-44a2a059f5c7	231acc30-a8d5-4141-a4e9-9a4b3759eca5	7d8c5ead-922d-483c-913c-b34dcbc7f99e	Eredményszámító	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.392394	EREDMÉNYSZÁMÍTÁS PART	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	dd3dfff492c4713b624d773bbccb18c63ca7b3dae573259a6f05479aff5d0380	\N	eredmények, díjkiosztó forgatókönyv	74	xlsx-import	74
7cacf27d-2d9d-43c3-a33a-ae736d4c86cd	231acc30-a8d5-4141-a4e9-9a4b3759eca5	282ecebb-07b2-4054-88e1-c8129202482a	Eredményszámító	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.393191	EREDMÉNYSZÁMÍTÁS PART	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	06e46f4ff6c8ba012f121a16257e29f89491dceb89cfc477a099ae9477c54638	\N	átfutó írás	75	xlsx-import	75
f34fd480-52d1-4234-bbfd-aba50d3d1156	231acc30-a8d5-4141-a4e9-9a4b3759eca5	621930e2-c57f-4254-9635-9b86f70718ec	Eredményszámító	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.393925	EREDMÉNYSZÁMÍTÁS PART	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	bd5743bfbcd3aaab3ef3a6ff706fbd7edf9f2cbffcab5f2ede2aca30f2786406	\N	átfutó írás	76	xlsx-import	76
6ff5e066-2c50-4200-8442-4388f5cd10ef	231acc30-a8d5-4141-a4e9-9a4b3759eca5	4a2b95a1-c488-44d1-ac33-e9beffd570c4	Eredményszámító	\N	\N	\N	Planned	\N	2026-07-19 10:57:10.292665	2026-07-19 10:57:10.395388	EREDMÉNYSZÁMÍTÁS PART	\N	\N	0e0f4ea7-2ba5-4a8c-b90b-da2d3a6d6063	0	8b8f29db737fb404cf0ac2d267b5ee8cd3af2e5bc1f298d9cd52bebb4c129d53	\N	átfutó írás	77	xlsx-import	77
\.


--
-- TOC entry 5147 (class 0 OID 16389)
-- Dependencies: 220
-- Data for Name: Events; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Events" ("Id", "Name", "Status", "OrganizingClub", "HostingClub", "StartDate", "EndDate", "Location", "WaterArea", "RaceOfficer", "RaceOffice", "VhfChannel", "TrunkGroup", "Notes", "JuryChair", "TechnicalCommitteeChair") FROM stdin;
f92b145c-cb86-4d4b-9441-9d8837fe5660	Kékszalag 2027	Draft	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N
c23bba4c-ebe9-4604-9a6a-2629854aefa7	Fehér Szalag 2027	Planning	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N
eee84886-2315-4566-9337-5b97203cfb70	évadnyitó	Draft	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N
e29cbd28-afcf-45b7-b043-2a7d4954fbbf	Kékpántlika2026	Active	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N
3a35b60c-73ad-436b-922b-c25c5ca6d29b	Balaton Kupa 2027	Draft	MVSZ	MVSZ	2027-03-13 09:00:00	2027-03-15 20:00:00	Balatonfüred	Balatonfüred	Kanyicska Márk	Molnár Dóra	79	Balaton Roaming 2a	próba	\N	\N
231acc30-a8d5-4141-a4e9-9a4b3759eca5	Fehér szalag 2026	Draft	Magyar Vitorlás Szövetség	Magyar Vitorlás Szövetség	2026-07-25 09:00:00	2026-07-25 20:30:00	Kékszalag Port	Balatonfüred- Balatonkenese- Siófok- Tihany	Verebély Tibor	Fluck Réka	79	Roaming 2A	\N	Váradi Imre	Csepregi Panka
\.


--
-- TOC entry 5148 (class 0 OID 16399)
-- Dependencies: 221
-- Data for Name: Officials; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Officials" ("Id", "Name", "Phone", "Email", "Role", "Status", "CreatedAt", "Club", "ShirtSize", "ProfileImageUrl", "Qualification", "QualificationValidUntil", "QualificationDocumentUrl", "BoatLicenseType", "BoatLicenseValidUntil", "BoatLicenseDocumentUrl", "FoodAllergies", "DrugAllergies", "HealthNotes", "EmergencyContactName", "EmergencyContactPhone", "EmergencyContactRelation", "Languages", "OtherDocumentUrl", "OrganizerQualification", "JudgeQualification", "MeasurerQualification", "IsCompetitionAdministrator", "IsResultScorer") FROM stdin;
818472bb-66b3-4e73-8f63-96dc510db1d3	Kanyicska János	06302988407	janos.kanyicska@gmail.com	Nyugdíjas	Active	2026-07-12 14:28:53.829172+02	\N	\N	\N	iro	\N	\N	van	2134-01-14	\N	\N	\N	\N	\N	\N	\N	\N	\N	iro	\N	\N	f	f
1fa9d2be-c04c-42a5-81e3-265e913d2dea	mark	\N	\N	Rendező	Available	2026-07-14 20:13:52.333569+02	mvsz	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
220c8064-f2cf-411a-8ac7-bfe409f7e406	tomi	\N	\N	Rendező	Available	2026-07-18 12:10:25.709258+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
10bbc774-deb6-4b62-bb09-c51db4a1ebaf	Verebély Tibor	+36308154973	tverebely@gmail.com	Versenyvezető	Available	2026-07-19 10:57:06.448566+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
282ecebb-07b2-4054-88e1-c8129202482a	Fluck Réka	+36305401295	fluck.reka@gmail.com	versenyiroda vezető	Available	2026-07-19 10:57:06.463394+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
d6ba2893-5a26-4fc9-a21d-539cc3af7966	Farkas Péter	+36707706363	peter.farkas82@gmail.com	Óvási bizottság tagja	Available	2026-07-19 10:57:06.469889+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
cf9de61c-50f4-4850-aefe-6a01a286722a	Kerekes Kázmér	+36306337604	kazmer.kerekes@gmail.com	Óvási bizottság tagja - stand by	Available	2026-07-19 10:57:06.473491+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
310059ee-d4c4-4cbf-aed5-7e414f4893d5	Csepregi Panka	+36305863737	panka.csepregi@me.com	Technikai Bizottság elnöke	Available	2026-07-19 10:57:06.477474+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
621930e2-c57f-4254-9635-9b86f70718ec	Molnár Dóra	+36305247455	molnar@hunsail.hu	Versenyadminisztrátor	Available	2026-07-19 10:57:06.480062+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
25e96362-8383-4513-9403-6992e6ddce32	Kosztoványi Dóri	+36309603417	kosztovanyi@hunsail.hu	Versenyadminisztrátor	Available	2026-07-19 10:57:06.482644+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
78a1c2bd-b099-4d77-b9cd-98dd3de9dfa5	Sáfár Zsófia	+36301148189	safar@hunsail.hu	Versenyadminisztrátor	Available	2026-07-19 10:57:06.484159+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
e6c84f38-ca0c-4521-ada9-b778242cc1be	Bakos Tímea	+36305247544	bakos@hunsail.hu	Pénzügy	Available	2026-07-19 10:57:06.497087+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
42871993-896d-443a-a867-4694ccaeea8d	Kissné Farkas Gabriella	+36304588734	penzugy@hunsail.hu	Pénzügy	Available	2026-07-19 10:57:06.49869+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
e8858806-d4e2-4807-bfd1-7606f7ef01ec	Bálint Anna	+36707739084	office@hunsail.hu	Versenyadminisztrátor	Available	2026-07-19 10:57:06.501925+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
33f357f3-717b-48c5-b9ad-d937516d2c84	Ralovich Dávid	+36309154715	david.ralovich@gmail.com	Versenyadminisztrátor	Available	2026-07-19 10:57:06.503283+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
0ebc9b40-d76e-4c08-83e7-5a63a116ba61	Abdai Krisztina	+36309866392	abdai.kriszta@gmail.com	Adminisztrátor	Available	2026-07-19 10:57:06.513839+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
976e0a6a-04db-4d26-a05e-aa7616a8cca2	Abdai Réka	+36307904450	abdai.reka@gmail.com	Adminisztrátor	Available	2026-07-19 10:57:06.515404+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
53141a09-9893-4153-ad0c-a9d919d83217	Kovács Krisztián	+36309020784	kov19750715@gmail.com	Mentőtiszt	Available	2026-07-19 10:57:06.519687+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
73648d87-45ba-4905-b34b-8463d602534b	Weöres Zita	+36703552546	zita.weores@gmail.com	Adminisztrátor	Available	2026-07-19 10:57:06.521018+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
db6f307e-1482-4645-a400-9cbfcd7f5bb0	AMVK	\N	\N	Adminisztrátor	Available	2026-07-19 10:57:06.523552+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
f938d208-fb18-4fa2-9175-05b3d5bdbd6b	THE	\N	\N	Motorosvezető	Available	2026-07-19 10:57:06.526246+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
e2e40ed3-d30c-4983-8b90-7a025f7a4aa0	Náray Bendegúz	+36309752448	naraybeno@gmail.com	Motorosvezető	Available	2026-07-19 10:57:06.529015+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
f8c62834-e5e6-438f-bed6-7fe0881bf989	Béres Evelin	+36205168563	beres.evelin@gmail.com	Adminisztrátor	Available	2026-07-19 10:57:06.531023+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
19027a48-b2c0-44de-bd5c-36714b5e15ad	Drávucz Dia	+36204833137	drvczdia7@gmail.com	Adminisztrátor	Available	2026-07-19 10:57:06.532702+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
45fae019-2ae4-431e-b9cd-cbd64de4f169	Bartos György	+36704335500	bartos.gyorgy169@gmail.com	Pályatűző	Available	2026-07-19 10:57:06.534213+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
284826ff-54a9-40e8-ba69-ec06b0942585	Márton Ferenc	+36705158154	martonferenc0512@gmail.com	Adminisztrátor	Available	2026-07-19 10:57:06.536179+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
fbd9aa47-8cc9-4922-8a30-830ad92c8498	Martin Henrik	+36702480839	cpt.martin.henrik@gmail.com	Pályatűző	Available	2026-07-19 10:57:06.537637+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
1ee56ee9-97c0-464b-b8b9-53bb7f93cf89	Fluck Daniella	+36306388751	fluck.daniella@gmail.com	Adminisztrátor	Available	2026-07-19 10:57:06.539919+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
7b26c1fe-128d-43d3-a050-bdbd591a2568	Fluck Izabella	+36309997447	f.zuzu.bella@gmail.com	Adminisztrátor	Available	2026-07-19 10:57:06.541327+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
1fa5d1a8-413d-40e8-b1c1-dd7a687e66e4	Juhász Huba	+36705161999	huba.juhasz@gmail.com	Pályatűző	Available	2026-07-19 10:57:06.542768+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
a12ce6c2-4068-4f8f-9552-d2bbbf733d09	Darvai Flóra	+36307868048	darvai.flora@gmail.com	Adminisztrátor	Available	2026-07-19 10:57:06.544735+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
6ed6d768-b5bb-4190-88be-27e5b3f51e80	Sándor Anna	\N	\N	Motorosvezető	Available	2026-07-19 10:57:06.549069+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
dd3dabc9-fc12-4397-9439-e7747bca46cd	Urbán Petra	\N	\N	Versenyadminisztrátor	Available	2026-07-19 10:57:06.551098+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
bf3fdb32-a5f8-4765-9335-127f1d7f0617	Fodor Helga	\N	\N	Motorosvezető	Available	2026-07-19 10:57:06.552432+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
bbad0758-d8e7-405a-92d0-890a9155a3f6	Ruszthi Csilla	+36302600512	media@hunsail.hu	Média és Kommunikáció	Available	2026-07-19 10:57:06.5004+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
7fa1cffb-5215-437b-8b02-378f8e539775	Váradi Imre	+36309484164	imi.varadi@gmail.com	Óvási bizottság elnöke	Available	2026-07-19 10:57:06.465208+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
a5cd6992-3f39-4673-bb6f-38544b10563a	Csepregi Gyuri	\N	\N	Média motorosvezető	Available	2026-07-19 10:57:06.557252+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
25b1d91f-98c7-48e1-928a-486a41cb4fb3	Cserta Gábor	\N	\N	fotós	Available	2026-07-19 10:57:06.559204+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
964ecb9b-bb20-4991-8eb4-cd95b1c0160c	Lehoczky Ádám	\N	\N	Média motorosvezető	Available	2026-07-19 10:57:06.560576+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
5f50aa00-c5e7-4639-b92d-99cf41417c57	Tumbász Hédi	\N	\N	fotós	Available	2026-07-19 10:57:06.562573+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
8cf60b5a-af21-4c7d-9b3e-3279f72cf5ed	Fejes Szilárd	\N	\N	Média motorosvezető	Available	2026-07-19 10:57:06.564243+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
aeefeca3-189c-432f-bb1c-8e43a862c418	Ladányi János	\N	\N	videós	Available	2026-07-19 10:57:06.566268+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
4a2b95a1-c488-44d1-ac33-e9beffd570c4	Bathó Éva	+36305304588	evibatho@gmail.com	Eredményszámító	Available	2026-07-19 10:57:06.572571+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
7d8c5ead-922d-483c-913c-b34dcbc7f99e	Rutai Nóra	+36302834903	nora.rutai@gmail.com	Eredményszámító	Available	2026-07-19 10:57:06.569213+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
3717e644-b660-439d-ba89-166b95c898c8	Fejes Dániel	+36308228598	fejesdani99@gmail.com	Adminisztrátor	Available	2026-07-19 10:57:06.547351+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
6e1b010b-e6e7-4563-872a-ed3796e6cbb0	Makai Péter	\N	\N	Motorosvezető	Available	2026-07-19 10:57:06.578122+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
4582ccbf-bf8b-48a8-b239-138f0d02f62d	Nemessányi Bence	\N	\N	Adminisztrátor	Available	2026-07-19 10:57:06.580235+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
5e42dffd-4d3b-4024-93e7-c65138962f9e	Sipos Gábor	\N	\N	Motorosvezető	Available	2026-07-19 10:57:06.581671+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
a07784d1-00df-42e7-916a-200ce109831e	Verebély Lilla	\N	\N	Adminisztrátor	Available	2026-07-19 10:57:06.583616+02	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	f	f
3c9dcffa-9f0e-4a25-b663-ac5ca06c5b64	Kanyicska Márk	+36302056093	markkanyicska@gmail.com	Rendező	Available	2026-07-11 14:48:24.594505+02	MVSZ	S	\N	B	\N	\N	Hivatásos kisgéphajó-vezető	2027-12-31	/uploads/officials/518fdcd700184069a7a8e5f6db99b64d.pdf	\N	\N	\N	\N	\N	\N	\N	\N	B	\N	\N	f	f
\.


--
-- TOC entry 5150 (class 0 OID 16425)
-- Dependencies: 223
-- Data for Name: Shifts; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Shifts" ("Id", "Name", "StartAt", "EndAt", "Location", "CreatedAt") FROM stdin;
\.


--
-- TOC entry 5152 (class 0 OID 16518)
-- Dependencies: 225
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Users" ("Id", "Name", "Email", "Phone", "Role", "IsActive", "CreatedAt") FROM stdin;
76c5be0a-3077-4339-9e0d-ed9ffe21b84f	Admin User	admin@eventhelm.local		Admin	t	2026-07-13 17:52:49.476282
53ad9f9d-1904-43b7-b5bf-8e786a232f9c	Admin User	admin@eventhelm.local		Admin	t	2026-07-13 19:26:07.624756
cd334314-ac92-40b1-9533-71d0a968eceb	Kanyicska Márk	markkanyicska@gmail.com	+36302056093	Admin	t	2026-07-13 17:44:35.586676
\.


--
-- TOC entry 4972 (class 2606 OID 16454)
-- Name: Assignments Assignments_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Assignments"
    ADD CONSTRAINT "Assignments_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4966 (class 2606 OID 16424)
-- Name: Boats Boats_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Boats"
    ADD CONSTRAINT "Boats_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4990 (class 2606 OID 16600)
-- Name: EventAssignmentImportBatches EventAssignmentImportBatches_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EventAssignmentImportBatches"
    ADD CONSTRAINT "EventAssignmentImportBatches_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4979 (class 2606 OID 16554)
-- Name: EventOfficialAssignments EventOfficialAssignments_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EventOfficialAssignments"
    ADD CONSTRAINT "EventOfficialAssignments_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4959 (class 2606 OID 16398)
-- Name: Events Events_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Events"
    ADD CONSTRAINT "Events_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4964 (class 2606 OID 16411)
-- Name: Officials Officials_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Officials"
    ADD CONSTRAINT "Officials_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4970 (class 2606 OID 16436)
-- Name: Shifts Shifts_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Shifts"
    ADD CONSTRAINT "Shifts_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4986 (class 2606 OID 16633)
-- Name: EventOfficialAssignments UQ_EventOfficialAssignments_EventId_ImportKey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EventOfficialAssignments"
    ADD CONSTRAINT "UQ_EventOfficialAssignments_EventId_ImportKey" UNIQUE ("EventId", "ImportKey");


--
-- TOC entry 4977 (class 2606 OID 16532)
-- Name: Users Users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "Users_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4973 (class 1259 OID 16471)
-- Name: IX_Assignments_BoatId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Assignments_BoatId" ON public."Assignments" USING btree ("BoatId");


--
-- TOC entry 4974 (class 1259 OID 16470)
-- Name: IX_Assignments_OfficialId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Assignments_OfficialId" ON public."Assignments" USING btree ("OfficialId");


--
-- TOC entry 4975 (class 1259 OID 16472)
-- Name: IX_Assignments_ShiftId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Assignments_ShiftId" ON public."Assignments" USING btree ("ShiftId");


--
-- TOC entry 4967 (class 1259 OID 16473)
-- Name: IX_Boats_CallSign; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Boats_CallSign" ON public."Boats" USING btree ("CallSign");


--
-- TOC entry 4968 (class 1259 OID 16516)
-- Name: IX_Boats_Status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Boats_Status" ON public."Boats" USING btree ("Status");


--
-- TOC entry 4991 (class 1259 OID 16612)
-- Name: IX_EventAssignmentImportBatches_EventId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_EventAssignmentImportBatches_EventId" ON public."EventAssignmentImportBatches" USING btree ("EventId");


--
-- TOC entry 4980 (class 1259 OID 16575)
-- Name: IX_EventOfficialAssignments_Attendance; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_EventOfficialAssignments_Attendance" ON public."EventOfficialAssignments" USING btree ("AttendanceStatus");


--
-- TOC entry 4981 (class 1259 OID 16574)
-- Name: IX_EventOfficialAssignments_Event; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_EventOfficialAssignments_Event" ON public."EventOfficialAssignments" USING btree ("EventId");


--
-- TOC entry 4982 (class 1259 OID 16611)
-- Name: IX_EventOfficialAssignments_EventId_GroupName; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_EventOfficialAssignments_EventId_GroupName" ON public."EventOfficialAssignments" USING btree ("EventId", "GroupName");


--
-- TOC entry 4983 (class 1259 OID 16634)
-- Name: IX_EventOfficialAssignments_Event_Boat; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_EventOfficialAssignments_Event_Boat" ON public."EventOfficialAssignments" USING btree ("EventId", "BoatId") WHERE ("BoatId" IS NOT NULL);


--
-- TOC entry 4984 (class 1259 OID 16635)
-- Name: IX_EventOfficialAssignments_Event_Official; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_EventOfficialAssignments_Event_Official" ON public."EventOfficialAssignments" USING btree ("EventId", "OfficialId");


--
-- TOC entry 4960 (class 1259 OID 16533)
-- Name: IX_Events_StartDate; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Events_StartDate" ON public."Events" USING btree ("StartDate");


--
-- TOC entry 4961 (class 1259 OID 16577)
-- Name: IX_Officials_BoatLicenseValidUntil; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Officials_BoatLicenseValidUntil" ON public."Officials" USING btree ("BoatLicenseValidUntil");


--
-- TOC entry 4962 (class 1259 OID 16576)
-- Name: IX_Officials_Club; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Officials_Club" ON public."Officials" USING btree ("Club");


--
-- TOC entry 4987 (class 1259 OID 16571)
-- Name: UX_EventOfficialAssignments_Event_Trunk; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "UX_EventOfficialAssignments_Event_Trunk" ON public."EventOfficialAssignments" USING btree ("EventId", "TrunkRadioNumber") WHERE ("TrunkRadioNumber" IS NOT NULL);


--
-- TOC entry 4988 (class 1259 OID 16572)
-- Name: UX_EventOfficialAssignments_Event_Vhf; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "UX_EventOfficialAssignments_Event_Vhf" ON public."EventOfficialAssignments" USING btree ("EventId", "VhfRadioNumber") WHERE ("VhfRadioNumber" IS NOT NULL);


--
-- TOC entry 4992 (class 2606 OID 16460)
-- Name: Assignments Assignments_BoatId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Assignments"
    ADD CONSTRAINT "Assignments_BoatId_fkey" FOREIGN KEY ("BoatId") REFERENCES public."Boats"("Id") ON DELETE SET NULL;


--
-- TOC entry 4993 (class 2606 OID 16455)
-- Name: Assignments Assignments_OfficialId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Assignments"
    ADD CONSTRAINT "Assignments_OfficialId_fkey" FOREIGN KEY ("OfficialId") REFERENCES public."Officials"("Id") ON DELETE CASCADE;


--
-- TOC entry 4994 (class 2606 OID 16465)
-- Name: Assignments Assignments_ShiftId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Assignments"
    ADD CONSTRAINT "Assignments_ShiftId_fkey" FOREIGN KEY ("ShiftId") REFERENCES public."Shifts"("Id") ON DELETE CASCADE;


--
-- TOC entry 4999 (class 2606 OID 16601)
-- Name: EventAssignmentImportBatches FK_EventAssignmentImportBatches_Events_EventId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EventAssignmentImportBatches"
    ADD CONSTRAINT "FK_EventAssignmentImportBatches_Events_EventId" FOREIGN KEY ("EventId") REFERENCES public."Events"("Id") ON DELETE CASCADE;


--
-- TOC entry 4995 (class 2606 OID 16565)
-- Name: EventOfficialAssignments FK_EventOfficialAssignments_Boats; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EventOfficialAssignments"
    ADD CONSTRAINT "FK_EventOfficialAssignments_Boats" FOREIGN KEY ("BoatId") REFERENCES public."Boats"("Id") ON DELETE SET NULL;


--
-- TOC entry 4996 (class 2606 OID 16555)
-- Name: EventOfficialAssignments FK_EventOfficialAssignments_Events; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EventOfficialAssignments"
    ADD CONSTRAINT "FK_EventOfficialAssignments_Events" FOREIGN KEY ("EventId") REFERENCES public."Events"("Id") ON DELETE CASCADE;


--
-- TOC entry 4997 (class 2606 OID 16606)
-- Name: EventOfficialAssignments FK_EventOfficialAssignments_ImportBatch; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EventOfficialAssignments"
    ADD CONSTRAINT "FK_EventOfficialAssignments_ImportBatch" FOREIGN KEY ("ImportBatchId") REFERENCES public."EventAssignmentImportBatches"("Id") ON DELETE SET NULL;


--
-- TOC entry 4998 (class 2606 OID 16560)
-- Name: EventOfficialAssignments FK_EventOfficialAssignments_Officials; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EventOfficialAssignments"
    ADD CONSTRAINT "FK_EventOfficialAssignments_Officials" FOREIGN KEY ("OfficialId") REFERENCES public."Officials"("Id") ON DELETE RESTRICT;


-- Completed on 2026-07-19 20:00:10

--
-- PostgreSQL database dump complete
--

\unrestrict XtekjniANFdJOZ6ScwhxVSZbfgcYiKWt0R5xsmPZUbfP9QrMaCN1OHGwWYYRmQ6

