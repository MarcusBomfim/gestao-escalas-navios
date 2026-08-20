using PortManagement.Domain.Common;

namespace PortManagement.Domain.Vessels;

public sealed record ImoNumber
{
    private ImoNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ImoNumber Parse(string value)
    {
        var normalized = value.Trim().ToUpperInvariant().Replace(" ", string.Empty, StringComparison.Ordinal);
        if (!normalized.StartsWith("IMO", StringComparison.Ordinal))
        {
            normalized = $"IMO{normalized}";
        }

        var digits = normalized[3..];
        if (digits.Length != 7 || !digits.All(char.IsAsciiDigit))
        {
            throw new DomainException("O número IMO deve conter o prefixo IMO seguido de sete dígitos.");
        }

        var checksum = 0;
        for (var index = 0; index < 6; index++)
        {
            checksum += (digits[index] - '0') * (7 - index);
        }

        if (checksum % 10 != digits[6] - '0')
        {
            throw new DomainException("O dígito verificador do número IMO é inválido.");
        }

        return new ImoNumber(normalized);
    }

    public override string ToString() => Value;
}
