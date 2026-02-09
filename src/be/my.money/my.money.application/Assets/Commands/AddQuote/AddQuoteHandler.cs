using System;
using System.Threading;
using System.Threading.Tasks;
using my.money.application.Ports.Persistence;
using my.money.domain.Aggregates.Assets;
using my.money.domain.Common.ValueObject;

namespace my.money.application.Assets.Commands.AddQuote
{
    public sealed class AddQuoteHandler
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddQuoteHandler(IAssetRepository assetRepository, IUnitOfWork unitOfWork)
        {
            _assetRepository = assetRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(AddQuoteCommand command, CancellationToken ct)
        {
            var asset = await _assetRepository.GetByIdAsync(command.AssetId, ct);
            if (asset is null)
                throw new NotFoundException($"Asset {command.AssetId} not found.");

            if (command.Price <= 0)
                throw new ArgumentException("Price must be positive.", nameof(command.Price));

            var asOfUtc = command.AsOfUtc ?? DateTime.UtcNow;
            var money = Money.Of(command.Price, asset.Currency);
            asset.AddQuote(money, asOfUtc, command.Source ?? "manual");

            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
