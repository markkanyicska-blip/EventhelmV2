import type { CSSProperties } from "react";
import { Menu } from "lucide-react";
import {
    BrowserRouter,
    Navigate,
    Route,
    Routes,
} from "react-router-dom";

import BoatsApp from "./BoatsApp";
import Dashboard from "./Dashboard";
import EventsApp from "./EventsApp";
import OfficialsApp from "./OfficialsApp";
import SidebarNew from "./components/SidebarNew";
import UsersApp from "./UsersApp";

function App() {
    return (
        <BrowserRouter>
            <div style={styles.application}>
                <SidebarNew />

                <div style={styles.workspace}>
                    <header style={styles.topbar}>
                        <div style={styles.topbarTitle}>
                            <button
                                type="button"
                                style={styles.menuButton}
                                aria-label="Navigációs menü"
                            >
                                <Menu size={21} strokeWidth={1.8} />
                            </button>

                            <span style={styles.divider} />

                            <span>EventHelm operációs rendszer</span>
                        </div>

                        <div style={styles.systemState}>
                            <span style={styles.statusDot} />
                            Fejlesztői környezet
                        </div>
                    </header>

                    <main style={styles.content}>
                        <Routes>
                            <Route
                                path="/"
                                element={
                                    <Navigate
                                        to="/dashboard"
                                        replace
                                    />
                                }
                            />

                            <Route
                                path="/dashboard"
                                element={<Dashboard />}
                            />

                            <Route
                                path="/events"
                                element={<EventsApp />}
                            />

                            <Route
                                path="/officials"
                                element={<OfficialsApp />}
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
                                element={
                                    <Navigate
                                        to="/dashboard"
                                        replace
                                    />
                                }
                            />
                        </Routes>
                    </main>
                </div>
            </div>
        </BrowserRouter>
    );
}

const styles: Record<string, CSSProperties> = {
    application: {
        display: "grid",
        gridTemplateColumns: "264px minmax(0, 1fr)",
        minHeight: "100vh",
        background: "#07131E",
        fontFamily:
            "Inter, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
    },

    workspace: {
        minWidth: 0,
        minHeight: "100vh",
        background: "#EDF1F4",
    },

    topbar: {
        position: "sticky",
        top: 0,
        zIndex: 20,
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        boxSizing: "border-box",
        height: 72,
        padding: "0 30px",
        borderBottom: "1px solid #DCE3E8",
        background: "rgba(248, 250, 252, 0.92)",
        backdropFilter: "blur(14px)",
        WebkitBackdropFilter: "blur(14px)",
    },

    topbarTitle: {
        display: "flex",
        alignItems: "center",
        gap: 16,
        color: "#12212F",
        fontSize: 15,
        fontWeight: 700,
    },

    menuButton: {
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        width: 38,
        height: 38,
        padding: 0,
        border: 0,
        borderRadius: 9,
        background: "transparent",
        color: "#273847",
        cursor: "pointer",
        transition:
            "background 150ms ease, color 150ms ease",
    },

    divider: {
        width: 1,
        height: 22,
        background: "#D6DEE4",
    },

    systemState: {
        display: "flex",
        alignItems: "center",
        gap: 8,
        padding: "8px 11px",
        border: "1px solid #D9E2E8",
        borderRadius: 999,
        background: "#FFFFFF",
        color: "#536473",
        fontSize: 11,
        fontWeight: 700,
    },

    statusDot: {
        width: 7,
        height: 7,
        borderRadius: "50%",
        background: "#2BB673",
        boxShadow:
            "0 0 0 4px rgba(43, 182, 115, 0.12)",
    },

    content: {
        minWidth: 0,
        minHeight: "calc(100vh - 72px)",
        background:
            "radial-gradient(circle at 90% 0%, rgba(224, 168, 46, 0.055), transparent 25%), #EDF1F4",
    },
};

export default App;