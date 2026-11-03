import { api } from "./client";

export type AssignmentImportRow = {
    rowNumber: number;
    groupName: string;
    subgroupName: string;
    name: string;
    position: string;
    task: string;
    boatName: string;
    phone: string;
    email: string;
    importKey: string;
    isValid: boolean;
    officialExists: boolean;
    boatExists: boolean;
    assignmentExists: boolean;
    errors: string[];
};

export type AssignmentImportPreview = {
    fileName: string;
    totalRows: number;
    validRows: number;
    invalidRows: number;
    uniqueOfficials: number;
    newOfficials: number;
    existingOfficials: number;
    newBoats: number;
    groups: number;
    assignmentsToCreate: number;
    rows: AssignmentImportRow[];
    warnings: string[];
};

export type AssignmentImportResult = {
    batchId: string;
    importedRows: number;
    skippedRows: number;
    createdOfficials: number;
    updatedOfficials: number;
    createdBoats: number;
    createdAssignments: number;
    updatedAssignments: number;
    removedAssignments: number;
    groups: number;
    warnings: string[];
};

export async function previewEventAssignmentImport(
    eventId: string,
    file: File,
): Promise<AssignmentImportPreview> {
    const formData = new FormData();
    formData.append("file", file);

    const response = await api.post<AssignmentImportPreview>(
        `/events/${eventId}/assignment-import/preview`,
        formData,
    );

    return response.data;
}

export async function commitEventAssignmentImport(
    eventId: string,
    preview: AssignmentImportPreview,
    replacePreviousImport: boolean,
): Promise<AssignmentImportResult> {
    const response = await api.post<AssignmentImportResult>(
        `/events/${eventId}/assignment-import/commit`,
        {
            fileName: preview.fileName,
            replacePreviousImport,
            rows: preview.rows,
        },
    );

    return response.data;
}
