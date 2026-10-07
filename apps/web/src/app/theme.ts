import { createTheme } from "@mui/material/styles";

const ink = "#141414";
const canvasSoft = "#f3f3f3";
const field = "#f0f0f0";
const hairline = "#e0e0e0";
const cardBorder = "#cfcfcf";
const muted = "#707070";
const accent = "#0066ff";

export const theme = createTheme({
  palette: {
    primary: { main: ink, contrastText: "#ffffff" },
    secondary: { main: accent, contrastText: "#ffffff" },
    background: { default: "#ffffff", paper: "#ffffff" },
    text: { primary: ink, secondary: muted },
    divider: hairline,
  },
  shape: { borderRadius: 16 },
  typography: {
    fontFamily:
      '"Inter Variable", -apple-system, "Helvetica Neue", Arial, sans-serif',
    fontWeightRegular: 450,
    fontWeightMedium: 600,
    fontWeightBold: 650,
    h1: { fontSize: "3.5rem", fontWeight: 650, lineHeight: 1 },
    h2: { fontSize: "2.75rem", fontWeight: 650, lineHeight: 1.13 },
    h3: { fontSize: "2rem", fontWeight: 650, lineHeight: 1.13 },
    h4: { fontSize: "1.5rem", fontWeight: 650, lineHeight: 1.25 },
    h5: { fontSize: "1.25rem", fontWeight: 600, lineHeight: 1.3 },
    h6: { fontSize: "1.25rem", fontWeight: 600, lineHeight: 1.3 },
    body1: { fontSize: "1rem", lineHeight: 1.38 },
    body2: { fontSize: "0.875rem", lineHeight: 1.43 },
    button: { textTransform: "none", fontWeight: 600 },
  },
  components: {
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: { borderRadius: 9999, paddingInline: 16, minHeight: 44 },
        outlined: {
          borderColor: hairline,
          color: ink,
          "&:hover": { borderColor: ink, backgroundColor: "transparent" },
        },
      },
    },
    MuiPaper: {
      defaultProps: { elevation: 0 },
      styleOverrides: { root: { backgroundImage: "none" } },
    },
    MuiCard: {
      styleOverrides: {
        root: { borderRadius: 24, border: `1px solid ${cardBorder}` },
      },
    },
    MuiChip: {
      styleOverrides: { root: { borderRadius: 9999, fontWeight: 600 } },
    },
    MuiFilledInput: {
      defaultProps: { disableUnderline: true },
      styleOverrides: {
        root: {
          backgroundColor: field,
          borderRadius: 16,
          "&:hover": { backgroundColor: field },
          "&.Mui-focused": {
            backgroundColor: field,
            boxShadow: `0 0 0 2px ${ink}`,
          },
        },
      },
    },
    MuiAlert: {
      styleOverrides: { root: { borderRadius: 16 } },
    },
    MuiTableCell: {
      styleOverrides: {
        root: { borderBottom: `1px solid ${hairline}` },
        head: { backgroundColor: canvasSoft, fontWeight: 600 },
      },
    },
  },
});
