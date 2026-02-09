import { useEffect, useMemo, useState } from "react";

export type AssetDetailDto = {
  assetId: string;
  symbol: string;
  name: string;
  type: string;
  currentPrice: number;
  quantityOwned: number;
  valuation: number;
};

type Props = {
  assetId: string;
  onClose: () => void;
  onSuccess: () => void;
};

export function AssetDetailInline({ assetId, onClose, onSuccess }: Props) {
  const [data, setData] = useState<AssetDetailDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
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
          let msg = body;
          try {
            const parsed = JSON.parse(body);
            if (parsed && typeof parsed.message === "string") {
              msg = parsed.message;
            }
          } catch {}
          throw new Error(msg || "Error");
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

  const handleBuy = async () => {
    if (!isQuantityValid || !data) return;
    setLoading(true);
    setError(null);
    try {
      const token = localStorage.getItem("access_token");
      const res = await fetch("/api/portfolio/buy", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ assetId: data.assetId, quantity: tradeQuantity }),
      });
      if (!res.ok) {
        const body = await res.text();
        let msg = body;
        try {
          const parsed = JSON.parse(body);
          if (parsed && typeof parsed.message === "string") {
            msg = parsed.message;
          }
        } catch {}
        throw new Error(msg || "Error");
      }
      // Solo si fue exitoso:
      onSuccess();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
      // No llamar onSuccess si hay error
    } finally {
      setLoading(false);
    }
  };

  const handleSell = async () => {
    if (!isQuantityValid || !data) return;
    setLoading(true);
    setError(null);
    try {
      const token = localStorage.getItem("access_token");
      const res = await fetch("/api/portfolio/sell", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ assetId: data.assetId, quantity: tradeQuantity }),
      });
      if (!res.ok) {
        const body = await res.text();
        let msg = body;
        try {
          const parsed = JSON.parse(body);
          if (parsed && typeof parsed.message === "string") {
            msg = parsed.message;
          }
        } catch {}
        throw new Error(msg || "Error");
      }
      // Solo si fue exitoso:
      onSuccess();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
      // No llamar onSuccess si hay error
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div style={{ padding: 16 }}>Loading...</div>;
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
      <div style={{ marginTop: 24 }}>
        <h3>Trade</h3>
        <label>
          Quantity:
          <input
            type="number"
            min={0}
            value={tradeQuantity}
            onChange={(e) => {
              const v = e.target.value;
              setTradeQuantity(v === "" ? 0 : Number(v));
            }}
            style={{ marginLeft: 8 }}
          />
        </label>
        <div style={{ marginTop: 12 }}>
          <strong>Total:</strong> ${tradeTotal.toFixed(2)}
        </div>
        <div style={{ marginTop: 12, display: "flex", gap: 8 }}>
          <button onClick={handleBuy} disabled={!isQuantityValid || loading}>
            Buy
          </button>
          <button onClick={handleSell} disabled={!isQuantityValid || loading}>
            Sell
          </button>
          <button onClick={onClose} disabled={loading}>
            Back
          </button>
        </div>
      </div>
      {error && (
        <div style={{ marginTop: 24 }}>
          <div style={{ color: "crimson", fontWeight: 600 }}>Ocurrió un error:</div>
          <pre style={{ color: "crimson", background: "#fff0f0", padding: 8, borderRadius: 4 }}>{error}</pre>
        </div>
      )}
    </div>
  );
}
