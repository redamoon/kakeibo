# Ubiquitous language

English | [日本語](ubiquitous-language.ja.md)

A glossary for using the same words in the UI, documents, conversations, and code.
The UI is in Japanese, so each term lists its Japanese UI label alongside its name in code.

## Wording rules

- A single record in the budget book is called a **transaction** (UI: **明細**). In Japanese, do not use 「取引」「仕訳」 or 「レコード」
- Money going out and coming in is called **expense** (**支出**) and **income** (**収入**). In Japanese, do not use 「出金」「入金」「借方」 or 「貸方」
- Amounts are integers in yen. The UI shows them with thousands separators, and the unit (円) is shown in column headers

## Terms

### Transactions and categories

| Term | Japanese (UI) | Name in code | Meaning |
|---|---|---|---|
| Transaction | 明細 | `Transaction` | A single record in the budget book. Has a date, kind, amount, category, and memo |
| Kind | 種類 | `TransactionKind` | Whether a transaction or category is an **expense** (`Expense`, 支出) or **income** (`Income`, 収入) |
| Amount | 金額 | `Amount` | The amount of a transaction. An integer of at least 1 yen. Stored as a positive value even for expenses |
| Memo | メモ | `Memo` | Optional note on a transaction |
| Category | カテゴリ | `Category` | Classification of a transaction (food, salary, etc.). Separate for expenses and income |
| Default categories | 標準カテゴリ | `DefaultCategories` | Categories created automatically on first launch |
| Sort order | 並び順 | `SortOrder` | Order of categories in the entry form and settings screen. Starts at 0 for each kind |
| Entry form | 入力欄 | (UI) | The form at the top of the Transactions screen for adding and editing transactions |

### Aggregation

| Term | Japanese (UI) | Name in code | Meaning |
|---|---|---|---|
| Ledger | 帳簿 | `MonthlyLedger` | A month's transactions sorted from oldest to newest, with a balance. The table on the Transactions screen |
| Balance | 残高 | `LedgerRow.Balance` | Running total up to that row of the ledger. Carryover + income − expenses for the month |
| Carryover from the previous month | 前月繰越 | `MonthlyLedger.OpeningBalance` | Net total of all transactions before the month. The starting point of the ledger balance |
| Month-end balance | 月末残高 | `MonthlyLedger.ClosingBalance` / `MonthSummary.ClosingBalance` | The last balance of the month |
| Carryover from the previous year | 前年繰越 | `YearlySummary.OpeningBalance` | Net total of all transactions before the year |
| Difference | 差額 | `Difference` | Income − expenses for the period. Positive means a surplus, negative means a deficit |
| Yearly summary | 年間推移 | `YearlySummary` | Income, expenses, difference, and month-end balance for each month of a year, plus yearly totals |
| Category breakdown | カテゴリ別内訳 | `CategoryBreakdown` | Total and share per category for the selected month. Shown separately for expenses and income |

The "balance" is calculated from the transactions recorded in the app. It may differ from the actual balance of a wallet or bank account.

### Data and sync

| Term | Japanese | Name in code | Meaning |
|---|---|---|---|
| User | 利用者 | `ICurrentUser` / `UserId` | Owner of the data. `local` until sign-in is implemented |
| Soft delete | 論理削除 | `Deleted` | Marking a row as deleted instead of removing it, so that deletions can be sent to other devices |
| Version | 版(バージョン) | `Version` | Number of times a row has been updated. 1 when created |
| Last update time | 最終更新時刻 | `UpdatedAt` | When the row was last updated (UTC). Used to resolve conflicts |
| Schema version | スキーマの版 | `PRAGMA user_version` | Version of the local DB structure. Raised by migrations |
| Sync | 同期 | (not implemented) | Keeping the desktop's local DB and the cloud DB consistent |
| push | push | (not implemented) | The part of sync that sends the device's changes to the server |
| pull | pull | (not implemented) | The part of sync that fetches the server's changes since the last sync |
| Last Write Wins | Last Write Wins | (not implemented) | Conflict rule: the row with the more recent last update time wins |
