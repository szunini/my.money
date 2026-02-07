import { useAuth } from "../auth/AuthContext";

export function DashboardPage() {
  const { logout } = useAuth();

  return (
    <div style={{ maxWidth: 900, margin: "24px auto" }}>
      <h2>Dashboard</h2>
      <p>Si ves esto, estás autenticado ✅</p>
      <button onClick={logout}>Logout</button>
    </div>
  );
}
