import {
    Activity,
    CalendarDays,
    CheckCircle2,
    Clock3,
    RadioTower,
    RefreshCw,
    Sailboat,
    ShieldCheck,
    Users,
} from "lucide-react";
import {
    useCallback,
    useEffect,
    useMemo,
    useState,
} from "react";
import { NavLink } from "react-router-dom";

import DashboardStatCard from "./components/DashboardStatCard";
import HeroBanner from "./components/HeroBanner";

import "./Dashboard.css";

const API_BASE_URL = "http://localhost:5000";

type Official = {
    id: string;
    name: string;
    role: string;
    status: string;
};

type EventItem = {
    id: string;
    name: string;
    status: string;
};

type Boat = {
    id: string;
    name: string;
    callSign: string;
    status: string;
};

type User = {
    id: string;
    name: string;
    email: string;
    role: string;
    isActive: boolean;
};

type DashboardStatus = "loading" | "online" | "offline";

type DashboardData = {
    officials: Official[];
    events: EventItem[];
    boats: Boat[];
    users: User[];
};

const emptyData: DashboardData = {
    officials: [],
    events: [],
    boats: [],
    users: [],
};

function Dashboard() {
    const [data, setData] = useState<DashboardData>(emptyData);
    const [status, setStatus] =
        useState<DashboardStatus>("loading");
    const [lastUpdated, setLastUpdated] =
        useState<Date | null>(null);

    const loadDashboard = useCallback(async () => {
        setStatus("loading");

        try {
            const [
                healthResponse,
                officialsResponse,
                eventsResponse,
                boatsResponse,
                usersResponse,
            ] = await Promise.all([
                fetch(`${API_BASE_URL}/api/health`),
                fetch(`${API_BASE_URL}/api/officials`),
                fetch(`${API_BASE_URL}/api/events`),
                fetch(`${API_BASE_URL}/api/boats`),
                fetch(`${API_BASE_URL}/api/users`),
            ]);

            const failed = [
                healthResponse,
                officialsResponse,
                eventsResponse,
                boatsResponse,
                usersResponse,
            ].find((response) => !response.ok);

            if (failed) {
                throw new Error(
                    `Az API ${failed.status} hibát adott.`,
                );
            }

            const [officials, events, boats, users] =
                await Promise.all([
                    officialsResponse.json() as Promise<Official[]>,
                    eventsResponse.json() as Promise<EventItem[]>,
                    boatsResponse.json() as Promise<Boat[]>,
                    usersResponse.json() as Promise<User[]>,
                ]);

            setData({
                officials: Array.isArray(officials)
                    ? officials
                    : [],
                events: Array.isArray(events) ? events : [],
                boats: Array.isArray(boats) ? boats : [],
                users: Array.isArray(users) ? users : [],
            });

            setStatus("online");
            setLastUpdated(new Date());
        } catch (error) {
            console.error(
                "A Dashboard adatai nem tölthetők be:",
                error,
            );
            setStatus("offline");
        }
    }, []);

    useEffect(() => {
        void loadDashboard();

        const timer = window.setInterval(() => {
            void loadDashboard();
        }, 30000);

        return () => window.clearInterval(timer);
    }, [loadDashboard]);

    const statistics = useMemo(() => {
        const activeEvents = data.events.filter(
            (item) =>
                normalizeValue(item.status) === "active",
        ).length;

        const activeOfficials = data.officials.filter(
            isOfficialOnDuty,
        ).length;

        const availableOfficials = data.officials.filter(
            isOfficialAvailable,
        ).length;

        const activeBoats = data.boats.filter(
            (boat) =>
                normalizeValue(boat.status) === "active",
        ).length;

        const activeUsers = data.users.filter(
            (user) => user.isActive,
        ).length;

        return {
            activeEvents,
            activeOfficials,
            availableOfficials,
            activeBoats,
            activeUsers,
        };
    }, [data]);

    const visibleOfficials = useMemo(
        () =>
            data.officials
                .filter(isOfficialOnDuty)
                .slice(0, 6),
        [data.officials],
    );

    const visibleEvents = useMemo(
        () => data.events.slice(0, 5),
        [data.events],
    );

    return (
        <div className="eh-dashboard">
            <HeroBanner
                activeEvents={statistics.activeEvents}
                activeOfficials={statistics.activeOfficials}
                backendOnline={status === "online"}
            />

            {status === "offline" && (
                <section className="eh-dashboard__warning">
                    <RadioTower size={20} strokeWidth={1.8} />
                    <div>
                        <strong>
                            A backend jelenleg nem érhető el.
                        </strong>
                        <p>
                            Indítsd el az EventHelm.Api projektet,
                            majd próbáld újra.
                        </p>
                    </div>
                    <button
                        type="button"
                        onClick={() => void loadDashboard()}
                    >
                        <RefreshCw size={16} />
                        Újrapróbálás
                    </button>
                </section>
            )}

            <section className="eh-dashboard__section-heading">
                <div>
                    <span>OPERÁCIÓS ÁTTEKINTÉS</span>
                    <h2>Aktuális rendszerállapot</h2>
                </div>

                <button
                    type="button"
                    className="eh-dashboard__refresh"
                    onClick={() => void loadDashboard()}
                    disabled={status === "loading"}
                >
                    <RefreshCw
                        size={17}
                        className={
                            status === "loading"
                                ? "eh-dashboard__refresh-icon--loading"
                                : ""
                        }
                    />
                    {status === "loading"
                        ? "Frissítés..."
                        : "Adatok frissítése"}
                </button>
            </section>

            <section className="eh-dashboard__statistics">
                <DashboardStatCard
                    icon={CalendarDays}
                    label="Aktív rendezvények"
                    value={statistics.activeEvents}
                    description={`${data.events.length} összes rendezvény`}
                    variant="gold"
                />
                <DashboardStatCard
                    icon={Sailboat}
                    label="Aktív motorcsónakok"
                    value={statistics.activeBoats}
                    description={`${data.boats.length} regisztrált egység`}
                    variant="blue"
                />
                <DashboardStatCard
                    icon={ShieldCheck}
                    label="Szolgálatban"
                    value={statistics.activeOfficials}
                    description={`${statistics.availableOfficials} elérhető rendező`}
                    variant="green"
                />
                <DashboardStatCard
                    icon={Users}
                    label="Aktív felhasználók"
                    value={statistics.activeUsers}
                    description={`${data.users.length} regisztrált felhasználó`}
                    variant="red"
                />
            </section>

            <section className="eh-dashboard__main-grid">
                <DashboardPanel
                    eyebrow="SZEMÉLYZET"
                    title="Szolgálatban lévő rendezők"
                    action={
                        <NavLink to="/officials">
                            Összes rendező
                        </NavLink>
                    }
                >
                    {visibleOfficials.length === 0 ? (
                        <EmptyState
                            icon={ShieldCheck}
                            title="Nincs aktív rendező"
                            description={
                                status === "offline"
                                    ? "Az élő adatokhoz indítsd el a backendet."
                                    : "Jelenleg nincs szolgálatban lévő rendező."
                            }
                        />
                    ) : (
                        <div className="eh-dashboard__official-list">
                            {visibleOfficials.map(
                                (official) => (
                                    <OfficialRow
                                        key={official.id}
                                        official={official}
                                    />
                                ),
                            )}
                        </div>
                    )}
                </DashboardPanel>

                <DashboardPanel
                    eyebrow="RENDEZVÉNYEK"
                    title="Versenyprogram"
                    action={
                        <NavLink to="/events">
                            Összes rendezvény
                        </NavLink>
                    }
                >
                    {visibleEvents.length === 0 ? (
                        <EmptyState
                            icon={CalendarDays}
                            title="Nincs rendezvény"
                            description="Hozd létre az első rendezvényt a Rendezvények oldalon."
                        />
                    ) : (
                        <div className="eh-dashboard__event-list">
                            {visibleEvents.map(
                                (eventItem) => (
                                    <EventRow
                                        key={eventItem.id}
                                        eventItem={eventItem}
                                    />
                                ),
                            )}
                        </div>
                    )}
                </DashboardPanel>
            </section>

            <section className="eh-dashboard__bottom-grid">
                <DashboardPanel
                    eyebrow="RENDSZER"
                    title="Operációs állapot"
                >
                    <div className="eh-dashboard__operation-list">
                        <OperationRow
                            icon={Activity}
                            title="Backend kapcsolat"
                            description={
                                status === "online"
                                    ? "Az EventHelm API megfelelően működik."
                                    : "A backend kapcsolat nem érhető el."
                            }
                            state={
                                status === "online"
                                    ? "Rendben"
                                    : "Offline"
                            }
                            variant={
                                status === "online"
                                    ? "success"
                                    : "danger"
                            }
                        />
                        <OperationRow
                            icon={CheckCircle2}
                            title="Adatszinkronizáció"
                            description={
                                lastUpdated
                                    ? `Utolsó frissítés: ${formatTime(
                                          lastUpdated,
                                      )}`
                                    : "Még nem történt sikeres adatfrissítés."
                            }
                            state={
                                lastUpdated ? "Friss" : "Várakozik"
                            }
                            variant={
                                lastUpdated
                                    ? "success"
                                    : "neutral"
                            }
                        />
                        <OperationRow
                            icon={Clock3}
                            title="Automatikus frissítés"
                            description="A Dashboard 30 másodpercenként frissíti az adatokat."
                            state="Aktív"
                            variant="information"
                        />
                    </div>
                </DashboardPanel>

                <DashboardPanel
                    eyebrow="GYORS ELÉRÉS"
                    title="Operációs műveletek"
                >
                    <div className="eh-dashboard__quick-actions">
                        <QuickAction
                            icon={CalendarDays}
                            title="Rendezvények kezelése"
                            description="Új rendezvény létrehozása és státuszkezelés"
                            to="/events"
                        />
                        <QuickAction
                            icon={ShieldCheck}
                            title="Rendezői állomány"
                            description="Személyzet és elérhetőség kezelése"
                            to="/officials"
                        />
                        <QuickAction
                            icon={Sailboat}
                            title="Motorcsónak-flotta"
                            description="Egységek és műszaki állapot"
                            to="/boats"
                        />
                    </div>
                </DashboardPanel>
            </section>
        </div>
    );
}

