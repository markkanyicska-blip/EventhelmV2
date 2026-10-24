import { useEffect, useMemo, useState } from "react";
import "./OperationsCenter.css";

const API_BASE_URL = import.meta.env.VITE_API_URL ?? "/api";

type TimelineView = "boats" | "officials";

type EventItem = {
    id: string;
    name: string;
    status?: string;
};

type EventAssignment = {
    id: string;
    eventId: string;
    officialId: string;
    eventTaskId?: string | null;
    eventBoatId?: string | null;
    eventAreaId?: string | null;
    assignmentGroupId?: string | null;
    title: string;
    role?: string | null;
    startAt: string;
    endAt: string;
    status: string;
    notes?: string | null;
    officialName: string;
    taskName?: string | null;
    boatName?: string | null;
    areaName?: string | null;
};

type OfficialTimelineRow = {
    officialId: string;
    officialName: string;
    plannedMinutes: number;
    actualMinutes: number;
    assignments: EventAssignment[];
};

type EventAssignmentGroupMember = {
    assignmentId: string;
    officialId: string;
    officialName: string;
    role?: string | null;
};

type EventAssignmentGroupTimelineItem = {
    assignmentGroupId?: string | null;
    eventBoatId?: string | null;
    boatName?: string | null;
    eventTaskId?: string | null;
    taskName?: string | null;
    eventAreaId?: string | null;
    areaName?: string | null;
    title: string;
    startAt: string;
    endAt: string;
    status: string;
    notes?: string | null;
    members: EventAssignmentGroupMember[];
};

type BoatTimelineRow = {
    eventBoatId: string;
    boatName: string;
    plannedMinutes: number;
    items: EventAssignmentGroupTimelineItem[];
};

type EventTimelineResult = {
    eventId: string;
    from?: string | null;
    to?: string | null;
    assignments: EventAssignment[];
    officials: OfficialTimelineRow[];
    boats: BoatTimelineRow[];
};

type TimelineItemModel = {
    id: string;
    title: string;
    subtitle?: string;
    startAt: Date;
    endAt: Date;
    status: string;
};

type TimelineRowModel = {
    id: string;
    name: string;
    meta: string;
    items: TimelineItemModel[];
};

