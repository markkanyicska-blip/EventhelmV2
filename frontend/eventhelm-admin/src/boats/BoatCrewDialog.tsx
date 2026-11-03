import {
    useEffect,
    useMemo,
    useState,
} from "react";

import type {
    CSSProperties,
    FormEvent,
    ReactNode,
} from "react";

import {
    createBoatCrewAssignment,
    deleteBoatCrewAssignment,
    getBoatCrew,
    getOfficials,
    updateBoatCrewAssignment,
} from "./boatApi";

import {
    emptyCrewForm,
} from "./boatTypes";

import type {
    Boat,
    BoatCrewAssignment,
    CrewForm,
    Official,
} from "./boatTypes";

type BoatCrewDialogProps = {
    boat: Boat;
    open: boolean;
    onClose: () => void;
    onCrewChanged?: (
        boatId: string,
        crew: BoatCrewAssignment[],
    ) => void;
};

function BoatCrewDialog({
    boat,
    open,
    onClose,
    onCrewChanged,
}: BoatCrewDialogProps) {
    const [crew, setCrew] = useState<BoatCrewAssignment[]>([]);
    const [officials, setOfficials] = useState<Official[]>([]);
    const [form, setForm] = useState<CrewForm>(emptyCrewForm);

    const [editingId, setEditingId] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);
    const [deletingId, setDeletingId] = useState<string | null>(null);

    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");

    useEffect(() => {
        if (!open) {
            return;
        }

        void loadData();
    }, [open, boat.id]);

    useEffect(() => {
        if (!open) {
            return;
        }

        function handleKeyDown(event: KeyboardEvent) {
            if (event.key === "Escape") {
                onClose();
            }
        }

        window.addEventListener("keydown", handleKeyDown);

        return () => {
            window.removeEventListener("keydown", handleKeyDown);
        };
    }, [open, onClose]);

    const availableOfficials = useMemo(() => {
        const assignedIds = new Set(
            crew
                .filter((assignment) => assignment.id !== editingId)
                .map((assignment) => assignment.officialId),
        );

        return officials.filter(
            (official) => !assignedIds.has(official.id),
        );
    }, [crew, officials, editingId]);

    const captain = useMemo(
        () => crew.find((assignment) => assignment.isCaptain) ?? null,
        [crew],
    );

    const selectedOfficial = useMemo(
        () =>
            officials.find(
                (official) => official.id === form.officialId,
            ) ?? null,
        [officials, form.officialId],
    );

    async function loadData() {
        setLoading(true);
        setError("");
        setSuccess("");

        try {
            const [loadedCrew, loadedOfficials] = await Promise.all([
                getBoatCrew(boat.id),
                getOfficials(),
            ]);

            setCrew(loadedCrew);
            setOfficials(loadedOfficials);
            onCrewChanged?.(boat.id, loadedCrew);
        } catch (loadError) {
            console.error(loadError);
            setError(
                getErrorMessage(
                    loadError,
                    "A személyzet adatai nem tölthetők be.",
                ),
            );
        } finally {
            setLoading(false);
        }
    }

    async function loadCrewOnly() {
        const loadedCrew = await getBoatCrew(boat.id);
        setCrew(loadedCrew);
        onCrewChanged?.(boat.id, loadedCrew);
    }

    function updateForm<TField extends keyof CrewForm>(
        field: TField,
        value: CrewForm[TField],
    ) {
        setForm((currentForm) => ({
            ...currentForm,
            [field]: value,
        }));

        setError("");
        setSuccess("");
    }

    function startEdit(assignment: BoatCrewAssignment) {
        setEditingId(assignment.id);
        setForm({
            officialId: assignment.officialId,
            role: assignment.role || "Rendező",
            isCaptain: assignment.isCaptain,
            fromDate: toDateInputValue(assignment.fromDate),
            toDate: toDateInputValue(assignment.toDate),
            notes: assignment.notes ?? "",
        });
        setError("");
        setSuccess("");
    }

    function resetForm() {
        setEditingId(null);
        setForm(emptyCrewForm);
    }

    function validateForm(): string | null {
        if (!form.officialId) {
            return "A rendező kiválasztása kötelező.";
        }

        if (!form.role.trim()) {
            return "A szerepkör megadása kötelező.";
        }

        if (form.fromDate && form.toDate && form.toDate < form.fromDate) {
            return "A befejező dátum nem lehet korábbi a kezdő dátumnál.";
        }

        return null;
    }

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();

        const validationError = validateForm();
        if (validationError) {
            setError(validationError);
            return;
        }

        setSaving(true);
        setError("");
        setSuccess("");

        const sharedPayload = {
            role: form.role.trim(),
            isCaptain: form.isCaptain,
            fromDate: form.fromDate || null,
            toDate: form.toDate || null,
            notes: form.notes.trim() || null,
        };

        try {
            if (editingId) {
                await updateBoatCrewAssignment(
                    boat.id,
                    editingId,
                    sharedPayload,
                );
                setSuccess("A személyzeti beosztás módosítása sikerült.");
            } else {
                await createBoatCrewAssignment(boat.id, {
                    officialId: form.officialId,
                    ...sharedPayload,
                });
                setSuccess("A rendező hozzáadása sikerült.");
            }

            resetForm();
            await loadCrewOnly();
        } catch (saveError) {
            console.error(saveError);
            setError(
                getErrorMessage(
                    saveError,
                    "A személyzeti beosztás mentése sikertelen.",
                ),
            );
        } finally {
            setSaving(false);
        }
    }

    async function handleDelete(assignment: BoatCrewAssignment) {
        const confirmed = window.confirm(
            `Biztosan eltávolítod a hajó személyzetéből?\n\n${assignment.officialName}`,
        );

        if (!confirmed) {
            return;
        }

        setDeletingId(assignment.id);
        setError("");
        setSuccess("");

        try {
            await deleteBoatCrewAssignment(boat.id, assignment.id);

            if (editingId === assignment.id) {
                resetForm();
            }

            setSuccess("A személyzeti beosztás törlése sikerült.");
            await loadCrewOnly();
        } catch (deleteError) {
            console.error(deleteError);
            setError(
                getErrorMessage(
                    deleteError,
                    "A személyzeti beosztás törlése sikertelen.",
                ),
            );
        } finally {
            setDeletingId(null);
        }
    }

    if (!open) {
        return null;
    }

    return (
        <div
            style={styles.backdrop}
            onMouseDown={(event) => {
                if (event.target === event.currentTarget) {
                    onClose();
                }
            }}
        >
            <section
                role="dialog"
                aria-modal="true"
                aria-labelledby="boat-crew-title"
                style={styles.dialog}
            >
                <header style={styles.header}>
                    <div>
                        <div style={styles.eyebrow}>MOTORCSÓNAK-SZEMÉLYZET</div>
                        <h2 id="boat-crew-title" style={styles.title}>
                            {boat.callSign || boat.name}
                        </h2>
                        <p style={styles.subtitle}>
                            {boat.name}
                            {boat.registrationNumber
                                ? ` · ${boat.registrationNumber}`
                                : ""}
                        </p>
                    </div>

                    <button
                        type="button"
                        style={styles.closeButton}
                        onClick={onClose}
                        aria-label="Bezárás"
                    >
                        ×
                    </button>
                </header>

                {error && <MessageBox variant="error" message={error} />}
                {success && <MessageBox variant="success" message={success} />}

                <section style={styles.summaryGrid}>
                    <SummaryCard label="Személyzet" value={`${crew.length} fő`} />
                    <SummaryCard
                        label="Kapitány"
                        value={captain?.officialName ?? "Nincs kijelölve"}
                    />
                    <SummaryCard
                        label="Kapacitás"
                        value={
                            boat.capacity
                                ? `${boat.capacity} fő`
                                : "Nincs megadva"
                        }
                    />
                </section>

                <div style={styles.contentGrid}>
                    <section style={styles.panel}>
                        <div style={styles.panelHeader}>
                            <div>
                                <h3 style={styles.panelTitle}>Jelenlegi személyzet</h3>
                                <p style={styles.panelSubtitle}>
                                    A hajóhoz rendelt rendezők és szerepkörök.
                                </p>
                            </div>

                            <button
                                type="button"
                                style={styles.refreshButton}
                                onClick={() => void loadData()}
                                disabled={loading}
                            >
                                {loading ? "Frissítés..." : "Frissítés"}
                            </button>
                        </div>

                        {loading ? (
                            <div style={styles.empty}>Személyzet betöltése...</div>
                        ) : crew.length === 0 ? (
                            <div style={styles.empty}>
                                Ehhez a hajóhoz még nincs személyzet rendelve.
                            </div>
                        ) : (
                            <div style={styles.crewList}>
                                {crew.map((assignment) => (
                                    <CrewRow
                                        key={assignment.id}
                                        assignment={assignment}
                                        deleting={deletingId === assignment.id}
                                        onEdit={() => startEdit(assignment)}
                                        onDelete={() => void handleDelete(assignment)}
                                    />
                                ))}
                            </div>
                        )}
                    </section>

                    <section style={styles.panel}>
                        <h3 style={styles.panelTitle}>
                            {editingId
                                ? "Beosztás szerkesztése"
                                : "Rendező hozzáadása"}
                        </h3>
                        <p style={styles.panelSubtitle}>
                            Válassz rendezőt, majd add meg a hajón betöltött szerepét.
                        </p>

                        <form onSubmit={handleSubmit} style={styles.form}>
                            <Field label="Rendező">
                                <select
                                    style={styles.input}
                                    value={form.officialId}
                                    onChange={(event) =>
                                        updateForm("officialId", event.target.value)
                                    }
                                    disabled={saving || Boolean(editingId)}
                                >
                                    <option value="">Válassz rendezőt...</option>
                                    {availableOfficials.map((official) => (
                                        <option key={official.id} value={official.id}>
                                            {official.name}
                                        </option>
                                    ))}
                                </select>
                            </Field>

                            {selectedOfficial && (
                                <div style={styles.selectedOfficial}>
                                    <strong>{selectedOfficial.name}</strong>
                                    <span>{selectedOfficial.email || "Nincs e-mail"}</span>
                                    <span>{selectedOfficial.phone || "Nincs telefonszám"}</span>
                                </div>
                            )}

                            <Field label="Szerepkör">
                                <input
                                    style={styles.input}
                                    value={form.role}
                                    onChange={(event) =>
                                        updateForm("role", event.target.value)
                                    }
                                    placeholder="Például: Rendező, hajóvezető, bíró"
                                />
                            </Field>

                            <label style={styles.checkboxRow}>
                                <input
                                    type="checkbox"
                                    checked={form.isCaptain}
                                    onChange={(event) =>
                                        updateForm("isCaptain", event.target.checked)
                                    }
                                />
                                <span>Kapitányként kijelölve</span>
                            </label>

                            <div style={styles.dateGrid}>
                                <Field label="Kezdő dátum">
                                    <input
                                        type="date"
                                        style={styles.input}
                                        value={form.fromDate}
                                        onChange={(event) =>
                                            updateForm("fromDate", event.target.value)
                                        }
                                    />
                                </Field>

                                <Field label="Befejező dátum">
                                    <input
                                        type="date"
                                        style={styles.input}
                                        value={form.toDate}
                                        onChange={(event) =>
                                            updateForm("toDate", event.target.value)
                                        }
                                    />
                                </Field>
                            </div>

                            <Field label="Megjegyzés">
                                <textarea
                                    style={{
                                        ...styles.input,
                                        minHeight: 100,
                                        resize: "vertical",
                                    }}
                                    value={form.notes}
                                    onChange={(event) =>
                                        updateForm("notes", event.target.value)
                                    }
                                    placeholder="Például: VHF-kezelés, bójafeladat, műszak..."
                                />
                            </Field>

                            <div style={styles.formActions}>
                                {editingId && (
                                    <button
                                        type="button"
                                        style={styles.secondaryButton}
                                        onClick={resetForm}
                                        disabled={saving}
                                    >
                                        Mégse
                                    </button>
                                )}

                                <button
                                    type="submit"
                                    style={{
                                        ...styles.primaryButton,
                                        ...(saving ? styles.disabledButton : {}),
                                    }}
                                    disabled={saving}
                                >
                                    {saving
                                        ? "Mentés..."
                                        : editingId
                                            ? "Módosítás mentése"
                                            : "Rendező hozzáadása"}
                                </button>
                            </div>
                        </form>
                    </section>
                </div>
            </section>
        </div>
    );
}

