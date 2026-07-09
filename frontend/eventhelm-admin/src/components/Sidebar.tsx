export default function Sidebar() {
    const menu = [
        "Dashboard",
        "Events",
        "Boats",
        "Competitors",
        "Teams",
        "Tasks",
        "Documents",
        "Results",
        "Settings",
    ];

    return (
        <aside
            style={{
                width: 240,
                background: "#0f172a",
                color: "white",
                minHeight: "100vh",
                padding: 24,
                boxSizing: "border-box",
            }}
        >
            <h2 style={{ marginBottom: 40 }}>🚤 EventHelm</h2>

            {menu.map((item) => (
                <div
                    key={item}
                    style={{
                        padding: "12px 16px",
                        marginBottom: 8,
                        borderRadius: 8,
                        cursor: "pointer",
                    }}
                >
                    {item}
                </div>
            ))}
        </aside>
    );
}