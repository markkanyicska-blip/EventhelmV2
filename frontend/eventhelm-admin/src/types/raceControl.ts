export type RaceStatus =
    | "Preparation"
    | "Starting"
    | "InProgress"
    | "Finished";

export type RaceUnitStatus =
    | "Ready"
    | "OnWater"
    | "Standby"
    | "Offline";

export type WeatherState = {
    windSpeedKnots: number;
    gustSpeedKnots: number;
    windDirectionDegrees: number;
    pressureHpa: number;
    waterTemperatureCelsius: number;
};

export type RadioState = {
    channel: string;
    isOnline: boolean;
    activeCalls: number;
    lastCallSign?: string | null;
    lastMessage?: string | null;
};

export type RaceUnitState = {
    id: string;
    name: string;
    callSign: string;
    crew: number;
    status: RaceUnitStatus;
    latitude?: number | null;
    longitude?: number | null;
    lastSeenAt?: string | null;
};

export type RaceTimelineEntry = {
    id: string;
    timestamp: string;
    type: string;
    message: string;
};

export type RaceControlState = {
    id: string;
    raceName: string;
    location: string;
    status: RaceStatus;
    activeFlag: string;
    updatedAt: string;
    weather: WeatherState;
    radio: RadioState;
    units: RaceUnitState[];
    timeline: RaceTimelineEntry[];
};
