const apiBaseUrl = ["localhost", "127.0.0.1", "[::1]"].includes(window.location.hostname)
  ? "http://localhost:2001/api"
  : "/api";

async function fetchCollection(resource) {
  const response = await fetch(`${apiBaseUrl}/${resource}`, {
    signal: AbortSignal.timeout(10000)
  });

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }

  const items = await response.json();
  if (!Array.isArray(items)) {
    throw new Error("Expected a collection.");
  }

  return items;
}
