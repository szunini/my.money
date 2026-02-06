using my.money.domain.Enum;

namespace my.money.Infraestructure.Persistence.Seeding;

internal static class AssetSeedData
{
    public static IReadOnlyList<AssetSeed> GetAssets() => new List<AssetSeed>
    {
        // ACCIONES ARGENTINAS
        new("YPFD", "YPF S.A.", AssetType.Stock, "ARS"),
        new("GGAL", "Grupo Financiero Galicia", AssetType.Stock, "ARS"),
        new("BMA", "Banco Macro", AssetType.Stock, "ARS"),
        new("BBAR", "BBVA Argentina", AssetType.Stock, "ARS"),
        new("PAMP", "Pampa Energía", AssetType.Stock, "ARS"),
        new("TECO2", "Telecom Argentina", AssetType.Stock, "ARS"),
        new("TGSU2", "Transportadora de Gas del Sur", AssetType.Stock, "ARS"),
        new("TGNO4", "Transportadora de Gas del Norte", AssetType.Stock, "ARS"),
        new("ALUA", "Aluar", AssetType.Stock, "ARS"),
        new("LOMA", "Loma Negra", AssetType.Stock, "ARS"),
        new("TRAN", "Transener", AssetType.Stock, "ARS"),
        new("CEPU", "Central Puerto", AssetType.Stock, "ARS"),
        new("CRES", "Cresud", AssetType.Stock, "ARS"),
        new("IRSA", "IRSA", AssetType.Stock, "ARS"),
        
        // BONOS ARGENTINOS
        new("AL30", "Bono USD 2030 (Ley AR)", AssetType.Bond, "ARS"),
        new("GD30", "Global 2030", AssetType.Bond, "ARS"),
        new("AL35", "Bono USD 2035 (Ley AR)", AssetType.Bond, "ARS"),
        new("GD35", "Global 2035", AssetType.Bond, "ARS"),
        new("AE38", "Bono USD 2038 (Ley AR)", AssetType.Bond, "ARS"),
        new("GD38", "Global 2038", AssetType.Bond, "ARS"),
        new("AL41", "Bono USD 2041 (Ley AR)", AssetType.Bond, "ARS"),
        new("GD41", "Global 2041", AssetType.Bond, "ARS")
    };
}

internal record AssetSeed(string Ticker, string Name, AssetType Type, string Currency);
