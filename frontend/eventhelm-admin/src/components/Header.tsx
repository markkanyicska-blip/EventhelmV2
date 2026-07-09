export default function Header() {
    return (
        <header
            style={{
                height: 70,
                background: "white",
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                padding: "0 30px",
                borderBottom: "1px solid #ddd",
            }}
        >
            <h2>Dashboard</h2>

            <div>👤 USER</div>
        </header>
    );
}