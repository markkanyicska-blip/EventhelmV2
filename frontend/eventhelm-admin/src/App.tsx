import { useEffect, useState } from "react";
import axios from "axios";

type Event = {
    id: string;
    name: string;
    status: string;
};

const API_URL = "http://localhost:5100/api/events";

function App() {
    const [events, setEvents] = useState<Event[]>([]);
    const [newName, setNewName] = useState("");
    const [newStatus, setNewStatus] = useState("Draft");
    const [search, setSearch] = useState("");

    const loadEvents = () => {
        axios.get(API_URL).then((res) => setEvents(res.data)).catch(console.error);
    };

    useEffect(() => {
        loadEvents();
    }, []);

    const createEvent = async () => {
        if (!newName.trim()) return;

        await axios.post(API_URL, {
            name: newName,
            status: newStatus,
        });

        setNewName("");
        setNewStatus("Draft");
        loadEvents();
    };

    const editEvent = async (event: Event) => {
        const newName = prompt("Rendezvény neve:", event.name);
        if (!newName) return;

        await axios.put(`${API_URL}/${event.id}`, {
            name: newName,
            status: event.status,
        });

        loadEvents();
    };

    const deleteEvent = async (event: Event) => {
        if (!confirm(`Biztosan törlöd: ${event.name}?`)) return;

        await axios.delete(`${API_URL}/${event.id}`);
        loadEvents();
    };

    const filteredEvents = events.filter((event) =>
        event.name.toLowerCase().includes(search.toLowerCase())
    );

    const draftCount = events.filter((e) => e.status === "Draft").length;
    const activeCount = events.filter((e) => e.status === "Active").length;

    return (
        <div style={styles.page}>
            <div style={styles.shell}>
                <aside style={styles.sidebar}>
                    <div style={styles.logo}>🚤 EventHelm</div>

                    <nav>
                        {["Dashboard", "Rendezvények", "Hajók", "Feladatok", "Dokumentumok", "Beállítások"].map(
                            (item) => (
                                <div key={item} style={styles.navItem}>
                                    {item}
                                </div>
                            )
                        )}
                    </nav>
                </aside>

                <main style={styles.main}>
                    <header style={styles.header}>
                        <div>
                            <h1 style={styles.title}>Rendezvények</h1>
                            <p style={styles.subtitle}>Vitorlásversenyek kezelése egy helyen</p>
                        </div>

                        <button style={styles.primaryButton} onClick={createEvent}>
                            + Új rendezvény
                        </button>
                    </header>

                    <section style={styles.stats}>
                        <div style={styles.card}>
                            <span style={styles.cardLabel}>Összes rendezvény</span>
                            <strong style={styles.cardValue}>{events.length}</strong>
                        </div>

                        <div style={styles.card}>
                            <span style={styles.cardLabel}>Aktív</span>
                            <strong style={styles.cardValue}>{activeCount}</strong>
                        </div>

                        <div style={styles.card}>
                            <span style={styles.cardLabel}>Draft</span>
                            <strong style={styles.cardValue}>{draftCount}</strong>
                        </div>
                    </section>

                    <section style={styles.panel}>
                        <div style={styles.panelHeader}>
                            <input
                                style={styles.search}
                                placeholder="Keresés rendezvények között..."
                                value={search}
                                onChange={(e) => setSearch(e.target.value)}
                            />

                            <div style={styles.form}>
                                <input
                                    style={styles.input}
                                    placeholder="Új rendezvény neve"
                                    value={newName}
                                    onChange={(e) => setNewName(e.target.value)}
                                />

                                <select
                                    style={styles.select}
                                    value={newStatus}
                                    onChange={(e) => setNewStatus(e.target.value)}
                                >
                                    <option value="Draft">Draft</option>
                                    <option value="Active">Active</option>
                                    <option value="Planning">Planning</option>
                                    <option value="Closed">Closed</option>
                                </select>
                            </div>
                        </div>

                        <table style={styles.table}>
                            <thead>
                                <tr>
                                    <th style={styles.th}>Név</th>
                                    <th style={styles.th}>Státusz</th>
                                    <th style={{ ...styles.th, textAlign: "right" }}>Műveletek</th>
                                </tr>
                            </thead>

                            <tbody>
                                {filteredEvents.map((event) => (
                                    <tr key={event.id}>
                                        <td style={styles.td}>
                                            <strong>{event.name}</strong>
                                        </td>

                                        <td style={styles.td}>
                                            <span style={statusStyle(event.status)}>{event.status}</span>
                                        </td>

                                        <td style={{ ...styles.td, textAlign: "right" }}>
                                            <button style={styles.iconButton} onClick={() => editEvent(event)}>
                                                ✏️
                                            </button>

                                            <button style={styles.dangerButton} onClick={() => deleteEvent(event)}>
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
        </div>
    );
}

function statusStyle(status: string): React.CSSProperties {
    const base: React.CSSProperties = {
        padding: "6px 12px",
        borderRadius: 999,
        fontSize: 13,
        fontWeight: 700,
        display: "inline-block",
    };

    if (status === "Active") return { ...base, background: "#DCFCE7", color: "#166534" };
    if (status === "Draft") return { ...base, background: "#FEF3C7", color: "#92400E" };
    if (status === "Closed") return { ...base, background: "#E2E8F0", color: "#334155" };

    return { ...base, background: "#DBEAFE", color: "#1E40AF" };
}

const styles: Record<string, React.CSSProperties> = {
    page: {
        minHeight: "100vh",
        background: "#F8FAFC",
        fontFamily: "Inter, Segoe UI, Arial, sans-serif",
        color: "#0F172A",
    },
    shell: {
        display: "grid",
        gridTemplateColumns: "260px 1fr",
        minHeight: "100vh",
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
        padding: "13px 14px",
        borderRadius: 12,
        marginBottom: 8,
        color: "#CBD5E1",
        cursor: "pointer",
    },
    main: {
        padding: 40,
    },
    header: {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        marginBottom: 30,
    },
    title: {
        margin: 0,
        fontSize: 38,
    },
    subtitle: {
        marginTop: 8,
        color: "#64748B",
    },
    primaryButton: {
        background: "#2563EB",
        color: "white",
        border: "none",
        borderRadius: 12,
        padding: "14px 20px",
        fontWeight: 800,
        cursor: "pointer",
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
        color: "#64748B",
        display: "block",
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
    panelHeader: {
        display: "flex",
        justifyContent: "space-between",
        gap: 16,
        marginBottom: 22,
    },
    search: {
        flex: 1,
        padding: "13px 15px",
        border: "1px solid #CBD5E1",
        borderRadius: 12,
    },
    form: {
        display: "flex",
        gap: 10,
    },
    input: {
        padding: "13px 15px",
        border: "1px solid #CBD5E1",
        borderRadius: 12,
    },
    select: {
        padding: "13px 15px",
        border: "1px solid #CBD5E1",
        borderRadius: 12,
    },
    table: {
        width: "100%",
        borderCollapse: "collapse",
    },
    th: {
        textAlign: "left",
        padding: "14px 10px",
        color: "#64748B",
        borderBottom: "1px solid #E2E8F0",
        fontSize: 13,
        textTransform: "uppercase",
    },
    td: {
        padding: "18px 10px",
        borderBottom: "1px solid #F1F5F9",
    },
    iconButton: {
        border: "none",
        background: "#EEF2FF",
        borderRadius: 10,
        padding: "9px 11px",
        cursor: "pointer",
        marginRight: 8,
    },
    dangerButton: {
        border: "none",
        background: "#FEE2E2",
        borderRadius: 10,
        padding: "9px 11px",
        cursor: "pointer",
    },
};

export default App;