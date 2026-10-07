import { apiClient } from "@awesome-pizza/api-client";
import { beforeEach, describe, expect, it, type Mock, vi } from "vitest";
import { renderWithProviders } from "../test-utils";
import App from "./app";

vi.mock("@awesome-pizza/api-client", () => ({
  apiClient: { GET: vi.fn(), POST: vi.fn() },
}));

describe("App", () => {
  beforeEach(() => {
    (apiClient.GET as unknown as Mock).mockResolvedValue({ data: [] });
  });

  it("should render successfully", () => {
    const { baseElement } = renderWithProviders(<App />);
    expect(baseElement).toBeTruthy();
  });

  it("should render the home title", () => {
    const { getByRole } = renderWithProviders(<App />);
    expect(getByRole("heading", { name: "Awesome Pizza" })).toBeTruthy();
  });
});