function OperationsCenter() {
    const [events, setEvents] = useState<EventItem[]>([]);
    const [selectedEventId, setSelectedEventId] = useState("");
    const [timeline, setTimeline] = useState<EventTimelineResult | null>(null);
    const [view, setView] = useState<TimelineView>("boats");

    const [eventsLoading, setEventsLoading] = useState(true);
    const [timelineLoading, setTimelineLoading] = useState(false);

    const [error, setError] = useState("");
    const [selectedItem, setSelectedItem] =
        useState<TimelineItemModel | null>(null);

    useEffect(() => {
        void loadEvents();
    }, []);

    useEffect(() => {
        if (!selectedEventId) {
            setTimeline(null);
            return;
        }

        void loadTimeline(selectedEventId);
    }, [selectedEventId]);

    async function loadEvents() {
        setEventsLoading(true);
        setError("");

        try {
            const response = await fetch(`${API_BASE_URL}/events`);

            if (!response.ok) {
                throw new Error(
                    await getResponseError(
                        response,
                        "A rendezvények betöltése nem sikerült.",
                    ),
                );
            }

            const data = (await response.json()) as EventItem[];

            setEvents(data);

            if (data.length > 0) {
                setSelectedEventId((current) => current || data[0].id);
            }
        } catch (loadError) {
            setError(getErrorMessage(loadError));
        } finally {
            setEventsLoading(false);
        }
    }

    async function loadTimeline(eventId: string) {
        setTimelineLoading(true);
        setError("");
        setSelectedItem(null);

        try {
            const response = await fetch(
                `${API_BASE_URL}/events/${eventId}/timeline`,
            );

            if (!response.ok) {
                throw new Error(
                    await getResponseError(
                        response,
                        "A timeline betöltése nem sikerült.",
                    ),
                );
            }

            const data = (await response.json()) as EventTimelineResult;
            setTimeline(data);
        } catch (loadError) {
            setTimeline(null);
            setError(getErrorMessage(loadError));
        } finally {
            setTimelineLoading(false);
        }
    }

    const selectedEvent = useMemo(
        () => events.find((event) => event.id === selectedEventId) ?? null,
        [events, selectedEventId],
    );

    const rows = useMemo<TimelineRowModel[]>(() => {
        if (!timeline) {
            return [];
        }

        if (view === "officials") {
            return timeline.officials.map((official) => ({
                id: official.officialId,
                name: official.officialName,
                meta: formatMinutes(official.plannedMinutes),
                items: official.assignments.map((assignment) => ({
                    id: assignment.id,
                    title: assignment.title,
                    subtitle: [
                        assignment.boatName,
                        assignment.areaName,
                        assignment.role,
                    ]
                        .filter(Boolean)
                        .join(" · "),
                    startAt: new Date(assignment.startAt),
                    endAt: new Date(assignment.endAt),
                    status: assignment.status,
                })),
            }));
        }

        return timeline.boats.map((boat) => ({
            id: boat.eventBoatId,
            name: boat.boatName,
            meta: formatMinutes(boat.plannedMinutes),
            items: boat.items.map((item, index) => ({
                id:
                    item.assignmentGroupId ??
                    `${boat.eventBoatId}-${item.startAt}-${index}`,
                title: item.title,
                subtitle: [
                    item.taskName,
                    item.areaName,
                    item.members.map((member) => member.officialName).join(", "),
                ]
                    .filter(Boolean)
                    .join(" · "),
                startAt: new Date(item.startAt),
                endAt: new Date(item.endAt),
                status: item.status,
            })),
        }));
    }, [timeline, view]);

    const timelineBounds = useMemo(() => {
        const allItems = rows.flatMap((row) => row.items);

        if (allItems.length === 0) {
            const now = new Date();
            const start = startOfHour(now);
            const end = new Date(start.getTime() + 12 * 60 * 60 * 1000);

            return { start, end };
        }

        const earliestTime = Math.min(
            ...allItems.map((item) => item.startAt.getTime()),
        );

        const latestTime = Math.max(
            ...allItems.map((item) => item.endAt.getTime()),
        );

        const start = startOfHour(new Date(earliestTime));
        const calculatedEnd = endOfHour(new Date(latestTime));

        const minimumEnd = new Date(
            start.getTime() + 6 * 60 * 60 * 1000,
        );

        return {
            start,
            end:
                calculatedEnd.getTime() < minimumEnd.getTime()
                    ? minimumEnd
                    : calculatedEnd,
        };
    }, [rows]);

    const hourMarkers = useMemo(() => {
        const markers: Date[] = [];
        const current = new Date(timelineBounds.start);

        while (current <= timelineBounds.end) {
            markers.push(new Date(current));
            current.setHours(current.getHours() + 1);
        }

        return markers;
    }, [timelineBounds]);

    const totalTimelineMilliseconds =
        timelineBounds.end.getTime() -
        timelineBounds.start.getTime();

    const assignmentsCount = timeline?.assignments.length ?? 0;

    const activeCount =
        timeline?.assignments.filter(
            (assignment) =>
                assignment.status.toLowerCase() !== "cancelled",
        ).length ?? 0;

    return (
        <main className="operations-page">
            <header className="operations-header">
                <div>
                    <div className="operations-eyebrow">
                        OPERÁCIÓS KÖZPONT
                    </div>

                    <h1>
                        {selectedEvent?.name ?? "Rendezvény kiválasztása"}
                    </h1>

                    <p>
                        Hajók, rendezők és feladatok áttekintése egy közös
                        idővonalon.
                    </p>
                </div>

                <div className="operations-event-selector">
                    <label htmlFor="operations-event">
                        Aktív rendezvény
                    </label>

                    <select
                        id="operations-event"
                        value={selectedEventId}
                        disabled={eventsLoading || events.length === 0}
                        onChange={(event) =>
                            setSelectedEventId(event.target.value)
                        }
                    >
                        {events.length === 0 && (
                            <option value="">
                                Nincs elérhető rendezvény
                            </option>
                        )}

                        {events.map((event) => (
                            <option key={event.id} value={event.id}>
                                {event.name}
                            </option>
                        ))}
                    </select>
                </div>
            </header>

            {error && (
                <div className="operations-alert operations-alert-error">
                    <div>
                        <strong>Hiba történt</strong>
                        <span>{error}</span>
                    </div>

                    <button
                        type="button"
                        onClick={() => {
                            if (selectedEventId) {
                                void loadTimeline(selectedEventId);
                            } else {
                                void loadEvents();
                            }
                        }}
                    >
                        Újrapróbálás
                    </button>
                </div>
            )}

            <section className="operations-summary-grid">
                <article className="operations-summary-card">
                    <span>Beosztások</span>
                    <strong>{assignmentsCount}</strong>
                    <small>Összes timeline-bejegyzés</small>
                </article>

                <article className="operations-summary-card">
                    <span>Aktív beosztások</span>
                    <strong>{activeCount}</strong>
                    <small>Nem törölt beosztások</small>
                </article>

                <article className="operations-summary-card">
                    <span>Hajók</span>
                    <strong>{timeline?.boats.length ?? 0}</strong>
                    <small>Timeline-on szereplő hajók</small>
                </article>

                <article className="operations-summary-card">
                    <span>Rendezők</span>
                    <strong>{timeline?.officials.length ?? 0}</strong>
                    <small>Timeline-on szereplő személyek</small>
                </article>
            </section>

            <section className="timeline-panel">
                <div className="timeline-toolbar">
                    <div>
                        <h2>Timeline</h2>
                        <p>
                            Válassz nézetet a hajók vagy a rendezők szerinti
                            csoportosításhoz.
                        </p>
                    </div>

                    <div className="timeline-toolbar-actions">
                        <div
                            className="timeline-view-switch"
                            role="group"
                            aria-label="Timeline nézet"
                        >
                            <button
                                type="button"
                                className={view === "boats" ? "active" : ""}
                                onClick={() => setView("boats")}
                            >
                                Motorcsónakok
                            </button>

                            <button
                                type="button"
                                className={view === "officials" ? "active" : ""}
                                onClick={() => setView("officials")}
                            >
                                Rendezők
                            </button>
                        </div>

                        <button
                            type="button"
                            className="timeline-refresh-button"
                            disabled={!selectedEventId || timelineLoading}
                            onClick={() =>
                                selectedEventId &&
                                void loadTimeline(selectedEventId)
                            }
                        >
                            Frissítés
                        </button>
                    </div>
                </div>

                {timelineLoading ? (
                    <div className="timeline-state">
                        <div className="timeline-spinner" />
                        <strong>Timeline betöltése...</strong>
                    </div>
                ) : rows.length === 0 ? (
                    <div className="timeline-state">
                        <div className="timeline-empty-icon">⌁</div>
                        <strong>Még nincs megjeleníthető beosztás</strong>
                        <p>
                            Hozz létre EventAssignment rekordokat ehhez a
                            rendezvényhez, és itt automatikusan megjelennek.
                        </p>
                    </div>
                ) : (
                    <div className="timeline-scroll-container">
                        <div
                            className="timeline-table"
                            style={{
                                minWidth: `${Math.max(
                                    1100,
                                    280 + hourMarkers.length * 100,
                                )}px`,
                            }}
                        >
                            <div className="timeline-header-row">
                                <div className="timeline-name-header">
                                    {view === "boats"
                                        ? "Motorcsónak"
                                        : "Rendező"}
                                </div>

                                <div className="timeline-hours">
                                    {hourMarkers.map((marker) => (
                                        <div
                                            key={marker.toISOString()}
                                            className="timeline-hour"
                                        >
                                            <strong>{formatHour(marker)}</strong>
                                            <span>
                                                {marker.toLocaleDateString("hu-HU", {
                                                    month: "short",
                                                    day: "numeric",
                                                })}
                                            </span>
                                        </div>
                                    ))}
                                </div>
                            </div>

                            {rows.map((row) => (
                                <div className="timeline-data-row" key={row.id}>
                                    <div className="timeline-row-name">
                                        <strong>{row.name}</strong>
                                        <span>{row.meta}</span>
                                    </div>

                                    <div className="timeline-track">
                                        {hourMarkers.map((marker) => (
                                            <div
                                                key={`${row.id}-${marker.toISOString()}`}
                                                className="timeline-grid-line"
                                                style={{
                                                    left: `${getTimelinePosition(
                                                        marker,
                                                        timelineBounds.start,
                                                        totalTimelineMilliseconds,
                                                    )}%`,
                                                }}
                                            />
                                        ))}

                                        {row.items.map((item) => {
                                            const left = getTimelinePosition(
                                                item.startAt,
                                                timelineBounds.start,
                                                totalTimelineMilliseconds,
                                            );

                                            const right = getTimelinePosition(
                                                item.endAt,
                                                timelineBounds.start,
                                                totalTimelineMilliseconds,
                                            );

                                            const width = Math.max(1.5, right - left);

                                            return (
                                                <button
                                                    type="button"
                                                    key={item.id}
                                                    className={`timeline-item timeline-status-${normalizeStatus(
                                                        item.status,
                                                    )}`}
                                                    style={{
                                                        left: `${left}%`,
                                                        width: `${width}%`,
                                                    }}
                                                    title={`${item.title}
${formatDateTime(item.startAt)} – ${formatDateTime(
                                                        item.endAt,
                                                    )}
${item.subtitle ?? ""}`}
                                                    onClick={() => setSelectedItem(item)}
                                                >
                                                    <strong>{item.title}</strong>

                                                    <span>
                                                        {formatTime(item.startAt)}–
                                                        {formatTime(item.endAt)}
                                                    </span>

                                                    {item.subtitle && (
                                                        <small>{item.subtitle}</small>
                                                    )}
                                                </button>
                                            );
                                        })}
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}
            </section>

            {selectedItem && (
                <div
                    className="operations-modal-backdrop"
                    role="presentation"
                    onMouseDown={() => setSelectedItem(null)}
                >
                    <section
                        className="operations-modal"
                        role="dialog"
                        aria-modal="true"
                        aria-labelledby="timeline-item-title"
                        onMouseDown={(event) => event.stopPropagation()}
                    >
                        <div className="operations-modal-header">
                            <div>
                                <span>BEOSZTÁS RÉSZLETEI</span>
                                <h2 id="timeline-item-title">
                                    {selectedItem.title}
                                </h2>
                            </div>

                            <button
                                type="button"
                                aria-label="Bezárás"
                                onClick={() => setSelectedItem(null)}
                            >
                                ×
                            </button>
                        </div>

                        <dl className="operations-modal-details">
                            <div>
                                <dt>Kezdés</dt>
                                <dd>{formatDateTime(selectedItem.startAt)}</dd>
                            </div>

                            <div>
                                <dt>Befejezés</dt>
                                <dd>{formatDateTime(selectedItem.endAt)}</dd>
                            </div>

                            <div>
                                <dt>Időtartam</dt>
                                <dd>
                                    {formatMinutes(
                                        Math.round(
                                            (selectedItem.endAt.getTime() -
                                                selectedItem.startAt.getTime()) /
                                            60000,
                                        ),
                                    )}
                                </dd>
                            </div>

                            <div>
                                <dt>Állapot</dt>
                                <dd>{translateStatus(selectedItem.status)}</dd>
                            </div>
                        </dl>

                        {selectedItem.subtitle && (
                            <div className="operations-modal-description">
                                {selectedItem.subtitle}
                            </div>
                        )}

                        <div className="operations-modal-footer">
                            <button
                                type="button"
                                onClick={() => setSelectedItem(null)}
                            >
                                Bezárás
                            </button>
                        </div>
                    </section>
                </div>
            )}
        </main>
    );
}

function getTimelinePosition(
    date: Date,
    timelineStart: Date,
    totalMilliseconds: number,
) {
    if (totalMilliseconds <= 0) {
        return 0;
    }

    const position =
        ((date.getTime() - timelineStart.getTime()) /
            totalMilliseconds) *
        100;

    return Math.max(0, Math.min(100, position));
}

function startOfHour(date: Date) {
    const result = new Date(date);
    result.setMinutes(0, 0, 0);
    return result;
}

function endOfHour(date: Date) {
    const result = new Date(date);
    result.setMinutes(0, 0, 0);
    result.setHours(result.getHours() + 1);
    return result;
}

function formatHour(date: Date) {
    return date.toLocaleTimeString("hu-HU", {
        hour: "2-digit",
        minute: "2-digit",
    });
}

function formatTime(date: Date) {
    return date.toLocaleTimeString("hu-HU", {
        hour: "2-digit",
        minute: "2-digit",
    });
}

function formatDateTime(date: Date) {
    return date.toLocaleString("hu-HU", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
    });
}

function formatMinutes(minutes: number) {
    const safeMinutes = Math.max(0, minutes);
    const hours = Math.floor(safeMinutes / 60);
    const remainingMinutes = safeMinutes % 60;

    if (hours === 0) {
        return `${remainingMinutes} perc`;
    }

    if (remainingMinutes === 0) {
        return `${hours} óra`;
    }

    return `${hours} óra ${remainingMinutes} perc`;
}

function normalizeStatus(status: string) {
    return status.trim().toLowerCase().replaceAll(" ", "");
}

function translateStatus(status: string) {
    switch (normalizeStatus(status)) {
        case "planned":
            return "Tervezett";
        case "confirmed":
            return "Megerősített";
        case "inprogress":
            return "Folyamatban";
        case "completed":
            return "Befejezett";
        case "cancelled":
            return "Törölt";
        default:
            return status;
    }
}

function getErrorMessage(error: unknown) {
    if (error instanceof Error) {
        return error.message;
    }

    return "Ismeretlen hiba történt.";
}

async function getResponseError(
    response: Response,
    fallbackMessage: string,
) {
    try {
        const body = (await response.json()) as {
            message?: string;
            title?: string;
        };

        return body.message || body.title || fallbackMessage;
    } catch {
        return fallbackMessage;
    }
}

export default OperationsCenter;