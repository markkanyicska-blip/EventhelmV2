import { useCallback, useEffect, useMemo, useState } from "react";
import type { CSSProperties } from "react";

import {
    createUser,
    deleteUser,
    getUsers,
    updateUser,
} from "./api/userApi";

import type { User } from "./types/User";

type UsersAppProps = {
    embedded?: boolean;
};

type UserForm = {
    name: string;
    email: string;
    phone: string;
    role: string;
    isActive: boolean;
};

const emptyForm: UserForm = {
    name: "",
    email: "",
    phone: "",
    role: "User",
    isActive: true,
};

function UsersApp({ embedded = false }: UsersAppProps) {
    const [users, setUsers] = useState<User[]>([]);
    const [form, setForm] = useState<UserForm>(emptyForm);
    const [search, setSearch] = useState("");
    const [editingId, setEditingId] = useState<string | null>(null);

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [deletingId, setDeletingId] = useState<string | null>(null);

    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");

    const loadUsers = useCallback(async () => {
        setLoading(true);
        setError("");

        try {
            const data = await getUsers();
            setUsers(data);
        } catch (loadError) {
            console.error(loadError);
            setError(
                "A felhasználók nem tölthetők be. Ellenőrizd, hogy fut-e az EventHelm.Api.",
            );
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        void loadUsers();
    }, [loadUsers]);

    const filteredUsers = useMemo(() => {
        const term = search.trim().toLowerCase();

        if (!term) {
            return users;
        }

        return users.filter((user) =>
            [
                user.name,
                user.email,
                user.phone ?? "",
                user.role,
            ]
                .join(" ")
                .toLowerCase()
                .includes(term),
        );
    }, [search, users]);

    const statistics = useMemo(
        () => ({
            total: users.length,
            active: users.filter((user) => user.isActive).length,
            inactive: users.filter((user) => !user.isActive).length,
            administrators: users.filter(
                (user) => user.role === "Admin",
            ).length,
        }),
        [users],
    );

    function updateForm<K extends keyof UserForm>(
        field: K,
        value: UserForm[K],
    ) {
        setForm((current) => ({
            ...current,
            [field]: value,
        }));

        setError("");
        setSuccess("");
    }

    function startEdit(user: User) {
        setEditingId(user.id);

        setForm({
            name: user.name,
            email: user.email,
            phone: user.phone ?? "",
            role: user.role,
            isActive: user.isActive,
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
            return "A név megadása kötelező.";
        }

        if (!form.email.trim()) {
            return "Az e-mail-cím megadása kötelező.";
        }

        if (!form.email.includes("@")) {
            return "Adj meg érvényes e-mail-címet.";
        }

        return null;
    }

    async function saveUser() {
        const validationError = validateForm();

        if (validationError) {
            setError(validationError);
            return;
        }

        setSaving(true);
        setError("");
        setSuccess("");

        try {
            if (editingId) {
                const existingUser = users.find(
                    (user) => user.id === editingId,
                );

                if (!existingUser) {
                    throw new Error(
                        "A szerkesztett felhasználó nem található.",
                    );
                }

                await updateUser({
                    ...existingUser,
                    name: form.name.trim(),
                    email: form.email.trim(),
                    phone: form.phone.trim() || undefined,
                    role: form.role,
                    isActive: form.isActive,
                });

                setSuccess(
                    "A felhasználó módosítása sikerült.",
                );
            } else {
                await createUser({
                    name: form.name.trim(),
                    email: form.email.trim(),
                    phone: form.phone.trim() || undefined,
                    role: form.role,
                    isActive: form.isActive,
                });

                setSuccess(
                    "A felhasználó létrehozása sikerült.",
                );
            }

            resetForm();
            await loadUsers();
        } catch (saveError) {
            console.error(saveError);

            setError(
                saveError instanceof Error
                    ? saveError.message
                    : "A felhasználó mentése sikertelen.",
            );
        } finally {
            setSaving(false);
        }
    }

    async function removeUser(user: User) {
        const confirmed = window.confirm(
            `Biztosan törlöd ezt a felhasználót?\n\n${user.name} – ${user.email}`,
        );

        if (!confirmed) {
            return;
        }

        setDeletingId(user.id);
        setError("");
        setSuccess("");

        try {
            await deleteUser(user.id);

            if (editingId === user.id) {
                resetForm();
            }

            setSuccess(
                "A felhasználó törlése sikerült.",
            );

            await loadUsers();
        } catch (deleteError) {
            console.error(deleteError);

            setError(
                "A felhasználó törlése sikertelen.",
            );
        } finally {
            setDeletingId(null);
        }
    }

    return (
        <section
            style={
                embedded
                    ? styles.embeddedPage
                    : styles.page
            }
        >
            <header style={styles.header}>
                <div>
                    <div style={styles.eyebrow}>
                        EVENTHELM ADMIN
                    </div>

                    <h1 style={styles.title}>
                        Felhasználók
                    </h1>

                    <p style={styles.subtitle}>
                        Felhasználói adatok, szerepkörök és
                        hozzáférési állapotok kezelése.
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
                    onClick={() => void loadUsers()}
                    disabled={loading}
                >
                    {loading
                        ? "Frissítés..."
                        : "Lista frissítése"}
                </button>
            </header>

            {error && (
                <div style={styles.errorMessage}>
                    {error}
                </div>
            )}

            {success && (
                <div style={styles.successMessage}>
                    {success}
                </div>
            )}

            <section style={styles.statistics}>
                <Metric
                    label="Összes felhasználó"
                    value={statistics.total}
                />

                <Metric
                    label="Aktív"
                    value={statistics.active}
                />

                <Metric
                    label="Inaktív"
                    value={statistics.inactive}
                />

                <Metric
                    label="Adminisztrátor"
                    value={statistics.administrators}
                />
            </section>

            <section style={styles.grid}>
                <article style={styles.panel}>
                    <h2 style={styles.panelTitle}>
                        {editingId
                            ? "Felhasználó szerkesztése"
                            : "Új felhasználó"}
                    </h2>

                    <p style={styles.panelSubtitle}>
                        Add meg a felhasználó alapadatait.
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
                                placeholder="Például: Kovács Péter"
                            />
                        </Field>

                        <Field label="E-mail">
                            <input
                                style={styles.input}
                                type="email"
                                value={form.email}
                                onChange={(event) =>
                                    updateForm(
                                        "email",
                                        event.target.value,
                                    )
                                }
                                placeholder="peter@example.com"
                            />
                        </Field>

                        <Field label="Telefon">
                            <input
                                style={styles.input}
                                value={form.phone}
                                onChange={(event) =>
                                    updateForm(
                                        "phone",
                                        event.target.value,
                                    )
                                }
                                placeholder="+36 30 123 4567"
                            />
                        </Field>

                        <Field label="Szerepkör">
                            <select
                                style={styles.input}
                                value={form.role}
                                onChange={(event) =>
                                    updateForm(
                                        "role",
                                        event.target.value,
                                    )
                                }
                            >
                                <option value="User">
                                    Felhasználó
                                </option>

                                <option value="Organizer">
                                    Szervező
                                </option>

                                <option value="RaceOfficer">
                                    Versenyvezető
                                </option>

                                <option value="Admin">
                                    Adminisztrátor
                                </option>
                            </select>
                        </Field>

                        <Field label="Állapot">
                            <select
                                style={styles.input}
                                value={
                                    form.isActive
                                        ? "active"
                                        : "inactive"
                                }
                                onChange={(event) =>
                                    updateForm(
                                        "isActive",
                                        event.target.value ===
                                        "active",
                                    )
                                }
                            >
                                <option value="active">
                                    Aktív
                                </option>

                                <option value="inactive">
                                    Inaktív
                                </option>
                            </select>
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
                                void saveUser()
                            }
                            disabled={saving}
                        >
                            {saving
                                ? "Mentés..."
                                : editingId
                                    ? "Módosítás mentése"
                                    : "Felhasználó hozzáadása"}
                        </button>
                    </div>
                </article>

                <article style={styles.panel}>
                    <div style={styles.panelHeader}>
                        <div>
                            <h2 style={styles.panelTitle}>
                                Felhasználólista
                            </h2>

                            <p style={styles.panelSubtitle}>
                                {filteredUsers.length} találat
                            </p>
                        </div>
                    </div>

                    <input
                        style={styles.search}
                        value={search}
                        onChange={(event) =>
                            setSearch(event.target.value)
                        }
                        placeholder="Keresés név, e-mail, telefon vagy szerepkör alapján..."
                    />

                    {loading ? (
                        <div style={styles.emptyState}>
                            Felhasználók betöltése...
                        </div>
                    ) : filteredUsers.length === 0 ? (
                        <div style={styles.emptyState}>
                            Nincs megjeleníthető
                            felhasználó.
                        </div>
                    ) : (
                        <div style={styles.list}>
                            {filteredUsers.map((user) => (
                                <UserRow
                                    key={user.id}
                                    user={user}
                                    deleting={
                                        deletingId === user.id
                                    }
                                    onEdit={() =>
                                        startEdit(user)
                                    }
                                    onDelete={() =>
                                        void removeUser(user)
                                    }
                                />
                            ))}
                        </div>
                    )}
                </article>
            </section>
        </section>
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

function Field({
    label,
    children,
}: {
    label: string;
    children: React.ReactNode;
}) {
    return (
        <label style={styles.field}>
            <span style={styles.label}>{label}</span>
            {children}
        </label>
    );
}

function UserRow({
    user,
    deleting,
    onEdit,
    onDelete,
}: {
    user: User;
    deleting: boolean;
    onEdit: () => void;
    onDelete: () => void;
}) {
    return (
        <div style={styles.row}>
            <div style={styles.avatar}>
                {getInitials(user.name)}
            </div>

            <div style={styles.userInformation}>
                <div style={styles.userName}>
                    {user.name}
                </div>

                <div style={styles.userMeta}>
                    {user.email}
                </div>

                <div style={styles.userMeta}>
                    {user.phone || "Nincs telefonszám"}
                </div>
            </div>

            <div style={styles.roleBadge}>
                {roleLabel(user.role)}
            </div>

            <StatusBadge active={user.isActive} />

            <div style={styles.rowActions}>
                <button
                    type="button"
                    style={styles.editButton}
                    onClick={onEdit}
                    disabled={deleting}
                >
                    Szerkesztés
                </button>

                <button
                    type="button"
                    style={{
                        ...styles.deleteButton,
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
    active,
}: {
    active: boolean;
}) {
    return (
        <span
            style={{
                ...styles.statusBadge,
                background: active
                    ? "#DCFCE7"
                    : "#FEE2E2",
                color: active
                    ? "#166534"
                    : "#991B1B",
            }}
        >
            {active ? "Aktív" : "Inaktív"}
        </span>
    );
}

function roleLabel(role: string): string {
    const labels: Record<string, string> = {
        User: "Felhasználó",
        Organizer: "Szervező",
        RaceOfficer: "Versenyvezető",
        Admin: "Adminisztrátor",
    };

    return labels[role] ?? role;
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

const styles: Record<string, CSSProperties> = {
    page: {
        minHeight: "100vh",
        boxSizing: "border-box",
        padding: 32,
        background: "#F8FAFC",
    },

    embeddedPage: {
        minHeight: "100vh",
        boxSizing: "border-box",
        padding: 32,
        background: "#F8FAFC",
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
        fontWeight: 800,
    },

    subtitle: {
        margin: "8px 0 0",
        color: "#64748B",
        fontSize: 15,
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

    errorMessage: {
        marginBottom: 20,
        padding: 14,
        border: "1px solid #FECACA",
        borderRadius: 12,
        background: "#FEF2F2",
        color: "#991B1B",
    },

    successMessage: {
        marginBottom: 20,
        padding: 14,
        border: "1px solid #BBF7D0",
        borderRadius: 12,
        background: "#F0FDF4",
        color: "#166534",
    },

    statistics: {
        display: "grid",
        gridTemplateColumns:
            "repeat(auto-fit, minmax(170px, 1fr))",
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
        boxShadow:
            "0 8px 24px rgba(15, 23, 42, 0.05)",
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

    search: {
        boxSizing: "border-box",
        width: "100%",
        marginBottom: 18,
        padding: "12px 14px",
        border: "1px solid #CBD5E1",
        borderRadius: 10,
        font: "inherit",
    },

    list: {
        display: "flex",
        flexDirection: "column",
        gap: 10,
    },

    row: {
        display: "grid",
        gridTemplateColumns:
            "48px minmax(0, 1fr) auto auto auto",
        alignItems: "center",
        gap: 12,
        padding: 14,
        border: "1px solid #E2E8F0",
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
        fontWeight: 800,
    },

    userInformation: {
        minWidth: 0,
    },

    userName: {
        overflow: "hidden",
        color: "#0F172A",
        fontSize: 15,
        fontWeight: 800,
        textOverflow: "ellipsis",
        whiteSpace: "nowrap",
    },

    userMeta: {
        marginTop: 3,
        overflow: "hidden",
        color: "#64748B",
        fontSize: 12,
        textOverflow: "ellipsis",
        whiteSpace: "nowrap",
    },

    roleBadge: {
        padding: "7px 10px",
        borderRadius: 999,
        background: "#E0E7FF",
        color: "#3730A3",
        fontSize: 11,
        fontWeight: 800,
        whiteSpace: "nowrap",
    },

    statusBadge: {
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

    editButton: {
        border: "1px solid #CBD5E1",
        borderRadius: 8,
        padding: "8px 10px",
        background: "#FFFFFF",
        color: "#334155",
        cursor: "pointer",
    },

    deleteButton: {
        border: "1px solid #FCA5A5",
        borderRadius: 8,
        padding: "8px 10px",
        background: "#FEF2F2",
        color: "#B91C1C",
        cursor: "pointer",
    },

    emptyState: {
        padding: "50px 20px",
        color: "#64748B",
        textAlign: "center",
    },
};

export default UsersApp;