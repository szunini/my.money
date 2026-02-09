import type { DashboardHoldingDto } from "../../api/investmentsApi";

type Props = {
  holdings: DashboardHoldingDto[];
  formatMoney: (n: number) => string;
};

export function HoldingsTable({ holdings, formatMoney }: Props) {
  return (
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
        {holdings.map((h) => {
          const latestPrice = h.latestPrice;
          const value = latestPrice == null ? null : h.quantity * latestPrice;

          return (
            <tr key={h.assetId}>
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
  );
}
