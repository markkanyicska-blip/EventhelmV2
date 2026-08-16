const API_BASE_URL =
    import.meta.env.VITE_API_BASE_URL ??
    "http://localhost:5100";

export type EventAssignmentStatus =
    | "Planned"
    | "Confirmed"
    | "InProgress"
    | "Completed"
    | "Cancelled";

export type EventAssignment = {
    id: string;
    eventId: string;
    officialId: string;

    eventTaskId?: string | null;
    eventBoatId?: string | null;
    eventAreaId?: string | null;
    assignmentGroupId?: string | null;

    title: string;
    role?: string | null;

    startAt: string;
    endAt: string;

    status: EventAssignmentStatus;
    notes?: string | null;

    isLocked: boolean;

    actualStartAt?: string | null;
    actualEndAt?: string | null;

    createdAt: string;
    updatedAt: string;

    officialName: string;
    taskName?: string | null;
    boatName?: string | null;
    areaName?: string | null;

    plannedMinutes: number;
    actualMinutes?: number | null;
};

export type CreateEventAssignmentRequest = {
    officialId: string;

    eventTaskId?: string | null;
    eventBoatId?: string | null;
    eventAreaId?: string | null;
    assignmentGroupId?: string | null;

    title: string;
    role?: string | null;

    startAt: string;
    endAt: string;

    status: EventAssignmentStatus;
    notes?: string | null;

    isLocked: boolean;
};

export type UpdateEventAssignmentRequest = {
    officialId: string;

    eventTaskId?: string | null;
    eventBoatId?: string | null;
    eventAreaId?: string | null;

    title: string;
    role?: string | null;

    startAt: string;
    endAt: string;

    status: EventAssignmentStatus;
    notes?: string | null;

    isLocked: boolean;
};

export type AssignmentValidationMessage = {
    code: string;
    message: string;
};

export type AssignmentWorkloadSummary = {
    officialId: string;
    officialName: string;

    existingPlannedMinutes: number;
    newAssignmentMinutes: number;
    totalPlannedMinutes: number;

    restBeforeMinutes?: number | null;
    restAfterMinutes?: number | null;
};

export type AssignmentValidationResult = {
    canAssign: boolean;

    errors: AssignmentValidationMessage[];
    warnings: AssignmentValidationMessage[];

    workload?: AssignmentWorkloadSummary | null;
};

export type ValidateEventAssignmentRequest = {
    officialId: string;

    eventTaskId?: string | null;
    eventBoatId?: string | null;
    assignmentGroupId?: string | null;
    excludedAssignmentId?: string | null;

    startAt: string;
    endAt: string;
};

type ApiErrorBody = {
    message?: string;
    title?: string;
    validation?: AssignmentValidationResult;
};

export class EventAssignmentApiError extends Error {
    status: number;
    validation?: AssignmentValidationResult;

    constructor(
        message: string,
        status: number,
        validation?: AssignmentValidationResult,
    ) {
        super(message);

        this.name = "EventAssignmentApiError";
        this.status = status;
        this.validation = validation;
    }
}

export async function getEventAssignments(
    eventId: string,
    options?: {
        from?: string;
        to?: string;
    },
): Promise<EventAssignment[]> {
    const query = new URLSearchParams();

    if (options?.from) {
        query.set("from", options.from);
    }

    if (options?.to) {
        query.set("to", options.to);
    }

    const queryString = query.toString();

    const response = await fetch(
        `${API_BASE_URL}/api/events/${eventId}/assignments${queryString ? `?${queryString}` : ""
        }`,
    );

    return handleResponse<EventAssignment[]>(response);
}

export async function getEventAssignment(
    eventId: string,
    assignmentId: string,
): Promise<EventAssignment> {
    const response = await fetch(
        `${API_BASE_URL}/api/events/${eventId}/assignments/${assignmentId}`,
    );

    return handleResponse<EventAssignment>(response);
}

export async function createEventAssignment(
    eventId: string,
    request: CreateEventAssignmentRequest,
): Promise<EventAssignment> {
    const response = await fetch(
        `${API_BASE_URL}/api/events/${eventId}/assignments`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        },
    );

    return handleResponse<EventAssignment>(response);
}

export async function updateEventAssignment(
    eventId: string,
    assignmentId: string,
    request: UpdateEventAssignmentRequest,
): Promise<EventAssignment> {
    const response = await fetch(
        `${API_BASE_URL}/api/events/${eventId}/assignments/${assignmentId}`,
        {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        },
    );

    return handleResponse<EventAssignment>(response);
}

export async function deleteEventAssignment(
    eventId: string,
    assignmentId: string,
): Promise<void> {
    const response = await fetch(
        `${API_BASE_URL}/api/events/${eventId}/assignments/${assignmentId}`,
        {
            method: "DELETE",
        },
    );

    await handleResponse<void>(response);
}

export async function validateEventAssignment(
    eventId: string,
    request: ValidateEventAssignmentRequest,
): Promise<AssignmentValidationResult> {
    const response = await fetch(
        `${API_BASE_URL}/api/events/${eventId}/assignments/validate`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        },
    );

    return handleResponse<AssignmentValidationResult>(
        response,
    );
}

async function handleResponse<T>(
    response: Response,
): Promise<T> {
    if (response.ok) {
        if (response.status === 204) {
            return undefined as T;
        }

        return response.json() as Promise<T>;
    }

    const body = await readErrorBody(response);

    throw new EventAssignmentApiError(
        body?.message ??
        body?.title ??
        `HTTP hiba: ${response.status}`,
        response.status,
        body?.validation,
    );
}

async function readErrorBody(
    response: Response,
): Promise<ApiErrorBody | null> {
    try {
        return (await response.json()) as ApiErrorBody;
    } catch {
        try {
            const message = await response.text();

            return message
                ? {
                    message,
                }
                : null;
        } catch {
            return null;
        }
    }
}