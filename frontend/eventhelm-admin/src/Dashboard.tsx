
import {
    useCallback,
    useEffect,
    useMemo,
    useState,
} from "react";

import type { CSSProperties, ReactNode } from "react";

const API_BASE_URL = "http://localhost:5000";

type Official = {
    id?: number | string;
    name?: string;
    fullName?: string;
    status?: string;
    availability?: string;
    role?: string;
};

type DashboardStatus = "loading" | "online" | "offline";

function Dashboard() {
    const [officials, setOfficials] = useState<Official[]>([]);
    const [status, setStatus] =
        useState<DashboardStatus>("loading");
    const [lastUpdated, setLastUpdated] =
        useState<Date | null>(null);

    const loadDashboard = useCallback(async () => {
        setStatus((currentStatus) =>
            currentStatus === "online"
                ? "online"
                : "loading",
        );

        try {
            const response = await fetch(
                `${API_BASE_URL}/api/officials`,
            );

            if (!response.ok) {
                throw new Error(
                    `Az API ${response.status} hibát adott.`,
                );
            }

            const result: unknown = await response.json();

            const officialList = extractArray(result);

            setOfficials(officialList as Official[]);
            setStatus("online");
            setLastUpdated(new Date());
        } catch (error) {
            console.error(
                "A dashboard adatai nem tölthetők be:",
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

        return () => {
            window.clearInterval(timer);
        };
    }, [loadDashboard]);

    const statistics = useMemo(() => {
        const active = officials.filter(isOfficialOnDuty).length;

        const available = officials.filter(
            isOfficialAvailable,
        ).length;

        const unavailable = Math.max(
            officials.length - available,
            0,
        );

        return {
            total: officials.length,
            active,
            available,
            unavailable,
        };
    }, [officials]);

    const activeOfficials = useMemo(
        () => officials.filter(isOfficialOnDuty).slice(0, 8),
        [officials],
    );

    return (
        <div style={styles.page}>
            <header style={styles.header}>
                <div>
                    <div style={styles.eyebrow}>
                        EVENTHELM OPERATIONS
                    </div>

                    <h1 style={styles.title}>
                        Operációs központ
                    </h1>

                    <p style={styles.subtitle}>
                        A versenyrendezés aktuális állapota
                        egyetlen képernyőn.
                    </p>
                </div>

                <div style={styles.headerActions}>
                    <SystemStatus status={status} />

                    <button
                        type="button"
                        style={styles.refreshButton}
                        onClick={() => void loadDashboard()}
                        disabled={status === "loading"}
                    >
                        {status === "loading"
                            ? "Frissítés..."
                            : "Adatok frissítése"}
                    </button>
                </div>
            </header>

            {status === "offline" && (
                <div style={styles.warning}>
                    <strong>
                        A backend jelenleg nem érhető el.
                    </strong>

                    <span>
                        Indítsd el az EventHelm.Api projektet
                        Visual Studióból. A dashboard ettől még
                        használható, de élő adatokat nem tud
                        megjeleníteni.
                    </span>
                </div>
            )}

            <section style={styles.statisticsGrid}>
                <StatisticCard
                    icon="👥"
                    label="Összes rendező"
                    value={statistics.total}
                    description="Regisztrált személyzet"
                />

                <StatisticCard
                    icon="🟢"
                    label="Szolgálatban"
                    value={statistics.active}
                    description="Aktív rendezők"
                />

                <StatisticCard
                    icon="🟡"
                    label="Elérhető"
                    value={statistics.available}
                    description="Beosztható személyzet"
                />

                <StatisticCard
                    icon="🔴"
                    label="Nem elérhető"
                    value={statistics.unavailable}
                    description="Jelenleg nem beosztható"
                />
            </section>

            <section style={styles.mainGrid}>
                <DashboardPanel
                    title="Szolgálatban lévő rendezők"
                    subtitle="Az aktuálisan aktív személyzet"
                    action={
                        <a
                            href="/officials"
                            style={styles.panelLink}
                        >
                            Összes rendező
                        </a>
                    }
                >
                    {activeOfficials.length === 0 ? (
                        <EmptyState
                            icon="👥"
                            title="Nincs aktív rendező"
                            description={
                                status === "offline"
                                    ? "Az adatokhoz indítsd el a backendet."
                                    : "Jelenleg nincs szolgálatban lévő rendező."
                            }
                        />
                    ) : (
                        <div style={styles.officialList}>
                            {activeOfficials.map(
                                (official, index) => (
                                    <OfficialRow
                                        key={
                                            official.id ??
                                            `${getOfficialName(
                                                official,
                                            )}-${index}`
                                        }
                                        official={official}
                                    />
                                ),
                            )}
                        </div>
                    )}
                </DashboardPanel>

                <DashboardPanel
                    title="Operációs állapot"
                    subtitle="A következő modulok előkészítése"
                >
                    <div style={styles.moduleList}>
                        <ModuleRow
                            icon="🚤"
                            title="Motorcsónakok"
                            description="Aktív hajók és műszaki állapot"
                            status="Következő"
                        />

                        <ModuleRow
                            icon="📋"
                            title="Feladatok"
                            description="Nyitott és sürgős feladatok"
                            status="Következő"
                        />

                        <ModuleRow
                            icon="🕒"
                            title="Műszakok"
                            description="Aktuális és következő műszak"
                            status="Következő"
                        />

                        <ModuleRow
                            icon="🌦️"
                            title="Időjárás"
                            description="Szél és időjárási riasztások"
                            status="Tervezett"
                        />
                    </div>
                </DashboardPanel>
            </section>

            <section style={styles.bottomGrid}>
                <DashboardPanel
                    title="Legutóbbi események"
                    subtitle="Operációs napló"
                >
                    <TimelineItem
                        time={formatTime(lastUpdated)}
                        title={
                            status === "online"
                                ? "A dashboard adatai frissültek"
                                : "Várakozás a backend kapcsolatra"
                        }
                        description={
                            status === "online"
                                ? `${officials.length} rendező adatai betöltve.`
                                : "Indítsd el az API-t az élő adatokhoz."
                        }
                    />

                    <TimelineItem
                        time="—"
                        title="Motorcsónak modul"
                        description="A hajók állapota itt jelenik majd meg."
                    />

                    <TimelineItem
                        time="—"
                        title="Feladatkezelő modul"
                        description="A kiosztott feladatok eseményei itt jelennek majd meg."
                    />
                </DashboardPanel>

                <DashboardPanel
                    title="Gyors műveletek"
                    subtitle="Gyakran használt funkciók"
                >
                    <div style={styles.quickActions}>
                        <QuickAction
                            icon="➕"
                            title="Új rendező"
                            href="/officials"
                        />

                        <QuickAction
                            icon="👤"
                            title="Felhasználók"
                            href="/users"
                        />

                        <QuickAction
                            icon="🔄"
                            title="Dashboard frissítése"
                            onClick={() =>
                                void loadDashboard()
                            }
                        />
                    </div>
                </DashboardPanel>
            </section>
        </div>
    );
}

function StatisticCard({
    icon,
    label,
    value,
    description,
}: {
    icon: ReactNode;
    label: string;
    value: number;
    description: string;
}) {
    return (
        <article style={styles.statisticCard}>
            <div style={styles.statisticHeader}>
                <span style={styles.statisticIcon}>
                    {icon}
                </span>

                <span style={styles.statisticLabel}>
                    {label}
                </span>
            </div>

            <div style={styles.statisticValue}>{value}</div>

            <div style={styles.statisticDescription}>
                {description}
            </div>
        </article>
    );
}

function DashboardPanel({
    title,
    subtitle,
    action,
    children,
}: {
    title: string;
    subtitle: string;
    action?: ReactNode;
    children: ReactNode;
}) {
    return (
        <article style={styles.panel}>
            <div style={styles.panelHeader}>
                <div>
                    <h2 style={styles.panelTitle}>{title}</h2>

                    <p style={styles.panelSubtitle}>
                        {subtitle}
                    </p>
                </div>

                {action}
            </div>

            <div>{children}</div>
        </article>
    );
}

function SystemStatus({
    status,
}: {
    status: DashboardStatus;
}) {
    const configuration = {
        loading: {
            label: "Kapcsolódás",
            background: "#FEF3C7",
            color: "#92400E",
            dot: "#F59E0B",
        },
        online: {
            label: "Backend online",
            background: "#DCFCE7",
            color: "#166534",
            dot: "#22C55E",
        },
        offline: {
            label: "Backend offline",
            background: "#FEE2E2",
            color: "#991B1B",
            dot: "#EF4444",
        },
    }[status];

    return (
        <div
            style={{
                ...styles.systemStatus,
                background: configuration.background,
                color: configuration.color,
            }}
        >
            <span
                style={{
                    ...styles.statusDot,
                    background: configuration.dot,
                }}
            />

            {configuration.label}
        </div>
    );
}

function OfficialRow({
    official,
}: {
    official: Official;
}) {
    return (
        <div style={styles.officialRow}>
            <div style={styles.avatar}>
                {getInitials(getOfficialName(official))}
            </div>

            <div style={styles.officialInformation}>
                <div style={styles.officialName}>
                    {getOfficialName(official)}
                </div>

                <div style={styles.officialRole}>
                    {official.role || "Versenyrendező"}
                </div>
            </div>

            <div style={styles.activeBadge}>
                Szolgálatban
            </div>
        </div>
    );
}

function ModuleRow({
    icon,
    title,
    description,
    status,
}: {
    icon: ReactNode;
    title: string;
    description: string;
    status: string;
}) {
    return (
        <div style={styles.moduleRow}>
            <div style={styles.moduleIcon}>{icon}</div>

            <div style={styles.moduleInformation}>
                <div style={styles.moduleTitle}>{title}</div>

                <div style={styles.moduleDescription}>
                    {description}
                </div>
            </div>

            <div style={styles.moduleStatus}>{status}</div>
        </div>
    );
}

function TimelineItem({
    time,
    title,
    description,
}: {
    time: string;
    title: string;
    description: string;
}) {
    return (
        <div style={styles.timelineItem}>
            <div style={styles.timelineTime}>{time}</div>

            <div style={styles.timelineMarker} />

            <div>
                <div style={styles.timelineTitle}>
                    {title}
                </div>

                <div style={styles.timelineDescription}>
                    {description}
                </div>
            </div>
        </div>
    );
}

function QuickAction({
    icon,
    title,
    href,
    onClick,
}: {
    icon: ReactNode;
    title: string;
    href?: string;
    onClick?: () => void;
}) {
    const content = (
        <>
            <span style={styles.quickActionIcon}>
                {icon}
            </span>

            <span>{title}</span>
        </>
    );

    if (href) {
        return (
            <a href={href} style={styles.quickAction}>
                {content}
            </a>
        );
    }

    return (
        <button
            type="button"
            style={styles.quickAction}
            onClick={onClick}
        >
            {content}
        </button>
    );
}

function EmptyState({
    icon,
    title,
    description,
}: {
    icon: ReactNode;
    title: string;
    description: string;
}) {
    return (
        <div style={styles.emptyState}>
            <div style={styles.emptyIcon}>{icon}</div>

            <div style={styles.emptyTitle}>{title}</div>

            <div style={styles.emptyDescription}>
                {description}
            </div>
        </div>
    );
}

function extractArray(value: unknown): unknown[] {
    if (Array.isArray(value)) {
        return value;
    }

    if (
        typeof value === "object" &&
        value !== null
    ) {
        const record = value as Record<string, unknown>;

        const possibleArrays = [
            record.items,
            record.data,
            record.results,
            record.officials,
        ];

        const matchingArray = possibleArrays.find(
            Array.isArray,
        );

        if (matchingArray) {
            return matchingArray;
        }
    }

    return [];
}

function getOfficialName(official: Official): string {
    return (
        official.fullName ||
        official.name ||
        "Névtelen rendező"
    );
}

function getInitials(name: string): string {
    const initials = name
        .split(" ")
        .filter(Boolean)
        .slice(0, 2)
        .map((part) => part.charAt(0).toUpperCase())
        .join("");

    return initials || "?";
}

function normalizeValue(value?: string): string {
    return value?.trim().toLowerCase() ?? "";
}

function isOfficialOnDuty(official: Official): boolean {
    const value = normalizeValue(
        official.status || official.availability,
    );

    return [
        "active",
        "on duty",
        "onduty",
        "szolgálatban",
        "szolgalatban",
        "aktív",
        "aktiv",
    ].includes(value);
}

function isOfficialAvailable(
    official: Official,
): boolean {
    const value = normalizeValue(
        official.availability || official.status,
    );

    if (!value) {
        return true;
    }

    return ![
        "unavailable",
        "offline",
        "nem elérhető",
        "nem elerheto",
        "inactive",
        "inaktív",
        "inaktiv",
    ].includes(value);
}

function formatTime(date: Date | null): string {
    if (!date) {
        return "—";
    }

    return new Intl.DateTimeFormat("hu-HU", {
        hour: "2-digit",
        minute: "2-digit",
    }).format(date);
}

const styles: Record<string, CSSProperties> = {
    page: {
        boxSizing: "border-box",
        minHeight: "100vh",
        padding: 32,
        background: "#F1F5F9",
    },

    header: {
        display: "flex",
        alignItems: "flex-start",
        justifyContent: "space-between",
        gap: 24,
        marginBottom: 28,
    },

    eyebrow: {
        marginBottom: 8,
        color: "#2563EB",
        fontSize: 12,
        fontWeight: 800,
        letterSpacing: "0.12em",
    },

    title: {
        margin: 0,
        color: "#0F172A",
        fontSize: 36,
        fontWeight: 850,
        lineHeight: 1.15,
    },

    subtitle: {
        margin: "10px 0 0",
        color: "#64748B",
        fontSize: 16,
    },

    headerActions: {
        display: "flex",
        alignItems: "center",
        gap: 12,
        flexWrap: "wrap",
    },

    systemStatus: {
        display: "flex",
        alignItems: "center",
        gap: 9,
        padding: "10px 14px",
        borderRadius: 999,
        fontSize: 13,
        fontWeight: 750,
    },

    statusDot: {
        width: 9,
        height: 9,
        borderRadius: "50%",
    },

    refreshButton: {
        border: 0,
        borderRadius: 10,
        padding: "11px 16px",
        background: "#0F172A",
        color: "#FFFFFF",
        fontSize: 14,
        fontWeight: 750,
        cursor: "pointer",
    },

    warning: {
        display: "flex",
        flexDirection: "column",
        gap: 4,
        marginBottom: 24,
        padding: "16px 18px",
        border: "1px solid #FECACA",
        borderRadius: 14,
        background: "#FEF2F2",
        color: "#991B1B",
        fontSize: 14,
    },

    statisticsGrid: {
        display: "grid",
        gridTemplateColumns:
            "repeat(auto-fit, minmax(210px, 1fr))",
        gap: 18,
        marginBottom: 20,
    },

    statisticCard: {
        padding: 22,
        border: "1px solid #E2E8F0",
        borderRadius: 18,
        background: "#FFFFFF",
        boxShadow:
            "0 10px 25px rgba(15, 23, 42, 0.05)",
    },

    statisticHeader: {
        display: "flex",
        alignItems: "center",
        gap: 10,
        marginBottom: 20,
    },

    statisticIcon: {
        fontSize: 20,
    },

    statisticLabel: {
        color: "#475569",
        fontSize: 14,
        fontWeight: 750,
    },

    statisticValue: {
        color: "#0F172A",
        fontSize: 38,
        fontWeight: 850,
        lineHeight: 1,
    },

    statisticDescription: {
        marginTop: 10,
        color: "#94A3B8",
        fontSize: 13,
    },

    mainGrid: {
        display: "grid",
        gridTemplateColumns:
            "minmax(0, 1.4fr) minmax(320px, 1fr)",
        gap: 20,
        marginBottom: 20,
    },

    bottomGrid: {
        display: "grid",
        gridTemplateColumns:
            "minmax(0, 1.4fr) minmax(320px, 1fr)",
        gap: 20,
    },

    panel: {
        padding: 24,
        border: "1px solid #E2E8F0",
        borderRadius: 18,
        background: "#FFFFFF",
        boxShadow:
            "0 10px 25px rgba(15, 23, 42, 0.04)",
    },

    panelHeader: {
        display: "flex",
        alignItems: "flex-start",
        justifyContent: "space-between",
        gap: 18,
        marginBottom: 22,
    },

    panelTitle: {
        margin: 0,
        color: "#0F172A",
        fontSize: 19,
        fontWeight: 800,
    },

    panelSubtitle: {
        margin: "6px 0 0",
        color: "#94A3B8",
        fontSize: 13,
    },

    panelLink: {
        color: "#2563EB",
        fontSize: 13,
        fontWeight: 750,
        textDecoration: "none",
    },

    officialList: {
        display: "flex",
        flexDirection: "column",
        gap: 10,
    },

    officialRow: {
        display: "flex",
        alignItems: "center",
        gap: 13,
        padding: 12,
        border: "1px solid #F1F5F9",
        borderRadius: 12,
    },

    avatar: {
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        width: 42,
        height: 42,
        borderRadius: 12,
        background: "#DBEAFE",
        color: "#1D4ED8",
        fontSize: 13,
        fontWeight: 850,
    },

    officialInformation: {
        flex: 1,
        minWidth: 0,
    },

    officialName: {
        overflow: "hidden",
        color: "#0F172A",
        fontSize: 14,
        fontWeight: 750,
        textOverflow: "ellipsis",
        whiteSpace: "nowrap",
    },

    officialRole: {
        marginTop: 3,
        color: "#94A3B8",
        fontSize: 12,
    },

    activeBadge: {
        padding: "7px 10px",
        borderRadius: 999,
        background: "#DCFCE7",
        color: "#166534",
        fontSize: 11,
        fontWeight: 800,
    },

    moduleList: {
        display: "flex",
        flexDirection: "column",
        gap: 10,
    },

    moduleRow: {
        display: "flex",
        alignItems: "center",
        gap: 13,
        padding: 12,
        borderRadius: 12,
        background: "#F8FAFC",
    },

    moduleIcon: {
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        width: 42,
        height: 42,
        borderRadius: 11,
        background: "#FFFFFF",
        fontSize: 20,
    },

    moduleInformation: {
        flex: 1,
    },

    moduleTitle: {
        color: "#0F172A",
        fontSize: 14,
        fontWeight: 750,
    },

    moduleDescription: {
        marginTop: 3,
        color: "#94A3B8",
        fontSize: 12,
    },

    moduleStatus: {
        padding: "6px 9px",
        borderRadius: 999,
        background: "#E2E8F0",
        color: "#475569",
        fontSize: 10,
        fontWeight: 850,
        textTransform: "uppercase",
    },

    timelineItem: {
        display: "grid",
        gridTemplateColumns: "48px 12px 1fr",
        gap: 11,
        paddingBottom: 18,
    },

    timelineTime: {
        paddingTop: 1,
        color: "#64748B",
        fontSize: 12,
        fontWeight: 750,
    },

    timelineMarker: {
        width: 9,
        height: 9,
        marginTop: 5,
        borderRadius: "50%",
        background: "#2563EB",
        boxShadow:
            "0 0 0 4px rgba(37, 99, 235, 0.12)",
    },

    timelineTitle: {
        color: "#0F172A",
        fontSize: 14,
        fontWeight: 750,
    },

    timelineDescription: {
        marginTop: 4,
        color: "#94A3B8",
        fontSize: 12,
        lineHeight: 1.5,
    },

    quickActions: {
        display: "grid",
        gap: 10,
    },

    quickAction: {
        display: "flex",
        alignItems: "center",
        gap: 12,
        boxSizing: "border-box",
        width: "100%",
        padding: "13px 14px",
        border: "1px solid #E2E8F0",
        borderRadius: 12,
        background: "#FFFFFF",
        color: "#0F172A",
        fontFamily: "inherit",
        fontSize: 14,
        fontWeight: 750,
        textAlign: "left",
        textDecoration: "none",
        cursor: "pointer",
    },

    quickActionIcon: {
        fontSize: 19,
    },

    emptyState: {
        padding: "42px 20px",
        textAlign: "center",
    },

    emptyIcon: {
        marginBottom: 12,
        fontSize: 40,
    },

    emptyTitle: {
        color: "#0F172A",
        fontSize: 16,
        fontWeight: 800,
    },

    emptyDescription: {
        maxWidth: 420,
        margin: "8px auto 0",
        color: "#94A3B8",
        fontSize: 13,
        lineHeight: 1.5,
    },
};

export default Dashboard;