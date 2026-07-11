import axios from "axios";
import type { User } from "../types/Users";

const API_URL = "http://localhost:5000/api/users";

export async function getUsers(): Promise<User[]> {
    const response = await axios.get<User[]>(API_URL);
    return response.data;
}

export async function createUser(
    user: Omit<User, "id" | "createdAt">
): Promise<User> {
    const response = await axios.post<User>(
        API_URL,
        user
    );

    return response.data;
}

export async function updateUser(
    user: User
): Promise<User> {
    const response = await axios.put<User>(
        `${API_URL}/${user.id}`,
        user
    );

    return response.data;
}

export async function deleteUser(
    id: string
): Promise<void> {
    await axios.delete(`${API_URL}/${id}`);
}