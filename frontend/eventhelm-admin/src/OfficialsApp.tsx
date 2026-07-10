import { useEffect, useMemo, useState } from "react";
import type { Official } from "./types/Official";
import {
    createOfficial,
    deleteOfficial,
    getOfficials,
    updateOfficial,
} from "./api/officialApi";

function OfficialsApp() {
    const [officials, setOfficials] = useState<Official[]>([]);
    const [search, setSearch] = useState("");
    const [name, setName] = useState("");
    const [phone, setPhone] = useState("");
    const [email, setEmail] = useState("");
    const [role, setRole] = useState("Rendező");
    const [status, setStatus] = useState("Available");

    const loadOfficials = async () => {
        const data = await getOfficials();
        setOfficials(data);
    };

    useEffect(() => {
        loadOfficials();
    }, []);

    const filteredOfficials = useMemo(() => {
        const term = search.toLowerCase();

        return officials.filter((official) =>
            `${official.name} ${official.email ?? ""} ${official.phone ?? ""} ${official.role}`
                .toLowerCase()
                .includes(term)
        );
    }, [officials, search]);

    const addOfficial = async () => {
        if (!name.trim()) return;

        await createOfficial({
            name,
            phone: phone || undefined,
            email: email || undefined,
            role,
            status,
        });

        setName("");
        setPhone("");
        setEmail("");
        setRole("Rendező");
        setStatus("Available");

        await loadOfficials();
    };

    const editOfficial = async (official: Official) => {
        const newName = prompt("Rendező neve:", official.name);
        if (!newName?.trim()) return;

        const newRole = prompt("Szerepkör:", official.role);
        if (!newRole?.trim()) return;

        await updateOfficial({
            ...official,
            name: newName,
            role: newRole,
        });

        await loadOfficials();
    };

    const removeOfficial = async (official: Official) => {
        if (!confirm(`Biztosan törlöd: ${official.name}?`)) return;

        await deleteOfficial(official.id);
        await loadOfficials();
    };

    return (
        <div style={styles.page}>
            <aside style={styles.sidebar}>
                <div style={styles.logo}>🚤 EventHelm</div>

                <div style={styles.navItem}>Dashboard</div>
                <div style={styles.navItem}>Rendezvények</div>
                <div style={styles.navItemActive}>Rendezők</div>
                <div style={styles.navItem}>Beosztások</div>
                <div style={styles.navItem}>Üzenetek</div>
                <div style={styles.navItem}>Térkép</div>
            </aside>

            <main style={styles.main}>
                <header style={styles.header}>
                    <div>
                        <h1 style={styles.title}>Rendezők</h1>
                        <p style={styles.subtitle}>
                            Versenyrendezők, szerepkörök és elérhetőségek
                        </p>
                    </div>
                </header>

                <section style={styles.stats}>
                    <div style={styles.card}>
                        <span style={styles.cardLabel}>Összes rendező</span>
                        <strong style={styles.cardValue}>{officials.length}</strong>
                    </div>

                    <div style={styles.card}>
                        <span style={styles.cardLabel}>Elérhető</span>
                        <strong style={styles.cardValue}>
                            {officials.filter((x) => x.status === "Available").length}
                        </strong>
                    </div>

                    <div style={styles.card}>
                        <span style={styles.cardLabel}>Szolgálatban</span>
                        <strong style={styles.cardValue}>
                            {officials.filter((x) => x.status === "OnDuty").length}
                        </strong>
                    </div>
                </section>

                <section style={styles.panel}>
                    <div style={styles.toolbar}>
                        <input
                            style={styles.search}
                            placeholder="Keresés név, telefon, e-mail vagy szerepkör alapján..."
                            value={search}
                            onChange={(event) => setSearch(event.target.value)}
                        />
                    </div>

                    <div style={styles.formGrid}>
                        <input
                            style={styles.input}
                            placeholder="Név"
                            value={name}
                            onChange={(event) => setName(event.target.value)}
                        />

                        <input
                            style={styles.input}
                            placeholder="Telefon"
                            value={phone}
                            onChange={(event) => setPhone(event.target.value)}
                        />

                        <input
                            style={styles.input}
                            placeholder="E-mail"
                            value={email}
                            onChange={(event) => setEmail(event.target.value)}
                        />

                        <select
                            style={styles.input}
                            value={role}
                            onChange={(event) => setRole(event.target.value)}
                        >
                            <option value="Rendező">Rendező</option>
                            <option value="Versenyvezető">Versenyvezető</option>
                            <option value="Pályafelelős">Pályafelelős</option>
                            <option value="Rendezőhajó">Rendezőhajó</option>
                            <option value="Média">Média</option>
                        </select>

                        <select
                            style={styles.input}
                            value={status}
                            onChange={(event) => setStatus(event.target.value)}
                        >
                            <option value="Available">Elérhető</option>
                            <option value="OnDuty">Szolgálatban</option>
                            <option value="Unavailable">Nem elérhető</option>
                        </select>

                        <button style={styles.primaryButton} onClick={addOfficial}>
                            + Rendező hozzáadása
                        </button>
                    </div>

                    <table style={styles.table}>
                        <thead>
                            <tr>
                                <th style={styles.th}>Név</th>
                                <th style={styles.th}>Szerepkör</th>
                                <th style={styles.th}>Telefon</th>
                                <th style={styles.th}>E-mail</th>
                                <th style={styles.th}>Státusz</th>
                                <th style={{ ...styles.th, textAlign: "right" }}>Műveletek</th>
                            </tr>
                        </thead>

                        <tbody>
                            {filteredOfficials.map((official) => (
                                <tr key={official.id}>
                                    <td style={styles.td}>
                                        <strong>{official.name}</strong>
                                    </td>

                                    <td style={styles.td}>{official.role}</td>
                                    <td style={styles.td}>{official.phone || "—"}</td>
                                    <td style={styles.td}>{official.email || "—"}</td>

                                    <td style={styles.td}>
                                        <span style={statusStyle(official.status)}>
                                            {statusLabel(official.status)}
                                        </span>
                                    </td>

                                    <td style={{ ...styles.td, textAlign: "right" }}>
                                        <button
                                            style={styles.editButton}
                                            onClick={() => editOfficial(official)}
                                        >
                                            ✏️
                                        </button>

                                        <button
                                            style={styles.deleteButton}
                                            onClick={() => removeOfficial(official)}
                                        >
                                            🗑️
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </section>
            </main>
        </div>
    );
}

function statusLabel(status: string) {
    if (status === "Available") return "Elérhető";
    if (status === "OnDuty") return "Szolgálatban";
    return "Nem elérhető";
}

function statusStyle(status: string): React.CSSProperties {
    const base: React.CSSProperties = {
        padding: "6px 12px",
        borderRadius: 999,
        fontSize: 13,
        fontWeight: 700,
    };

    if (status === "Available") {
        return { ...base, background: "#DCFCE7", color: "#166534" };
    }

    if (status === "OnDuty") {
        return { ...base, background: "#DBEAFE", color: "#1E40AF" };
    }

    return { ...base, background: "#FEE2E2", color: "#991B1B" };
}

const styles: Record<string, React.CSSProperties> = {
    page: {
        minHeight: "100vh",
        display: "grid",
        gridTemplateColumns: "260px 1fr",
        background: "#F8FAFC",
        color: "#0F172A",
        fontFamily: "Inter, Segoe UI, Arial, sans-serif",
    },
    sidebar: {
        background: "#0F172A",
        color: "white",
        padding: 28,
    },
    logo: {
        fontSize: 26,
        fontWeight: 800,
        marginBottom: 40,
    },
    navItem: {
        padding: "14px 16px",
        borderRadius: 12,
        marginBottom: 8,
        color: "#CBD5E1",
    },
    navItemActive: {
        padding: "14px 16px",
        borderRadius: 12,
        marginBottom: 8,
        background: "#2563EB",
        color: "white",
        fontWeight: 700,
    },
    main: {
        padding: 40,
    },
    header: {
        marginBottom: 28,
    },
    title: {
        margin: 0,
        fontSize: 38,
    },
    subtitle: {
        color: "#64748B",
    },
    stats: {
        display: "grid",
        gridTemplateColumns: "repeat(3, 1fr)",
        gap: 20,
        marginBottom: 28,
    },
    card: {
        background: "white",
        borderRadius: 18,
        padding: 24,
        boxShadow: "0 12px 30px rgba(15, 23, 42, 0.08)",
    },
    cardLabel: {
        display: "block",
        color: "#64748B",
        marginBottom: 10,
    },
    cardValue: {
        fontSize: 36,
    },
    panel: {
        background: "white",
        borderRadius: 20,
        padding: 24,
        boxShadow: "0 12px 30px rgba(15, 23, 42, 0.08)",
    },
    toolbar: {
        marginBottom: 18,
    },
    search: {
        width: "100%",
        boxSizing: "border-box",
        padding: "13px 15px",
        border: "1px solid #CBD5E1",
        borderRadius: 12,
    },
    formGrid: {
        display: "grid",
        gridTemplateColumns: "repeat(3, minmax(0, 1fr))",
        gap: 10,
        marginBottom: 24,
    },
    input: {
        padding: "13px 15px",
        border: "1px solid #CBD5E1",
        borderRadius: 12,
    },
    primaryButton: {
        background: "#2563EB",
        color: "white",
        border: "none",
        borderRadius: 12,
        padding: "13px 18px",
        fontWeight: 700,
        cursor: "pointer",
    },
    table: {
        width: "100%",
        borderCollapse: "collapse",
    },
    th: {
        textAlign: "left",
        padding: "14px 10px",
        borderBottom: "1px solid #E2E8F0",
        color: "#64748B",
        fontSize: 12,
        textTransform: "uppercase",
    },
    td: {
        padding: "17px 10px",
        borderBottom: "1px solid #F1F5F9",
    },
    editButton: {
        border: "none",
        background: "#EEF2FF",
        borderRadius: 10,
        padding: "9px 11px",
        marginRight: 8,
        cursor: "pointer",
    },
    deleteButton: {
        border: "none",
        background: "#FEE2E2",
        borderRadius: 10,
        padding: "9px 11px",
        cursor: "pointer",
    },
};

export default OfficialsApp;