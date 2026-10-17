using System.Globalization;
using FinanceManager.Core.Interfaces;

namespace FinanceManager.Services.Import.Parsers;

public class GenericCsvBankParser : BaseCsvBankParser
{
    public override string ProfileName => "Generisches Bank CSV";
    public override char Delimiter => ';';
    public override string DateFormat => "dd.MM.yyyy";
    public override CultureInfo Culture => new("de-DE");

    private int _dateCol = -1;
    private int _amountCol = -1;
    private int _payeeCol = -1;
    private int _purposeCol = -1;
    private char _detectedDelimiter = ';';

    public override bool CanParse(string firstLines)
    {
        // Generic parser can always parse as fallback
        return true;
    }

    public override async Task<List<ParsedTransactionRow>> ParseAsync(Stream stream)
    {
        // First determine delimiter (semicolon vs comma vs tab)
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var testReader = new StreamReader(memoryStream, leaveOpen: true);
        string? sampleHeader = await testReader.ReadLineAsync();
        if (sampleHeader != null)
        {
            int semicolons = sampleHeader.Count(c => c == ';');
            int commas = sampleHeader.Count(c => c == ',');
            int tabs = sampleHeader.Count(c => c == '\t');

            if (semicolons >= commas && semicolons >= tabs) _detectedDelimiter = ';';
            else if (commas > semicolons && commas >= tabs) _detectedDelimiter = ',';
            else _detectedDelimiter = '\t';
        }

        memoryStream.Position = 0;
        return await base.ParseAsync(memoryStream);
    }

    protected override bool IsHeaderLine(string line)
    {
        var cols = SplitCsvLine(line, _detectedDelimiter);
        for (int i = 0; i < cols.Count; i++)
        {
            var col = cols[i].ToLowerInvariant();
            if (_dateCol == -1 && (col.Contains("datum") || col.Contains("date") || col.Contains("tag") || col.Contains("buchung")))
            {
                _dateCol = i;
            }
            else if (_amountCol == -1 && (col.Contains("betrag") || col.Contains("amount") || col.Contains("umsatz") || col.Contains("wert")))
            {
                _amountCol = i;
            }
            else if (_payeeCol == -1 && (col.Contains("empfänger") || col.Contains("empfaenger") || col.Contains("payee") || col.Contains("begünstigter") || col.Contains("beguenstigter") || col.Contains("partner") || col.Contains("name")))
            {
                _payeeCol = i;
            }
            else if (_purposeCol == -1 && (col.Contains("zweck") || col.Contains("purpose") || col.Contains("text") || col.Contains("verwendung") || col.Contains("beschreibung")))
            {
                _purposeCol = i;
            }
        }

        // If at least date and amount are identified, it's a valid header
        if (_dateCol != -1 && _amountCol != -1)
        {
            if (_payeeCol == -1) _payeeCol = _purposeCol != -1 ? _purposeCol : (_dateCol == 0 ? 1 : 0);
            if (_purposeCol == -1) _purposeCol = _payeeCol;
            return true;
        }

        return false;
    }

    protected override ParsedTransactionRow? ParseDataLine(string line)
    {
        var cols = SplitCsvLine(line, _detectedDelimiter);
        if (_dateCol == -1 || _amountCol == -1 || cols.Count <= Math.Max(_dateCol, _amountCol)) return null;

        var dateStr = cols[_dateCol];
        if (!DateTime.TryParse(dateStr, Culture, out var date))
        {
            if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, out date))
            {
                return null;
            }
        }

        decimal amount = ParseDecimal(cols[_amountCol], Culture);
        string payee = _payeeCol >= 0 && _payeeCol < cols.Count ? cols[_payeeCol] : "";
        string purpose = _purposeCol >= 0 && _purposeCol < cols.Count ? cols[_purposeCol] : "";

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
