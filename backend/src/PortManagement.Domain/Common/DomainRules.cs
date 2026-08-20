using System.Globalization;

namespace PortManagement.Domain.Common;

internal static class DomainRules
{
    public static string RequiredText(string? value, string fieldName, int maximumLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainException($"{fieldName} é obrigatório.");
        }

        if (normalized.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} deve possuir no máximo {maximumLength.ToString(CultureInfo.InvariantCulture)} caracteres.");
        }

        return normalized;
    }

    public static string? OptionalText(string? value, string fieldName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return RequiredText(value, fieldName, maximumLength);
    }

    public static decimal Positive(decimal value, string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainException($"{fieldName} deve ser maior que zero.");
        }

        return value;
    }

    public static DateTimeOffset ToUtc(DateTimeOffset value) => value.ToUniversalTime();
}
