namespace my.money.application.News.Queries.GetAssetNews;

public sealed class GetAssetNewsQuery
{
    public Guid AssetId { get; set; }
    public int MaxDays { get; set; } = 30;
}
