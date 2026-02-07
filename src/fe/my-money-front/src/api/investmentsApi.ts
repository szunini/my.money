import { http } from "./http";

export type DashboardHoldingDto = {
  assetId: string;
  ticker: string;
  name: string;
  type: string; // "Stock" | "Bond"
  quantity: number;
  latestPrice: number | null;
  latestPriceAsOfUtc: string | null;
  valuation: number; // si tu backend lo manda; si no, lo calculamos
};

export type DashboardTradableAssetDto = {
  assetId: string;
  ticker: string;
  name: string;
  type: string;
  latestPrice: number | null;
  latestPriceAsOfUtc: string | null;
};

export type PortfolioDashboardDto = {
  cashBalance: number;
  totalHoldingsValue: number;
  totalPortfolioValue: number;
  holdings: DashboardHoldingDto[];
  tradableAssets: DashboardTradableAssetDto[];
};

export async function getPortfolioDashboard(): Promise<PortfolioDashboardDto> {
  const res = await http.get<PortfolioDashboardDto>("/api/portfolio/dashboard");
  return res.data;
}
