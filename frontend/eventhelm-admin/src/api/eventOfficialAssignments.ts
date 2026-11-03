import { api } from "./client";
import type {
    EventOfficialAssignment,
    EventOfficialAssignmentPayload,
} from "../types/eventOfficialAssignments";

export async function getEventOfficialAssignments(
    eventId: string,
): Promise<EventOfficialAssignment[]> {
    const response =
        await api.get<EventOfficialAssignment[]>(
            `/events/${eventId}/official-assignments`,
        );

    return response.data;
}

export async function createEventOfficialAssignment(
    eventId: string,
    payload: EventOfficialAssignmentPayload,
): Promise<EventOfficialAssignment> {
    const response =
        await api.post<EventOfficialAssignment>(
            `/events/${eventId}/official-assignments`,
            payload,
        );

    return response.data;
}

export async function updateEventOfficialAssignment(
    eventId: string,
    assignmentId: string,
    payload: EventOfficialAssignmentPayload,
): Promise<EventOfficialAssignment> {
    const response =
        await api.put<EventOfficialAssignment>(
            `/events/${eventId}/official-assignments/${assignmentId}`,
            payload,
        );

    return response.data;
}

export async function deleteEventOfficialAssignment(
    eventId: string,
    assignmentId: string,
): Promise<void> {
    await api.delete(
        `/events/${eventId}/official-assignments/${assignmentId}`,
    );
}
