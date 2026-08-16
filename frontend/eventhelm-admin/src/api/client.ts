import axios from "axios";

const apiUrl =
    import.meta.env.VITE_API_URL ??
    "/api";

export const api = axios.create({
    baseURL: apiUrl,
    timeout: 10000,
});

api.interceptors.response.use(
    (response) => response,
    (error: unknown) => {
        if (axios.isAxiosError(error)) {
            console.error("EventHelm API hiba:", {
                url: error.config?.url,
                method: error.config?.method,
                status: error.response?.status,
                message: error.message,
                response: error.response?.data,
            });
        } else {
            console.error(
                "Ismeretlen EventHelm API hiba:",
                error,
            );
        }

        return Promise.reject(error);
    },
);