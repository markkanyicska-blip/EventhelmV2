export type AttendanceStatus =
    | "Planned"
    | "Present"
    | "Absent"
    | "Standby";

export type EventOfficialAssignment = {
    id: string;
    eventId: string;
    officialId: string;
    officialName: string;
    officialPhone?: string | null;
    officialEmail?: string | null;
    role: string;
    trunkRadioNumber?: string | null;
    vhfRadioNumber?: string | null;
    boatId?: string | null;
    boatName?: string | null;
    boatCallSign?: string | null;
    attendanceStatus: AttendanceStatus;
    notes?: string | null;
    createdAt: string;
    updatedAt: string;
};

export type EventOfficialAssignmentPayload = {
    officialId: string;
    role: string;
    trunkRadioNumber?: string | null;
    vhfRadioNumber?: string | null;
    boatId?: string | null;
    attendanceStatus: AttendanceStatus;
    notes?: string | null;
};
