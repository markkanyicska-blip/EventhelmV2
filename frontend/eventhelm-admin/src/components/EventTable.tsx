import type { CSSProperties } from "react";
import type { Event } from "../types/Event";

type Props = {
    events: Event[];
    onEdit: (event: Event) => void;
    onDelete: (event: Event) => void;
};

export default function EventTable({
    events,
    onEdit,
    onDelete,
}: Props) {
    return (
        <table style={styles.table}>
            <thead>
                <tr>
                    <th style={styles.th}>Név</th>
                    <th style={styles.th}>Státusz</th>
                    <th style={{ ...styles.th, textAlign: "right" }}>
                        Műveletek
                    </th>
                </tr>
            </thead>

            <tbody>
                {events.map((event) => (
                    <tr key={event.id}>
                        <td style={styles.td}>
                            <strong>{event.name}</strong>
                        </td>

                        <td style={styles.td}>
                            <span style={statusStyle(event.status)}>
                                {event.status}
                            </span>
                        </td>

                        <td
                            style={{
                                ...styles.td,
                                textAlign: "right",
                            }}
                        >
                            <button
                                style={styles.iconButton}
                                onClick={() => onEdit(event)}
                            >
                                ✏️
                            </button>

                            <button
                                style={styles.dangerButton}
                                onClick={() => onDelete(event)}
                            >
                                🗑️
                            </button>
                        </td>
                    </tr>
                ))}
            </tbody>
        </table>
    );
}

function statusStyle(status: string): CSSProperties {
    const base: CSSProperties = {
        padding: "6px 12px",
        borderRadius: 999,
        fontSize: 13,
        fontWeight: 700,
        display: "inline-block",
    };

    if (status === "Active") {
        return {
            ...base,
            background: "#DCFCE7",
            color: "#166534",
        };
    }

    if (status === "Draft") {
        return {
            ...base,
            background: "#FEF3C7",
            color: "#92400E",
        };
    }

    if (status === "Closed") {
        return {
            ...base,
            background: "#E2E8F0",
            color: "#334155",
        };
    }

    return {
        ...base,
        background: "#DBEAFE",
        color: "#1E40AF",
    };
}

const styles: Record<string, CSSProperties> = {
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