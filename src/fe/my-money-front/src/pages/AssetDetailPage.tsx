import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

type AssetDetailDto = {
  assetId: string;
  symbol: string;
  name: string;
  type: string; // "Stock" | "Bond" o lo que uses
  currentPrice: number;
  quantityOwned: number;
  valuation: number;
};

export function AssetDetailPage() {
  const navigate = useNavigate();
  const { assetId } = useParams<{ assetId: string }>();

  const [data, setData] = useState<AssetDetailDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // ✅ Paso 4: input + cálculo en vivo
  const [tradeQuantity, setTradeQuantity] = useState<number>(0);

  useEffect(() => {
    if (!assetId) return;

    const load = async () => {
      setLoading(true);
      setError(null);

      try {
        const token = localStorage.getItem("access_token");

        const res = await fetch(`/api/assets/${assetId}/detail`, {
          headers: {
            "Content-Type": "application/json",
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
        });

        if (!res.ok) {
          const body = await res.text();
          throw new Error(`HTTP ${res.status} - ${body || "Error"}`);
        }

        const json = (await res.json()) as AssetDetailDto;
        setData(json);
      } catch (e) {
        setError(e instanceof Error ? e.message : "Unknown error");
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [assetId]);

  const tradeTotal = useMemo(() => {
    if (!data) return 0;
    if (!Number.isFinite(tradeQuantity) || tradeQuantity <= 0) return 0;
    return tradeQuantity * data.currentPrice;
  }, [tradeQuantity, data]);

  const isQuantityValid = Number.isFinite(tradeQuantity) && tradeQuantity > 0;

  // 🔜 Paso 5 (todavía NO implementado): buy/sell + redirect
  const handleBuy = async () => {
    // TODO: POST /api/portfolio/buy { assetId, quantity: tradeQuantity }
    // on success: navigate("/")
    alert("TODO: Buy (Paso 5)");
  };

  const handleSell = async () => {
    // TODO: POST /api/portfolio/sell { assetId, quantity: tradeQuantity }
    // on success: navigate("/")
    alert("TODO: Sell (Paso 5)");
  };

  if (!assetId) return <div style={{ padding: 16 }}>Missing assetId</div>;
  if (loading) return <div style={{ padding: 16 }}>Loading...</div>;
  if (error) return <div style={{ padding: 16 }}>Error: {error}</div>;
  if (!data) return <div style={{ padding: 16 }}>No data</div>;

  return (
    <div style={{ padding: 16 }}>
      <h1>Asset detail</h1>

      <div style={{ marginTop: 12 }}>
        <div>
          <strong>Name:</strong> {data.name}
        </div>
        <div>
          <strong>Symbol:</strong> {data.symbol}
        </div>
        <div>
          <strong>Type:</strong> {data.type}
        </div>
        <div>
          <strong>Current price:</strong> {data.currentPrice}
        </div>
        <div>
          <strong>Quantity owned:</strong> {data.quantityOwned}
        </div>
        <div>
          <strong>Valuation:</strong> {data.valuation}
        </div>
      </div>

      {/* ✅ Paso 4 */}
      <div style={{ marginTop: 24 }}>
        <h3>Trade</h3>

        <label>
          Quantity:
          <input
            type="number"
            min={0}
            value={tradeQuantity}
            onChange={(e) => setTradeQuantity(Number(e.target.value))}
            style={{ marginLeft: 8 }}
          />
        </label>
