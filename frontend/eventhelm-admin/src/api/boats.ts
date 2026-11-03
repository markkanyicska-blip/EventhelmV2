import type { Boat } from "../types";
import { api } from "./client";

export async function getBoats(): Promise<Boat[]> {
    const response = await api.get<Boat[]>("/boats");
    return response.data;
}

export async function createBoat(
    payload: Omit<Boat, "id" | "createdAt">,
): Promise<Boat> {
    const response = await api.post<Boat>("/boats", payload);
    return response.data;
}

export async function updateBoat(item: Boat): Promise<Boat> {
    const response = await api.put<Boat>(
        `/boats/${item.id}`,
        item,
    );
    return response.data;
}

export async function deleteBoat(id: string): Promise<void> {
    await api.delete(`/boats/${id}`);
}
