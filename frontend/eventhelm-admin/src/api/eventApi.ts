import axios from "axios";
import type { Event } from "../types/Event";

const API_URL = "/api/events";

export async function getEvents(): Promise<Event[]> {
    const response = await axios.get<Event[]>(API_URL);
    return response.data;
}

export async function getEvent(id: string): Promise<Event> {
    const response = await axios.get<Event>(`${API_URL}/${id}`);
    return response.data;
}

export async function createEvent(
    name: string,
    status: string,
): Promise<Event> {
    const response = await axios.post<Event>(API_URL, {
        name,
        status,
    });

    return response.data;
}

export async function updateEvent(
    event: Event,
): Promise<Event> {
    const response = await axios.put<Event>(
        `${API_URL}/${event.id}`,
        {
            name: event.name,
            status: event.status,
            organizingClub: event.organizingClub,
            hostingClub: event.hostingClub,
            startDate: event.startDate,
            endDate: event.endDate,
            location: event.location,
            waterArea: event.waterArea,
            raceOfficer: event.raceOfficer,
            juryChair: event.juryChair,
            technicalCommitteeChair: event.technicalCommitteeChair,
            raceOffice: event.raceOffice,
            vhfChannel: event.vhfChannel,
            trunkGroup: event.trunkGroup,
            notes: event.notes,
        },
    );

    return response.data;
}

export async function deleteEvent(id: string): Promise<void> {
    await axios.delete(`${API_URL}/${id}`);
}