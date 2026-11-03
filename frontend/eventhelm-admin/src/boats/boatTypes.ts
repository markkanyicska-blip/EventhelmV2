export type BoatStatus =
    | "Active"
    | "Service"
    | "Unavailable";

export type Boat = {
    id: string;
    name: string;
    registrationNumber: string;
    callSign: string;
    type?: string | null;
    capacity?: number;
    manufacturer?: string | null;
    engine?: string | null;
    enginePower?: number | null;
    fuelType?: string | null;
    status: BoatStatus | string;
    notes?: string | null;
    createdAt?: string;
};

export type BoatForm = {
    name: string;
    registrationNumber: string;
    callSign: string;
    status: BoatStatus;
    notes: string;
};

export type Official = {
    id: string;
    name: string;
    email?: string | null;
    phone?: string | null;
    profileImageUrl?: string | null;
};

export type BoatCrewAssignment = {
    id: string;
    boatId: string;
    officialId: string;
    role: string;
    isCaptain: boolean;
    fromDate?: string | null;
    toDate?: string | null;
    notes?: string | null;
    createdAt: string;
    officialName: string;
    officialPhone?: string | null;
    officialEmail?: string | null;
    officialProfileImageUrl?: string | null;
};

export type CreateBoatCrewAssignmentRequest = {
    officialId: string;
    role: string;
    isCaptain: boolean;
    fromDate?: string | null;
    toDate?: string | null;
    notes?: string | null;
};

export type UpdateBoatCrewAssignmentRequest = {
    role: string;
    isCaptain: boolean;
    fromDate?: string | null;
    toDate?: string | null;
    notes?: string | null;
};

export type CrewForm = {
    officialId: string;
    role: string;
    isCaptain: boolean;
    fromDate: string;
    toDate: string;
    notes: string;
};

export const emptyBoatForm: BoatForm = {
    name: "",
    registrationNumber: "",
    callSign: "",
    status: "Active",
    notes: "",
};

export const emptyCrewForm: CrewForm = {
    officialId: "",
    role: "Rendező",
    isCaptain: false,
    fromDate: "",
    toDate: "",
    notes: "",
};