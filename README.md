# Kakeibo

English | [日本語](README.ja.md)

A household budget book ("kakeibo") app, developed as a personal project. Record your income and expenses, and review monthly balances and breakdowns by category.

The desktop app (macOS / Windows) currently works offline. Sign-in, cloud sync, and a web app are planned.

The app UI is in Japanese. This README shows the Japanese label next to its English meaning, for example **明細** (Transactions).

- [Architecture](docs/architecture.md): system overview, data design, decisions and open questions
- [Ubiquitous language](docs/ubiquitous-language.md): glossary

![Transactions screen](docs/images/transactions.png)

## Features

- Add, edit, and delete transactions (date, expense/income, amount, category, memo)
- View each month as a ledger, with a running balance carried over from the previous month
- View a monthly trend for the year (income, expenses, difference, month-end balance) and a category breakdown for each month
- Add, rename, reorder, and delete categories

## Usage

Use the sidebar on the left to switch between **明細** (Transactions), **集計** (Reports), and **設定** (Settings).

### Record a transaction

1. Select **明細** (Transactions) in the sidebar.
2. In the entry form at the top, enter the date, **支出** (expense) or **収入** (income), the amount, the category, and a memo.
   - The date defaults to today if you are viewing the current month, or to the 1st of the month otherwise.
   - The category list shows only the categories for the selected type (expense or income).
   - The memo is optional.
3. Click **追加** (Add). You can also press Enter in the amount or memo field.

After you add a transaction, the date and type stay the same and the other fields are cleared, so you can enter the next one right away.
If you add a transaction dated in a different month, the view switches to that month.

### Edit or delete a transaction

1. Click a transaction in the list.
2. Its details are loaded into the entry form, and the form is highlighted to show that you are editing.
3. Change the details and click **更新** (Update), or click **削除** (Delete) to delete it.
4. Click **キャンセル** (Cancel) to stop editing.

![Editing a transaction](docs/images/editing.png)

Deleted transactions cannot be restored.

### Check the monthly balance

On the Transactions screen, use `‹` and `›` to switch months.

- The top right shows the month's **収入** (income), **支出** (expenses), and **差額** (difference = income − expenses). The difference is shown in red when the month is in deficit.
- The list is sorted from oldest to newest. The **残高** (balance) column accumulates from **前月繰越** (carried over from the previous month).

The balance is calculated from the transactions recorded in the app. It may differ from the actual balance of your wallet or bank account.

### View the yearly trend and category breakdown

1. Select **集計** (Reports) in the sidebar.
2. Use `‹` and `›` to switch years.
3. Click a month in the table to show its **category breakdown** below. You can switch between expenses and income.

Months that have not come yet are shown as "-".

![Reports screen](docs/images/report.png)

### Manage categories

Select **設定** (Settings) in the sidebar. Expense categories and income categories are listed side by side.

| Action | How |
|---|---|
| Add | Enter a name in the field below the list and click **追加** (Add) |
| Rename | Click **名前変更** (Rename) and enter a new name. Past transactions show the new name too |
| Reorder | Use **↑** and **↓**. The category list in the entry form follows this order |
| Delete | Click **削除** (Delete). The category is removed from the entry form, but existing transactions keep its name |

Common categories (food, household goods, salary, and so on) are created on first launch.

![Settings screen](docs/images/settings.png)

### Where data is stored

Data is stored on your device in SQLite. It is not sent to the cloud yet.

| OS | Location |
|---|---|
| macOS | `~/Library/Containers/dev.redamoon.kakeibo.desktop/Data/Library/kakeibo.db3` |

## Development

### Requirements (macOS)

| Tool | Version | Notes |
|---|---|---|
| macOS | Apple Silicon | A version that can run Xcode 27 |
| Xcode | **27.0** | Its major.minor version must match the MAUI workload |
| .NET SDK | **10.0.401** or a later 10.0.4xx | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| .NET MAUI workload | **10.0.401.1** | Pinned by `workloadVersion` in `global.json` |

The SDK and workload versions are pinned in [global.json](global.json).

### Setup

```sh
git clone https://github.com/redamoon/kakeibo.git
cd kakeibo

# Select Xcode and accept its license
sudo xcode-select -s /Applications/Xcode.app
sudo xcodebuild -license accept
sudo xcodebuild -runFirstLaunch

# Install the MAUI workload and match the version in global.json
sudo dotnet workload install maui
sudo dotnet workload update --version 10.0.401.1

# Verify: the output should say that workload version 10.0.401.1 from global.json is used
dotnet workload list
```

### Build and run

```sh
# Build and launch the Mac Catalyst app
dotnet build src/Kakeibo.Desktop -f net10.0-maccatalyst
open "src/Kakeibo.Desktop/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/家計簿.app"

# Run the tests (Core only; MAUI is not required)
dotnet test tests/Kakeibo.Core.Tests
```

The Windows app (`net10.0-windows10.0.19041.0`) can only be built on Windows. The Windows build has not been verified yet.

### Troubleshooting

**`This version of .NET for MacCatalyst (x) requires Xcode y`**

Your Xcode version does not match the workload. The MAUI Mac Catalyst SDK builds only when the major.minor version of Xcode matches exactly.

- Check your Xcode version with `xcodebuild -version`.
- After updating Xcode, find a matching workload version with `dotnet workload search version`. Then update both `workloadVersion` in `global.json` and the installed workload with `sudo dotnet workload update --version <version>`.

**`You have not agreed to the Xcode license agreements`**

This appears right after installing or updating Xcode. Run `sudo xcodebuild -license accept`.

### Project structure

```
src/Kakeibo.Core/          Domain and data access (no dependency on MAUI)
src/Kakeibo.Desktop/       .NET MAUI desktop app
tests/Kakeibo.Core.Tests/  Unit tests for Core
docs/                      Design documents
```

See [Architecture](docs/architecture.md) for details.
