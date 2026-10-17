using System.Globalization;
using FinanceManager.Core.Interfaces;

namespace FinanceManager.Services.Import.Parsers;

public class IngCsvParser : BaseCsvBankParser
{
    public override string ProfileName => "ING DiBa";
    public override char Delimiter => ';';
    public override string DateFormat => "dd.MM.yyyy";
    public override CultureInfo Culture => new("de-DE");

    public override bool CanParse(string firstLines)
    {
        return (firstLines.Contains("IBAN") || firstLines.Contains("ING-DiBa") || firstLines.Contains("Girokonto")) &&
               firstLines.Contains("Buchung") &&
               (firstLines.Contains("Auftraggeber/Empfänger") || firstLines.Contains("Empfänger/Zahlungspflichtiger"));
    }

    protected override bool IsHeaderLine(string line)
    {
        return line.StartsWith("Buchung;") || (line.Contains("Buchung") && line.Contains("Betrag"));
    }

    protected override ParsedTransactionRow? ParseDataLine(string line)
    {
        var cols = SplitCsvLine(line, Delimiter);
        if (cols.Count < 6) return null;

        // Typical ING format:
        // Buchung;Valuta;Auftraggeber/Empfänger;Buchungstext;Verwendungszweck;Saldo;Währung;Betrag;Währung
        // Or:
        // Buchung;Valuta;Auftraggeber/Empfänger;Buchungstext;Verwendungszweck;Betrag;Währung...
        if (!DateTime.TryParseExact(cols[0], "dd.MM.yyyy", Culture, DateTimeStyles.None, out var date))
        {
            if (!DateTime.TryParse(cols[0], Culture, out date)) return null;
        }

        string payee = cols[2];
        string purpose = cols.Count > 4 ? cols[4] : "";
        decimal amount = 0m;

        // Betrag is usually column 5 or 7
        if (cols.Count >= 8 && decimal.TryParse(cols[7].Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var a8))
        {
            amount = a8;
        }
        else if (cols.Count >= 6 && decimal.TryParse(cols[5].Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var a5))
        {
            amount = a5;
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

