import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api, authenticatedFetch, authenticationExpiredEvent } from "./client";
import { respond } from "../test/http";

describe("authenticated HTTP clients", () => {
    const expired = vi.fn();

    beforeEach(() => {
        window.addEventListener(authenticationExpiredEvent, expired);
        vi.spyOn(console, "error").mockImplementation(() => {});
    });

    afterEach(() => {
        window.removeEventListener(authenticationExpiredEvent, expired);
        api.defaults.adapter = axios.defaults.adapter;
    });

    it("includes credentials on the shared Axios client", () => {
        expect(api.defaults.withCredentials).toBe(true);
        expect(api.defaults.baseURL).toBe("/api");
    });

    it.each([401, 403, 500])(
        "Axios preserves HTTP %s errors and expires only on 401",
        async (status) => {
            api.defaults.adapter = (config) => respond(config, status);

            await expect(api.get("/synthetic")).rejects.toMatchObject({
                response: { status },
            });
            expect(expired).toHaveBeenCalledTimes(status === 401 ? 1 : 0);
            expect(console.error).toHaveBeenCalledOnce();
        },
    );

    it.each([200, 401, 403, 500])(
        "fetch preserves HTTP %s responses and expires only on 401",
        async (status) => {
            const response = new Response(null, { status });
            const fetchMock = vi.fn().mockResolvedValue(response);
            vi.stubGlobal("fetch", fetchMock);

            await expect(authenticatedFetch("/api/synthetic", {
                method: "POST",
                credentials: "omit",
                headers: { "X-Test": "synthetic" },
            })).resolves.toBe(response);

            expect(fetchMock).toHaveBeenCalledWith("/api/synthetic", {
                method: "POST",
                credentials: "include",
                headers: { "X-Test": "synthetic" },
            });
            expect(expired).toHaveBeenCalledTimes(status === 401 ? 1 : 0);
        },
    );

    it("does not treat a network failure as session expiry", async () => {
        const error = new Error("Synthetic network failure");
        vi.stubGlobal("fetch", vi.fn().mockRejectedValue(error));
        api.defaults.adapter = () => Promise.reject(error);

        await expect(authenticatedFetch("/api/synthetic")).rejects.toBe(error);
        await expect(api.get("/synthetic")).rejects.toBe(error);
        expect(expired).not.toHaveBeenCalled();
    });
});
