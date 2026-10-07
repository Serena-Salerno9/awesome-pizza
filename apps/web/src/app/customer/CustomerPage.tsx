import { apiClient } from "@awesome-pizza/api-client";
import { Alert, Box, Button, Card, Container, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { formatTime } from "../shared/format";

type OrderItem = { pizzaId: string; quantity: number };

export function CustomerPage() {
  const queryClient = useQueryClient();
  const [quantities, setQuantities] = useState<Record<string, number>>({});

  const menu = useQuery({
    queryKey: ["menu"],
    queryFn: async () => {
      const { data } = await apiClient.GET("/api/v1/menu");

      return data ?? [];
    },
  });

  const limits = useQuery({
    queryKey: ["order-limits"],
    queryFn: async () => {
      const { data } = await apiClient.GET("/api/v1/orders/limits");

      return data;
    },
    staleTime: Infinity,
  });
  const maxPizzas = Number(limits.data?.maxPizzasPerOrder ?? 0);

  const createOrder = useMutation({
    mutationFn: async (items: OrderItem[]) => {
      const { data, error } = await apiClient.POST("/api/v1/orders", {
        body: { items },
      });
      if (error) {
        throw new Error(
          error.detail ?? error.title ?? "Impossibile creare l'ordine",
        );
      }

      return data;
    },
    onSuccess: () => {
      setQuantities({});
      void queryClient.invalidateQueries({ queryKey: ["orders"] });
    },
  });

  const pizzas = menu.data ?? [];
  const items = pizzas.flatMap((pizza) => {
    const quantity = quantities[pizza.id] ?? 0;

    return quantity > 0 ? [{ pizzaId: pizza.id, quantity }] : [];
  });
  const totalPizzas = items.reduce((sum, item) => sum + item.quantity, 0);
  const total = pizzas.reduce(
    (sum, pizza) => sum + (quantities[pizza.id] ?? 0) * Number(pizza.price),
    0,
  );

  const change = (pizzaId: string, delta: number) =>
    setQuantities((current) => ({
      ...current,
      [pizzaId]: Math.max(0, (current[pizzaId] ?? 0) + delta),
    }));

  return (
    <Container maxWidth="sm" sx={{ py: 4 }}>
      <Typography variant="h3" component="h1" gutterBottom>
        Awesome Pizza
      </Typography>

      {createOrder.data && (
        <Card sx={{ p: 3, mb: 3, bgcolor: "#f3f3f3" }}>
          <Typography variant="h6">Ordine ricevuto</Typography>
          <Typography variant="h2" component="p">
            {createOrder.data.code}
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 2 }}>
            Pronto verso le {formatTime(createOrder.data.estimatedReadyAt)}
          </Typography>
          <Button variant="outlined" onClick={() => createOrder.reset()}>
            Nuovo ordine
          </Button>
        </Card>
      )}

      {menu.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Impossibile caricare il menu
        </Alert>
      )}
      {createOrder.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {createOrder.error.message}
        </Alert>
      )}

      <Box sx={{ display: "grid", gap: 2 }}>
        {pizzas.map((pizza) => {
          const quantity = quantities[pizza.id] ?? 0;

          return (
            <Card
              key={pizza.id}
              sx={{ p: 2, display: "flex", alignItems: "center", gap: 2 }}
            >
              <Box sx={{ flex: 1 }}>
                <Typography variant="h6">{pizza.name}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {pizza.description}
                </Typography>
                <Typography variant="body2">
                  {Number(pizza.price).toFixed(2)} €
                </Typography>
              </Box>
              <Button
                variant="outlined"
                sx={{ minWidth: 44, px: 0 }}
                disabled={quantity === 0}
                onClick={() => change(pizza.id, -1)}
              >
                −
              </Button>
              <Typography sx={{ minWidth: 24, textAlign: "center" }}>
                {quantity}
              </Typography>
              <Button
                variant="outlined"
                sx={{ minWidth: 44, px: 0 }}
                disabled={totalPizzas >= maxPizzas}
                onClick={() => change(pizza.id, 1)}
              >
                +
              </Button>
            </Card>
          );
        })}
      </Box>

      <Button
        variant="contained"
        fullWidth
        sx={{ mt: 3 }}
        disabled={totalPizzas === 0 || createOrder.isPending}
        onClick={() => createOrder.mutate(items)}
      >
        {totalPizzas === 0
          ? "Scegli le pizze"
          : `Ordina ${totalPizzas} · ${total.toFixed(2)} €`}
      </Button>
    </Container>
  );
}
