using Kakeibo.Core.Accounts;
using Kakeibo.Core.Categories;
using Kakeibo.Core.Data;
using Kakeibo.Core.Transactions;
using Kakeibo.Desktop.ViewModels;
using Kakeibo.Desktop.Views;
using Microsoft.Extensions.Logging;

namespace Kakeibo.Desktop;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton(TimeProvider.System);
		builder.Services.AddSingleton<ICurrentUser, LocalUser>();
		builder.Services.AddSingleton(_ => new KakeiboDatabase(Path.Combine(FileSystem.AppDataDirectory, "kakeibo.db3")));
		builder.Services.AddSingleton<ITransactionRepository, SqliteTransactionRepository>();
		builder.Services.AddSingleton<ICategoryRepository, SqliteCategoryRepository>();

		builder.Services.AddTransient<MonthlyTransactionsViewModel>();
		builder.Services.AddTransient<MonthlyTransactionsPage>();
		builder.Services.AddTransient<ReportViewModel>();
		builder.Services.AddTransient<ReportPage>();
		builder.Services.AddTransient<CategorySettingsViewModel>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
