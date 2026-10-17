---
project: finance-manager-winforms
description: Lokale Haushaltsbuch- und Finanzmanagement-Anwendung in C# / .NET 8 Windows Forms.
agents:
  - csharp-developer
  - winforms-ui-architect
  - code-reviewer
guidelines:
  - Folge strikt den Clean-Code-Prinzipien für .NET 8.
  - Verwende ausschließlich Windows Forms mit modernen Steuerelementen und Layout-Containern.
  - Halte die Geschäftslogik strikt von der UI getrennt (Services / Repositories).
  - Dokumentiere Änderungen und teste kritische Berechnungen (Einnahmen, Ausgaben, Salden).
---

# Projektkontext: Lokaler Finanzmanager (C# / .NET 8 WinForms)

Dieses Projekt umfasst die Entwicklung einer maßgeschneiderten Desktop-Anwendung zur Verwaltung persönlicher Finanzen, Fixkosten und Einnahmen/Ausgaben – komplett offline und datenschutzfreundlich.

### Verfügbare Subagents & Rollen:
1. `@csharp-developer`: Erstellt die Kernlogik, Datenmodelle, Datenbankschicht und Services.
2. `@winforms-ui-architect`: Entwirft und optimiert die Windows Forms-Oberfläche, Dialoge und DataGrids.
3. `@code-reviewer`: Validiert Implementierungen, prüft auf Sauberkeit, Performance und Robustheit.

---

### 🏛️ Modulaufbau der Anwendung

Die Anwendung ist in folgende logische Module und Schichten unterteilt:

```text
FinanceManager/
│
├── Core/                         # Gemeinsame Modelle, Enums und Schnittstellen
│   ├── Models/                   # Datenmodelle (Transaction, RecurringExpense, Category, Budget)
│   └── Interfaces/               # Service-Verträge (IRepository, IBudgetService, etc.)
│
├── Data/                         # Datenzugriffsschicht
│   ├── DatabaseContext.sqlite    # Lokale SQLite-Datenbankdatei
│   └── Repositories/             # Datenzugriffslogik (TransactionRepository, CategoryRepository)
│
├── Services/                     # Geschäftslogik & Berechnungen
│   ├── FinanceCalculator.cs      # Saldenberechnung, Prognosen, monatliche Auswertung
│   └── ExportService.cs          # CSV / PDF Export-Funktionen
│
├── UI/                           # Präsentationsschicht (Windows Forms)
│   ├── Forms/                    # Haupt- und Dialogfenster
│   │   ├── MainForm.cs           # Dashboard & Hauptnavigation
│   │   ├── TransactionForm.cs    # Erfassung & Bearbeitung von Einnahmen/Ausgaben
│   │   ├── RecurringForm.cs      # Verwaltung von Fixkosten
│   │   └── StatisticsForm.cs     # Grafische Auswertungen & Charts
│   └── Controls/                 # Wiederverwendbare Custom Controls / Widgets
│
└── Program.cs                    # Einstiegspunkt & DI-Container Konfiguration
```

#### Modul-Beschreibungen:
1. **Core:** Enthält die reinen Datenstrukturen (`records` / `classes`) und fachlichen Grundregeln ohne UI- oder Datenbankabhängigkeiten.
2. **Data:** Kapselt den SQLite-Datenbankzugriff (z. B. via Entity Framework Core oder Dapper) und stellt sicher, dass alle Transaktionen performant persistiert werden.
3. **Services:** Beherbergt die gesamte Berechnungslogik (z. B. monatliche Fixkosten-Projektion, Kategorisierung, Saldenermittlung).
4. **UI:** Die Windows Forms Schicht. Das `MainForm` dient als zentrale Anlaufstelle mit Dashboard-Widgets, während Spezialformulare für die Dateneingabe und Auswertung (Charts) bereitstehen.
