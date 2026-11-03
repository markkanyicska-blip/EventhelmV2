import Planner from "../components/planner/Planner";
import {
    Activity,
    ArrowLeft,
    CalendarDays,
    ClipboardList,
    FileText,
    Flag,
    Fuel,
    Gauge,
    MapPin,
    Radio,
    Sailboat,
    Save,
    ShieldCheck,
    Signal,
    Timer,
    UserCheck,
    Upload,
    Users,
    Wrench,
} from "lucide-react";
import {
    useCallback,
    useEffect,
    useState,
} from "react";
import {
    Link,
    useParams,
} from "react-router-dom";

import {
    getEventDetails,
    updateEventDetails,
} from "../api/eventDetails";
import { EventAssignmentImportPanel } from "../components/events/EventAssignmentImportPanel";
import EventModulePanel, {
    type EventModuleCard,
} from "../components/EventModulePanel";
import EventOfficialsPanel from "../components/EventOfficialsPanel";
import EventTabBar, {
    type EventTabItem,
} from "../components/EventTabBar";
import {
    Badge,
    Button,
    Card,
    PageHeader,
} from "../components/ui";
import type {
    EventDetails,
    EventDetailsPayload,
} from "../types/eventDetails";

import "../components/events/EventAssignmentImportPanel.css";
import "./EventDetailsPage.css";

type Tab =
    | "general"
    | "officials"
    | "assignments"
    | "assignment-import"
    | "boats"
    | "radios"
    | "race-control";

const tabs: readonly EventTabItem<Tab>[] = [
    { id: "general", label: "Általános", icon: ClipboardList },
    { id: "officials", label: "Rendezők", icon: Users },
    {
        id: "assignments",
        label: "Beosztások",
        icon: CalendarDays,
    },
    { id: "assignment-import", label: "Beosztás import", icon: Upload },
    { id: "boats", label: "Motorcsónakok", icon: Sailboat },
    { id: "radios", label: "Rádiók", icon: Radio },
    { id: "race-control", label: "Race Control", icon: ShieldCheck },
];

const boatCards: readonly EventModuleCard[] = [
    {
        title: "Kiosztott hajók",
        description: "Az aktuális versenyhez kijelölt motorcsónakok áttekintése.",
        icon: Sailboat,
        to: "/boats",
        badge: "Aktív",
    },
    {
        title: "Tartalék egységek",
        description: "Készenléti és helyettesítő vízi egységek kezelése.",
        icon: ShieldCheck,
        to: "/boats",
        badge: "Tartalék",
    },
    {
        title: "Üzemanyag",
        description: "Üzemanyagszint, tankolási igény és indulási készültség követése.",
        icon: Fuel,
        to: "/boats",
        badge: "Logisztika",
    },
    {
        title: "Műszaki állapot",
        description: "Motor, felszerelés és biztonsági eszközök állapotának ellenőrzése.",
        icon: Wrench,
        to: "/boats",
        badge: "Technika",
    },
];

const radioCards: readonly EventModuleCard[] = [
    {
        title: "Trönk rádiók",
        description: "A trönk készülékek és csoportok versenyenkénti kiosztása.",
        icon: Signal,
        to: "/radio-assignments",
        badge: "Trönk",
    },
    {
        title: "VHF rádiók",
        description: "A VHF készülékek, csatornák és hívójelek kezelése.",
        icon: Radio,
        to: "/radio-assignments",
        badge: "VHF",
    },
    {
        title: "Kiosztások",
        description: "Rendezőnkénti rádió-hozzárendelések összesített áttekintése.",
        icon: UserCheck,
        to: "/radio-assignments",
        badge: "Személyhez",
    },
    {
        title: "Hiányzó eszközök",
        description: "A kiosztás nélküli vagy hiányzó rádiók gyors ellenőrzése.",
        icon: Activity,
        to: "/radio-assignments",
        badge: "Ellenőrzés",
    },
];

const raceControlCards: readonly EventModuleCard[] = [
    {
        title: "Élő állapot",
        description: "A verseny aktuális állapotának nyomon követése valós időben.",
        icon: Gauge,
        to: "/race-control",
        badge: "Live",
    },
    {
        title: "Jelzések kezelése",
        description: "Versenyjelzések hozzáadása, szerkesztése és közzététele.",
        icon: Flag,
        to: "/race-control",
        badge: "Jelzések",
    },
    {
        title: "Időmérés",
        description: "Időmérési adatok rögzítése, ellenőrzése és kezelése.",
        icon: Timer,
        to: "/race-control",
        badge: "Timing",
    },
    {
        title: "Napló és előzmények",
        description: "Minden operációs esemény és változás visszakereshető naplózása.",
        icon: FileText,
        to: "/race-control",
        badge: "Napló",
    },
];

