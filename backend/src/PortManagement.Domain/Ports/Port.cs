using PortManagement.Domain.Common;

namespace PortManagement.Domain.Ports;

public sealed class Port : AuditableEntity
{
    private Port()
    {
        Name = string.Empty;
        UnLocode = string.Empty;
        CountryCode = string.Empty;
        TimeZoneId = string.Empty;
    }

    public Port(
        Guid id,
        string name,
        string unLocode,
        string countryCode,
        string timeZoneId,
        DateTimeOffset createdAtUtc)
        : base(id, createdAtUtc)
    {
        Name = DomainRules.RequiredText(name, "Nome do porto", 160);
        UnLocode = NormalizeUnLocode(unLocode);
        CountryCode = NormalizeCountryCode(countryCode);
        TimeZoneId = DomainRules.RequiredText(timeZoneId, "Fuso horário", 80);
        IsActive = true;
    }

    public string Name { get; private set; }

    public string UnLocode { get; private set; }

    public string CountryCode { get; private set; }

    public string TimeZoneId { get; private set; }

    public bool IsActive { get; private set; }

    public void Update(
        string name,
        string unLocode,
        string countryCode,
        string timeZoneId,
        bool isActive,
        DateTimeOffset changedAtUtc)
    {
        Name = DomainRules.RequiredText(name, "Nome do porto", 160);
        UnLocode = NormalizeUnLocode(unLocode);
        CountryCode = NormalizeCountryCode(countryCode);
        TimeZoneId = DomainRules.RequiredText(timeZoneId, "Fuso horário", 80);
        IsActive = isActive;
        MarkUpdated(changedAtUtc);
    }

    private static string NormalizeUnLocode(string value)
    {
        var normalized = DomainRules.RequiredText(value, "UN/LOCODE", 5).ToUpperInvariant().Replace(" ", string.Empty, StringComparison.Ordinal);
        if (normalized.Length != 5 || !normalized.All(char.IsAsciiLetterOrDigit))
        {
            throw new DomainException("O UN/LOCODE deve possuir cinco caracteres alfanuméricos.");
        }

        return normalized;
    }

    private static string NormalizeCountryCode(string value)
    {
        var normalized = DomainRules.RequiredText(value, "Código do país", 2).ToUpperInvariant();
        if (normalized.Length != 2 || !normalized.All(char.IsAsciiLetter))
        {
            throw new DomainException("O código do país deve possuir duas letras.");
        }

        return normalized;
    }
}
