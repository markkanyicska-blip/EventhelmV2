import type { Official } from "../types";
import { api } from "./client";

export async function getOfficials(): Promise<Official[]> {
    const response = await api.get<Official[]>("/officials");
    return response.data;
}

export async function createOfficial(
    payload: Omit<Official, "id" | "createdAt">,
): Promise<Official> {
    const response = await api.post<Official>(
        "/officials",
        payload,
    );
    return response.data;
}

export async function updateOfficial(
    item: Official,
): Promise<Official> {
    const response = await api.put<Official>(
        `/officials/${item.id}`,
        item,
    );
    return response.data;
}

export async function deleteOfficial(id: string): Promise<void> {
    await api.delete(`/officials/${id}`);
}