function CrewRow({
    assignment,
    deleting,
    onEdit,
    onDelete,
}: {
    assignment: BoatCrewAssignment;
    deleting: boolean;
    onEdit: () => void;
    onDelete: () => void;
}) {
    return (
        <article style={styles.crewRow}>
            <div style={styles.avatar}>{getInitials(assignment.officialName)}</div>

            <div style={styles.crewMain}>
                <div style={styles.crewNameLine}>
                    <strong>{assignment.officialName}</strong>
                    {assignment.isCaptain && (
                        <span style={styles.captainBadge}>Kapitány</span>
                    )}
                </div>

                <div style={styles.crewRole}>{assignment.role}</div>

                {(assignment.fromDate || assignment.toDate) && (
                    <div style={styles.crewMeta}>
                        {formatDateRange(
                            assignment.fromDate,
                            assignment.toDate,
                        )}
                    </div>
                )}

                {(assignment.officialPhone || assignment.officialEmail) && (
                    <div style={styles.crewMeta}>
                        {[assignment.officialPhone, assignment.officialEmail]
                            .filter(Boolean)
                            .join(" · ")}
                    </div>
                )}

                {assignment.notes && (
                    <div style={styles.crewNotes}>{assignment.notes}</div>
                )}
            </div>

            <div style={styles.crewActions}>
                <button
                    type="button"
                    style={styles.linkButton}
                    onClick={onEdit}
                    disabled={deleting}
                >
                    Szerkesztés
                </button>

                <button
                    type="button"
                    style={{
                        ...styles.dangerButton,
                        ...(deleting ? styles.disabledButton : {}),
                    }}
                    onClick={onDelete}
                    disabled={deleting}
                >
                    {deleting ? "Törlés..." : "Törlés"}
                </button>
            </div>
        </article>
    );
}

