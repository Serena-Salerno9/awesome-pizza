import { apiClient, type Schemas } from "@awesome-pizza/api-client";
import {
  Box,
  Card,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { formatTime } from "../shared/format";

type Order = Schemas["OrderResponse"];

function OrderSection({ title, orders }: { title: string; orders: Order[] }) {
  return (
    <Card sx={{ p: 3 }}>
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          mb: 1,
        }}
      >
        <Typography variant="h5">{title}</Typography>
        <Chip label={orders.length} />
      </Box>
      {orders.length === 0 ? (
        <Typography color="text.secondary">Nessun ordine.</Typography>
      ) : (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Ordine</TableCell>
              <TableCell align="right">Pronto verso le</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {orders.map((order) => (
              <TableRow key={order.code}>
                <TableCell>{order.code}</TableCell>
                <TableCell align="right">
                  {formatTime(order.estimatedReadyAt)}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Card>
  );
}

export function OrderBoard() {
  const orders = useQuery({
    queryKey: ["orders"],
    queryFn: async () => {
      const { data } = await apiClient.GET("/api/v1/orders");

      return data ?? [];
    },
    refetchInterval: 5000,
  });

  const all = orders.data ?? [];

  return (
    <Box sx={{ display: "grid", gap: 2 }}>
      <OrderSection
        title="In preparazione adesso"
        orders={all.filter((order) => order.status === "InPreparation")}
      />
      <OrderSection
        title="Successivi"
        orders={all.filter((order) => order.status === "Queued")}
      />
    </Box>
  );
}
