import { api } from "./client";

export type DefectStatus = "Reported" | "InProgress" | "Resolved";
export type DefectPriority = "Low" | "Normal" | "High" | "Critical";
export type BoatDefect = {
    id: string;
    boatId: string;
    reportedAt: string;
    reportedBy: string;
    description: string;
    priority: DefectPriority;
    status: DefectStatus;
    createdAt: string;
};

export async function getBoatDefects(boatId: string): Promise<BoatDefect[]> {
    const response = await api.get<BoatDefect[]>(`/boats/${boatId}/defects`);
    return response.data;
}
export async function createBoatDefect(boatId: string, data: Pick<BoatDefect, "reportedBy" | "description" | "priority">): Promise<BoatDefect> {
    const response = await api.post<BoatDefect>(`/boats/${boatId}/defects`, data);
    return response.data;
}
export async function updateBoatDefectStatus(boatId: string, defectId: string, status: DefectStatus): Promise<void> {
    await api.patch(`/boats/${boatId}/defects/${defectId}/status`, { status });
}
