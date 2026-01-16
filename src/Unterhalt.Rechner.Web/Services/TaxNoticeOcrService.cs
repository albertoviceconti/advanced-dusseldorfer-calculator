using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Unterhalt.Rechner.Web.Services;

public sealed class TaxNoticeParsed
{
    public decimal? AnnualWageIncome { get; init; }
    public decimal? AdvertisingCosts { get; init; }
    public decimal? AnnualNetRentalIncome { get; init; }
    public decimal? AnnualIncomeTax { get; init; }
    public decimal? AnnualSolidaritySurcharge { get; init; }
    public decimal? AnnualChurchTax { get; init; }
    public decimal? AnnualTaxesWithheld { get; init; }
    public decimal? AnnualTaxRefund { get; init; }
    public decimal? AnnualPensionAndHealthContrib { get; init; }
}

public sealed class TaxNoticeOcrService
{
    public TaxNoticeParsed Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new TaxNoticeParsed();

        return new TaxNoticeParsed
        {
            AnnualWageIncome = Extract(text, "jahresarbeitslohn"),
            AdvertisingCosts = Extract(text, "werbungskosten"),
            AnnualNetRentalIncome = Extract(text, "eink.nfte aus vermietung", "einkuenfte aus vermietung"),
            AnnualIncomeTax = Extract(text, "festzusetzende einkommensteuer", "einkommensteuer"),
            AnnualSolidaritySurcharge = Extract(text, "solidarit.*tszuschlag"),
            AnnualChurchTax = Extract(text, "kirchensteuer"),
            AnnualTaxesWithheld = Extract(text, "steuerabzug vom lohn", "einbehaltene lohnsteuer"),
            AnnualTaxRefund = Extract(text, "erstattung", "r.ckzahlung", "nachzahlung"),
            AnnualPensionAndHealthContrib = Extract(text, "vorsorgeaufwendungen", "vorsorgeaufwand")
        };
    }

    private static decimal? Extract(string text, params string[] keys)
    {
        foreach (var key in keys)
        {
            var pattern = $"{key}[^0-9-]*([0-9]{{1,3}}(?:[\\.\\s][0-9]{{3}})*[,\\.]?[0-9]*)";
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                if (TryParseDecimal(match.Groups[1].Value, out var value))
                    return value;
            }
        }
        return null;
    }

    private static bool TryParseDecimal(string input, out decimal value)
    {
        // Normalize German/English formats
        var cleaned = input.Trim();
        // If both '.' and ',' exist, assume '.' as thousands and ',' as decimal
        if (cleaned.Contains('.') && cleaned.Contains(','))
        {
            cleaned = cleaned.Replace(".", "").Replace(',', '.');
        }
        else if (cleaned.Contains('.') && !cleaned.Contains(','))
        {
            // If only '.' present, assume decimal point
        }
        else if (cleaned.Contains(','))
        {
            cleaned = cleaned.Replace(".", "").Replace(',', '.');
        }

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }
}
