type Props = {
    total: number;
    active: number;
    draft: number;
};

export default function StatsCards({
    total,
    active,
    draft,
}: Props) {
    return (
        <section
            style={{
                display: "grid",
                gridTemplateColumns: "repeat(3,1fr)",
                gap: 20,
                marginBottom: 28,
            }}
        >
            <Card title="Összes rendezvény" value={total} />
            <Card title="Aktív" value={active} />
            <Card title="Draft" value={draft} />
        </section>
    );
}

function Card({
    title,
    value,
}: {
    title: string;
    value: number;
}) {
    return (
        <div
            style={{
                background: "white",
                borderRadius: 18,
                padding: 24,
                boxShadow: "0 12px 30px rgba(15,23,42,.08)",
            }}
        >
            <span
                style={{
                    color: "#64748B",
                    display: "block",
                    marginBottom: 10,
                }}
            >
                {title}
            </span>

            <strong
                style={{
                    fontSize: 36,
                }}
            >
                {value}
            </strong>
        </div>
    );
}