import type { ReactNode } from "react";

export type KpiItem = {
  label: string;
  value: ReactNode;
};

type Props = {
  items: KpiItem[];
};

export function KpiCards({ items }: Props) {
  return (
    <div className="kpi-grid">
      {items.map((kpi) => (
        <div key={kpi.label} className="kpi-card">
          <div className="kpi-label">{kpi.label}</div>
          <div className="kpi-value">{kpi.value}</div>
        </div>
      ))}
    </div>
  );
}
