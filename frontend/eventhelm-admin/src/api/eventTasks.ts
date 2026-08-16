import axios from "axios";

export type EventTask = {
    id: string;
    eventId: string;
    name: string;
    code: string | null;
    description: string | null;
    requiredOfficials: number;
    requiredBoats: number;
    requiredQualification: string | null;
    priority: string;
    notes: string | null;
    createdAt: string;
    updatedAt: string;
};

export async function getEventTasks(
    eventId: string,
): Promise<EventTask[]> {
    const response = await axios.get<EventTask[]>(
        `http://localhost:5100/api/events/${eventId}/tasks`,
    );

    return response.data;
}
