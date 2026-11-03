import axios from "axios";
import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import App from "./App";
import { api, authenticatedFetch, authenticationExpiredEvent } from "./api/client";
import { respond } from "./test/http";

vi.mock("./pages/BoatDetailsPage", () => ({ default: () => null }));
vi.mock("./pages/BoatsPage", () => ({ default: () => null }));
vi.mock("./pages/EventDetailsPage", () => ({ default: () => null }));
vi.mock("./pages/EventsPage", () => ({ default: () => null }));
vi.mock("./pages/MapPage", () => ({ default: () => null }));
vi.mock("./pages/OfficialProfilePage", () => ({ default: () => null }));
vi.mock("./pages/OfficialsPage", () => ({ default: () => null }));
vi.mock("./pages/OperationsCenter", () => ({ default: () => <h1>Synthetic dashboard</h1> }));
vi.mock("./pages/UsersPage", () => ({ default: () => null }));

beforeEach(() => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    api.defaults.adapter = (config) => {
        if (config.url === "/access/status") {
            return respond(config, 200, { authenticated: true });
        }
        return Promise.reject(new Error(`Unexpected request: ${config.url}`));
    };
});

afterEach(() => {
    api.defaults.adapter = axios.defaults.adapter;
});

async function renderAuthenticated() {
    render(<App />);
    await screen.findByRole("heading", { name: "Synthetic dashboard" });
}

it("returns to login on an expired-session event", async () => {
    await renderAuthenticated();

    act(() => window.dispatchEvent(new Event(authenticationExpiredEvent)));

    expect(screen.getByRole("heading", { name: "Bejelentkezés" })).toBeTruthy();
    expect(screen.queryByRole("heading", { name: "Synthetic dashboard" })).toBeNull();
});

it.each(["axios", "fetch"])("returns to login after an HTTP 401 through %s", async (transport) => {
    await renderAuthenticated();
    api.defaults.adapter = (config) => respond(config, 401);
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 401 })));

    await act(async () => {
        if (transport === "axios") {
            await expect(api.get("/synthetic")).rejects.toMatchObject({ response: { status: 401 } });
        } else {
            expect((await authenticatedFetch("/api/synthetic")).status).toBe(401);
        }
    });

    expect(screen.getByRole("heading", { name: "Bejelentkezés" })).toBeTruthy();
});

it.each(["axios", "fetch"])("keeps the session after an HTTP 403 through %s", async (transport) => {
    await renderAuthenticated();
    api.defaults.adapter = (config) => respond(config, 403);
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 403 })));

    await act(async () => {
        if (transport === "axios") {
            await expect(api.get("/synthetic")).rejects.toMatchObject({ response: { status: 403 } });
        } else {
            expect((await authenticatedFetch("/api/synthetic")).status).toBe(403);
        }
    });

    expect(screen.getByRole("heading", { name: "Synthetic dashboard" })).toBeTruthy();
    expect(screen.queryByRole("heading", { name: "Bejelentkezés" })).toBeNull();
});

it("shows an availability error rather than login when status fails", async () => {
    api.defaults.adapter = (config) => respond(config, 503);
    render(<App />);

    await screen.findByRole("alert");
    expect(screen.queryByRole("heading", { name: "Bejelentkezés" })).toBeNull();
});

it("logs out through the existing endpoint", async () => {
    await renderAuthenticated();
    api.defaults.adapter = (config) => {
        expect(config.url).toBe("/access/logout");
        expect(config.method).toBe("post");
        return respond(config, 200, { authenticated: false });
    };

    fireEvent.click(screen.getByRole("button", { name: "Kijelentkezés" }));

    await screen.findByRole("heading", { name: "Bejelentkezés" });
});
