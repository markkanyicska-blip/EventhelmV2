import { api } from "./client";
import type { EventDetails, EventDetailsPayload } from "../types/eventDetails";

export async function getEventDetails(id: string): Promise<EventDetails> {
    const response = await api.get<EventDetails>(`/events/${id}`);
    return response.data;
}

export async function updateEventDetails(id: string, payload: EventDetailsPayload): Promise<EventDetails> {
    const response = await api.put<EventDetails>(`/events/${id}`, payload);
    return response.data;
}
