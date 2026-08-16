import {
    useCallback,
    useEffect,
    useMemo,
    useState,
} from "react";

import type {
    CSSProperties,
    ReactNode,
} from "react";

const API_BASE_URL = "";

type Boat = {
    id: string;
    name: string;
    registrationNumber: string;
    callSign: string;
    status: string;
    notes?: string | null;
    createdAt?: string;
};

type BoatForm = {
    name: string;
    registrationNumber: string;
    callSign: string;
    status: string;
    notes: string;
};

const emptyForm: BoatForm = {
    name: "",
    registrationNumber: "",
    callSign: "",
    status: "Active",
    notes: "",
};

function BoatsApp() {
    const [boats, setBoats] = useState<Boat[]>([]);
    const [form, setForm] = useState<BoatForm>(emptyForm);
    const [editingId, setEditingId] =
        useState<string | null>(null);

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [deletingId, setDeletingId] =
        useState<string | null>(null);

    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");

    const loadBoats = useCallback(async () => {
        setLoading(true);
        setError("");

        try {
            const response = await fetch(
                `${API_BASE_URL}/api/boats`,
            );

            if (!response.ok) {
                throw new Error(
                    await getResponseError(
                        response,
                        "A motorcsónakok betöltése sikertelen.",
                    ),
                );
            }

            const data: unknown = await response.json();

            if (!Array.isArray(data)) {
                throw new Error(
                    "Az API nem megfelelő listaformátumot adott vissza.",
                );
            }

            setBoats(data as Boat[]);
        } catch (loadError) {
            console.error(loadError);

            setError(
                loadError instanceof Error
                    ? loadError.message
                    : "A motorcsónakok nem tölthetők be.",
            );
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        void loadBoats();
    }, [loadBoats]);

    const statistics = useMemo(() => {
        return {
            total: boats.length,
            active: boats.filter(
                (boat) => boat.status === "Active",
            ).length,
            service: boats.filter(
                (boat) => boat.status === "Service",
            ).length,
            unavailable: boats.filter(
                (boat) => boat.status === "Unavailable",
            ).length,
        };
    }, [boats]);

    function updateForm(
        field: keyof BoatForm,
        value: string,
    ) {
        setForm((currentForm) => ({
            ...currentForm,
            [field]: value,
        }));

        setError("");
        setSuccess("");
    }

    function startEdit(boat: Boat) {
        setEditingId(boat.id);

        setForm({
            name: boat.name ?? "",
            registrationNumber:
                boat.registrationNumber ?? "",
            callSign: boat.callSign ?? "",
            status: boat.status || "Active",
            notes: boat.notes ?? "",
        });

        setError("");
        setSuccess("");

        window.scrollTo({
            top: 0,
            behavior: "smooth",
        });
    }

    function resetForm() {
        setEditingId(null);
        setForm(emptyForm);
    }

    function cancelEdit() {
        resetForm();
        setError("");
        setSuccess("");
    }

    function validateForm(): string | null {
        if (!form.name.trim()) {
            return "A motorcsónak neve kötelező.";
        }

        if (!form.callSign.trim()) {
            return "A hívójel megadása kötelező.";
        }

        if (!form.registrationNumber.trim()) {
            return "A regisztrációs szám megadása kötelező.";
        }

        return null;
    }

    async function saveBoat() {
        const validationError = validateForm();

        if (validationError) {
            setError(validationError);
            return;
        }

        setSaving(true);
        setError("");
        setSuccess("");

        const payload = {
            name: form.name.trim(),
            registrationNumber:
                form.registrationNumber.trim(),
            callSign: form.callSign.trim(),
            status: form.status,
            notes: form.notes.trim() || null,
        };

        try {
            const url = editingId
                ? `${API_BASE_URL}/api/boats/${editingId}`
                : `${API_BASE_URL}/api/boats`;

            const response = await fetch(url, {
                method: editingId ? "PUT" : "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify(payload),
            });

            if (!response.ok) {
                throw new Error(
                    await getResponseError(
                        response,
                        editingId
                            ? "A motorcsónak módosítása sikertelen."
                            : "A motorcsónak létrehozása sikertelen.",
                    ),
                );
            }

            setSuccess(
                editingId
                    ? "A motorcsónak módosítása sikerült."
                    : "A motorcsónak létrehozása sikerült.",
            );

            resetForm();
            await loadBoats();
        } catch (saveError) {
            console.error(saveError);

            setError(
                saveError instanceof Error
                    ? saveError.message
                    : "A motorcsónak mentése sikertelen.",
            );
        } finally {
            setSaving(false);
        }
    }

    async function deleteBoat(boat: Boat) {
        const confirmed = window.confirm(
            `Biztosan törlöd a következő motorcsónakot?\n\n${boat.callSign} – ${boat.name}`,
        );

        if (!confirmed) {
            return;
        }

        setDeletingId(boat.id);
        setError("");
        setSuccess("");

        try {
            const response = await fetch(
                `${API_BASE_URL}/api/boats/${boat.id}`,
                {
                    method: "DELETE",
                },
            );

            if (!response.ok) {
                throw new Error(
                    await getResponseError(
                        response,
                        "A motorcsónak törlése sikertelen.",
                    ),
                );
            }

            if (editingId === boat.id) {
                resetForm();
            }

            setSuccess(
                "A motorcsónak törlése sikerült.",
            );

            await loadBoats();
        } catch (deleteError) {
            console.error(deleteError);

            setError(
                deleteError instanceof Error
                    ? deleteError.message
                    : "A motorcsónak törlése sikertelen.",
            );
        } finally {
            setDeletingId(null);
        }
    }

    return (
        <div style={styles.page}>
            <header style={styles.header}>
                <div>
                    <div style={styles.eyebrow}>
                        EVENTHELM OPERATIONS
                    </div>

                    <h1 style={styles.title}>
                        Motorcsónakok
                    </h1>

                    <p style={styles.subtitle}>
                        A versenyrendezésben részt vevő
                        motoros egységek kezelése.
                    </p>
                </div>

                <button
                    type="button"
                    style={{
                        ...styles.refreshButton,
                        ...(loading
                            ? styles.disabledButton
                            : {}),
                    }}
                    onClick={() => void loadBoats()}
                    disabled={loading}
                >
                    {loading
                        ? "Frissítés..."
                        : "Lista frissítése"}
                </button>
            </header>

            {error && (
                <MessageBox
                    variant="error"
                    message={error}
                />
            )}

            {success && (
                <MessageBox
                    variant="success"
                    message={success}
                />
            )}

            <section style={styles.statistics}>
                <Metric
                    label="Összes hajó"
                    value={statistics.total}
                />

                <Metric
                    label="Aktív"
                    value={statistics.active}
                />

                <Metric
                    label="Szerviz"
                    value={statistics.service}
                />

                <Metric
                    label="Nem elérhető"
                    value={statistics.unavailable}
                />
            </section>

            <section style={styles.grid}>
                <article style={styles.panel}>
                    <h2 style={styles.panelTitle}>
                        {editingId
                            ? "Motorcsónak szerkesztése"
                            : "Új motorcsónak"}
                    </h2>

                    <p style={styles.panelSubtitle}>
                        Add meg a hajó alapadatait.
                    </p>

                    <div style={styles.formGrid}>
                        <Field label="Név">
                            <input
                                style={styles.input}
                                value={form.name}
                                onChange={(event) =>
                                    updateForm(
                                        "name",
                                        event.target.value,
                                    )
                                }
                                placeholder="Például: Balaton RIB 01"
                            />
                        </Field>

                        <Field label="Hívójel">
                            <input
                                style={styles.input}
                                value={form.callSign}
                                onChange={(event) =>
                                    updateForm(
                                        "callSign",
                                        event.target.value,
                                    )
                                }
                                placeholder="Például: Safety 2"
                            />
                        </Field>

                        <Field label="Regisztrációs szám">
                            <input
                                style={styles.input}
                                value={
                                    form.registrationNumber
                                }
                                onChange={(event) =>
                                    updateForm(
                                        "registrationNumber",
                                        event.target.value,
                                    )
                                }
                                placeholder="Például: H-12345"
                            />
                        </Field>

                        <Field label="Állapot">
                            <select
                                style={styles.input}
                                value={form.status}
                                onChange={(event) =>
                                    updateForm(
                                        "status",
                                        event.target.value,
                                    )
                                }
                            >
                                <option value="Active">
                                    Aktív
                                </option>

                                <option value="Service">
                                    Szerviz
                                </option>

                                <option value="Unavailable">
                                    Nem elérhető
                                </option>
                            </select>
                        </Field>

                        <Field
                            label="Megjegyzés"
                            fullWidth
                        >
                            <textarea
                                style={{
                                    ...styles.input,
                                    minHeight: 110,
                                    resize: "vertical",
                                }}
                                value={form.notes}
                                onChange={(event) =>
                                    updateForm(
                                        "notes",
                                        event.target.value,
                                    )
                                }
                                placeholder="Felszerelés, műszaki állapot vagy egyéb megjegyzés..."
                            />
                        </Field>
                    </div>

                    <div style={styles.actions}>
                        {editingId && (
                            <button
                                type="button"
                                style={
                                    styles.secondaryButton
                                }
                                onClick={cancelEdit}
                                disabled={saving}
                            >
                                Mégse
                            </button>
                        )}

                        <button
                            type="button"
                            style={{
                                ...styles.primaryButton,
                                ...(saving
                                    ? styles.disabledButton
                                    : {}),
                            }}
                            onClick={() =>
                                void saveBoat()
                            }
                            disabled={saving}
                        >
                            {saving
                                ? "Mentés..."
                                : editingId
                                    ? "Módosítás mentése"
                                    : "Motorcsónak hozzáadása"}
                        </button>
                    </div>
                </article>

                <article style={styles.panel}>
                    <div style={styles.panelHeader}>
                        <div>
                            <h2 style={styles.panelTitle}>
                                Motorcsónaklista
                            </h2>

                            <p style={styles.panelSubtitle}>
                                {boats.length} egység
                            </p>
                        </div>
                    </div>

                    {loading ? (
                        <div style={styles.empty}>
                            Motorcsónakok betöltése...
                        </div>
                    ) : boats.length === 0 ? (
                        <div style={styles.empty}>
                            Még nincs rögzített
                            motorcsónak.
                        </div>
                    ) : (
                        <div style={styles.list}>
                            {boats.map((boat) => (
                                <BoatRow
                                    key={boat.id}
                                    boat={boat}
                                    deleting={
                                        deletingId === boat.id
                                    }
                                    onEdit={() =>
                                        startEdit(boat)
                                    }
                                    onDelete={() =>
                                        void deleteBoat(boat)
                                    }
                                />
                            ))}
                        </div>
                    )}
                </article>
            </section>
        </div>
    );
}

function Metric({
    label,
    value,
}: {
    label: string;
    value: number;
}) {
    return (
        <article style={styles.metric}>
            <div style={styles.metricLabel}>
                {label}
            </div>

            <div style={styles.metricValue}>
                {value}
            </div>
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
                borderColor: isError
                    ? "#FECACA"
                    : "#BBF7D0",
                background: isError
                    ? "#FEF2F2"
                    : "#F0FDF4",
                color: isError
                    ? "#991B1B"
                    : "#166534",
            }}
        >
            {message}
        </div>
    );
}

