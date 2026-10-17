using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FinanceManager.Core.Interfaces;

namespace FinanceManager.Services.Import;

public abstract class BaseCsvBankParser : IBankStatementParser
{
    public abstract string ProfileName { get; }
    public abstract char Delimiter { get; }
    public abstract string DateFormat { get; }
    public abstract CultureInfo Culture { get; }

    public abstract bool CanParse(string firstLines);

    public virtual async Task<List<ParsedTransactionRow>> ParseAsync(Stream stream)
    {
        var rows = new List<ParsedTransactionRow>();

        // Autodetect encoding (UTF8 with BOM / ANSI / Windows-1252)
        using var reader = new StreamReader(stream, Encoding.GetEncoding("ISO-8859-1"), detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        
        string? line;
        bool headerFound = false;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (!headerFound)
            {
                if (IsHeaderLine(line))
                {
                    headerFound = true;
                }
                continue;
            }

            var parsed = ParseDataLine(line);
            if (parsed != null)
            {
                rows.Add(parsed);
            }
        }

        return rows;
    }

    protected abstract bool IsHeaderLine(string line);
    protected abstract ParsedTransactionRow? ParseDataLine(string line);

    public static string GenerateFingerprint(DateTime date, decimal amount, string payee, string purpose)
    {
        var raw = $"{date:yyyyMMdd}_{amount:F2}_{payee.Trim().ToUpperInvariant()}_{purpose.Trim().ToUpperInvariant()}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).Substring(0, 32);
    }

    protected static List<string> SplitCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    current.Append('\"');
                    i++; // Skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(current.ToString().Trim(' ', '\"'));
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString().Trim(' ', '\"'));
        return result;
    }

    protected static decimal ParseDecimal(string value, CultureInfo culture)
    {
        var cleaned = value.Replace("€", "").Replace("EUR", "").Trim();
        if (decimal.TryParse(cleaned, NumberStyles.Any, culture, out var result))
        {
            return result;
        }

        // Fallback for inverted separators
        cleaned = cleaned.Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        return 0m;
    }
}

