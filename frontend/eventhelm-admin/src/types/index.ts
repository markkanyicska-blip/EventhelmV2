export type EventItem = {
    id: string;
    name: string;
    status: string;
};

export type Boat = {
    id: string;
    callSign: string;
    name: string;
    type: string;
    status: string;
    capacity: number;
    notes?: string | null;
    createdAt?: string;
};

export type Official = {
    id: string;
    name: string;
    phone?: string | null;
    email?: string | null;
    role: string;
    status: string;
    createdAt?: string;
};

export type User = {
    id: string;
    name: string;
    email: string;
    phone?: string | null;
    role: string;
    isActive: boolean;
    createdAt?: string;
};
