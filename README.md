# FinanceManager - Lokale Haushaltsbuch- und Mehrpersonen-Finanzverwaltung

Eine moderne, datenschutzfreundliche Desktop-Anwendung in C# / .NET 8/10 Windows Forms zur Verwaltung persönlicher Finanzen, Fixkosten, Bankumsätzen und gemeinsamer Haushaltsausgaben.

---

## 🏛️ Architekturübersicht

Die Solution folgt strikt den Clean-Architecture-Prinzipien:

- **`FinanceManager.Core`**:
  - Datenmodelle: `Person`, `Account`, `Category`, `CategoryRule`, `Transaction`, `TransactionSplit`, `ImportBatch`, `ImportProfile`, `SettlementModels`
  - Schnittstellen: `IFinanceRepository`, `IBankStatementParser`, `IImportService`, `ISettlementService`, `ICategoryService`
  - Völlig frei von UI- oder Datenbankabhängigkeiten.

- **`FinanceManager.Data`**:
  - `FinanceDbContext`: Entity Framework Core mit SQLite-Datenbank (`%LocalAppData%/FinanceManager/FinanceManager.db`).
  - `SqliteFinanceRepository`: Implementierung mit automatischem Seeding von Standardkategorien, Haushaltsmitgliedern, Konten und Auto-Kategorisierungsregeln.

- **`FinanceManager.Services`**:
  - **Bank- & CSV-Import-Pipeline**:
    - `BaseCsvBankParser`: Basisklasse mit Erkennung von Zeichenkodierungen (UTF-8, Windows-1252, ISO-8859-1), CSV-Escaping und SHA-256-Fingerprint-Generierung.
    - `SparkasseCsvParser`: Spezialisierter Parser für Sparkassen-Kontoauszüge.
    - `IngCsvParser`: Spezialisierter Parser für ING DiBa Girokonten.
    - `GenericCsvBankParser`: Universeller Parser mit automatischer Trennzeichen-Erkennung (`;`, `,`, Tab) und intelligenter Spaltenerkennung.
    - `ImportService`: Dublettenprüfung gegen vorhandene Fingerprints, Vorab-Kategorisierung, Staging & Batch-Import.
  - **Mehrpersonen-Haushaltsausgleich (`SettlementService`)**:
    - Berechnung gemeinsamer Haushaltsausgaben.
    - Aufteilungsmethoden: **50:50 (Gleichmäßig)** oder **Einkommens-proportional (Fair Share)** anhand des monatlichen Nettoeinkommens.
    - Automatische Ermittlung von Ausgleichszahlungen („Wer zahlt wem wie viel?“).
  - **Auto-Kategorisierung (`CategoryService`)**:
    - Regelbasiertes Erkennen von Ausgaben anhand von Mustern/Keywords in Empfänger und Verwendungszweck.

- **`FinanceManager.UI`**:
  - Windows Forms Präsentationsschicht mit `Microsoft.Extensions.DependencyInjection`.
  - `MainForm`: Dashboard mit Live-Metrik-Karten (Gesamtsaldo, Einnahmen, Ausgaben, Gemeinsame Haushaltsausgaben), Filter nach Konto/Person/Zeitraum und Buchungstabelle.
  - `ImportDialog`: CSV-Import-Assistent mit Vorschau-Gitter, farblicher Hervorhebung von neuen Buchungen vs. Dubletten, Konten- und Personenauswahl.
  - `SettlementDialog`: Haushalts-Ausgleichsrechner mit Soll/Ist-Vergleich und Transfer-Empfehlungen.
  - `PersonsDialog`: Verwaltung von Haushaltsmitgliedern und monatlichen Nettoeinkommen.
  - `StatCard`: Modernes WinForms Widget für Dashboard-Kennzahlen.

- **`FinanceManager.Services.Tests`**:
  - xUnit-Testsuite zur Verifikation der Bank-Parser, Dubletten-Fingerprints und der Kostenteilungslogik (50:50 und einkommensproportional).

---

## 🚀 Schnellstart

```bash
# Solution kompilieren
dotnet build FinanceManager.slnx

# Unit-Tests ausführen
dotnet test tests/FinanceManager.Services.Tests/FinanceManager.Services.Tests.csproj

# WinForms-Anwendung starten
dotnet run --project src/FinanceManager.UI/FinanceManager.UI.csproj
```

