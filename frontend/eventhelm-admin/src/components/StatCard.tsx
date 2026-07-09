type Props = {
    title: string;
    value: number;
};

export default function StatCard({ title, value }: Props) {
    return (
        <div
            style={{
                background: "white",
                borderRadius: 12,
                padding: 25,
                boxShadow: "0 5px 15px rgba(0,0,0,.08)",
            }}
        >
            <div
                style={{
                    color: "#64748b",
                    marginBottom: 8,
                }}
            >
                {title}
            </div>

            <div
                style={{
                    fontSize: 34,
                    fontWeight: 700,
                }}
            >
                {value}
            </div>
        </div>
    );
}