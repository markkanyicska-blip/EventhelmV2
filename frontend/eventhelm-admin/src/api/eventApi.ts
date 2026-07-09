import axios from "axios";
import { Event } from "../types/Event";

const api = axios.create({
    baseURL: "http://localhost:5105/api",
});

export const getEvents = async () => {
    const res = await api.get<Event[]>("/events");
    return res.data;
};

export const createEvent = async (name: string, status: string) => {
    await api.post("/events", {
        name,
        status,
    });
};