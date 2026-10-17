using System.Globalization;
using FinanceManager.Core.Interfaces;

namespace FinanceManager.Services.Import.Parsers;

public class SparkasseCsvParser : BaseCsvBankParser
{
    public override string ProfileName => "Sparkasse Standard";
    public override char Delimiter => ';';
    public override string DateFormat => "dd.MM.yy";
    public override CultureInfo Culture => new("de-DE");

    public override bool CanParse(string firstLines)
    {
        return firstLines.Contains("Auftragskonto") &&
               (firstLines.Contains("Buchungstag") || firstLines.Contains("Buchungsdatum")) &&
               (firstLines.Contains("Beguenstigter") || firstLines.Contains("Begünstigter") || firstLines.Contains("Zahlungsempfänger*in"));
    }

    protected override bool IsHeaderLine(string line)
    {
        return line.Contains("Buchungstag") || line.Contains("Buchungsdatum");
    }

    protected override ParsedTransactionRow? ParseDataLine(string line)
    {
        var cols = SplitCsvLine(line, Delimiter);
        if (cols.Count < 5) return null;

        // Sparkasse format typical columns:
        // Auftragskonto;Buchungstag;Valutadatum;Buchungstext;Verwendungszweck;Glaeubiger ID;Mandatsreferenz;Kundenreferenz (End-to-End);Sammlerreferenz;Lastschrift Ursprungsbetrag;Auslagenersatz Ruecklastschrift;Beguenstigter/Zahlungspflichtiger;Kontonummer/IBAN;BIC (SWIFT-Code);Betrag;Waehrung;Info
        // Let's find columns flexibly
        DateTime date = DateTime.MinValue;
        decimal amount = 0m;
        string payee = "";
        string purpose = "";

        // Sparkasse typically:
        // Col 1: Buchungstag
        // Col 4: Verwendungszweck
        // Col 11: Beguenstigter
        // Col 14: Betrag
        if (DateTime.TryParseExact(cols[1], new[] { "dd.MM.yy", "dd.MM.yyyy" }, Culture, DateTimeStyles.None, out var d))
        {
            date = d;
        }
        else if (!DateTime.TryParse(cols[1], Culture, out date))
        {
            return null;
        }

        if (cols.Count >= 15)
        {
            purpose = cols[4];
            payee = cols[11];
            amount = ParseDecimal(cols[14], Culture);
        }
        else if (cols.Count >= 8)
        {
            payee = cols[2];
            purpose = cols[3];
            amount = ParseDecimal(cols[^1], Culture);
        }

        var fp = GenerateFingerprint(date, amount, payee, purpose);

        return new ParsedTransactionRow
        {
            BookingDate = date,
            Amount = amount,
            PayeeOrPayer = payee,
            Purpose = purpose,
            RawRow = line,
            Fingerprint = fp
        };
    }
}

