import { api } from "./client";

export type FileUploadResponse = {
    url: string;
    originalFileName: string;
};

export async function uploadOfficialDocument(
    file: File,
): Promise<FileUploadResponse> {
    const formData = new FormData();

    formData.append("file", file);

    const response = await api.post<FileUploadResponse>(
        "/uploads/official-document",
        formData,
    );

    return response.data;
}

export async function deleteOfficialDocument(
    url: string,
): Promise<void> {
    await api.delete("/uploads/official-document", {
        params: {
            url,
        },
    });
}