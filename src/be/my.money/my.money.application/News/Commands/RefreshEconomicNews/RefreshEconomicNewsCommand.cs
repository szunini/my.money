namespace my.money.application.News.Commands.RefreshEconomicNews;

public sealed class RefreshEconomicNewsCommand
{
    public decimal ConfidenceThreshold { get; set; } = 0.55m;
}
