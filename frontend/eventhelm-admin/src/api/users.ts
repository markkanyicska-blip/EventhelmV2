import type { User } from "../types";
import { api } from "./client";

export async function getUsers(): Promise<User[]> {
    const response = await api.get<User[]>("/users");
    return response.data;
}

export async function createUser(
    payload: Omit<User, "id" | "createdAt">,
): Promise<User> {
    const response = await api.post<User>("/users", payload);
    return response.data;
}

export async function updateUser(item: User): Promise<User> {
    const response = await api.put<User>(
        `/users/${item.id}`,
        item,
    );
    return response.data;
}

export async function deleteUser(id: string): Promise<void> {
    await api.delete(`/users/${id}`);
}
