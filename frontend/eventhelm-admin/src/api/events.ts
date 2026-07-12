import type { EventItem } from "../types";
import { api } from "./client";

export async function getEvents(): Promise<EventItem[]> {
    const response = await api.get<EventItem[]>("/events");
    return response.data;
}

export async function createEvent(
    payload: Omit<EventItem, "id">,
): Promise<EventItem> {
    const response = await api.post<EventItem>("/events", payload);
    return response.data;
}

export async function updateEvent(
    item: EventItem,
): Promise<EventItem> {
    const response = await api.put<EventItem>(
        `/events/${item.id}`,
        {
            name: item.name,
            status: item.status,
        },
    );
    return response.data;
}

export async function deleteEvent(id: string): Promise<void> {
    await api.delete(`/events/${id}`);
}
