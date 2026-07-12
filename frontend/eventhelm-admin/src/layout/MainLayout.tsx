import { Outlet } from "react-router-dom";
import Sidebar from "../components/Sidebar";
import Topbar from "../components/Topbar";
import "./MainLayout.css";

export default function MainLayout() {
    return (
        <div className="eh-app-shell">
            <Sidebar />
            <div className="eh-workspace">
                <Topbar />
                <main className="eh-content">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}
