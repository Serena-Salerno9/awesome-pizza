import { apiClient } from "@awesome-pizza/api-client";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, type Mock, vi } from "vitest";
import { renderWithProviders } from "../../test-utils";
import { CustomerPage } from "./CustomerPage";

vi.mock("@awesome-pizza/api-client", () => ({
  apiClient: { GET: vi.fn(), POST: vi.fn() },
}));

const get = apiClient.GET as unknown as Mock;
const post = apiClient.POST as unknown as Mock;

const margherita = {
  id: "p1",
  name: "Margherita",
  description: "Pomodoro e mozzarella",
  price: 6,
};
const created = {
  code: "001",
  status: "Queued",
  estimatedReadyAt: "2026-10-07T12:10:00Z",
  readyAt: null,
};

async function findPlusButton() {
  const plus = (await screen.findByRole("button", {
    name: "+",
  })) as HTMLButtonElement;
  await waitFor(() => expect(plus.disabled).toBe(false));

  return plus;
}

describe("CustomerPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    get.mockImplementation(async (path: string) =>
      path === "/api/v1/orders/limits"
        ? { data: { maxPizzasPerOrder: 2 } }
        : { data: [margherita] },
    );
  });

  it("shows the menu", async () => {
    renderWithProviders(<CustomerPage />);

    expect(await screen.findByText("Margherita")).toBeTruthy();
    expect(screen.getByText("6.00 €")).toBeTruthy();
  });

  it("stops adding pizzas at the limit returned by the API", async () => {
    renderWithProviders(<CustomerPage />);
    const plus = await findPlusButton();

    fireEvent.click(plus);
    fireEvent.click(plus);

    expect(plus.disabled).toBe(true);
  });

  it("creates the order and shows its code", async () => {
    post.mockResolvedValue({ data: created });
    renderWithProviders(<CustomerPage />);
    const plus = await findPlusButton();

    fireEvent.click(plus);
    fireEvent.click(plus);
    fireEvent.click(screen.getByRole("button", { name: "Ordina 2 · 12.00 €" }));

    expect(await screen.findByText("001")).toBeTruthy();
    expect(post).toHaveBeenCalledWith("/api/v1/orders", {
      body: { items: [{ pizzaId: "p1", quantity: 2 }] },
    });
    expect(screen.getByRole("button", { name: "Scegli le pizze" })).toBeTruthy();
  });

  it("shows the error returned by the API", async () => {
    post.mockResolvedValue({ error: { detail: "Pizza non disponibile." } });
    renderWithProviders(<CustomerPage />);
    const plus = await findPlusButton();

    fireEvent.click(plus);
    fireEvent.click(screen.getByRole("button", { name: "Ordina 1 · 6.00 €" }));

    expect(await screen.findByText("Pizza non disponibile.")).toBeTruthy();
  });
});
