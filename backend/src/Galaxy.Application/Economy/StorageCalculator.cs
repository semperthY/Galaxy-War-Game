using Galaxy.Domain.Entities;

namespace Galaxy.Application.Economy;

public static class StorageCalculator
{
    private const decimal BaseMaterialsCapacity = 1000m;
    private const decimal BaseDeuteriumCapacity = 500m;

    public static StorageCapacity Calculate(Planet planet)
    {
        var multiplier = Pow(2m, planet.WarehouseLevel);

        return new StorageCapacity(
            BaseMaterialsCapacity * multiplier,
            BaseDeuteriumCapacity * multiplier);
    }

    private static decimal Pow(
        decimal value,
        int exponent)
    {
        var result = 1m;

        for (var index = 0; index < exponent; index++)
        {
            result *= value;
        }

        return result;
    }
}

public sealed record StorageCapacity(
    decimal Materials,
    decimal Deuterium);
