import { useState } from "react";
import { login } from "../api/authApi";
import { useAuth } from "../auth/AuthContext";
import { useNavigate } from "react-router-dom";

export function LoginPage() {
  const { setToken, isAuthenticated } = useAuth();
  const nav = useNavigate();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);

  // Si ya está autenticado, lo mando al dashboard
  if (isAuthenticated) {
    nav("/", { replace: true });
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    try {
      const result = await login(email, password);
      setToken(result.accessToken);
      nav("/", { replace: true });
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "Login failed");
    }
  }

  return (
    <div style={{ maxWidth: 420, margin: "40px auto" }}>
      <h2>Login</h2>
      <form onSubmit={handleSubmit}>
        <div>
          <label>Email</label>
          <input value={email} onChange={(e) => setEmail(e.target.value)} style={{ width: "100%" }} />
        </div>

        <div style={{ marginTop: 12 }}>
          <label>Password</label>
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} style={{ width: "100%" }} />
        </div>

        {error && <div style={{ marginTop: 12, color: "crimson" }}>{error}</div>}
        <button style={{ marginTop: 16, width: "100%" }}>Login</button>
      </form>
    </div>
  );
}