const emptyForm: EventDetailsPayload = {
    name: "",
    status: "Draft",
    organizingClub: "",
    hostingClub: "",
    startDate: "",
    endDate: "",
    location: "",
    waterArea: "",
    raceOfficer: "",
    juryChair: "",
    technicalCommitteeChair: "",
    raceOffice: "",
    vhfChannel: "",
    trunkGroup: "",
    notes: "",
};

export default function EventDetailsPage() {
    const { eventId } = useParams<{ eventId: string }>();
    const [eventItem, setEventItem] = useState<EventDetails | null>(null);
    const [form, setForm] = useState<EventDetailsPayload>(emptyForm);
    const [activeTab, setActiveTab] = useState<Tab>("general");
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    const load = useCallback(async () => {
        if (!eventId) {
            setError("A rendezvény azonosítója hiányzik.");
            setLoading(false);
            return;
        }

        try {
            setLoading(true);
            const result = await getEventDetails(eventId);
            setEventItem(result);
            setForm(toPayload(result));
            setError(null);
        } catch (loadError) {
            console.error(loadError);
            setError("A rendezvény részletes adatai nem tölthetők be.");
        } finally {
            setLoading(false);
        }
    }, [eventId]);

    useEffect(() => {
        void load();
    }, [load]);

    async function save() {
        if (!eventId) return;

        if (!form.name.trim()) {
            setError("A rendezvény neve kötelező.");
            return;
        }

        try {
            setSaving(true);
            setSuccess(null);

            const updated = await updateEventDetails(
                eventId,
                normalizePayload(form),
            );

            setEventItem(updated);
            setForm(toPayload(updated));
            setError(null);
            setSuccess("A rendezvény adatai mentve.");
        } catch (saveError) {
            console.error(saveError);
            setError("A rendezvény mentése nem sikerült.");
        } finally {
            setSaving(false);
        }
    }

    if (loading) {
        return (
            <div className="eh-event-record">
                <Card>
                    <div className="eh-event-record__state">
                        Adatok betöltése...
                    </div>
                </Card>
            </div>
        );
    }

    if (!eventItem || !eventId) {
        return (
            <div className="eh-event-record">
                <Link className="eh-event-record__back" to="/events">
                    <ArrowLeft size={17} />
                    Vissza
                </Link>

                <Card>
                    <div className="eh-event-record__state">
                        {error ?? "A rendezvény nem található."}
                    </div>
                </Card>
            </div>
        );
    }

    return (
        <div className="eh-event-record">
            <Link className="eh-event-record__back" to="/events">
                <ArrowLeft size={17} />
                Vissza a rendezvényekhez
            </Link>

            <PageHeader
                eyebrow="RENDEZVÉNY ADATLAP"
                title={eventItem.name}
                description="A verseny részletes operációs és szervezési adatainak kezelése."
                actions={
                    <Badge variant={statusVariant(eventItem.status)}>
                        {statusLabel(eventItem.status)}
                    </Badge>
                }
            />

            {error && (
                <div className="eh-event-record__message eh-event-record__message--error">
                    {error}
                </div>
            )}

            {success && (
                <div className="eh-event-record__message eh-event-record__message--success">
                    {success}
                </div>
            )}

            <section className="eh-event-record__summary">
                <SummaryItem
                    icon={CalendarDays}
                    label="Időpont"
                    value={dateRange(eventItem.startDate, eventItem.endDate)}
                />
                <SummaryItem
                    icon={MapPin}
                    label="Helyszín"
                    value={eventItem.location || "Nincs megadva"}
                />
                <SummaryItem
                    icon={Flag}
                    label="Versenyvezető"
                    value={eventItem.raceOfficer || "Nincs megadva"}
                />
                <SummaryItem
                    icon={Radio}
                    label="Kommunikáció"
                    value={
                        [eventItem.vhfChannel, eventItem.trunkGroup]
                            .filter(Boolean)
                            .join(" · ") || "Nincs megadva"
                    }
                />
            </section>

            <EventTabBar
                items={tabs}
                activeTab={activeTab}
                onChange={(tab) => setActiveTab(tab as Tab)}
            />

            {activeTab === "general" && (
                <Card
                    eyebrow="ÁLTALÁNOS ADATOK"
                    title="Rendezvény szerkesztése"
                    action={
                        <Button
                            icon={<Save size={17} />}
                            loading={saving}
                            onClick={() => void save()}
                        >
                            Mentés
                        </Button>
                    }
                >
                    <div className="eh-event-record__form">
                        <Field label="Verseny neve">
                            <input
                                value={form.name}
                                onChange={(event) =>
                                    update("name", event.target.value)
                                }
                            />
                        </Field>

                        <Field label="Státusz">
                            <select
                                value={form.status}
                                onChange={(event) =>
                                    update("status", event.target.value)
                                }
                            >
                                <option value="Draft">Tervezet</option>
                                <option value="Active">Aktív</option>
                                <option value="Finished">Befejezett</option>
                            </select>
                        </Field>

                        <Field label="Szervező klub">
                            <input
                                value={form.organizingClub ?? ""}
                                onChange={(event) =>
                                    update("organizingClub", event.target.value)
                                }
                            />
                        </Field>

                        <Field label="Rendező klub">
                            <input
                                value={form.hostingClub ?? ""}
                                onChange={(event) =>
                                    update("hostingClub", event.target.value)
                                }
                            />
                        </Field>

                        <Field label="Kezdés">
                            <input
                                type="datetime-local"
                                value={form.startDate ?? ""}
                                onChange={(event) =>
                                    update("startDate", event.target.value)
                                }
                            />
                        </Field>

                        <Field label="Befejezés">
                            <input
                                type="datetime-local"
                                value={form.endDate ?? ""}
                                onChange={(event) =>
                                    update("endDate", event.target.value)
                                }
                            />
                        </Field>

                        <Field label="Helyszín">
                            <input
                                value={form.location ?? ""}
                                onChange={(event) =>
                                    update("location", event.target.value)
                                }
                            />
                        </Field>

                        <Field label="Vízterület">
                            <input
                                value={form.waterArea ?? ""}
                                onChange={(event) =>
                                    update("waterArea", event.target.value)
                                }
                            />
                        </Field>

                        <Field label="Versenyvezető">
                            <input
                                value={form.raceOfficer ?? ""}
                                onChange={(event) =>
                                    update("raceOfficer", event.target.value)
                                }
                            />
                        </Field>

                        <Field label="Óvási bizottság elnöke">
                            <input
                                value={form.juryChair ?? ""}
                                onChange={(event) =>
                                    update("juryChair", event.target.value)
                                }
                            />
                        </Field>

                        <Field label="Technikai bizottság elnöke">
                            <input
                                value={form.technicalCommitteeChair ?? ""}
                                onChange={(event) =>
                                    update(
                                        "technicalCommitteeChair",
                                        event.target.value,
                                    )
                                }
                            />
                        </Field>

                        <Field label="Versenyiroda vezető">
                            <input
                                value={form.raceOffice ?? ""}
                                onChange={(event) =>
                                    update("raceOffice", event.target.value)
                                }
                            />
                        </Field>

                        <Field label="VHF csatorna">
                            <input
                                value={form.vhfChannel ?? ""}
                                onChange={(event) =>
                                    update("vhfChannel", event.target.value)
                                }
                            />
                        </Field>

                        <Field label="Trönk csoport">
                            <input
                                value={form.trunkGroup ?? ""}
                                onChange={(event) =>
                                    update("trunkGroup", event.target.value)
                                }
                            />
                        </Field>

                        <Field label="Megjegyzések" fullWidth>
                            <textarea
                                rows={5}
                                value={form.notes ?? ""}
                                onChange={(event) =>
                                    update("notes", event.target.value)
                                }
                            />
                        </Field>
                    </div>
                </Card>
            )}

            {activeTab === "officials" && (
                <EventOfficialsPanel eventId={eventId} />
            )}
            {activeTab === "assignments" && (
                <Planner eventId={eventId} />
            )}

            {activeTab === "assignment-import" && (
                <EventAssignmentImportPanel
                    eventId={eventId}
                    onImported={() => {
                        setSuccess("A beosztás importálása sikeresen befejeződött.");
                    }}
                />
            )}

            {activeTab === "boats" && (
                <EventModulePanel
                    eyebrow="VÍZI EGYSÉGEK"
                    title="Motorcsónakok"
                    description="A rendezői, pályajelző és biztonsági egységek teljes operációs kezelése."
                    cards={boatCards}
                />
            )}

            {activeTab === "radios" && (
                <EventModulePanel
                    eyebrow="KOMMUNIKÁCIÓS MODUL"
                    title="Rádiók"
                    description="Trönk- és VHF-kommunikáció, eszközkiosztás és kapcsolati állapot áttekintése."
                    cards={radioCards}
                />
            )}

            {activeTab === "race-control" && (
                <EventModulePanel
                    eyebrow="RACE CONTROL MODUL"
                    title="Race Control"
                    description="A verseny aktuális operációs állapotának, jelzéseinek és eseményeinek kezelése."
                    cards={raceControlCards}
                />
            )}
        </div>
    );

    function update<K extends keyof EventDetailsPayload>(
        key: K,
        value: EventDetailsPayload[K],
    ) {
        setForm((current) => ({
            ...current,
            [key]: value,
        }));
        setSuccess(null);
    }
}