function SummaryCard({
    label,
    value,
}: {
    label: string;
    value: string;
}) {
    return (
        <article style={styles.summaryCard}>
            <div style={styles.summaryLabel}>{label}</div>
            <div style={styles.summaryValue}>{value}</div>
        </article>
    );
}

function MessageBox({
    variant,
    message,
}: {
    variant: "error" | "success";
    message: string;
}) {
    const isError = variant === "error";

    return (
        <div
            style={{
                ...styles.message,
                borderColor: isError ? "#FECACA" : "#BBF7D0",
                background: isError ? "#FEF2F2" : "#F0FDF4",
                color: isError ? "#991B1B" : "#166534",
            }}
        >
            {message}
        </div>
    );
}

function Field({
    label,
    children,
}: {
    label: string;
    children: ReactNode;
}) {
    return (
        <label style={styles.field}>
            <span style={styles.label}>{label}</span>
            {children}
        </label>
    );
}

function getInitials(name: string): string {
    const parts = name.trim().split(/\s+/).filter(Boolean);

    if (parts.length === 0) {
        return "?";
    }

    return parts
        .slice(0, 2)
        .map((part) => part.charAt(0).toUpperCase())
        .join("");
}

function toDateInputValue(value?: string | null): string {
    return value ? value.slice(0, 10) : "";
}

