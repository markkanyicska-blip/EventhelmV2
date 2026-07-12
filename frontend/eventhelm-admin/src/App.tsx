import {
    BrowserRouter,
    Navigate,
    Route,
    Routes,
} from "react-router-dom";

import MainLayout from "./layout/MainLayout";
import BoatsPage from "./pages/BoatsPage";
import DashboardPage from "./pages/DashboardPage";
import EventsPage from "./pages/EventsPage";
import OfficialsPage from "./pages/OfficialsPage";
import RaceControlPage from "./pages/RaceControlPage";
import UsersPage from "./pages/UsersPage";

export default function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route element={<MainLayout />}>
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
                        element={<DashboardPage />}
                    />

                    <Route
                        path="/race-control"
                        element={<RaceControlPage />}
                    />

                    <Route
                        path="/events"
                        element={<EventsPage />}
                    />

                    <Route
                        path="/officials"
                        element={<OfficialsPage />}
                    />

                    <Route
                        path="/boats"
                        element={<BoatsPage />}
                    />

                    <Route
                        path="/users"
                        element={<UsersPage />}
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
                </Route>
            </Routes>
        </BrowserRouter>
    );
}
