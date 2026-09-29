using Kakeibo.Core.Accounts;
using Kakeibo.Core.Data;
using Kakeibo.Core.Transactions;
using Microsoft.Extensions.Time.Testing;

namespace Kakeibo.Core.Tests;

public sealed class SqliteTransactionRepositoryTests : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"kakeibo-test-{Guid.NewGuid()}.db3");
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));
    private KakeiboDatabase _database = null!;
    private SqliteTransactionRepository _repository = null!;

    public Task InitializeAsync()
    {
        _database = new KakeiboDatabase(_path);
        _repository = new SqliteTransactionRepository(_database, new LocalUser(), _clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
        File.Delete(_path);
    }

    private static TransactionDraft Draft(DateOnly date, long amount = 1000, string category = "食費") =>
        new(date, TransactionKind.Expense, amount, category, "");

    [Fact]
    public async Task Add_sets_sync_metadata()
    {
        var added = await _repository.AddAsync(Draft(new DateOnly(2026, 9, 1)));

        Assert.NotEqual(Guid.Empty, added.Id);
        Assert.Equal(LocalUser.LocalUserId, added.UserId);
        Assert.Equal(_clock.GetUtcNow(), added.UpdatedAt);
        Assert.Equal(1, added.Version);
        Assert.False(added.Deleted);
        Assert.Equal(added, await _repository.FindAsync(added.Id));
    }

    [Fact]
    public async Task Update_bumps_version_and_updated_at()
    {
        var added = await _repository.AddAsync(Draft(new DateOnly(2026, 9, 1)));
        _clock.Advance(TimeSpan.FromMinutes(5));

        var updated = await _repository.UpdateAsync(added.Id, Draft(new DateOnly(2026, 9, 2), amount: 2500, category: " 日用品 "));

        Assert.Equal(added.Id, updated.Id);
        Assert.Equal(new DateOnly(2026, 9, 2), updated.Date);
        Assert.Equal(2500, updated.Amount);
        Assert.Equal("日用品", updated.Category);
        Assert.Equal(2, updated.Version);
        Assert.Equal(added.UpdatedAt.AddMinutes(5), updated.UpdatedAt);
    }

    [Fact]
    public async Task Delete_is_logical_and_hides_the_row()
    {
        var added = await _repository.AddAsync(Draft(new DateOnly(2026, 9, 1)));

        await _repository.DeleteAsync(added.Id);

        Assert.Null(await _repository.FindAsync(added.Id));
        Assert.Empty(await _repository.GetByMonthAsync(2026, 9));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _repository.UpdateAsync(added.Id, Draft(new DateOnly(2026, 9, 1))));
    }

    [Fact]
    public async Task GetByMonth_returns_only_that_month_newest_first()
    {
        await _repository.AddAsync(Draft(new DateOnly(2026, 8, 31)));
        var first = await _repository.AddAsync(Draft(new DateOnly(2026, 9, 1)));
        var last = await _repository.AddAsync(Draft(new DateOnly(2026, 9, 30)));
        await _repository.AddAsync(Draft(new DateOnly(2026, 10, 1)));

        var result = await _repository.GetByMonthAsync(2026, 9);

        Assert.Equal([last.Id, first.Id], result.Select(t => t.Id));
    }

    [Fact]
    public async Task Other_users_rows_are_not_visible()
    {
        var added = await _repository.AddAsync(Draft(new DateOnly(2026, 9, 1)));
        var otherUser = new SqliteTransactionRepository(_database, new FixedUser("someone-else"), _clock);

        Assert.Null(await otherUser.FindAsync(added.Id));
        Assert.Empty(await otherUser.GetByMonthAsync(2026, 9));
    }

    [Theory]
    [InlineData(0, "食費")]
    [InlineData(-100, "食費")]
    [InlineData(100, " ")]
    public async Task Invalid_draft_is_rejected(long amount, string category)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _repository.AddAsync(Draft(new DateOnly(2026, 9, 1), amount, category)));
    }

    private sealed class FixedUser(string userId) : ICurrentUser
    {
        public string UserId => userId;
    }
}
