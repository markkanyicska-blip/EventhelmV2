import type { CSSProperties, ReactNode } from "react";
import {
    BrowserRouter,
    Navigate,
    NavLink,
    Route,
    Routes,
} from "react-router-dom";

import BoatsApp from "./BoatsApp";
import Dashboard from "./Dashboard";
import OfficialsApp from "./OfficialsApp";
import UsersApp from "./UsersApp";

function App() {
    return (
        <BrowserRouter>
            <div style={styles.page}>
                <aside style={styles.sidebar}>
                    <div style={styles.logo}>
                        <span style={styles.logoIcon}>🚤</span>
                        <span>EventHelm</span>
                    </div>

                    <div style={styles.sectionTitle}>OPERÁCIÓ</div>

                    <nav>
                        <MenuLink
                            to="/dashboard"
                            icon="📊"
                            label="Operációs központ"
                        />

                        <MenuLink
                            to="/officials"
                            icon="👥"
                            label="Rendezők"
                        />

                        <MenuLink
                            to="/boats"
                            icon="🚤"
                            label="Motorcsónakok"
                        />

                        <MenuLink
                            to="/users"
                            icon="👤"
                            label="Felhasználók"
                        />
                    </nav>

                    <div style={styles.sidebarFooter}>
                        <div style={styles.environmentIndicator}>
                            <span style={styles.onlineDot} />
                            Fejlesztői környezet
                        </div>
                    </div>
                </aside>

                <main style={styles.content}>
                    <Routes>
                        <Route
                            path="/"
                            element={<Navigate to="/dashboard" replace />}
                        />

                        <Route
                            path="/dashboard"
                            element={<Dashboard />}
                        />

                        <Route
                            path="/officials"
                            element={<OfficialsApp embedded />}
                        />

                        <Route
                            path="/boats"
                            element={<BoatsApp />}
                        />

                        <Route
                            path="/users"
                            element={<UsersApp embedded />}
                        />

                        <Route
                            path="*"
                            element={<Navigate to="/dashboard" replace />}
                        />
                    </Routes>
                </main>
            </div>
        </BrowserRouter>
    );
}

type MenuLinkProps = {
    to: string;
    icon: ReactNode;
    label: string;
};

function MenuLink({ to, icon, label }: MenuLinkProps) {
    return (
        <NavLink
            to={to}
            style={({ isActive }) => ({
                ...styles.menuLink,
                color: isActive ? "#FFFFFF" : "#CBD5E1",
                background: isActive ? "#2563EB" : "transparent",
            })}
        >
            <span style={styles.menuIcon}>{icon}</span>
            <span>{label}</span>
        </NavLink>
    );
}

const styles: Record<string, CSSProperties> = {
    page: {
        display: "grid",
        gridTemplateColumns: "280px minmax(0, 1fr)",
        minHeight: "100vh",
        background: "#F1F5F9",
    },

    sidebar: {
        position: "sticky",
        top: 0,
        display: "flex",
        flexDirection: "column",
        height: "100vh",
        boxSizing: "border-box",
        padding: "28px 20px",
        background: "#0F172A",
    },

    logo: {
        display: "flex",
        alignItems: "center",
        gap: 12,
        marginBottom: 40,
        color: "#FFFFFF",
        fontSize: 26,
        fontWeight: 800,
    },

    logoIcon: {
        fontSize: 30,
    },

    sectionTitle: {
        marginBottom: 12,
        paddingLeft: 14,
        color: "#64748B",
        fontSize: 11,
        fontWeight: 800,
        letterSpacing: "0.12em",
    },

    menuLink: {
        display: "flex",
        alignItems: "center",
        gap: 12,
        marginBottom: 8,
        padding: "14px 16px",
        borderRadius: 12,
        textDecoration: "none",
        fontSize: 15,
        fontWeight: 700,
        transition: "background 150ms ease",
    },

    menuIcon: {
        display: "inline-flex",
        justifyContent: "center",
        width: 24,
        fontSize: 18,
    },

    sidebarFooter: {
        marginTop: "auto",
        paddingTop: 24,
    },

    environmentIndicator: {
        display: "flex",
        alignItems: "center",
        gap: 9,
        padding: "12px 14px",
        border: "1px solid #1E293B",
        borderRadius: 12,
        color: "#94A3B8",
        fontSize: 13,
        fontWeight: 600,
    },

    onlineDot: {
        width: 9,
        height: 9,
        borderRadius: "50%",
        background: "#22C55E",
        boxShadow: "0 0 0 4px rgba(34, 197, 94, 0.12)",
    },

    content: {
        minWidth: 0,
        minHeight: "100vh",
    },
};

export default App;