function Field({
    label,
    children,
    fullWidth = false,
}: {
    label: string;
    children: ReactNode;
    fullWidth?: boolean;
}) {
    return (
        <label
            style={{
                ...styles.field,
                ...(fullWidth
                    ? styles.fullWidth
                    : {}),
            }}
        >
            <span style={styles.label}>
                {label}
            </span>

            {children}
        </label>
    );
}

function BoatRow({
    boat,
    deleting,
    onEdit,
    onDelete,
}: {
    boat: Boat;
    deleting: boolean;
    onEdit: () => void;
    onDelete: () => void;
}) {
    return (
        <div style={styles.row}>
            <div style={styles.rowMain}>
                <div style={styles.callSign}>
                    {boat.callSign ||
                        "Nincs hívójel"}
                </div>

                <div style={styles.rowMeta}>
                    {boat.name ||
                        "Névtelen hajó"}
                </div>

                <div style={styles.registrationNumber}>
                    Regisztrációs szám:{" "}
                    {boat.registrationNumber ||
                        "Nincs megadva"}
                </div>

                {boat.notes && (
                    <div style={styles.notes}>
                        {boat.notes}
                    </div>
                )}
            </div>

            <StatusBadge
                status={boat.status}
            />

            <div style={styles.rowActions}>
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
                        ...(deleting
                            ? styles.disabledButton
                            : {}),
                    }}
                    onClick={onDelete}
                    disabled={deleting}
                >
                    {deleting
                        ? "Törlés..."
                        : "Törlés"}
                </button>
            </div>
        </div>
    );
}

