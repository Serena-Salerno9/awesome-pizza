import { apiClient } from "@awesome-pizza/api-client";
import { fireEvent, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, type Mock, vi } from "vitest";
import { renderWithProviders } from "../test-utils";
import App from "./app";

vi.mock("@awesome-pizza/api-client", () => ({
  apiClient: { GET: vi.fn(), POST: vi.fn() },
}));

describe("App", () => {
  beforeEach(() => {
    window.history.pushState({}, "", "/");
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

  it("links to the three pages", () => {
    renderWithProviders(<App />);

    const hrefs = ["Ordina", "Tabellone", "Cucina"].map((name) =>
      screen.getByRole("link", { name }).getAttribute("href"),
    );

    expect(hrefs).toEqual(["/", "/tabellone", "/kitchen"]);
  });

  it("opens the board and the kitchen from the navigation", async () => {
    renderWithProviders(<App />);

    fireEvent.click(screen.getByRole("link", { name: "Tabellone" }));
    expect(await screen.findByRole("heading", { name: "Tabellone" })).toBeTruthy();

    fireEvent.click(screen.getByRole("link", { name: "Cucina" }));
    expect(await screen.findByRole("heading", { name: "Cucina" })).toBeTruthy();
  });
});
