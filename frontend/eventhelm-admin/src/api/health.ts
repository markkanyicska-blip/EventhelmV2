import { api } from "./client";

export type HealthStatus = {
    status: string;
};

export async function getHealth(): Promise<HealthStatus> {
    const response = await api.get<HealthStatus>(
        "/health",
    );

    return response.data;
}
