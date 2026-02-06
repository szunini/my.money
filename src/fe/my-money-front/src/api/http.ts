import axios from "axios";

export const http = axios.create({
  baseURL: "http://localhost:5000", // <-- cambiá si tu API usa otro puerto
});

// Adjunta el token automáticamente en cada request
http.interceptors.request.use((config) => {
  const token = localStorage.getItem("access_token");
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});