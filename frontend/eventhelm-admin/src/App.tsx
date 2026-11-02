import axios from "axios";
import {
    CalendarDays,
    LayoutDashboard,
    Map,
    Sailboat,
    ShieldCheck,
    Users,
} from "lucide-react";
import {
    BrowserRouter,
    Navigate,
    NavLink,
    Route,
    Routes,
} from "react-router-dom";
import { useEffect, useState } from "react";
import type { FormEvent } from "react";

import BoatDetailsPage from "./pages/BoatDetailsPage";
import BoatsPage from "./pages/BoatsPage";
import EventDetailsPage from "./pages/EventDetailsPage";
import EventsPage from "./pages/EventsPage";
import MapPage from "./pages/MapPage";
import OfficialProfilePage from "./pages/OfficialProfilePage";
import OfficialsPage from "./pages/OfficialsPage";
import OperationsCenter from "./pages/OperationsCenter";
import UsersPage from "./pages/UsersPage";
import { api } from "./api/client";

import "./App.css";

type MenuItem = {
    path: string;
    label: string;
    icon: typeof LayoutDashboard;
};

const menuItems: MenuItem[] = [
    {
        path: "/dashboard",
        label: "Operációs központ",
        icon: LayoutDashboard,
    },
    {
        path: "/events",
        label: "Rendezvények",
        icon: CalendarDays,
    },
    {
        path: "/officials",
        label: "Rendezők",
        icon: ShieldCheck,
    },
    {
        path: "/boats",
        label: "Motorcsónakok",
        icon: Sailboat,
    },
    {
        path: "/users",
        label: "Felhasználók",
        icon: Users,
    },
    {
        path: "/map",
        label: "Térkép",
        icon: Map,
    },
];

type AppLayoutProps = {
    onLogout: () => void;
    logoutError: string;
};

function AppLayout({ onLogout, logoutError }: AppLayoutProps) {
    return (
        <div className="eventhelm-app">
            <aside className="eventhelm-sidebar">
                <div className="eventhelm-sidebar__brand">
                    <div className="eventhelm-sidebar__logo">
                        EH
                    </div>

                    <div>
                        <strong>EventHelm</strong>
                        <span>Regatta Operations</span>
                    </div>
                </div>

                <nav className="eventhelm-sidebar__navigation">
                    {menuItems.map((item) => {
                        const Icon = item.icon;

                        return (
                            <NavLink
                                key={item.path}
                                to={item.path}
                                className={({ isActive }) =>
                                    [
                                        "eventhelm-sidebar__link",
                                        isActive
                                            ? "eventhelm-sidebar__link--active"
                                            : "",
                                    ]
                                        .filter(Boolean)
                                        .join(" ")
                                }
                            >
                                <Icon size={20} />
                                <span>{item.label}</span>
                            </NavLink>
                        );
                    })}
                </nav>

                <div className="eventhelm-sidebar__footer">
                    <span className="eventhelm-sidebar__status">
                        <i />
                        Rendszer elérhető
                    </span>

                    <small>EventHelm V2</small>
                    {logoutError && (
                        <p className="eventhelm-logout-error" role="alert">
                            {logoutError}
                        </p>
                    )}
                    <button
                        type="button"
                        className="eventhelm-logout"
                        onClick={onLogout}
                    >
                        Kijelentkezés
                    </button>
                </div>
            </aside>

            <main className="eventhelm-main">
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
                        element={<OperationsCenter />}
                    />

                    <Route
                        path="/events"
                        element={<EventsPage />}
                    />

                    <Route
                        path="/events/:eventId"
                        element={<EventDetailsPage />}
                    />

                    <Route
                        path="/officials"
                        element={<OfficialsPage />}
                    />

                    <Route
                        path="/officials/:officialId"
                        element={<OfficialProfilePage />}
                    />

                    <Route
                        path="/boats"
                        element={<BoatsPage />}
                    />

                    <Route
                        path="/boats/:id"
                        element={<BoatDetailsPage />}
                    />

                    <Route
                        path="/users"
                        element={<UsersPage />}
                    />

                    <Route
                        path="/map"
                        element={<MapPage />}
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
    );
}

