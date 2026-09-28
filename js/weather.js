async function fetchTemperature() {
  const controller = new AbortController();
  const timeout = setTimeout(function() {
    controller.abort();
  }, 10000);

  try {
    const response = await fetch(
      "https://api.open-meteo.com/v1/forecast?latitude=52.07&longitude=4.30&current=temperature_2m&temperature_unit=celsius",
      { signal: controller.signal }
    );
    if (!response.ok) {
      throw new Error("Weather request failed.");
    }

    const data = await response.json();
    const temperature = data.current?.temperature_2m;
    if (!Number.isFinite(temperature)) {
      throw new Error("Weather response contains no temperature.");
    }
    return temperature;
  } finally {
    clearTimeout(timeout);
  }
}

function showWeatherState(state, message) {
  document.getElementById("weather").dataset.state = state;
  document.getElementById("weather-status").textContent = message;
  document.getElementById("weather-retry").hidden = state !== "error";
}

async function loadWeather() {
  showWeatherState("loading", "Loading weather...");
  try {
    const temperature = await fetchTemperature();
    showWeatherState("success", Math.round(temperature) + " \u00b0C");
  } catch {
    showWeatherState("error", "Weather unavailable");
  }
}

function initializeWeather() {
  const weather = document.getElementById("weather");
  if (!weather) return;

  weather.hidden = false;
  document.getElementById("weather-retry").addEventListener("click", loadWeather);
  loadWeather();
}

initializeWeather();
