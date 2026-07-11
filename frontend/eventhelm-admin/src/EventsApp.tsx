import { useEffect, useMemo, useState } from "react";
import type { Event } from "./types/Event";
import {
    createEvent,
    deleteEvent,
    getEvents,
    updateEvent,
} from "./api/eventApi";

function EventsApp() {
    const [events, setEvents] = useState<Event[]>([]);
    const [search, setSearch] = useState("");

    const [name, setName] = useState("");
    const [status, setStatus] = useState("Draft");

    const loadEvents = async () => {
        const data = await getEvents();
        setEvents(data);
    };

    useEffect(() => {
        void loadEvents();
    }, []);

    const filteredEvents = useMemo(() => {
        const term = search.toLowerCase();

        return events.filter((event) =>
            `${event.name} ${event.status}`
                .toLowerCase()
                .includes(term)
        );
    }, [events, search]);

    const addEvent = async () => {
        if (!name.trim()) return;

        await createEvent(name, status);

        setName("");
        setStatus("Draft");

        await loadEvents();
    };

    const editEvent = async (event: Event) => {
        const newName = prompt("Esemény neve:", event.name);

        if (!newName?.trim()) return;

        const newStatus = prompt(
            "Státusz:",
            event.status
        );

        if (!newStatus?.trim()) return;

        await updateEvent({
            ...event,
            name: newName,
            status: newStatus,
        });

        await loadEvents();
    };

    const removeEvent = async (event: Event) => {
        if (
            !confirm(
                `Biztosan törlöd ezt az eseményt?\n\n${event.name}`
            )
        )
            return;

        await deleteEvent(event.id);

        await loadEvents();
    };

    return (
        <div style={styles.page}>
            <header style={styles.header}>
                <div>
                    <h1 style={styles.title}>Rendezvények</h1>

                    <p style={styles.subtitle}>
                        Rendezvények kezelése
                    </p>
                </div>
            </header>

            <section style={styles.stats}>
                <div style={styles.card}>
                    <span style={styles.cardLabel}>
                        Összes esemény
                    </span>

                    <strong style={styles.cardValue}>
                        {events.length}
                    </strong>
                </div>

                <div style={styles.card}>
                    <span style={styles.cardLabel}>
                        Draft
                    </span>

                    <strong style={styles.cardValue}>
                        {
                            events.filter(
                                (x) =>
                                    x.status === "Draft"
                            ).length
                        }
                    </strong>
                </div>

                <div style={styles.card}>
                    <span style={styles.cardLabel}>
                        Aktív
                    </span>

                    <strong style={styles.cardValue}>
                        {
                            events.filter(
                                (x) =>
                                    x.status ===
                                    "Active"
                            ).length
                        }
                    </strong>
                </div>
            </section>

            <section style={styles.panel}>
                <div style={styles.toolbar}>
                    <input
                        style={styles.search}
                        placeholder="Keresés..."
                        value={search}
                        onChange={(e) =>
                            setSearch(
                                e.target.value
                            )
                        }
                    />
                </div>

                <div style={styles.formGrid}>
                    <input
                        style={styles.input}
                        placeholder="Rendezvény neve"
                        value={name}
                        onChange={(e) =>
                            setName(
                                e.target.value
                            )
                        }
                    />

                    <select
                        style={styles.input}
                        value={status}
                        onChange={(e) =>
                            setStatus(
                                e.target.value
                            )
                        }
                    >
                        <option value="Draft">
                            Draft
                        </option>

                        <option value="Active">
                            Aktív
                        </option>

                        <option value="Finished">
                            Befejezett
                        </option>
                    </select>

                    <button
                        style={styles.primaryButton}
                        onClick={addEvent}
                    >
                        + Rendezvény hozzáadása
                    </button>
                </div>

                <table style={styles.table}>
                    <thead>
                        <tr>
                            <th style={styles.th}>
                                Név
                            </th>

                            <th style={styles.th}>
                                Státusz
                            </th>

                            <th
                                style={{
                                    ...styles.th,
                                    textAlign:
                                        "right",
                                }}
                            >
                                Műveletek
                            </th>
                        </tr>
                    </thead>

                    <tbody>
                        {filteredEvents.map(
                            (event) => (
                                <tr
                                    key={event.id}
                                >
                                    <td
                                        style={
                                            styles.td
                                        }
                                    >
                                        <strong>
                                            {
                                                event.name
                                            }
                                        </strong>
                                    </td>

                                    <td
                                        style={
                                            styles.td
                                        }
                                    >
                                        {
                                            event.status
                                        }
                                    </td>

                                    <td
                                        style={{
                                            ...styles.td,
                                            textAlign:
                                                "right",
                                        }}
                                    >
                                        <button
                                            style={
                                                styles.editButton
                                            }
                                            onClick={() =>
                                                editEvent(
                                                    event
                                                )
                                            }
                                        >
                                            ✏️
                                        </button>

                                        <button
                                            style={
                                                styles.deleteButton
                                            }
                                            onClick={() =>
                                                removeEvent(
                                                    event
                                                )
                                            }
                                        >
                                            🗑️
                                        </button>
                                    </td>
                                </tr>
                            )
                        )}
                    </tbody>
                </table>
            </section>
        </div>
    );
}
const styles: Record<string, React.CSSProperties> = {
    page: {
        minHeight: "100vh",
        boxSizing: "border-box",
        padding: 32,
        background: "#F8FAFC",
        color: "#0F172A",
        fontFamily: "Inter, Segoe UI, Arial, sans-serif",
    },

    header: {
        marginBottom: 28,
    },

    title: {
        margin: 0,
        fontSize: 36,
        fontWeight: 800,
    },

    subtitle: {
        margin: "8px 0 0",
        color: "#64748B",
        fontSize: 15,
    },

    stats: {
        display: "grid",
        gridTemplateColumns:
            "repeat(auto-fit, minmax(190px, 1fr))",
        gap: 18,
        marginBottom: 24,
    },

    card: {
        padding: 22,
        border: "1px solid #E2E8F0",
        borderRadius: 16,
        background: "#FFFFFF",
        boxShadow:
            "0 8px 24px rgba(15, 23, 42, 0.05)",
    },

    cardLabel: {
        display: "block",
        marginBottom: 10,
        color: "#64748B",
        fontSize: 13,
        fontWeight: 700,
    },

    cardValue: {
        color: "#0F172A",
        fontSize: 34,
        fontWeight: 800,
    },

    panel: {
        padding: 24,
        border: "1px solid #E2E8F0",
        borderRadius: 18,
        background: "#FFFFFF",
        boxShadow:
            "0 8px 24px rgba(15, 23, 42, 0.05)",
    },

    toolbar: {
        marginBottom: 18,
    },

    search: {
        boxSizing: "border-box",
        width: "100%",
        padding: "12px 14px",
        border: "1px solid #CBD5E1",
        borderRadius: 10,
        font: "inherit",
    },

    formGrid: {
        display: "grid",
        gridTemplateColumns:
            "minmax(0, 1.5fr) minmax(180px, 0.7fr) auto",
        gap: 10,
        marginBottom: 24,
    },

    input: {
        boxSizing: "border-box",
        width: "100%",
        padding: "12px 14px",
        border: "1px solid #CBD5E1",
        borderRadius: 10,
        background: "#FFFFFF",
        color: "#0F172A",
        font: "inherit",
    },

    primaryButton: {
        border: 0,
        borderRadius: 10,
        padding: "12px 16px",
        background: "#2563EB",
        color: "#FFFFFF",
        fontWeight: 700,
        cursor: "pointer",
    },

    table: {
        width: "100%",
        borderCollapse: "collapse",
    },

    th: {
        padding: "13px 10px",
        borderBottom: "1px solid #E2E8F0",
        color: "#64748B",
        fontSize: 12,
        fontWeight: 800,
        textAlign: "left",
        textTransform: "uppercase",
    },

    td: {
        padding: "16px 10px",
        borderBottom: "1px solid #F1F5F9",
        color: "#334155",
        fontSize: 14,
    },

    editButton: {
        border: 0,
        borderRadius: 9,
        padding: "8px 10px",
        marginRight: 8,
        background: "#EEF2FF",
        cursor: "pointer",
    },

    deleteButton: {
        border: 0,
        borderRadius: 9,
        padding: "8px 10px",
        background: "#FEE2E2",
        cursor: "pointer",
    },
};

export default EventsApp;