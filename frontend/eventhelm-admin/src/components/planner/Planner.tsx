import {
    CalendarDays,
    Clock3,
    Pencil,
    Plus,
    RefreshCw,
    Trash2,
    UserRound,
} from "lucide-react";
import {
    useCallback,
    useEffect,
    useMemo,
    useState,
} from "react";

import {
    createEventAssignment,
    deleteEventAssignment,
    EventAssignmentApiError,
    getEventAssignments,
    updateEventAssignment,
    validateEventAssignment,
    type AssignmentValidationResult,
    type CreateEventAssignmentRequest,
    type EventAssignment,
    type EventAssignmentStatus,
} from "../../api/eventAssignments";
import {
    getEventOfficialAssignments,
} from "../../api/eventOfficialAssignments";
import type {
    EventOfficialAssignment,
} from "../../types/eventOfficialAssignments";

import {
    Button,
    Card,
    Modal,
} from "../ui";

import "./planner.css";

type PlannerProps = {
    eventId: string;
};

type PlannerRow = {
    officialId: string;
    officialName: string;
    role: string;
    assignments: EventAssignment[];
    plannedMinutes: number;
};

type TimelineBounds = {
    start: Date;
    end: Date;
};

type AssignmentForm = {
    officialId: string;
    title: string;
    role: string;
    startAt: string;
    endAt: string;
    status: EventAssignmentStatus;
    notes: string;
    isLocked: boolean;
};

const minimumTimelineHours = 12;

const emptyForm: AssignmentForm = {
    officialId: "",
    title: "",
    role: "",
    startAt: "",
    endAt: "",
    status: "Planned",
    notes: "",
    isLocked: false,
};

