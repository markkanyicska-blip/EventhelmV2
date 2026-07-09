import axios from "axios";
import { Event } from "../types/Event";

const API_URL = "http://localhost:5100/api/events";

export async function getEvents(): Promise<Event[]> {
    const response = await axios.get<Event[]>(API_URL);
    return response.data;
}

export async function createEvent(name: string, status: string): Promise<void> {
    await axios.post(API_URL, { name, status });
}

export async function updateEvent(event: Event): Promise<void> {
    await axios.put(`${API_URL}/${event.id}`, {
        name: event.name,
        status: event.status,
    });
}

export async function deleteEvent(id: string): Promise<void> {
    await axios.delete(`${API_URL}/${id}`);
}