import { api } from "./client";

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
    const response = await api.get<EventTask[]>(
        `/events/${eventId}/tasks`,
    );

    return response.data;
}

