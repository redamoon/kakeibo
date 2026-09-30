using Kakeibo.Core.Accounts;
using Kakeibo.Core.Data;
using Microsoft.Extensions.Time.Testing;

namespace Kakeibo.Core.Tests;

/// <summary>テストごとに一時ファイルの SQLite を用意する。</summary>
public abstract class TestDatabase : IAsyncLifetime
{
    protected string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"kakeibo-test-{Guid.NewGuid()}.db3");

    protected FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));

    protected KakeiboDatabase Database { get; private set; } = null!;

    protected ICurrentUser User { get; set; } = new LocalUser();

    public virtual Task InitializeAsync()
    {
        Database = new KakeiboDatabase(Path);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Database.DisposeAsync();
        File.Delete(Path);
    }

    protected sealed class FixedUser(string userId) : ICurrentUser
    {
        public string UserId => userId;
    }
}