function Field({
    label,
    children,
    fullWidth = false,
}: {
    label: string;
    children: React.ReactNode;
    fullWidth?: boolean;
}) {
    return (
        <label
            className={
                fullWidth
                    ? "eh-event-record__field eh-event-record__field--full"
                    : "eh-event-record__field"
            }
        >
            <span>{label}</span>
            {children}
        </label>
    );
}

function SummaryItem({
    icon: Icon,
    label,
    value,
}: {
    icon: typeof CalendarDays;
    label: string;
    value: string;
}) {
    return (
        <article className="eh-event-record__summary-item">
            <div>
                <Icon size={20} />
            </div>
            <span>{label}</span>
            <strong>{value}</strong>
        </article>
    );
}

function toPayload(item: EventDetails): EventDetailsPayload {
    return {
        name: item.name,
        status: item.status,
        organizingClub: item.organizingClub ?? "",
        hostingClub: item.hostingClub ?? "",
        startDate: toLocalInput(item.startDate),
        endDate: toLocalInput(item.endDate),
        location: item.location ?? "",
        waterArea: item.waterArea ?? "",
        raceOfficer: item.raceOfficer ?? "",
        juryChair: item.juryChair ?? "",
        technicalCommitteeChair:
            item.technicalCommitteeChair ?? "",
        raceOffice: item.raceOffice ?? "",
        vhfChannel: item.vhfChannel ?? "",
        trunkGroup: item.trunkGroup ?? "",
        notes: item.notes ?? "",
    };
}