function DashboardPanel({
    eyebrow,
    title,
    action,
    children,
}: {
    eyebrow: string;
    title: string;
    action?: React.ReactNode;
    children: React.ReactNode;
}) {
    return (
        <article className="eh-dashboard-panel">
            <header className="eh-dashboard-panel__header">
                <div>
                    <span>{eyebrow}</span>
                    <h2>{title}</h2>
                </div>
                {action && (
                    <div className="eh-dashboard-panel__action">
                        {action}
                    </div>
                )}
            </header>
            <div className="eh-dashboard-panel__content">
                {children}
            </div>
        </article>
    );
}

function OfficialRow({
    official,
}: {
    official: Official;
}) {
    return (
        <div className="eh-dashboard__official-row">
            <div className="eh-dashboard__avatar">
                {getInitials(official.name)}
            </div>
            <div className="eh-dashboard__official-information">
                <strong>{official.name}</strong>
                <span>{official.role || "Versenyrendező"}</span>
            </div>
            <span className="eh-dashboard__status-badge eh-dashboard__status-badge--success">
                Szolgálatban
            </span>
        </div>
    );
}

function EventRow({
    eventItem,
}: {
    eventItem: EventItem;
}) {
    return (
        <div className="eh-dashboard__event-row">
            <div className="eh-dashboard__event-icon">
                <CalendarDays size={18} />
            </div>
            <div className="eh-dashboard__event-information">
                <strong>{eventItem.name}</strong>
                <span>Vitorlás rendezvény</span>
            </div>
            <EventStatusBadge status={eventItem.status} />
        </div>
    );
}