export default function Planner({
    eventId,
}: PlannerProps) {
    const [officials, setOfficials] = useState<
        EventOfficialAssignment[]
    >([]);

    const [assignments, setAssignments] = useState<
        EventAssignment[]
    >([]);

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    const [editorOpen, setEditorOpen] = useState(false);
    const [editing, setEditing] =
        useState<EventAssignment | null>(null);
    const [form, setForm] = useState<AssignmentForm>(emptyForm);
    const [validation, setValidation] =
        useState<AssignmentValidationResult | null>(null);

    const load = useCallback(async () => {
        try {
            setLoading(true);

            const [officialItems, assignmentItems] =
                await Promise.all([
                    getEventOfficialAssignments(eventId),
                    getEventAssignments(eventId),
                ]);

            setOfficials(officialItems);
            setAssignments(assignmentItems);
            setError(null);
        } catch (loadError) {
            console.error(loadError);

            setError(
                loadError instanceof Error
                    ? loadError.message
                    : "A beosztástervező adatai nem tölthetők be.",
            );
        } finally {
            setLoading(false);
        }
    }, [eventId]);

    useEffect(() => {
        void load();
    }, [load]);

    const rows = useMemo<PlannerRow[]>(() => {
        return officials
            .map((official) => {
                const officialAssignments = assignments
                    .filter(
                        (assignment) =>
                            assignment.officialId ===
                            official.officialId,
                    )
                    .sort(
                        (left, right) =>
                            new Date(left.startAt).getTime() -
                            new Date(right.startAt).getTime(),
                    );

                const plannedMinutes = officialAssignments
                    .filter(
                        (assignment) =>
                            assignment.status !== "Cancelled",
                    )
                    .reduce(
                        (total, assignment) =>
                            total +
                            getDurationMinutes(
                                assignment.startAt,
                                assignment.endAt,
                            ),
                        0,
                    );

                return {
                    officialId: official.officialId,
                    officialName: official.officialName,
                    role: official.role,
                    assignments: officialAssignments,
                    plannedMinutes,
                };
            })
            .sort((left, right) =>
                left.officialName.localeCompare(
                    right.officialName,
                    "hu",
                ),
            );
    }, [assignments, officials]);

    const bounds = useMemo<TimelineBounds>(() => {
        const dates = assignments.flatMap((assignment) => [
            new Date(assignment.startAt),
            new Date(assignment.endAt),
        ]);

        if (dates.length === 0) {
            const start = startOfHour(new Date());

            return {
                start,
                end: new Date(
                    start.getTime() +
                        minimumTimelineHours * 60 * 60 * 1000,
                ),
            };
        }

        const start = startOfHour(
            new Date(
                Math.min(
                    ...dates.map((date) => date.getTime()),
                ),
            ),
        );

        const latest = endOfHour(
            new Date(
                Math.max(
                    ...dates.map((date) => date.getTime()),
                ),
            ),
        );

        const minimumEnd = new Date(
            start.getTime() +
                minimumTimelineHours * 60 * 60 * 1000,
        );

        return {
            start,
            end:
                latest.getTime() > minimumEnd.getTime()
                    ? latest
                    : minimumEnd,
        };
    }, [assignments]);

    const hourMarkers = useMemo(() => {
        const result: Date[] = [];
        const current = new Date(bounds.start);

        while (current.getTime() <= bounds.end.getTime()) {
            result.push(new Date(current));
            current.setHours(current.getHours() + 1);
        }

        return result;
    }, [bounds]);

    const totalMilliseconds =
        bounds.end.getTime() - bounds.start.getTime();

    const totalPlannedMinutes = rows.reduce(
        (total, row) => total + row.plannedMinutes,
        0,
    );

    function openCreate(officialId?: string) {
        const now = roundToQuarterHour(new Date());
        const end = new Date(now.getTime() + 60 * 60 * 1000);

        setEditing(null);
        setForm({
            ...emptyForm,
            officialId:
                officialId ?? officials[0]?.officialId ?? "",
            startAt: toLocalInput(now),
            endAt: toLocalInput(end),
        });
        setValidation(null);
        setError(null);
        setSuccess(null);
        setEditorOpen(true);
    }

    function openEdit(assignment: EventAssignment) {
        setEditing(assignment);
        setForm({
            officialId: assignment.officialId,
            title: assignment.title,
            role: assignment.role ?? "",
            startAt: toLocalInput(new Date(assignment.startAt)),
            endAt: toLocalInput(new Date(assignment.endAt)),
            status: assignment.status,
            notes: assignment.notes ?? "",
            isLocked: assignment.isLocked,
        });
        setValidation(null);
        setError(null);
        setSuccess(null);
        setEditorOpen(true);
    }

    async function runValidation() {
        const validationError = validateForm(form);

        if (validationError) {
            setError(validationError);
            return null;
        }

        try {
            const result = await validateEventAssignment(eventId, {
                officialId: form.officialId,
                excludedAssignmentId: editing?.id ?? null,
                startAt: new Date(form.startAt).toISOString(),
                endAt: new Date(form.endAt).toISOString(),
            });

            setValidation(result);
            setError(null);
            return result;
        } catch (validationErrorValue) {
            setError(getErrorMessage(validationErrorValue));
            return null;
        }
    }

    async function saveAssignment() {
        const validationError = validateForm(form);

        if (validationError) {
            setError(validationError);
            return;
        }

        try {
            setSaving(true);
            setError(null);
            setSuccess(null);

            const validationResult = await runValidation();

            if (!validationResult?.canAssign) {
                setError(
                    validationResult?.errors
                        .map((item) => item.message)
                        .join(" ") ||
                        "A beosztás nem menthető.",
                );
                return;
            }

            const request: CreateEventAssignmentRequest = {
                officialId: form.officialId,
                eventTaskId: null,
                eventBoatId: null,
                eventAreaId: null,
                assignmentGroupId: editing?.assignmentGroupId ?? null,
                title: form.title.trim(),
                role: emptyToNull(form.role),
                startAt: new Date(form.startAt).toISOString(),
                endAt: new Date(form.endAt).toISOString(),
                status: form.status,
                notes: emptyToNull(form.notes),
                isLocked: form.isLocked,
            };

            if (editing) {
                await updateEventAssignment(
                    eventId,
                    editing.id,
                    {
                        officialId: request.officialId,
                        eventTaskId: request.eventTaskId,
                        eventBoatId: request.eventBoatId,
                        eventAreaId: request.eventAreaId,
                        title: request.title,
                        role: request.role,
                        startAt: request.startAt,
                        endAt: request.endAt,
                        status: request.status,
                        notes: request.notes,
                        isLocked: request.isLocked,
                    },
                );
                setSuccess("A beosztás módosítva.");
            } else {
                await createEventAssignment(eventId, request);
                setSuccess("A beosztás létrehozva.");
            }

            setEditorOpen(false);
            await load();
        } catch (saveError) {
            if (saveError instanceof EventAssignmentApiError) {
                setValidation(saveError.validation ?? null);
            }

            setError(getErrorMessage(saveError));
        } finally {
            setSaving(false);
        }
    }

    async function removeAssignment() {
        if (!editing) {
            return;
        }

        if (
            !window.confirm(
                `Biztosan törlöd ezt a beosztást?\n\n${editing.title}`,
            )
        ) {
            return;
        }

        try {
            setSaving(true);
            await deleteEventAssignment(eventId, editing.id);
            setEditorOpen(false);
            setSuccess("A beosztás törölve.");
            await load();
        } catch (removeError) {
            setError(getErrorMessage(removeError));
        } finally {
            setSaving(false);
        }
    }

    return (
        <div className="eh-planner">
            {error && (
                <div className="eh-planner__error">{error}</div>
            )}

            {success && (
                <div className="eh-planner__success">{success}</div>
            )}

            <section className="eh-planner__metrics">
                <PlannerMetric
                    icon={UserRound}
                    label="Rendezők"
                    value={rows.length}
                />

                <PlannerMetric
                    icon={CalendarDays}
                    label="Beosztások"
                    value={assignments.length}
                />

                <PlannerMetric
                    icon={Clock3}
                    label="Tervezett munkaidő"
                    value={formatMinutes(totalPlannedMinutes)}
                />
            </section>

            <Card
                eyebrow="RENDEZŐI TERVEZŐ"
                title="Beosztási idővonal"
                action={
                    <div className="eh-planner__toolbar-actions">
                        <Button
                            icon={<Plus size={17} />}
                            onClick={() => openCreate()}
                            disabled={officials.length === 0}
                        >
                            Új beosztás
                        </Button>

                        <Button
                            variant="secondary"
                            icon={<RefreshCw size={17} />}
                            loading={loading}
                            onClick={() => void load()}
                        >
                            Frissítés
                        </Button>
                    </div>
                }
            >
                {loading ? (
                    <div className="eh-planner__state">
                        Beosztások betöltése...
                    </div>
                ) : rows.length === 0 ? (
                    <div className="eh-planner__state">
                        <strong>
                            Ehhez a rendezvényhez még nincs rendező
                            hozzáadva.
                        </strong>

                        <span>
                            Először a Rendezők fülön add hozzá a
                            résztvevőket.
                        </span>
                    </div>
                ) : (
                    <div className="eh-planner__scroll">
                        <div
                            className="eh-planner__table"
                            style={{
                                minWidth: Math.max(
                                    1100,
                                    280 + hourMarkers.length * 100,
                                ),
                            }}
                        >
                            <div className="eh-planner__header">
                                <div className="eh-planner__person-header">
                                    Rendező
                                </div>

                                <div className="eh-planner__hours">
                                    {hourMarkers.map((marker) => (
                                        <div
                                            key={marker.toISOString()}
                                            className="eh-planner__hour"
                                        >
                                            <strong>
                                                {formatTime(marker)}
                                            </strong>
                                            <span>
                                                {formatDate(marker)}
                                            </span>
                                        </div>
                                    ))}
                                </div>
                            </div>

                            {rows.map((row) => (
                                <div
                                    className="eh-planner__row"
                                    key={row.officialId}
                                >
                                    <button
                                        type="button"
                                        className="eh-planner__person eh-planner__person--button"
                                        onClick={() =>
                                            openCreate(row.officialId)
                                        }
                                        title="Új beosztás hozzáadása ehhez a rendezőhöz"
                                    >
                                        <strong>{row.officialName}</strong>
                                        <span>
                                            {row.role ||
                                                "Nincs szerepkör"}
                                        </span>
                                        <small>
                                            {formatMinutes(
                                                row.plannedMinutes,
                                            )}
                                        </small>
                                    </button>

                                    <div className="eh-planner__track">
                                        {hourMarkers.map((marker) => (
                                            <div
                                                key={`${row.officialId}-${marker.toISOString()}`}
                                                className="eh-planner__grid-line"
                                                style={{
                                                    left: `${getPosition(
                                                        marker,
                                                        bounds.start,
                                                        totalMilliseconds,
                                                    )}%`,
                                                }}
                                            />
                                        ))}

                                        {row.assignments.map(
                                            (assignment) => {
                                                const start = new Date(
                                                    assignment.startAt,
                                                );
                                                const end = new Date(
                                                    assignment.endAt,
                                                );

                                                const left = getPosition(
                                                    start,
                                                    bounds.start,
                                                    totalMilliseconds,
                                                );
                                                const right = getPosition(
                                                    end,
                                                    bounds.start,
                                                    totalMilliseconds,
                                                );
                                                const width = Math.max(
                                                    1.5,
                                                    right - left,
                                                );

                                                return (
                                                    <button
                                                        type="button"
                                                        key={assignment.id}
                                                        className={`eh-planner__assignment eh-planner__assignment--${normalizeStatus(
                                                            assignment.status,
                                                        )}`}
                                                        style={{
                                                            left: `${left}%`,
                                                            width: `${width}%`,
                                                        }}
                                                        title={buildAssignmentTitle(
                                                            assignment,
                                                        )}
                                                        onClick={() =>
                                                            openEdit(
                                                                assignment,
                                                            )
                                                        }
                                                    >
                                                        <strong>
                                                            {
                                                                assignment.title
                                                            }
                                                        </strong>
                                                        <span>
                                                            {formatTime(
                                                                start,
                                                            )}
                                                            –
                                                            {formatTime(end)}
                                                        </span>
                                                        <small>
                                                            {[
                                                                assignment.boatName,
                                                                assignment.areaName,
                                                                assignment.role,
                                                            ]
                                                                .filter(
                                                                    Boolean,
                                                                )
                                                                .join(" · ")}
                                                        </small>
                                                    </button>
                                                );
                                            },
                                        )}
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}
            </Card>

            <Modal
                open={editorOpen}
                title={
                    editing
                        ? "Beosztás szerkesztése"
                        : "Új beosztás"
                }
                onClose={() => setEditorOpen(false)}
                footer={
                    <>
                        {editing && (
                            <Button
                                variant="danger"
                                icon={<Trash2 size={16} />}
                                loading={saving}
                                onClick={() =>
                                    void removeAssignment()
                                }
                            >
                                Törlés
                            </Button>
                        )}

                        <div className="eh-planner__modal-spacer" />

                        <Button
                            variant="secondary"
                            onClick={() => setEditorOpen(false)}
                        >
                            Mégse
                        </Button>

                        <Button
                            variant="secondary"
                            onClick={() => void runValidation()}
                        >
                            Ellenőrzés
                        </Button>

                        <Button
                            icon={
                                editing ? (
                                    <Pencil size={16} />
                                ) : (
                                    <Plus size={16} />
                                )
                            }
                            loading={saving}
                            onClick={() => void saveAssignment()}
                        >
                            Mentés
                        </Button>
                    </>
                }
            >
                <div className="eh-planner__form">
                    <label>
                        Rendező
                        <select
                            value={form.officialId}
                            onChange={(event) =>
                                setForm((current) => ({
                                    ...current,
                                    officialId: event.target.value,
                                }))
                            }
                        >
                            <option value="">
                                Válassz rendezőt
                            </option>
                            {officials.map((official) => (
                                <option
                                    key={official.officialId}
                                    value={official.officialId}
                                >
                                    {official.officialName}
                                </option>
                            ))}
                        </select>
                    </label>

                    <label>
                        Beosztás megnevezése
                        <input
                            value={form.title}
                            onChange={(event) =>
                                setForm((current) => ({
                                    ...current,
                                    title: event.target.value,
                                }))
                            }
                            placeholder="Például rajtvonal kitűzése"
                        />
                    </label>

                    <label>
                        Szerepkör
                        <input
                            value={form.role}
                            onChange={(event) =>
                                setForm((current) => ({
                                    ...current,
                                    role: event.target.value,
                                }))
                            }
                            placeholder="Például kormányos"
                        />
                    </label>

                    <label>
                        Állapot
                        <select
                            value={form.status}
                            onChange={(event) =>
                                setForm((current) => ({
                                    ...current,
                                    status: event.target
                                        .value as EventAssignmentStatus,
                                }))
                            }
                        >
                            <option value="Planned">
                                Tervezett
                            </option>
                            <option value="Confirmed">
                                Megerősített
                            </option>
                            <option value="InProgress">
                                Folyamatban
                            </option>
                            <option value="Completed">
                                Befejezett
                            </option>
                            <option value="Cancelled">
                                Törölt
                            </option>
                        </select>
                    </label>

                    <label>
                        Kezdés
                        <input
                            type="datetime-local"
                            value={form.startAt}
                            onChange={(event) =>
                                setForm((current) => ({
                                    ...current,
                                    startAt: event.target.value,
                                }))
                            }
                        />
                    </label>

                    <label>
                        Befejezés
                        <input
                            type="datetime-local"
                            value={form.endAt}
                            onChange={(event) =>
                                setForm((current) => ({
                                    ...current,
                                    endAt: event.target.value,
                                }))
                            }
                        />
                    </label>

                    <label className="eh-planner__form-full">
                        Megjegyzés
                        <textarea
                            rows={4}
                            value={form.notes}
                            onChange={(event) =>
                                setForm((current) => ({
                                    ...current,
                                    notes: event.target.value,
                                }))
                            }
                        />
                    </label>

                    <label className="eh-planner__checkbox eh-planner__form-full">
                        <input
                            type="checkbox"
                            checked={form.isLocked}
                            onChange={(event) =>
                                setForm((current) => ({
                                    ...current,
                                    isLocked: event.target.checked,
                                }))
                            }
                        />
                        Beosztás zárolása
                    </label>
                </div>

                {validation && (
                    <div
                        className={`eh-planner__validation ${
                            validation.canAssign
                                ? "eh-planner__validation--success"
                                : "eh-planner__validation--error"
                        }`}
                    >
                        <strong>
                            {validation.canAssign
                                ? "A beosztás menthető."
                                : "A beosztás nem menthető."}
                        </strong>

                        {validation.errors.map((item) => (
                            <div key={item.code}>
                                {item.message}
                            </div>
                        ))}

                        {validation.warnings.map((item) => (
                            <div key={item.code}>
                                Figyelmeztetés: {item.message}
                            </div>
                        ))}

                        {validation.workload && (
                            <small>
                                Napi összes tervezett munkaidő:{" "}
                                {formatMinutes(
                                    validation.workload
                                        .totalPlannedMinutes,
                                )}
                            </small>
                        )}
                    </div>
                )}
            </Modal>
        </div>
    );
}

function PlannerMetric({
    icon: Icon,
    label,
    value,
}: {
    icon: typeof UserRound;
    label: string;
    value: number | string;
}) {
    return (
        <article className="eh-planner__metric">
            <div>
                <Icon size={20} />
            </div>
            <span>{label}</span>
            <strong>{value}</strong>
        </article>
    );
}

function validateForm(form: AssignmentForm): string | null {
    if (!form.officialId) {
        return "Válassz rendezőt.";
    }

    if (!form.title.trim()) {
        return "Add meg a beosztás megnevezését.";
    }

    if (!form.startAt || !form.endAt) {
        return "Add meg a kezdési és befejezési időpontot.";
    }

    const start = new Date(form.startAt);
    const end = new Date(form.endAt);

    if (
        Number.isNaN(start.getTime()) ||
        Number.isNaN(end.getTime())
    ) {
        return "Érvénytelen dátum vagy időpont.";
    }

    if (end <= start) {
        return "A befejezésnek későbbinek kell lennie a kezdésnél.";
    }

    return null;
}

function getPosition(
    date: Date,
    timelineStart: Date,
    totalMilliseconds: number,
): number {
    if (totalMilliseconds <= 0) {
        return 0;
    }

    const result =
        ((date.getTime() - timelineStart.getTime()) /
            totalMilliseconds) *
        100;

    return Math.max(0, Math.min(100, result));
}

function getDurationMinutes(
    startAt: string,
    endAt: string,
): number {
    const start = new Date(startAt).getTime();
    const end = new Date(endAt).getTime();

    if (
        Number.isNaN(start) ||
        Number.isNaN(end) ||
        end <= start
    ) {
        return 0;
    }

    return Math.round((end - start) / 60000);
}

function startOfHour(date: Date): Date {
    const result = new Date(date);
    result.setMinutes(0, 0, 0);
    return result;
}

function endOfHour(date: Date): Date {
    const result = startOfHour(date);
    result.setHours(result.getHours() + 1);
    return result;
}

function roundToQuarterHour(date: Date): Date {
    const result = new Date(date);
    const roundedMinutes = Math.ceil(result.getMinutes() / 15) * 15;
    result.setMinutes(roundedMinutes, 0, 0);
    return result;
}

function toLocalInput(date: Date): string {
    const offset = date.getTimezoneOffset() * 60000;

    return new Date(date.getTime() - offset)
        .toISOString()
        .slice(0, 16);
}

function formatTime(date: Date): string {
    return date.toLocaleTimeString("hu-HU", {
        hour: "2-digit",
        minute: "2-digit",
    });
}

function formatDate(date: Date): string {
    return date.toLocaleDateString("hu-HU", {
        month: "short",
        day: "numeric",
    });
}

function formatMinutes(value: number): string {
    const minutes = Math.max(0, Math.round(value));
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;

    if (hours === 0) {
        return `${remainingMinutes} perc`;
    }

    if (remainingMinutes === 0) {
        return `${hours} óra`;
    }

    return `${hours} óra ${remainingMinutes} perc`;
}

function normalizeStatus(value: string): string {
    return value
        .trim()
        .toLowerCase()
        .replaceAll(" ", "");
}

function buildAssignmentTitle(
    assignment: EventAssignment,
): string {
    return [
        assignment.title,
        `${new Date(assignment.startAt).toLocaleString(
            "hu-HU",
        )} – ${new Date(assignment.endAt).toLocaleString(
            "hu-HU",
        )}`,
        assignment.boatName
            ? `Hajó: ${assignment.boatName}`
            : null,
        assignment.areaName
            ? `Terület: ${assignment.areaName}`
            : null,
        assignment.taskName
            ? `Feladat: ${assignment.taskName}`
            : null,
    ]
        .filter(Boolean)
        .join("\n");
}

function emptyToNull(value: string): string | null {
    return value.trim() ? value.trim() : null;
}

function getErrorMessage(error: unknown): string {
    if (error instanceof Error) {
        return error.message;
    }

    return "Ismeretlen hiba történt.";
}
