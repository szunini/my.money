import { useEffect, useMemo, useState } from "react";
import { fetchAssetNews, type AssetNewsItemDto } from "../api/news";

export type PortfolioAssetRef = {
  assetId: string;
  ticker: string;
};

type Props = {
  assets: PortfolioAssetRef[];
};

type NewsItemView = AssetNewsItemDto & {
  assetId: string;
  ticker: string;
};

export function NewsWidget({ assets }: Props) {
  const [items, setItems] = useState<NewsItemView[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // para no hacer calls si no hay assets
  const assetIds = useMemo(() => assets.map(a => a.assetId), [assets]);

  useEffect(() => {
    if (assetIds.length === 0) {
      setItems([]);
      return;
    }

    const load = async () => {
      setLoading(true);
      setError(null);

      try {
        // Pedimos noticias por asset en paralelo
        const results = await Promise.all(
          assets.map(async (a) => {
            const news = await fetchAssetNews(a.assetId, 5);
            return news.map(n => ({ ...n, assetId: a.assetId, ticker: a.ticker }));
          })
        );

        // Flatten
        const merged = results.flat();

        // Orden simple: confianza desc, luego fecha desc (si existe)
        merged.sort((a, b) => {
          if (b.confidence !== a.confidence) return b.confidence - a.confidence;
          const da = a.publishedAtUtc ? Date.parse(a.publishedAtUtc) : 0;
          const db = b.publishedAtUtc ? Date.parse(b.publishedAtUtc) : 0;
          return db - da;
        });

        setItems(merged);
      } catch (e) {
        setError(e instanceof Error ? e.message : "Unknown error");
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [assetIds.join("|")]); // dependencia estable

  return (
    <div style={{ marginTop: 24, padding: 12, border: "1px solid #ddd", borderRadius: 8 }}>
      <h3 style={{ margin: 0 }}>News</h3>

      {loading && <div style={{ marginTop: 8 }}>Loading news...</div>}
      {error && <div style={{ marginTop: 8 }}>Error: {error}</div>}

      {!loading && !error && items.length === 0 && (
        <div style={{ marginTop: 8 }}>No related news yet.</div>
      )}

      {!loading && !error && items.length > 0 && (
        <ul style={{ marginTop: 8 }}>
          {items.slice(0, 10).map((n, idx) => (
            <li key={`${n.url}-${idx}`} style={{ marginBottom: 10 }}>
              <div>
                <strong>[{n.ticker}]</strong>{" "}
                <a href={n.url} target="_blank" rel="noreferrer">
                  {n.title}
                </a>
              </div>

              <div style={{ fontSize: 12, opacity: 0.8 }}>
                {n.source}
                {n.publishedAtUtc ? ` • ${n.publishedAtUtc}` : ""}
                {" • "}
                confidence {(n.confidence * 100).toFixed(0)}%
              </div>

              <div style={{ fontSize: 12 }}>{n.explanation}</div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