function EventStatusBadge({
    status,
}: {
    status: string;
}) {
    const normalized = normalizeValue(status);
    const className =
        normalized === "active"
            ? "eh-dashboard__status-badge--success"
            : normalized === "draft"
              ? "eh-dashboard__status-badge--warning"
              : "eh-dashboard__status-badge--neutral";

    return (
        <span
            className={[
                "eh-dashboard__status-badge",
                className,
            ].join(" ")}
        >
            {eventStatusLabel(status)}
        </span>
    );
}

function OperationRow({
    icon: Icon,
    title,
    description,
    state,
    variant,
}: {
    icon: typeof Activity;
    title: string;
    description: string;
    state: string;
    variant:
        | "success"
        | "danger"
        | "information"
        | "neutral";
}) {
    return (
        <div className="eh-dashboard__operation-row">
            <div
                className={[
                    "eh-dashboard__operation-icon",
                    `eh-dashboard__operation-icon--${variant}`,
                ].join(" ")}
            >
                <Icon size={19} />
            </div>
            <div className="eh-dashboard__operation-information">
                <strong>{title}</strong>
                <span>{description}</span>
            </div>
            <span
                className={[
                    "eh-dashboard__operation-state",
                    `eh-dashboard__operation-state--${variant}`,
                ].join(" ")}
            >
                {state}
            </span>
        </div>
    );
}

