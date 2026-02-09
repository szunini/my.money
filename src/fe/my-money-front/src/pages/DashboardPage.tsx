import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getPortfolioDashboard, type PortfolioDashboardDto } from "../api/investmentsApi";
import { useAuth } from "../auth/AuthContext";
import { AllocationPie, type AllocationBucket } from "../components/dashboard/AllocationPie";
import { KpiCards } from "../components/dashboard/KpiCards";
import { NewsWidget } from "../components/NewsWidget";
import { AssetDetailInline } from "../components/dashboard/AssetDetailInline";
import "./dashboard.css";

const moneyFmt = new Intl.NumberFormat("es-AR", {
  style: "currency",
  currency: "ARS",
});

function formatMoney(n: number) {
  return moneyFmt.format(n);
}


export function DashboardPage() {
  const nav = useNavigate();
  const { logout } = useAuth();

  const [data, setData] = useState<PortfolioDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedAssetId, setSelectedAssetId] = useState<string | null>(null);

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

  if (loading) return <div className="dashboard-container">Cargando...</div>;

  if (error)
    return (
      <div className="dashboard-container">
        <div className="dashboard-header">
          <h1>Dashboard</h1>
          <button
            onClick={() => {
              logout();
              nav("/login", { replace: true });
            }}
          >
            Logout
          </button>
        </div>

        <div className="error-box">
          <p style={{ color: "crimson", fontWeight: 600 }}>Ocurrió un error al cargar el dashboard:</p>
          <pre style={{ color: "crimson", background: "#fff0f0", padding: 8, borderRadius: 4 }}>{error}</pre>
          <button onClick={load} style={{ marginTop: 8 }}>Reintentar</button>
        </div>
      </div>
    );

  if (!data)
    return (
      <div className="dashboard-container">
        <div className="dashboard-header">
          <h1>Dashboard</h1>
          <button
            onClick={() => {
              logout();
              nav("/login", { replace: true });
            }}
          >
            Logout
          </button>
        </div>

        <p>No hay datos.</p>
      </div>
    );

  const cash = data.cashBalance;
  const stocks = data.holdings
    .filter((h) => h.type === "Stock")
    .reduce((acc, h) => acc + (h.latestPrice == null ? 0 : h.quantity * h.latestPrice), 0);
  const bonds = data.holdings
    .filter((h) => h.type === "Bond")
    .reduce((acc, h) => acc + (h.latestPrice == null ? 0 : h.quantity * h.latestPrice), 0);

  const allocationBuckets: AllocationBucket[] = [
    { name: "Cash", value: cash },
    { name: "Stocks", value: stocks },
    { name: "Bonds", value: bonds },
  ];

  // Custom HoldingsTable and TradableAssetsTable with row click
  function handleAssetClick(assetId: string) {
    setSelectedAssetId(assetId);
  }

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <h1>Dashboard</h1>
        <button
          onClick={() => {
            logout();
            nav("/login", { replace: true });
          }}
        >
          Logout
        </button>
      </div>

      <KpiCards
        items={[
          { label: "Cash Balance", value: formatMoney(data.cashBalance) },
          { label: "Total Holdings Value", value: formatMoney(data.totalHoldingsValue) },
          { label: "Total Portfolio Value", value: formatMoney(data.totalPortfolioValue) },
        ]}
      />

      <div className="dashboard-main-cols">
        <div className="dashboard-main-left">
          <div className="dash-section">
            <h2>My Holdings</h2>
            {data.holdings.length === 0 ? (
              <p>No holdings yet.</p>
            ) : (
              <table className="dash-table">
                <thead>
                  <tr>
                    <th>Ticker</th>
                    <th>Name</th>
                    <th>Type</th>
                    <th className="num">Qty</th>
                    <th className="num">Latest Price</th>
                    <th className="num">Value</th>
                  </tr>
                </thead>
                <tbody>
                  {data.holdings.map((h) => {
                    const latestPrice = h.latestPrice;
                    const value = latestPrice == null ? null : h.quantity * latestPrice;
                    return (
                      <tr
                        key={h.assetId}
                        style={{ cursor: "pointer" }}
                        onClick={() => handleAssetClick(h.assetId)}
                        tabIndex={0}
                        role="button"
                        onKeyDown={(e) => {
                          if (e.key === "Enter" || e.key === " ") handleAssetClick(h.assetId);
                        }}
                      >
                        <td>{h.ticker}</td>
                        <td>{h.name}</td>
                        <td>{h.type}</td>
                        <td className="num">{h.quantity}</td>
                        <td className="num">{latestPrice == null ? "—" : formatMoney(latestPrice)}</td>
                        <td className="num">{value == null ? "—" : formatMoney(value)}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            )}
          </div>
          <div className="dash-section">
            <h2>Tradable Assets</h2>
            <table className="dash-table">
              <thead>
                <tr>
                  <th>Ticker</th>
                  <th>Name</th>
                  <th>Type</th>
                  <th className="num">Latest Price</th>
                </tr>
              </thead>
              <tbody>
                {data.tradableAssets.map((a) => (
                  <tr
                    key={a.assetId}
                    style={{ cursor: "pointer" }}
                    onClick={() => handleAssetClick(a.assetId)}
                    tabIndex={0}
                    role="button"
                    onKeyDown={(e) => {
                      if (e.key === "Enter" || e.key === " ") handleAssetClick(a.assetId);
                    }}
                  >
                    <td>{a.ticker}</td>
                    <td>{a.name}</td>
                    <td>{a.type}</td>
                    <td className="num">{a.latestPrice == null ? "—" : formatMoney(a.latestPrice)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
        <div className="dashboard-main-right">
          {selectedAssetId ? (
            <AssetDetailInline
              assetId={selectedAssetId}
              onClose={() => setSelectedAssetId(null)}
              onSuccess={() => {
                setSelectedAssetId(null);
                load();
              }}
            />
          ) : (
            <>
              <AllocationPie buckets={allocationBuckets} formatMoney={formatMoney} />
              <NewsWidget assets={data.holdings.map(h => ({ assetId: h.assetId, ticker: h.ticker }))} />
            </>
          )}
        </div>
      </div>

      <div className="dashboard-footer">
        <button onClick={load}>Refresh</button>
      </div>
    </div>
  );
}