function formatDateRange(
    fromDate?: string | null,
    toDate?: string | null,
): string {
    if (fromDate && toDate) {
        return `${formatDate(fromDate)} – ${formatDate(toDate)}`;
    }

    if (fromDate) {
        return `${formatDate(fromDate)}-tól`;
    }

    if (toDate) {
        return `${formatDate(toDate)}-ig`;
    }

    return "";
}

function formatDate(value: string): string {
    const parsedDate = new Date(value);

    if (Number.isNaN(parsedDate.getTime())) {
        return value;
    }

    return new Intl.DateTimeFormat("hu-HU", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
    }).format(parsedDate);
}

function getErrorMessage(
    error: unknown,
    fallbackMessage: string,
): string {
    return error instanceof Error ? error.message : fallbackMessage;
}

const styles: Record<string, CSSProperties> = {
    backdrop: {
        position: "fixed",
        inset: 0,
        zIndex: 1000,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 24,
        background: "rgba(15, 23, 42, 0.65)",
        backdropFilter: "blur(4px)",
    },
    dialog: {
        boxSizing: "border-box",
        width: "min(1180px, 100%)",
        maxHeight: "calc(100vh - 48px)",
        overflowY: "auto",
        border: "1px solid #CBD5E1",
        borderRadius: 22,
        padding: 24,
        background: "#F8FAFC",
        boxShadow: "0 24px 80px rgba(15, 23, 42, 0.35)",
    },
    header: {
        display: "flex",
        justifyContent: "space-between",
        gap: 20,
        marginBottom: 20,
    },
    eyebrow: {
        marginBottom: 7,
        color: "#2563EB",
        fontSize: 11,
        fontWeight: 800,
        letterSpacing: "0.13em",
    },
    title: {
        margin: 0,
        color: "#0F172A",
        fontSize: 30,
    },
    subtitle: {
        margin: "6px 0 0",
        color: "#64748B",
        fontSize: 14,
    },
    closeButton: {
        width: 42,
        height: 42,
        border: "1px solid #CBD5E1",
        borderRadius: 12,
        background: "#FFFFFF",
        color: "#334155",
        fontSize: 28,
        lineHeight: 1,
        cursor: "pointer",
    },
    message: {
        marginBottom: 16,
        padding: 13,
        border: "1px solid",
        borderRadius: 12,
        fontSize: 14,
    },
    summaryGrid: {
        display: "grid",
        gridTemplateColumns: "repeat(3, minmax(0, 1fr))",
        gap: 14,
        marginBottom: 18,
    },
    summaryCard: {
        padding: 16,
        border: "1px solid #E2E8F0",
        borderRadius: 14,
        background: "#FFFFFF",
    },
    summaryLabel: {
        color: "#64748B",
        fontSize: 12,
        fontWeight: 700,
    },
    summaryValue: {
        marginTop: 8,
        color: "#0F172A",
        fontSize: 20,
        fontWeight: 800,
    },
    contentGrid: {
        display: "grid",
        gridTemplateColumns: "minmax(0, 1.35fr) minmax(320px, 0.85fr)",
        gap: 18,
    },
    panel: {
        padding: 20,
        border: "1px solid #E2E8F0",
        borderRadius: 16,
        background: "#FFFFFF",
    },
    panelHeader: {
        display: "flex",
        alignItems: "flex-start",
        justifyContent: "space-between",
        gap: 14,
        marginBottom: 16,
    },
    panelTitle: {
        margin: 0,
        color: "#0F172A",
        fontSize: 19,
    },
    panelSubtitle: {
        margin: "5px 0 0",
        color: "#94A3B8",
        fontSize: 13,
    },
    refreshButton: {
        border: "1px solid #CBD5E1",
        borderRadius: 9,
        padding: "8px 11px",
        background: "#FFFFFF",
        color: "#334155",
        fontWeight: 700,
        cursor: "pointer",
    },
    crewList: {
        display: "flex",
        flexDirection: "column",
        gap: 10,
    },
    crewRow: {
        display: "grid",
        gridTemplateColumns: "44px minmax(0, 1fr) auto",
        alignItems: "start",
        gap: 12,
        padding: 14,
        border: "1px solid #E2E8F0",
        borderRadius: 12,
        background: "#F8FAFC",
    },
    avatar: {
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        width: 44,
        height: 44,
        borderRadius: 999,
        background: "#DBEAFE",
        color: "#1D4ED8",
        fontSize: 13,
        fontWeight: 900,
    },
    crewMain: {
        minWidth: 0,
    },
    crewNameLine: {
        display: "flex",
        alignItems: "center",
        flexWrap: "wrap",
        gap: 8,
        color: "#0F172A",
    },
    captainBadge: {
        padding: "4px 8px",
        borderRadius: 999,
        background: "#FEF3C7",
        color: "#92400E",
        fontSize: 10,
        fontWeight: 900,
        textTransform: "uppercase",
        letterSpacing: "0.04em",
    },
    crewRole: {
        marginTop: 4,
        color: "#475569",
        fontSize: 13,
        fontWeight: 700,
    },
    crewMeta: {
        marginTop: 4,
        color: "#64748B",
        fontSize: 12,
    },
    crewNotes: {
        marginTop: 7,
        color: "#475569",
        fontSize: 12,
        lineHeight: 1.5,
    },
    crewActions: {
        display: "flex",
        flexDirection: "column",
        gap: 7,
    },
    form: {
        display: "flex",
        flexDirection: "column",
        gap: 14,
        marginTop: 18,
    },
    field: {
        display: "flex",
        flexDirection: "column",
        gap: 7,
    },
    label: {
        color: "#334155",
        fontSize: 13,
        fontWeight: 700,
    },
    input: {
        boxSizing: "border-box",
        width: "100%",
        border: "1px solid #CBD5E1",
        borderRadius: 10,
        padding: "11px 12px",
        background: "#FFFFFF",
        color: "#0F172A",
        font: "inherit",
    },
    selectedOfficial: {
        display: "flex",
        flexDirection: "column",
        gap: 3,
        padding: 12,
        borderRadius: 10,
        background: "#EFF6FF",
        color: "#1E3A8A",
        fontSize: 12,
    },
    checkboxRow: {
        display: "flex",
        alignItems: "center",
        gap: 9,
        padding: 12,
        border: "1px solid #CBD5E1",
        borderRadius: 10,
        color: "#334155",
        fontSize: 13,
        fontWeight: 700,
        cursor: "pointer",
    },
    dateGrid: {
        display: "grid",
        gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
        gap: 12,
    },
    formActions: {
        display: "flex",
        justifyContent: "flex-end",
        gap: 9,
        marginTop: 4,
    },
    primaryButton: {
        border: 0,
        borderRadius: 10,
        padding: "11px 15px",
        background: "#2563EB",
        color: "#FFFFFF",
        fontWeight: 800,
        cursor: "pointer",
    },
    secondaryButton: {
        border: "1px solid #CBD5E1",
        borderRadius: 10,
        padding: "11px 15px",
        background: "#FFFFFF",
        color: "#334155",
        fontWeight: 700,
        cursor: "pointer",
    },
    linkButton: {
        border: "1px solid #CBD5E1",
        borderRadius: 8,
        padding: "8px 10px",
        background: "#FFFFFF",
        color: "#334155",
        cursor: "pointer",
    },
    dangerButton: {
        border: "1px solid #FCA5A5",
        borderRadius: 8,
        padding: "8px 10px",
        background: "#FEF2F2",
        color: "#B91C1C",
        cursor: "pointer",
    },
    disabledButton: {
        cursor: "not-allowed",
        opacity: 0.55,
    },
    empty: {
        padding: "48px 18px",
        color: "#64748B",
        textAlign: "center",
    },
};

export default BoatCrewDialog;