function StatusBadge({
    status,
}: {
    status: string;
}) {
    const configuration =
        status === "Active"
            ? {
                label: "Aktív",
                background: "#DCFCE7",
                color: "#166534",
            }
            : status === "Service"
                ? {
                    label: "Szerviz",
                    background: "#FEF3C7",
                    color: "#92400E",
                }
                : {
                    label: "Nem elérhető",
                    background: "#FEE2E2",
                    color: "#991B1B",
                };

    return (
        <span
            style={{
                ...styles.badge,
                background:
                    configuration.background,
                color: configuration.color,
            }}
        >
            {configuration.label}
        </span>
    );
}

async function getResponseError(
    response: Response,
    fallbackMessage: string,
): Promise<string> {
    const responseText =
        await response.text();

    if (responseText.trim()) {
        return responseText;
    }

    return `${fallbackMessage} HTTP ${response.status}`;
}

const styles: Record<string, CSSProperties> = {
    page: {
        minHeight: "100vh",
        boxSizing: "border-box",
        padding: 32,
        background: "#F1F5F9",
    },

    header: {
        display: "flex",
        alignItems: "flex-start",
        justifyContent: "space-between",
        gap: 20,
        marginBottom: 24,
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
    },

    subtitle: {
        margin: "8px 0 0",
        color: "#64748B",
    },

    refreshButton: {
        border: 0,
        borderRadius: 10,
        padding: "11px 16px",
        background: "#0F172A",
        color: "#FFFFFF",
        fontWeight: 700,
        cursor: "pointer",
    },

    message: {
        marginBottom: 20,
        padding: 14,
        border: "1px solid",
        borderRadius: 12,
        fontSize: 14,
    },

    statistics: {
        display: "grid",
        gridTemplateColumns:
            "repeat(auto-fit, minmax(180px, 1fr))",
        gap: 16,
        marginBottom: 20,
    },

    metric: {
        padding: 18,
        border: "1px solid #E2E8F0",
        borderRadius: 16,
        background: "#FFFFFF",
    },

    metricLabel: {
        color: "#64748B",
        fontSize: 13,
        fontWeight: 700,
    },

    metricValue: {
        marginTop: 10,
        color: "#0F172A",
        fontSize: 32,
        fontWeight: 800,
    },

    grid: {
        display: "grid",
        gridTemplateColumns:
            "minmax(320px, 0.9fr) minmax(0, 1.5fr)",
        gap: 20,
    },

    panel: {
        padding: 24,
        border: "1px solid #E2E8F0",
        borderRadius: 18,
        background: "#FFFFFF",
    },

    panelHeader: {
        display: "flex",
        justifyContent: "space-between",
        marginBottom: 18,
    },

    panelTitle: {
        margin: 0,
        color: "#0F172A",
        fontSize: 20,
    },

    panelSubtitle: {
        margin: "6px 0 0",
        color: "#94A3B8",
        fontSize: 13,
    },

    formGrid: {
        display: "grid",
        gridTemplateColumns:
            "repeat(2, minmax(0, 1fr))",
        gap: 14,
        marginTop: 20,
    },

    field: {
        display: "flex",
        flexDirection: "column",
        gap: 7,
    },

    fullWidth: {
        gridColumn: "1 / -1",
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

    actions: {
        display: "flex",
        justifyContent: "flex-end",
        gap: 10,
        marginTop: 20,
    },

    primaryButton: {
        border: 0,
        borderRadius: 10,
        padding: "11px 16px",
        background: "#2563EB",
        color: "#FFFFFF",
        fontWeight: 700,
        cursor: "pointer",
    },

    secondaryButton: {
        border: "1px solid #CBD5E1",
        borderRadius: 10,
        padding: "11px 16px",
        background: "#FFFFFF",
        color: "#334155",
        fontWeight: 700,
        cursor: "pointer",
    },

    disabledButton: {
        cursor: "not-allowed",
        opacity: 0.55,
    },

    list: {
        display: "flex",
        flexDirection: "column",
        gap: 10,
    },

    row: {
        display: "grid",
        gridTemplateColumns:
            "minmax(0, 1fr) auto auto",
        alignItems: "center",
        gap: 14,
        padding: 14,
        border: "1px solid #E2E8F0",
        borderRadius: 12,
    },

    rowMain: {
        minWidth: 0,
    },

    callSign: {
        color: "#0F172A",
        fontSize: 16,
        fontWeight: 800,
    },

    rowMeta: {
        marginTop: 4,
        color: "#475569",
        fontSize: 13,
        fontWeight: 600,
    },

    registrationNumber: {
        marginTop: 4,
        color: "#64748B",
        fontSize: 12,
    },

    notes: {
        marginTop: 7,
        color: "#475569",
        fontSize: 12,
    },

    badge: {
        padding: "7px 10px",
        borderRadius: 999,
        fontSize: 11,
        fontWeight: 800,
        whiteSpace: "nowrap",
    },

    rowActions: {
        display: "flex",
        gap: 8,
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

    empty: {
        padding: "50px 20px",
        color: "#64748B",
        textAlign: "center",
    },
};

export default BoatsApp;