import { Container, Typography } from "@mui/material";
import { OrderBoard } from "./OrderBoard";

export function BoardPage() {
  return (
    <Container maxWidth="sm" sx={{ py: 4 }}>
      <Typography variant="h3" component="h1" gutterBottom>
        Tabellone
      </Typography>
      <OrderBoard />
    </Container>
  );
}
