import { Event } from "../types/Event";

type Props = {
    events: Event[];
};

const badge = (status: string) => {
    switch (status.toLowerCase()) {
        case "active":
            return "#22c55e";
        case "draft":
            return "#f59e0b";
        case "closed":
            return "#64748b";
        default:
            return "#3b82f6";
    }
};

export default function EventTable({ events }: Props) {
    return (
        <div
            style={{
                background: "white",
                borderRadius: 12,
                padding: 20,
                boxShadow: "0 5px 15px rgba(0,0,0,.08)",
            }}
        >
            <h2>Rendezvények</h2>

            <table
                style={{
                    width: "100%",
                    borderCollapse: "collapse",
                }}
            >
                <thead>
                    <tr>
                        <th align="left">Név</th>
                        <th align="left">Státusz</th>
                        <th align="right">Műveletek</th>
                    </tr>
                </thead>

                <tbody>
                    {events.map((event) => (
                        <tr key={event.id}>
                            <td
                                style={{
                                    padding: "14px 0",
                                    borderBottom: "1px solid #eee",
                                }}
                            >
                                {event.name}
                            </td>

                            <td
                                style={{
                                    borderBottom: "1px solid #eee",
                                }}
                            >
                                <span
                                    style={{
                                        background: badge(event.status),
                                        color: "white",
                                        borderRadius: 20,
                                        padding: "4px 12px",
                                        fontSize: 13,
                                    }}
                                >
                                    {event.status}
                                </span>
                            </td>

                            <td
                                align="right"
                                style={{
                                    borderBottom: "1px solid #eee",
                                }}
                            >
                                <button>✏️</button>

                                <button
                                    style={{
                                        marginLeft: 10,
                                    }}
                                >
                                    🗑️
                                </button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}