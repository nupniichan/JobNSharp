namespace JobNSharp.Utils;

public static class CountryMapper
{
    private static readonly Dictionary<string, (string Code, string Domain)> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        // Americas
        ["US"] = ("US", "www"), ["USA"] = ("US", "www"), ["UNITED STATES"] = ("US", "www"),
        ["CA"] = ("CA", "ca"), ["CANADA"] = ("CA", "ca"),
        ["MX"] = ("MX", "mx"), ["MEXICO"] = ("MX", "mx"),
        ["BR"] = ("BR", "br"), ["BRAZIL"] = ("BR", "br"),
        ["AR"] = ("AR", "ar"), ["ARGENTINA"] = ("AR", "ar"),
        ["CL"] = ("CL", "cl"), ["CHILE"] = ("CL", "cl"),
        ["CO"] = ("CO", "co"), ["COLOMBIA"] = ("CO", "co"),

        // Europe
        ["GB"] = ("GB", "uk"), ["UK"] = ("GB", "uk"), ["UNITED KINGDOM"] = ("GB", "uk"), ["ENGLAND"] = ("GB", "uk"),
        ["FR"] = ("FR", "fr"), ["FRANCE"] = ("FR", "fr"),
        ["DE"] = ("DE", "de"), ["GERMANY"] = ("DE", "de"),
        ["ES"] = ("ES", "es"), ["SPAIN"] = ("ES", "es"),
        ["IT"] = ("IT", "it"), ["ITALY"] = ("IT", "it"),
        ["NL"] = ("NL", "nl"), ["NETHERLANDS"] = ("NL", "nl"),
        ["BE"] = ("BE", "be"), ["BELGIUM"] = ("BE", "be"),
        ["CH"] = ("CH", "ch"), ["SWITZERLAND"] = ("CH", "ch"),
        ["AT"] = ("AT", "at"), ["AUSTRIA"] = ("AT", "at"),
        ["PL"] = ("PL", "pl"), ["POLAND"] = ("PL", "pl"),
        ["SE"] = ("SE", "se"), ["SWEDEN"] = ("SE", "se"),
        ["DK"] = ("DK", "dk"), ["DENMARK"] = ("DK", "dk"),
        ["NO"] = ("NO", "no"), ["NORWAY"] = ("NO", "no"),
        ["PT"] = ("PT", "pt"), ["PORTUGAL"] = ("PT", "pt"),
        ["IE"] = ("IE", "ie"), ["IRELAND"] = ("IE", "ie"),
        ["RU"] = ("RU", "ru"), ["RUSSIA"] = ("RU", "ru"),
        ["TR"] = ("TR", "tr"), ["TURKEY"] = ("TR", "tr"), ["TÜRKIYE"] = ("TR", "tr"),

        // Asia-Pacific
        ["IN"] = ("IN", "in"), ["INDIA"] = ("IN", "in"),
        ["JP"] = ("JP", "jp"), ["JAPAN"] = ("JP", "jp"),
        ["CN"] = ("CN", "cn"), ["CHINA"] = ("CN", "cn"),
        ["TW"] = ("TW", "tw"), ["TAIWAN"] = ("TW", "tw"),
        ["HK"] = ("HK", "hk"), ["HONG KONG"] = ("HK", "hk"),
        ["SG"] = ("SG", "sg"), ["SINGAPORE"] = ("SG", "sg"),
        ["KR"] = ("KR", "kr"), ["SOUTH KOREA"] = ("KR", "kr"), ["KOREA"] = ("KR", "kr"),
        ["AU"] = ("AU", "au"), ["AUSTRALIA"] = ("AU", "au"),
        ["NZ"] = ("NZ", "nz"), ["NEW ZEALAND"] = ("NZ", "nz"),
        ["VN"] = ("VN", "vn"), ["VIETNAM"] = ("VN", "vn"), ["VIET NAM"] = ("VN", "vn"),
        ["PH"] = ("PH", "ph"), ["PHILIPPINES"] = ("PH", "ph"),
        ["MY"] = ("MY", "my"), ["MALAYSIA"] = ("MY", "my"),
        ["TH"] = ("TH", "th"), ["THAILAND"] = ("TH", "th"),
        ["ID"] = ("ID", "id"), ["INDONESIA"] = ("ID", "id"),

        // Middle East & Africa
        ["AE"] = ("AE", "ae"), ["UAE"] = ("AE", "ae"), ["UNITED ARAB EMIRATES"] = ("AE", "ae"),
        ["SA"] = ("SA", "sa"), ["SAUDI ARABIA"] = ("SA", "sa"),
        ["QA"] = ("QA", "qa"), ["QATAR"] = ("QA", "qa"),
        ["KW"] = ("KW", "kw"), ["KUWAIT"] = ("KW", "kw"),
        ["BH"] = ("BH", "bh"), ["BAHRAIN"] = ("BH", "bh"),
        ["OM"] = ("OM", "om"), ["OMAN"] = ("OM", "om"),
        ["EG"] = ("EG", "eg"), ["EGYPT"] = ("EG", "eg"),
        ["MA"] = ("MA", "ma"), ["MOROCCO"] = ("MA", "ma"),
        ["ZA"] = ("ZA", "za"), ["SOUTH AFRICA"] = ("ZA", "za"),
        ["IL"] = ("IL", "il"), ["ISRAEL"] = ("IL", "il"),
    };

    public static string GetCountryCode(string? country)
    {
        if (string.IsNullOrWhiteSpace(country)) return "US";
        return Map.TryGetValue(country.Trim(), out var entry) ? entry.Code : FallbackCode(country);
    }

    public static string GetDomain(string? country)
    {
        if (string.IsNullOrWhiteSpace(country)) return "www";
        return Map.TryGetValue(country.Trim(), out var entry) ? entry.Domain : FallbackDomain(country);
    }

    private static string FallbackCode(string country)
    {
        var trimmed = country.Trim();
        return trimmed.Length == 2 ? trimmed.ToUpperInvariant() : "US";
    }

    private static string FallbackDomain(string country)
    {
        var trimmed = country.Trim();
        return trimmed.Length == 2 ? trimmed.ToLowerInvariant() : "www";
    }
}
