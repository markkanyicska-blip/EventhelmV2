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

import BoatDetailsPage from "./pages/BoatDetailsPage";
import BoatsPage from "./pages/BoatsPage";
import EventDetailsPage from "./pages/EventDetailsPage";
import EventsPage from "./pages/EventsPage";
import MapPage from "./pages/MapPage";
import OfficialProfilePage from "./pages/OfficialProfilePage";
import OfficialsPage from "./pages/OfficialsPage";
import OperationsCenter from "./pages/OperationsCenter";
import UsersPage from "./pages/UsersPage";

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

function AppLayout() {
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
    return (
        <BrowserRouter>
            <AppLayout />
        </BrowserRouter>
    );
}