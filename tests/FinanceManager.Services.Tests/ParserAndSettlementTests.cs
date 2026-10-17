using System.Text;
using FinanceManager.Core.Models;
using FinanceManager.Services.Categorization;
using FinanceManager.Services.Import;
using FinanceManager.Services.Import.Parsers;
using FinanceManager.Services.Settlement;
using Moq;
using Xunit;
using FinanceManager.Core.Interfaces;

namespace FinanceManager.Services.Tests;

public class ParserAndSettlementTests
{
    [Fact]
    public async Task SparkasseCsvParser_ShouldParseValidSample()
    {
        var parser = new SparkasseCsvParser();

        var csvContent =
            "Auftragskonto;Buchungstag;Valutadatum;Buchungstext;Verwendungszweck;Glaeubiger ID;Mandatsreferenz;Kundenreferenz (End-to-End);Sammlerreferenz;Lastschrift Ursprungsbetrag;Auslagenersatz Ruecklastschrift;Beguenstigter/Zahlungspflichtiger;Kontonummer/IBAN;BIC (SWIFT-Code);Betrag;Waehrung;Info\n" +
            "DE12345678;15.10.26;15.10.26;KARTENZAHLUNG;REWE SAGT DANKE;DE123;;;;;;REWE Supermarkt;DE9876;WELADED1XXX;-48,50;EUR;Umsatz gebucht\n" +
            "DE12345678;16.10.26;16.10.26;LOHN/GEHALT;GEHALT OKTOBER;;;;;;;ARBEITGEBER GMBH;DE1122;WELADED1XXX;2850,00;EUR;Umsatz gebucht";

        using var ms = new MemoryStream(Encoding.GetEncoding("ISO-8859-1").GetBytes(csvContent));
        var rows = await parser.ParseAsync(ms);

        Assert.Equal(2, rows.Count);
        Assert.Equal(-48.50m, rows[0].Amount);
        Assert.Equal("REWE Supermarkt", rows[0].PayeeOrPayer);
        Assert.Equal(2850.00m, rows[1].Amount);
        Assert.False(string.IsNullOrEmpty(rows[0].Fingerprint));
    }

    [Fact]
    public async Task GenericCsvBankParser_ShouldDetectColumnsAndParse()
    {
        var parser = new GenericCsvBankParser();

        var csvContent =
            "Datum;Empfaenger;Verwendungszweck;Betrag\n" +
            "12.10.2026;Aral Tankstelle;Tanken Super E10;-75,20\n" +
            "14.10.2026;Netflix Services;Monatsabo;-17,99";

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        var rows = await parser.ParseAsync(ms);

        Assert.Equal(2, rows.Count);
        Assert.Equal(-75.20m, rows[0].Amount);
        Assert.Equal("Aral Tankstelle", rows[0].PayeeOrPayer);
    }

    [Fact]
    public async Task SettlementService_ShouldCalculateFiftyFiftyCorrectly()
    {
        var mockRepo = new Mock<IFinanceRepository>();

        var p1 = new Person { Id = 1, Name = "Bernd", MonthlyNetIncome = 3000m };
        var p2 = new Person { Id = 2, Name = "Partnerin", MonthlyNetIncome = 3000m };

        mockRepo.Setup(r => r.GetPersonsAsync()).ReturnsAsync(new List<Person> { p1, p2 });

        // Bernd paid 120€, Partnerin paid 40€ -> Total 160€
        // Each share 50% = 80€
        // Bernd balance: 120 - 80 = +40€
        // Partnerin balance: 40 - 80 = -40€
        // Transfer: Partnerin pays Bernd 40€
        var txs = new List<Transaction>
        {
            new Transaction { Amount = -120m, PaidByPersonId = 1, IsHouseholdExpense = true, BookingDate = new DateTime(2026, 10, 5) },
            new Transaction { Amount = -40m, PaidByPersonId = 2, IsHouseholdExpense = true, BookingDate = new DateTime(2026, 10, 8) }
        };

        mockRepo.Setup(r => r.GetTransactionsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(txs);

        var service = new SettlementService(mockRepo.Object);
        var result = await service.CalculateSettlementAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), SplitMethod.FiftyFifty);

        Assert.Equal(160m, result.TotalHouseholdExpenses);
        Assert.Single(result.RecommendedTransfers);
        Assert.Equal("Partnerin", result.RecommendedTransfers[0].FromPerson);
        Assert.Equal("Bernd", result.RecommendedTransfers[0].ToPerson);
        Assert.Equal(40m, result.RecommendedTransfers[0].Amount);
    }

    [Fact]
    public async Task SettlementService_ShouldCalculateIncomeProportionalCorrectly()
    {
        var mockRepo = new Mock<IFinanceRepository>();

        // Person 1: 3000€ (60%), Person 2: 2000€ (40%) -> Total 5000€
        var p1 = new Person { Id = 1, Name = "Max", MonthlyNetIncome = 3000m };
        var p2 = new Person { Id = 2, Name = "Anna", MonthlyNetIncome = 2000m };

        mockRepo.Setup(r => r.GetPersonsAsync()).ReturnsAsync(new List<Person> { p1, p2 });

        // Total household expense = 1000€, both paid on Joint Account by Max (1000€)
        // Max target share: 60% = 600€, balance = +400€
        // Anna target share: 40% = 400€, balance = -400€
        var txs = new List<Transaction>
        {
            new Transaction { Amount = -1000m, PaidByPersonId = 1, IsHouseholdExpense = true, BookingDate = new DateTime(2026, 10, 1) }
        };

        mockRepo.Setup(r => r.GetTransactionsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(txs);

        var service = new SettlementService(mockRepo.Object);
        var result = await service.CalculateSettlementAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), SplitMethod.IncomeProportional);

        Assert.Equal(1000m, result.TotalHouseholdExpenses);
        Assert.Single(result.RecommendedTransfers);
        Assert.Equal("Anna", result.RecommendedTransfers[0].FromPerson);
        Assert.Equal("Max", result.RecommendedTransfers[0].ToPerson);
        Assert.Equal(400m, result.RecommendedTransfers[0].Amount);
    }
}

