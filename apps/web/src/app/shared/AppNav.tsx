import { Box, Button } from "@mui/material";
import { NavLink } from "react-router-dom";

const links = [
  { to: "/", label: "Ordina" },
  { to: "/tabellone", label: "Tabellone" },
  { to: "/kitchen", label: "Cucina" },
];

export function AppNav() {
  return (
    <Box
      component="nav"
      sx={{
        position: "sticky",
        top: 16,
        zIndex: 10,
        display: "flex",
        justifyContent: "center",
        pt: 2,
      }}
    >
      <Box
        sx={{
          display: "flex",
          gap: 0.5,
          p: 0.5,
          borderRadius: 9999,
          bgcolor: "#f3f3f3",
        }}
      >
        {links.map((link) => (
          <Button
            key={link.to}
            component={NavLink}
            to={link.to}
            end
            sx={{
              color: "text.primary",
              "&.active": {
                bgcolor: "#ffffff",
                boxShadow: "0 1px 3px rgba(0, 0, 0, 0.12)",
              },
            }}
          >
            {link.label}
          </Button>
        ))}
      </Box>
    </Box>
  );
}
