namespace my.money.application.Portfolios.Commands.SellAsset;

public sealed record SellAssetCommand(Guid AssetId, decimal Quantity);
