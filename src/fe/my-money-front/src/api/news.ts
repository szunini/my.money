import { http } from "./http";

export type AssetNewsItemDto = {
  title: string;
  source: string;
  url: string;
  publishedAtUtc: string | null;
  confidence: number;
  explanation: string;
};

export async function fetchAssetNews(assetId: string, take = 10, maxDays = 30): Promise<AssetNewsItemDto[]> {
  const res = await http.get<AssetNewsItemDto[]>(`/api/news/assets/${assetId}/mentions`, {
    params: { maxDays },
  });

  // El backend puede devolver más ítems; respetamos `take` del widget cortando en el FE.
  return res.data.slice(0, take);
}
