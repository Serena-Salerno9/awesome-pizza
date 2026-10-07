import { apiClient, type Schemas } from "@awesome-pizza/api-client";
import {
  Alert,
  Box,
  Button,
  Card,
  Chip,
  Container,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { formatTime } from "../shared/format";

type Batch = Schemas["KitchenBatchResponse"];

function BatchCard({
  batch,
  children,
}: {
  batch: Batch;
  children?: ReactNode;
}) {
  const pizzas = batch.lines.reduce(
    (sum, line) => sum + Number(line.quantity),
    0,
  );

  return (
    <Card sx={{ p: 3 }}>
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          mb: 1,
        }}
      >
        <Typography variant="h6">{pizzas} pizze</Typography>
        <Chip
          label={batch.batchId ? "Assegnata" : "Proposta"}
          color={batch.batchId ? "primary" : "default"}
        />
      </Box>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        Inizio {formatTime(batch.estimatedStartAt)} · in forno{" "}
        {formatTime(batch.estimatedBakeStartAt)} · esce{" "}
        {formatTime(batch.estimatedReadyAt)}
      </Typography>
      {batch.lines.map((line, index) => (
        <Typography key={index}>
          {line.orderCode} · {Number(line.quantity)}× {line.pizzaName}
        </Typography>
      ))}
      {children}
    </Card>
  );
}

export function KitchenPage() {
  const queryClient = useQueryClient();
  const refresh = () =>
    queryClient.invalidateQueries({ queryKey: ["kitchen"] });

  const kitchen = useQuery({
    queryKey: ["kitchen"],
    queryFn: async () => {
      const { data } = await apiClient.GET("/api/v1/kitchen");

      return data?.batches ?? [];
    },
    refetchInterval: 3000,
  });

  const takeCharge = useMutation({
    mutationFn: async (id: string) => {
      const { error } = await apiClient.POST(
        "/api/v1/kitchen/batches/{id}/take-charge",
        { params: { path: { id } } },
      );
      if (error) {
        throw new Error(error.detail ?? error.title ?? "Azione non riuscita");
      }
    },
    onSettled: refresh,
  });

  const ready = useMutation({
    mutationFn: async (id: string) => {
      const { error } = await apiClient.POST(
        "/api/v1/kitchen/batches/{id}/ready",
        { params: { path: { id } } },
      );
      if (error) {
        throw new Error(error.detail ?? error.title ?? "Azione non riuscita");
      }
    },
    onSettled: () => {
      void refresh();
      void queryClient.invalidateQueries({ queryKey: ["orders"] });
    },
  });

  const batches = kitchen.data ?? [];
  const assigned = batches.filter((batch) => batch.batchId);
  const upcoming = batches.filter((batch) => !batch.batchId);
  const busy = takeCharge.isPending || ready.isPending;
  const actionError = takeCharge.error ?? ready.error;

  return (
    <Container maxWidth="sm" sx={{ py: 4 }}>
      <Typography variant="h3" component="h1" gutterBottom>
        Cucina
      </Typography>

      {kitchen.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Impossibile caricare la cucina
        </Alert>
      )}
      {actionError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {actionError.message}
        </Alert>
      )}

      <Typography variant="h5" gutterBottom>
        In corso
      </Typography>
      <Box sx={{ display: "grid", gap: 2, mb: 4 }}>
        {assigned.length === 0 && (
          <Typography color="text.secondary">
            Nessuna infornata assegnata.
          </Typography>
        )}
        {assigned.map((batch) => (
          <BatchCard key={batch.batchId} batch={batch}>
            <Box sx={{ display: "flex", gap: 1, mt: 2 }}>
              <Button
                variant="outlined"
                disabled={busy}
                onClick={() => takeCharge.mutate(batch.batchId as string)}
              >
                Presa in carico
              </Button>
              <Button
                variant="contained"
                disabled={busy}
                onClick={() => ready.mutate(batch.batchId as string)}
              >
                Pronta
              </Button>
            </Box>
          </BatchCard>
        ))}
      </Box>

      <Typography variant="h5" gutterBottom>
        Prossime
      </Typography>
      <Box sx={{ display: "grid", gap: 2 }}>
        {upcoming.length === 0 && (
          <Typography color="text.secondary">
            Nessuna infornata in coda.
          </Typography>
        )}
        {upcoming.map((batch, index) => (
          <BatchCard key={index} batch={batch} />
        ))}
      </Box>
    </Container>
  );
}
