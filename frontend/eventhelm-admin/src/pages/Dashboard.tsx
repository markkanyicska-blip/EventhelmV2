import { useEffect, useMemo, useState } from "react";
import { Event } from "../types/Event";
import { getEvents } from "../api/eventApi";
import StatCard from "../components/StatCard";
import EventTable from "../components/EventTable";

export default function Dashboard() {
    const [events, setEvents] = useState<Event[]>([]);

    useEffect(() => {
        load();
    }, []);

    const load = async () => {
        const data = await getEvents();
        setEvents(data);
    };

    const draft = useMemo(
        () => events.filter((x) => x.status === "Draft").length,
        [events]
    );

    const active = useMemo(
        () => events.filter((x) => x.status === "Active").length,
        [events]
    );

    return (
        <>
            <div
                style={{
                    display: "grid",
                    gridTemplateColumns: "repeat(3,1fr)",
                    gap: 20,
                    marginBottom: 25,
                }}
            >
                <StatCard title="Events" value={events.length} />
                <StatCard title="Draft" value={draft} />
                <StatCard title="Active" value={active} />
            </div>

            <EventTable events={events} />
        </>
    );
}