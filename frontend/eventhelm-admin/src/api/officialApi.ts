import axios from "axios";
import type { Official } from "../types/Official";

const API_URL = "http://localhost:5000/api/officials";

export async function getOfficials(): Promise<Official[]> {
    const response = await axios.get<Official[]>(API_URL);
    return response.data;
}

export async function createOfficial(
    official: Omit<Official, "id" | "createdAt">,
): Promise<Official> {
    const response = await axios.post<Official>(
        API_URL,
        official,
    );

    return response.data;
}

export async function updateOfficial(
    official: Official,
): Promise<Official> {
    const response = await axios.put<Official>(
        `${API_URL}/${official.id}`,
        official,
    );

    return response.data;
}

export async function deleteOfficial(
    id: string,
): Promise<void> {
    await axios.delete(`${API_URL}/${id}`);
}