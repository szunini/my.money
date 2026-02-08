namespace my.money.application.Portfolios.Commands.BuyAsset;

public sealed record BuyAssetCommand(Guid AssetId, decimal Quantity);
