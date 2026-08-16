export type Event = {
    id: string;
    name: string;
    status: string;

    organizingClub?: string | null;
    hostingClub?: string | null;

    startDate?: string | null;
    endDate?: string | null;

    location?: string | null;
    waterArea?: string | null;

    raceOfficer?: string | null;
    juryChair?: string | null;
    technicalCommitteeChair?: string | null;

    raceOffice?: string | null;
    vhfChannel?: string | null;
    trunkGroup?: string | null;

    notes?: string | null;
};