import type { DashboardTradableAssetDto } from "../../api/investmentsApi";
import { useNavigate } from "react-router-dom";

type Props = {
  assets: DashboardTradableAssetDto[];
  formatMoney: (n: number) => string;
};

export function TradableAssetsTable({ assets, formatMoney }: Props) {
  const navigate = useNavigate();
  return (
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
        {assets.map((a) => (
          <tr
            key={a.assetId}
            role="button"
            tabIndex={0}
            style={{ cursor: "pointer" }}
            onClick={() => navigate(`/assets/${a.assetId}`)}
            onKeyDown={(e) => {
              if (e.key === "Enter" || e.key === " ") navigate(`/assets/${a.assetId}`);
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
  );
}
