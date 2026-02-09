import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";

export type AllocationBucket = {
  name: "Cash" | "Stocks" | "Bonds";
  value: number;
};

type Props = {
  buckets: AllocationBucket[];
  formatMoney: (n: number) => string;
};

const COLORS = ["#0088FE", "#00C49F", "#FFBB28"];

export function AllocationPie({ buckets, formatMoney }: Props) {
  const total = buckets.reduce((acc, b) => acc + b.value, 0);

  return (
    <div className="chart-card">
      <div className="section-title">Portfolio Allocation</div>

      <div className="chart-wrap">
        <div className="chart-area">
          <ResponsiveContainer width="100%" height={260}>
            <PieChart>
              <Pie data={buckets} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={90}>
                {buckets.map((_, idx) => (
                  // eslint-disable-next-line react/no-array-index-key
                  <Cell key={idx} fill={COLORS[idx % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip
                formatter={(value: number | string) => formatMoney(Number(value))}
                labelFormatter={(label: string | number) => String(label)}
              />
            </PieChart>
          </ResponsiveContainer>
        </div>

        <div className="chart-legend">
          {buckets.map((b) => {
            const pct = total <= 0 ? 0 : (b.value / total) * 100;
            return (
              <div key={b.name} className="legend-row">
                <div className="legend-name">{b.name}</div>
                <div className="legend-value">
                  {formatMoney(b.value)} ({pct.toFixed(0)}%)
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
