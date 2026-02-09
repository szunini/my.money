using System;

namespace my.money.application.Assets.Commands.AddQuote
{
    public sealed record AddQuoteCommand(
        Guid AssetId,
        decimal Price,
        DateTime? AsOfUtc,
        string? Source
    );
}
