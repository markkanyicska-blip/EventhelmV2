import { BrowserRouter, Navigate, NavLink, Route, Routes } from "react-router-dom";
import OfficialsApp from "./OfficialsApp";
import UsersApp from "./UsersApp";

function App() {
    return (
        <BrowserRouter>
            <div style={styles.page}>
                <aside style={styles.sidebar}>
                    <div style={styles.logo}>🚤 EventHelm</div>

                    <nav>
                        <MenuLink to="/officials" label="Rendezők" />
                        <MenuLink to="/users" label="Felhasználók" />
                    </nav>
                </aside>

                <main style={styles.content}>
                    <Routes>
                        <Route path="/" element={<Navigate to="/officials" replace />} />
                        <Route path="/officials" element={<OfficialsApp embedded />} />
                        <Route path="/users" element={<UsersApp embedded />} />
                    </Routes>
                </main>
            </div>
        </BrowserRouter>
    );
}

function MenuLink({ to, label }: { to: string; label: string }) {
    return (
        <NavLink
            to={to}
            style={({ isActive }) => ({
                display: "block",
                padding: "14px 16px",
                marginBottom: 8,
                borderRadius: 12,
                textDecoration: "none",
                fontWeight: 700,
                color: isActive ? "white" : "#CBD5E1",
                background: isActive ? "#2563EB" : "transparent",
            })}
        >
            {label}
        </NavLink>
    );
}

const styles: Record<string, React.CSSProperties> = {
    page: {
        display: "grid",
        gridTemplateColumns: "260px 1fr",
        minHeight: "100vh",
        background: "#F8FAFC",
    },
    sidebar: {
        background: "#0F172A",
        padding: 28,
    },
    logo: {
        color: "white",
        fontSize: 26,
        fontWeight: 800,
        marginBottom: 40,
    },
    content: {
        minWidth: 0,
    },
};

export default App;

