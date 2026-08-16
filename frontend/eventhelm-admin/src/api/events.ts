import type { EventItem } from "../types";

const API_BASE_URL =
    import.meta.env.VITE_API_BASE_URL ??
    "http://localhost:5100";

export type CreateEventRequest = {
    name: string;
    status: string;
    organizingClub?: string;
    hostingClub?: string;
    startDate?: string;
    endDate?: string;
    location?: string;
    waterArea?: string;
    raceOfficer?: string;
    juryChair?: string;
    technicalCommitteeChair?: string;
    raceOffice?: string;
    vhfChannel?: string;
    trunkGroup?: string;
    notes?: string;
};

export type UpdateEventRequest = CreateEventRequest;

async function handleResponse<T>(
    response: Response,
): Promise<T> {
    if (!response.ok) {
        const message = await response.text();

        throw new Error(
            message ||
            `HTTP hiba: ${response.status}`,
        );
    }

    if (response.status === 204) {
        return undefined as T;
    }

    return response.json() as Promise<T>;
}

export async function getEvents(): Promise<
    EventItem[]
> {
    const response = await fetch(
        `${API_BASE_URL}/api/events`,
    );

    return handleResponse<EventItem[]>(response);
}

export async function getEvent(
    id: string,
): Promise<EventItem> {
    const response = await fetch(
        `${API_BASE_URL}/api/events/${id}`,
    );

    return handleResponse<EventItem>(response);
}

export async function createEvent(
    request: CreateEventRequest,
): Promise<EventItem> {
    const response = await fetch(
        `${API_BASE_URL}/api/events`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        },
    );

    return handleResponse<EventItem>(response);
}

export async function updateEvent(
    id: string,
    request: UpdateEventRequest,
): Promise<EventItem> {
    const response = await fetch(
        `${API_BASE_URL}/api/events/${id}`,
        {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        },
    );

    return handleResponse<EventItem>(response);
}

export async function deleteEvent(
    id: string,
): Promise<void> {
    const response = await fetch(
        `${API_BASE_URL}/api/events/${id}`,
        {
            method: "DELETE",
        },
    );

    await handleResponse<void>(response);
}