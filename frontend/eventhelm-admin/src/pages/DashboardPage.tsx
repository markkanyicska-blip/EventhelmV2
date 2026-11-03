import {
    CalendarDays,
    Sailboat,
    ShieldCheck,
    Users,
} from "lucide-react";

import {
    useCallback,
    useEffect,
    useMemo,
    useState,
} from "react";

import { getBoats } from "../api/boats";
import { getEvents } from "../api/eventApi";
import { getHealth } from "../api/health";
import { getOfficials } from "../api/officials";
import { getUsers } from "../api/users";

import HeroBanner from "../components/HeroBanner";
import StatCard from "../components/StatCard";
import { Card } from "../components/ui";

import type {
    Boat,
    EventItem,
    Official,
    User,
} from "../types";

import "./DashboardPage.css";

type DashboardState = {
    events: EventItem[];
    boats: Boat[];
    officials: Official[];
    users: User[];
};

const emptyState: DashboardState = {
    events: [],
    boats: [],
    officials: [],
    users: [],
};

export default function DashboardPage() {
    const [data, setData] =
        useState<DashboardState>(emptyState);

    const [backendOnline, setBackendOnline] =
        useState(false);

    const loadDashboard = useCallback(async () => {
        try {
            await getHealth();

            const [
                events,
                boats,
                officials,
                users,
            ] = await Promise.all([
                getEvents(),
                getBoats(),
                getOfficials(),
                getUsers(),
            ]);

            setData({
                events,
                boats,
                officials,
                users,
            });

            setBackendOnline(true);
        } catch (error) {
            console.error(
                "A Dashboard adatai nem tölthetők be:",
                error,
            );

            setBackendOnline(false);
        }
    }, []);

    useEffect(() => {
        void loadDashboard();

        const timer = window.setInterval(() => {
            void loadDashboard();
        }, 30000);

        return () => {
            window.clearInterval(timer);
        };
    }, [loadDashboard]);

    const statistics = useMemo(() => {
        const activeEvents = data.events.filter(
            (item) =>
                item.status.toLowerCase() === "active",
        ).length;

        const activeBoats = data.boats.filter(
            (item) =>
                item.status.toLowerCase() === "active",
        ).length;

        const activeOfficials =
            data.officials.filter((item) =>
                [
                    "active",
                    "available",
                    "szolgálatban",
                    "szolgalatban",
                ].includes(
                    item.status.toLowerCase(),
                ),
            ).length;

        const activeUsers = data.users.filter(
            (item) => item.isActive,
        ).length;

        return {
            activeEvents,
            activeBoats,
            activeOfficials,
            activeUsers,
        };
    }, [data]);

    return (
        <div className="eh-dashboard-page">
            <HeroBanner
                activeEvents={
                    statistics.activeEvents
                }
                activeOfficials={
                    statistics.activeOfficials
                }
                backendOnline={backendOnline}
            />

            <section className="eh-dashboard-page__stats">
                <StatCard
                    icon={CalendarDays}
                    label="Aktív rendezvények"
                    value={statistics.activeEvents}
                    description={`${data.events.length} összes rendezvény`}
                    variant="gold"
                />

                <StatCard
                    icon={Sailboat}
                    label="Aktív motorcsónakok"
                    value={statistics.activeBoats}
                    description={`${data.boats.length} regisztrált egység`}
                    variant="blue"
                />

                <StatCard
                    icon={ShieldCheck}
                    label="Szolgálatban"
                    value={
                        statistics.activeOfficials
                    }
                    description={`${data.officials.length} rendező`}
                    variant="green"
                />

                <StatCard
                    icon={Users}
                    label="Aktív felhasználók"
                    value={statistics.activeUsers}
                    description={`${data.users.length} regisztrált felhasználó`}
                    variant="red"
                />
            </section>

            <section className="eh-dashboard-page__grid">
                <Card
                    eyebrow="RENDEZVÉNYEK"
                    title="Versenyprogram"
                >
                    <div className="eh-list">
                        {data.events.length === 0 ? (
                            <div>
                                <strong>
                                    Nincs megjeleníthető
                                    rendezvény
                                </strong>
                                <span>
                                    Hozd létre az első
                                    rendezvényt.
                                </span>
                            </div>
                        ) : (
                            data.events
                                .slice(0, 5)
                                .map((item) => (
                                    <div key={item.id}>
                                        <strong>
                                            {item.name}
                                        </strong>
                                        <span>
                                            {item.status}
                                        </span>
                                    </div>
                                ))
                        )}
                    </div>
                </Card>

                <Card
                    eyebrow="SZEMÉLYZET"
                    title="Rendezői állomány"
                >
                    <div className="eh-list">
                        {data.officials.length === 0 ? (
                            <div>
                                <strong>
                                    Nincs megjeleníthető
                                    rendező
                                </strong>
                                <span>
                                    Az állomány itt fog
                                    megjelenni.
                                </span>
                            </div>
                        ) : (
                            data.officials
                                .slice(0, 5)
                                .map((item) => (
                                    <div key={item.id}>
                                        <strong>
                                            {item.name}
                                        </strong>
                                        <span>
                                            {item.role}
                                        </span>
                                    </div>
                                ))
                        )}
                    </div>
                </Card>
            </section>
        </div>
    );
}
