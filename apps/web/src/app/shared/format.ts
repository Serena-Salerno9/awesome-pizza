export function formatTime(iso?: string | null) {
  if (!iso) {
    return "-";
  }

  return new Date(iso).toLocaleTimeString("it-IT", {
    hour: "2-digit",
    minute: "2-digit",
  });
}
