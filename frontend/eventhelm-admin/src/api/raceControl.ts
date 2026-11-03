import { api } from "./client";
import type {
    RaceControlState,
    RaceStatus,
    RaceUnitStatus,
} from "../types/raceControl";

export async function getRaceControlState():
    Promise<RaceControlState> {
    const response =
        await api.get<RaceControlState>(
            "/race-control",
        );

    return response.data;
}

export async function updateRaceStatus(
    status: RaceStatus,
): Promise<RaceControlState> {
    const response =
        await api.put<RaceControlState>(
            "/race-control/status",
            { status },
        );

    return response.data;
}

export async function updateRaceFlag(
    flag: string,
): Promise<RaceControlState> {
    const response =
        await api.put<RaceControlState>(
            "/race-control/flag",
            { flag },
        );

    return response.data;
}

export async function updateRaceUnit(
    id: string,
    status: RaceUnitStatus,
    latitude?: number | null,
    longitude?: number | null,
): Promise<RaceControlState> {
    const response =
        await api.put<RaceControlState>(
            `/race-control/units/${id}`,
            {
                status,
                latitude: latitude ?? null,
                longitude: longitude ?? null,
            },
        );

    return response.data;
}

export async function addRaceTimelineEntry(
    type: string,
    message: string,
): Promise<RaceControlState> {
    const response =
        await api.post<RaceControlState>(
            "/race-control/timeline",
            {
                type,
                message,
            },
        );

    return response.data;
}
