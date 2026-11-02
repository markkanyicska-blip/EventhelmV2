import axios from "axios";
import { cleanup } from "@testing-library/react";
import { afterEach, beforeEach, vi } from "vitest";

beforeEach(() => {
    axios.defaults.adapter = () =>
        Promise.reject(new Error("Unexpected Axios request: install a test adapter"));
    vi.stubGlobal("fetch", () =>
        Promise.reject(new Error("Unexpected fetch request: install a test mock")));
});

afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    window.history.replaceState({}, "", "/");
});