function QuickAction({
    icon: Icon,
    title,
    description,
    to,
}: {
    icon: typeof CalendarDays;
    title: string;
    description: string;
    to: string;
}) {
    return (
        <NavLink
            to={to}
            className="eh-dashboard__quick-action"
        >
            <span className="eh-dashboard__quick-action-icon">
                <Icon size={20} />
            </span>
            <span className="eh-dashboard__quick-action-content">
                <strong>{title}</strong>
                <small>{description}</small>
            </span>
            <span className="eh-dashboard__quick-action-arrow">
                →
            </span>
        </NavLink>
    );
}

function EmptyState({
    icon: Icon,
    title,
    description,
}: {
    icon: typeof CalendarDays;
    title: string;
    description: string;
}) {
    return (
        <div className="eh-dashboard__empty">
            <div className="eh-dashboard__empty-icon">
                <Icon size={27} />
            </div>
            <strong>{title}</strong>
            <p>{description}</p>
        </div>
    );
}

function normalizeValue(
    value?: string | null,
): string {
    return value?.trim().toLowerCase() ?? "";
}

function isOfficialOnDuty(
    official: Official,
): boolean {
    return [
        "active",
        "on duty",
        "onduty",
        "szolgálatban",
        "szolgalatban",
        "aktív",
        "aktiv",
    ].includes(normalizeValue(official.status));
}

function isOfficialAvailable(
    official: Official,
): boolean {
    return ![
        "unavailable",
        "offline",
        "inactive",
        "inaktív",
        "inaktiv",
        "nem elérhető",
        "nem elerheto",
    ].includes(normalizeValue(official.status));
}

function getInitials(name: string): string {
    return (
        name
            .split(" ")
            .filter(Boolean)
            .slice(0, 2)
            .map((part) =>
                part.charAt(0).toUpperCase(),
            )
            .join("") || "?"
    );
}

function eventStatusLabel(status: string): string {
    const normalized = normalizeValue(status);

    if (normalized === "active") {
        return "Aktív";
    }

    if (normalized === "draft") {
        return "Tervezet";
    }

    if (
        normalized === "finished" ||
        normalized === "closed"
    ) {
        return "Befejezett";
    }

    return status || "Ismeretlen";
}

function formatTime(date: Date): string {
    return new Intl.DateTimeFormat("hu-HU", {
        hour: "2-digit",
        minute: "2-digit",
    }).format(date);
}

export default Dashboard;
