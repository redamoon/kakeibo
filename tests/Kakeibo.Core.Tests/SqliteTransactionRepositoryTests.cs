using Kakeibo.Core.Accounts;
using Kakeibo.Core.Categories;
using Kakeibo.Core.Data;
using Kakeibo.Core.Transactions;

namespace Kakeibo.Core.Tests;

public sealed class SqliteTransactionRepositoryTests : TestDatabase
{
    private SqliteTransactionRepository _repository = null!;
    private Category _food = null!;
    private Category _goods = null!;
    private Category _salary = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _repository = new SqliteTransactionRepository(Database, User, Clock);

        var categories = await new SqliteCategoryRepository(Database, User, Clock).GetAllAsync();
        _food = categories.Single(c => c is { Kind: TransactionKind.Expense, Name: "食費" });
        _goods = categories.Single(c => c is { Kind: TransactionKind.Expense, Name: "日用品" });
        _salary = categories.Single(c => c is { Kind: TransactionKind.Income, Name: "給与" });
    }

    private TransactionDraft Draft(DateOnly date, long amount = 1000, Category? category = null) =>
        new(date, (category ?? _food).Kind, amount, (category ?? _food).Id, "");

    [Fact]
    public async Task Add_sets_sync_metadata()
    {
        var added = await _repository.AddAsync(Draft(new DateOnly(2026, 9, 1)));

        Assert.NotEqual(Guid.Empty, added.Id);
        Assert.Equal(LocalUser.LocalUserId, added.UserId);
        Assert.Equal(_food.Id, added.CategoryId);
        Assert.Equal(Clock.GetUtcNow(), added.UpdatedAt);
        Assert.Equal(1, added.Version);
        Assert.False(added.Deleted);
        Assert.Equal(added, await _repository.FindAsync(added.Id));
    }

    [Fact]
    public async Task Update_bumps_version_and_updated_at()
    {
        var added = await _repository.AddAsync(Draft(new DateOnly(2026, 9, 1)));
        Clock.Advance(TimeSpan.FromMinutes(5));

        var updated = await _repository.UpdateAsync(added.Id, Draft(new DateOnly(2026, 9, 2), amount: 2500, category: _goods));

        Assert.Equal(added.Id, updated.Id);
        Assert.Equal(new DateOnly(2026, 9, 2), updated.Date);
        Assert.Equal(2500, updated.Amount);
        Assert.Equal(_goods.Id, updated.CategoryId);
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
    public async Task GetByMonth_returns_only_that_month()
    {
        await _repository.AddAsync(Draft(new DateOnly(2026, 8, 31)));
        var first = await _repository.AddAsync(Draft(new DateOnly(2026, 9, 1)));
        var last = await _repository.AddAsync(Draft(new DateOnly(2026, 9, 30)));
        await _repository.AddAsync(Draft(new DateOnly(2026, 10, 1)));

        var result = await _repository.GetByMonthAsync(2026, 9);

        // 並び順は MonthlyLedger が決めるので、ここでは件数と中身だけを確かめる
        Assert.Equal(new[] { first.Id, last.Id }.Order(), result.Select(t => t.Id).Order());
    }

    [Fact]
    public async Task Other_users_rows_are_not_visible()
    {
        var added = await _repository.AddAsync(Draft(new DateOnly(2026, 9, 1)));
        var otherUser = new SqliteTransactionRepository(Database, new FixedUser("someone-else"), Clock);

        Assert.Null(await otherUser.FindAsync(added.Id));
        Assert.Empty(await otherUser.GetByMonthAsync(2026, 9));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Non_positive_amount_is_rejected(long amount)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _repository.AddAsync(Draft(new DateOnly(2026, 9, 1), amount)));
    }

    [Fact]
    public async Task Category_must_be_selected()
    {
        var draft = new TransactionDraft(new DateOnly(2026, 9, 1), TransactionKind.Expense, 100, Guid.Empty, "");

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _repository.AddAsync(draft));
    }

    [Fact]
    public async Task Category_kind_must_match_transaction_kind()
    {
        var draft = new TransactionDraft(new DateOnly(2026, 9, 1), TransactionKind.Expense, 100, _salary.Id, "");

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _repository.AddAsync(draft));
    }

    [Fact]
    public async Task Other_users_category_is_rejected()
    {
        var otherCategories = await new SqliteCategoryRepository(Database, new FixedUser("someone-else"), Clock).GetAllAsync();
        var othersFood = otherCategories.Single(c => c is { Kind: TransactionKind.Expense, Name: "食費" });

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _repository.AddAsync(Draft(new DateOnly(2026, 9, 1), category: othersFood)));
    }

    [Fact]
    public async Task Transaction_with_deleted_category_can_still_be_edited()
    {
        var added = await _repository.AddAsync(Draft(new DateOnly(2026, 9, 1)));
        await new SqliteCategoryRepository(Database, User, Clock).DeleteAsync(_food.Id);

        var updated = await _repository.UpdateAsync(added.Id, Draft(new DateOnly(2026, 9, 1), amount: 1200));

        Assert.Equal(_food.Id, updated.CategoryId);
    }
}
