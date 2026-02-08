import { useEffect, useState, type ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { getPortfolioDashboard, type PortfolioDashboardDto } from "../api/investmentsApi";
import { NewsWidget, type PortfolioAssetRef } from "../components/NewsWidget";
import { useAuth } from "../auth/AuthContext";
function money(n: number) {
  return n.toLocaleString("es-AR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export function DashboardPage() {
  const { logout } = useAuth();

  const [data, setData] = useState<PortfolioDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);

    try {
      const result = await getPortfolioDashboard();
      setData(result);
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "No se pudo cargar el dashboard");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  if (loading) return <div style={{ maxWidth: 900, margin: "24px auto" }}>Cargando...</div>;

  if (error)
    return (
      <div style={{ maxWidth: 900, margin: "24px auto" }}>
        <h2>Dashboard</h2>
        <p style={{ color: "crimson" }}>{error}</p>
        <button onClick={load}>Reintentar</button>{" "}
        <button onClick={logout}>Logout</button>
      </div>
    );

  if (!data)
    return (
      <div style={{ maxWidth: 900, margin: "24px auto" }}>
        <h2>Dashboard</h2>
        <p>No hay datos.</p>
        <button onClick={logout}>Logout</button>
      </div>
    );

  const assetsForNews: PortfolioAssetRef[] = data.holdings.map((h) => ({ assetId: h.assetId, ticker: h.ticker }));

  return (
    <div style={{ maxWidth: 900, margin: "24px auto" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h2>Dashboard</h2>
        <button onClick={logout}>Logout</button>
      </div>

      {/* Summary */}
      <div style={{ display: "flex", gap: 16, marginTop: 16 }}>
        <div style={{ border: "1px solid #ddd", padding: 12, flex: 1 }}>
          <div style={{ fontSize: 12, opacity: 0.7 }}>Cash Balance</div>
          <div style={{ fontSize: 20 }}>${money(data.cashBalance)}</div>
        </div>

        <div style={{ border: "1px solid #ddd", padding: 12, flex: 1 }}>
          <div style={{ fontSize: 12, opacity: 0.7 }}>Total Holdings Value</div>
          <div style={{ fontSize: 20 }}>${money(data.totalHoldingsValue)}</div>
        </div>

        <div style={{ border: "1px solid #ddd", padding: 12, flex: 1 }}>
          <div style={{ fontSize: 12, opacity: 0.7 }}>Total Portfolio Value</div>
          <div style={{ fontSize: 20 }}>${money(data.totalPortfolioValue)}</div>
        </div>
      </div>

      {/* Holdings */}
      <h3 style={{ marginTop: 24 }}>My Holdings</h3>
      {data.holdings.length === 0 ? (
        <p>No holdings yet.</p>
      ) : (
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <thead>
            <tr>
              <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 8 }}>Ticker</th>
              <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 8 }}>Name</th>
              <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 8 }}>Type</th>
              <th style={{ textAlign: "right", borderBottom: "1px solid #ddd", padding: 8 }}>Qty</th>
              <th style={{ textAlign: "right", borderBottom: "1px solid #ddd", padding: 8 }}>Latest Price</th>
              <th style={{ textAlign: "right", borderBottom: "1px solid #ddd", padding: 8 }}>Value</th>
            </tr>
          </thead>
          <tbody>
            {data.holdings.map((h) => {
              const price = h.latestPrice ?? 0;
              const value = (h as any).valuation ?? (h.quantity * price); // por si no viene valuation
              return (
                <tr key={h.assetId}>
                  <td style={{ borderBottom: "1px solid #eee", padding: 8 }}>{h.ticker}</td>
                  <td style={{ borderBottom: "1px solid #eee", padding: 8 }}>{h.name}</td>
                  <td style={{ borderBottom: "1px solid #eee", padding: 8 }}>{h.type}</td>
                  <td style={{ borderBottom: "1px solid #eee", padding: 8, textAlign: "right" }}>{h.quantity}</td>
                  <td style={{ borderBottom: "1px solid #eee", padding: 8, textAlign: "right" }}>
                    {h.latestPrice == null ? "-" : `$${money(h.latestPrice)}`}
                  </td>
                  <td style={{ borderBottom: "1px solid #eee", padding: 8, textAlign: "right" }}>
                    ${money(value)}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}

      {/* Tradable Assets */}
      <h3 style={{ marginTop: 24 }}>Tradable Assets</h3>
      <table style={{ width: "100%", borderCollapse: "collapse" }}>
        <thead>
          <tr>
            <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 8 }}>Ticker</th>
            <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 8 }}>Name</th>
            <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 8 }}>Type</th>
            <th style={{ textAlign: "right", borderBottom: "1px solid #ddd", padding: 8 }}>Latest Price</th>
          </tr>
        </thead>
        <tbody>
          {data.tradableAssets.map((a) => (
            <ClickableRow key={a.assetId} assetId={a.assetId}>
              <td style={{ borderBottom: "1px solid #eee", padding: 8 }}>{a.ticker}</td>
              <td style={{ borderBottom: "1px solid #eee", padding: 8 }}>{a.name}</td>
              <td style={{ borderBottom: "1px solid #eee", padding: 8 }}>{a.type}</td>
              <td style={{ borderBottom: "1px solid #eee", padding: 8, textAlign: "right" }}>
                {a.latestPrice == null ? "-" : `$${money(a.latestPrice)}`}
              </td>
            </ClickableRow>
          ))}
        </tbody>
      </table>

      <NewsWidget assets={assetsForNews} />
      <div style={{ marginTop: 16 }}>
        <button onClick={load}>Refresh</button>
      </div>
    </div>
  );
}

function ClickableRow({ children, assetId }: { children: ReactNode; assetId: string }) {
  const navigate = useNavigate();
  return (
    <tr
      role="button"
      tabIndex={0}
      onClick={() => navigate(`/assets/${assetId}`)}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") navigate(`/assets/${assetId}`);
      }}
      style={{ cursor: "pointer" }}
    >
      {children}
    </tr>
  );
}
