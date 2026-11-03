export type OfficialProfile = {
    id: string;
    name: string;
    phone?: string | null;
    email?: string | null;
    role?: string | null;
    status: string;
    club?: string | null;
    shirtSize?: string | null;
    profileImageUrl?: string | null;

    organizerQualification?: string | null;
    judgeQualification?: string | null;
    measurerQualification?: string | null;

    isCompetitionAdministrator: boolean;
    isResultScorer: boolean;

    qualificationValidUntil?: string | null;
    qualificationDocumentUrl?: string | null;

    boatLicenseType?: string | null;
    boatLicenseValidUntil?: string | null;
    boatLicenseDocumentUrl?: string | null;

    foodAllergies?: string | null;
    drugAllergies?: string | null;
    healthNotes?: string | null;

    emergencyContactName?: string | null;
    emergencyContactPhone?: string | null;
    emergencyContactRelation?: string | null;

    languages?: string | null;
    otherDocumentUrl?: string | null;

    createdAt: string;
    canDriveMotorboat: boolean;
};

export type OfficialCompetitionHistoryItem = {
    eventId: string;
    eventName: string;
    startDate?: string | null;
    location?: string | null;
    role: string;
    attendanceStatus: string;
};

export type OfficialRoleStatistic = {
    role: string;
    count: number;
};

export type OfficialProfileResponse = {
    official: OfficialProfile;
    totalCompetitions: number;
    currentYearCompetitions: number;
    roleStatistics: OfficialRoleStatistic[];
    competitionHistory: OfficialCompetitionHistoryItem[];
};

export type OfficialPayload = Omit<
    OfficialProfile,
    "id" | "createdAt" | "canDriveMotorboat"
>;