function normalizePayload(
    item: EventDetailsPayload,
): EventDetailsPayload {
    return {
        ...item,
        organizingClub: emptyToNull(item.organizingClub),
        hostingClub: emptyToNull(item.hostingClub),
        startDate: emptyToNull(item.startDate),
        endDate: emptyToNull(item.endDate),
        location: emptyToNull(item.location),
        waterArea: emptyToNull(item.waterArea),
        raceOfficer: emptyToNull(item.raceOfficer),
        juryChair: emptyToNull(item.juryChair),
        technicalCommitteeChair:
            emptyToNull(item.technicalCommitteeChair),
        raceOffice: emptyToNull(item.raceOffice),
        vhfChannel: emptyToNull(item.vhfChannel),
        trunkGroup: emptyToNull(item.trunkGroup),
        notes: emptyToNull(item.notes),
    };
}

function emptyToNull(
    value?: string | null,
): string | null {
    return value?.trim() ? value.trim() : null;
}

function toLocalInput(
    value?: string | null,
): string {
    if (!value) return "";

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "";

    const offset = date.getTimezoneOffset() * 60000;

    return new Date(date.getTime() - offset)
        .toISOString()
        .slice(0, 16);
}

function dateRange(
    start?: string | null,
    end?: string | null,
): string {
    if (!start && !end) return "Nincs megadva";

    const formatter = new Intl.DateTimeFormat(
        "hu-HU",
        {
            year: "numeric",
            month: "short",
            day: "numeric",
            hour: "2-digit",
            minute: "2-digit",
        },
    );

    const startText = start
        ? formatter.format(new Date(start))
        : "—";

    const endText = end
        ? formatter.format(new Date(end))
        : "—";

    return `${startText} – ${endText}`;
}

function statusLabel(value: string): string {
    if (value === "Active") return "Aktív";
    if (value === "Finished") return "Befejezett";
    return "Tervezet";
}

function statusVariant(
    value: string,
): "success" | "warning" | "neutral" {
    if (value === "Active") return "success";
    if (value === "Finished") return "neutral";
    return "warning";
}