export default function App() {
    const [authenticated, setAuthenticated] = useState<boolean | null>(null);
    const [statusError, setStatusError] = useState(false);
    const [logoutError, setLogoutError] = useState("");

    useEffect(() => {
        let active = true;

        api.get<{ authenticated: boolean }>("/access/status")
            .then(({ data }) => {
                if (active) {
                    setAuthenticated(data.authenticated);
                    setStatusError(false);
                }
            })
            .catch(() => {
                if (active) {
                    setStatusError(true);
                }
            });

        return () => {
            active = false;
        };
    }, []);

    async function login(password: string) {
        await api.post("/access/login", { password });
        setAuthenticated(true);
        setStatusError(false);
    }

    async function logout() {
        try {
            await api.post("/access/logout");
            setAuthenticated(false);
            setLogoutError("");
        } catch {
            setLogoutError("A kijelentkezés nem sikerült. Próbáld újra.");
        }
    }

    if (authenticated === null) {
        if (statusError) {
            return (
                <main className="eventhelm-auth-screen">
                    <section className="eventhelm-auth-card" role="alert">
                        <h1>Az EventHelm nem érhető el</h1>
                        <p>
                            A bejelentkezési állapotot nem sikerült
                            ellenőrizni.
                        </p>
                        <button
                            type="button"
                            onClick={() => window.location.reload()}
                        >
                            Újrapróbálás
                        </button>
                    </section>
                </main>
            );
        }

        return (
            <main className="eventhelm-auth-screen" aria-busy="true">
                <p>Bejelentkezési állapot ellenőrzése...</p>
            </main>
        );
    }

    if (!authenticated) {
        return <LoginPage onLogin={login} />;
    }

    return (
        <BrowserRouter>
            <AppLayout
                onLogout={() => void logout()}
                logoutError={logoutError}
            />
        </BrowserRouter>
    );
}

type LoginPageProps = {
    onLogin: (password: string) => Promise<void>;
};

function LoginPage({ onLogin }: LoginPageProps) {
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");
    const [submitting, setSubmitting] = useState(false);

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setSubmitting(true);
        setError("");

        try {
            await onLogin(password);
        } catch (loginError) {
            setError(getLoginErrorMessage(loginError));
        } finally {
            setSubmitting(false);
        }
    }

    return (
        <main className="eventhelm-auth-screen">
            <section className="eventhelm-auth-card">
                <div className="eventhelm-sidebar__logo">EH</div>
                <p className="eventhelm-auth-eyebrow">REGATTA OPERATIONS</p>
                <h1>Bejelentkezés</h1>
                <p>Az EventHelm használatához add meg a közös jelszót.</p>
                <form onSubmit={(event) => void handleSubmit(event)}>
                    <label htmlFor="access-password">Jelszó</label>
                    <input
                        id="access-password"
                        type="password"
                        autoComplete="current-password"
                        value={password}
                        onChange={(event) => setPassword(event.target.value)}
                        required
                        autoFocus
                    />
                    {error && (
                        <p className="eventhelm-auth-error" role="alert">
                            {error}
                        </p>
                    )}
                    <button type="submit" disabled={submitting || !password}>
                        {submitting ? "Belépés..." : "Belépés"}
                    </button>
                </form>
            </section>
        </main>
    );
}

function getLoginErrorMessage(error: unknown) {
    if (axios.isAxiosError<{ error?: string }>(error)) {
        return (
            error.response?.data?.error ??
            "A bejelentkezés nem sikerült. Ellenőrizd a jelszót."
        );
    }

    return "A bejelentkezés nem sikerült. Ellenőrizd a jelszót.";
}