import { apiClient } from "@awesome-pizza/api-client";
import { screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, type Mock, vi } from "vitest";
import { renderWithProviders } from "../../test-utils";
import { formatTime } from "../shared/format";
import { OrderBoard } from "./OrderBoard";

vi.mock("@awesome-pizza/api-client", () => ({
  apiClient: { GET: vi.fn(), POST: vi.fn() },
}));

const get = apiClient.GET as unknown as Mock;

const orders = [
  {
    code: "001",
    status: "InPreparation",
    estimatedReadyAt: "2026-10-07T12:10:00Z",
    readyAt: null,
  },
  {
    code: "002",
    status: "Queued",
    estimatedReadyAt: "2026-10-07T12:30:00Z",
    readyAt: null,
  },
  {
    code: "003",
    status: "Queued",
    estimatedReadyAt: "2026-10-07T12:45:00Z",
    readyAt: null,
  },
];

function section(title: string) {
  return within(screen.getByText(title).closest(".MuiCard-root") as HTMLElement);
}

describe("OrderBoard", () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it("puts each order in the section matching its status", async () => {
    get.mockResolvedValue({ data: orders });
    renderWithProviders(<OrderBoard />);

    expect(await screen.findByText("001")).toBeTruthy();

    const inPreparation = section("In preparazione adesso");
    expect(inPreparation.getByText("001")).toBeTruthy();
    expect(inPreparation.queryByText("002")).toBeNull();

    const next = section("Successivi");
    expect(next.getByText("002")).toBeTruthy();
    expect(next.getByText("003")).toBeTruthy();
    expect(next.queryByText("001")).toBeNull();
  });

  it("shows the estimated time of each order", async () => {
    get.mockResolvedValue({ data: orders });
    renderWithProviders(<OrderBoard />);

    expect(await screen.findByText("001")).toBeTruthy();

    expect(
      section("In preparazione adesso").getByText(formatTime(orders[0].estimatedReadyAt)),
    ).toBeTruthy();
    expect(
      section("Successivi").getByText(formatTime(orders[2].estimatedReadyAt)),
    ).toBeTruthy();
  });

  it("keeps the queue in the order returned by the API", async () => {
    get.mockResolvedValue({ data: orders });
    renderWithProviders(<OrderBoard />);

    expect(await screen.findByText("002")).toBeTruthy();

    const codes = section("Successivi")
      .getAllByRole("row")
      .slice(1)
      .map((row) => within(row).getAllByRole("cell")[0].textContent);

    expect(codes).toEqual(["002", "003"]);
  });

  it("shows an empty message in both sections when there are no orders", async () => {
    get.mockResolvedValue({ data: [] });
    renderWithProviders(<OrderBoard />);

    expect(await screen.findAllByText("Nessun ordine.")).toHaveLength(2);
  });
});
