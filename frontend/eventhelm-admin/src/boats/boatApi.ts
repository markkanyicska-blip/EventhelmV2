import type {
    Boat,
    BoatCrewAssignment,
    BoatForm,
    CreateBoatCrewAssignmentRequest,
    Official,
    UpdateBoatCrewAssignmentRequest,
} from "./boatTypes";

const API_BASE_URL = "";

export async function getBoats(): Promise<Boat[]> {
    const response = await fetch(
        `${API_BASE_URL}/api/boats`,
    );

    return readJsonResponse<Boat[]>(
        response,
        "A motorcsónakok betöltése sikertelen.",
    );
}

export async function createBoat(
    form: BoatForm,
): Promise<Boat> {
    const response = await fetch(
        `${API_BASE_URL}/api/boats`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(toBoatPayload(form)),
        },
    );

    return readJsonResponse<Boat>(
        response,
        "A motorcsónak létrehozása sikertelen.",
    );
}

export async function updateBoat(
    boatId: string,
    form: BoatForm,
): Promise<Boat> {
    const response = await fetch(
        `${API_BASE_URL}/api/boats/${boatId}`,
        {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(toBoatPayload(form)),
        },
    );

    return readJsonResponse<Boat>(
        response,
        "A motorcsónak módosítása sikertelen.",
    );
}

export async function deleteBoat(
    boatId: string,
): Promise<void> {
    const response = await fetch(
        `${API_BASE_URL}/api/boats/${boatId}`,
        {
            method: "DELETE",
        },
    );

    await ensureSuccess(
        response,
        "A motorcsónak törlése sikertelen.",
    );
}

export async function getOfficials(): Promise<Official[]> {
    const response = await fetch(
        `${API_BASE_URL}/api/officials`,
    );

    return readJsonResponse<Official[]>(
        response,
        "A rendezők betöltése sikertelen.",
    );
}

export async function getBoatCrew(
    boatId: string,
): Promise<BoatCrewAssignment[]> {
    const response = await fetch(
        `${API_BASE_URL}/boats/${boatId}/crew`,
    );

    return readJsonResponse<BoatCrewAssignment[]>(
        response,
        "A hajó személyzetének betöltése sikertelen.",
    );
}

export async function createBoatCrewAssignment(
    boatId: string,
    request: CreateBoatCrewAssignmentRequest,
): Promise<BoatCrewAssignment> {
    const response = await fetch(
        `${API_BASE_URL}/boats/${boatId}/crew`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        },
    );

    return readJsonResponse<BoatCrewAssignment>(
        response,
        "A személyzeti hozzárendelés létrehozása sikertelen.",
    );
}

export async function updateBoatCrewAssignment(
    boatId: string,
    assignmentId: string,
    request: UpdateBoatCrewAssignmentRequest,
): Promise<BoatCrewAssignment> {
    const response = await fetch(
        `${API_BASE_URL}/boats/${boatId}/crew/${assignmentId}`,
        {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        },
    );

    return readJsonResponse<BoatCrewAssignment>(
        response,
        "A személyzeti hozzárendelés módosítása sikertelen.",
    );
}

export async function deleteBoatCrewAssignment(
    boatId: string,
    assignmentId: string,
): Promise<void> {
    const response = await fetch(
        `${API_BASE_URL}/boats/${boatId}/crew/${assignmentId}`,
        {
            method: "DELETE",
        },
    );

    await ensureSuccess(
        response,
        "A személyzeti hozzárendelés törlése sikertelen.",
    );
}

function toBoatPayload(form: BoatForm) {
    return {
        name: form.name.trim(),
        registrationNumber:
            form.registrationNumber.trim(),
        callSign: form.callSign.trim(),
        status: form.status,
        notes: normalizeText(form.notes),
    };
}

function normalizeText(
    value: string | null | undefined,
): string | null {
    const normalizedValue = value?.trim();

    return normalizedValue
        ? normalizedValue
        : null;
}

async function readJsonResponse<T>(
    response: Response,
    fallbackMessage: string,
): Promise<T> {
    if (!response.ok) {
        throw new Error(
            await getResponseError(
                response,
                fallbackMessage,
            ),
        );
    }

    if (response.status === 204) {
        return undefined as T;
    }

    const responseText = await response.text();

    if (!responseText.trim()) {
        return undefined as T;
    }

    try {
        return JSON.parse(responseText) as T;
    } catch {
        throw new Error(
            "Az API nem megfelelő JSON választ adott vissza.",
        );
    }
}

async function ensureSuccess(
    response: Response,
    fallbackMessage: string,
): Promise<void> {
    if (!response.ok) {
        throw new Error(
            await getResponseError(
                response,
                fallbackMessage,
            ),
        );
    }
}

async function getResponseError(
    response: Response,
    fallbackMessage: string,
): Promise<string> {
    const responseText = await response.text();

    if (responseText.trim()) {
        try {
            const errorBody = JSON.parse(
                responseText,
            ) as {
                title?: string;
                detail?: string;
                message?: string;
                errors?: Record<string, string[]>;
            };

            if (errorBody.detail) {
                return errorBody.detail;
            }

            if (errorBody.message) {
                return errorBody.message;
            }

            if (errorBody.errors) {
                const validationMessages =
                    Object.values(
                        errorBody.errors,
                    ).flat();

                if (validationMessages.length > 0) {
                    return validationMessages.join(" ");
                }
            }

            if (errorBody.title) {
                return errorBody.title;
            }
        } catch {
            return responseText;
        }
    }

    return `${fallbackMessage} HTTP ${response.status}`;
}