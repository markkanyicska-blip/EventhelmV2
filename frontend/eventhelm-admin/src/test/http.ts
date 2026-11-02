import { AxiosError, AxiosHeaders } from "axios";
import type { AxiosResponse, InternalAxiosRequestConfig } from "axios";

export function respond(
    config: InternalAxiosRequestConfig,
    status: number,
    data: unknown = {},
): Promise<AxiosResponse> {
    const response: AxiosResponse = {
        config,
        status,
        statusText: String(status),
        headers: new AxiosHeaders(),
        data,
    };

    return status >= 400
        ? Promise.reject(new AxiosError(
            `Synthetic HTTP ${status}`,
            AxiosError.ERR_BAD_RESPONSE,
            config,
            undefined,
            response,
        ))
        : Promise.resolve(response);
}
