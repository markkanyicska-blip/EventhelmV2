import { api } from "./client";

import type {
    OfficialPayload,
    OfficialProfile,
    OfficialProfileResponse,
} from "../types/officialProfiles";

export async function getOfficialProfiles(): Promise<OfficialProfile[]> {
    const response = await api.get<OfficialProfile[]>("/officials");

    return response.data;
}

export async function getOfficialProfile(
    id: string,
): Promise<OfficialProfileResponse> {
    const response = await api.get<OfficialProfile>(
        `/officials/${id}`,
    );

    return {
        official: response.data,
        totalCompetitions: 0,
        currentYearCompetitions: 0,
        roleStatistics: [],
        competitionHistory: [],
    };
}

export async function createOfficialProfile(
    payload: OfficialPayload,
): Promise<OfficialProfile> {
    const response = await api.post<OfficialProfile>(
        "/officials",
        payload,
    );

    return response.data;
}

export async function updateOfficialProfile(
    id: string,
    payload: OfficialPayload,
): Promise<OfficialProfile> {
    const response = await api.put<OfficialProfile>(
        `/officials/${id}`,
        payload,
    );

    return response.data;
}

export async function deleteOfficialProfile(
    id: string,
): Promise<void> {
    await api.delete(`/officials/${id}`);
}