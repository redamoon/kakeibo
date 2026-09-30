using Kakeibo.Core.Categories;
using Kakeibo.Core.Data;
using Kakeibo.Core.Transactions;

namespace Kakeibo.Core.Tests;

public sealed class SqliteCategoryRepositoryTests : TestDatabase
{
    private SqliteCategoryRepository _repository = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _repository = new SqliteCategoryRepository(Database, User, Clock);
    }

    private async Task<List<string>> ActiveNamesAsync(TransactionKind kind) =>
        (await _repository.GetAllAsync()).Where(c => c.Kind == kind && !c.Deleted).Select(c => c.Name).ToList();

    [Fact]
    public async Task Defaults_are_seeded_once()
    {
        var first = await _repository.GetAllAsync();
        var second = await _repository.GetAllAsync();

        Assert.Equal(DefaultCategories.All.Count, first.Count);
        Assert.Equal(first.Select(c => c.Id), second.Select(c => c.Id));
        Assert.Equal(
            DefaultCategories.All.Where(c => c.Kind == TransactionKind.Expense).Select(c => c.Name),
            await ActiveNamesAsync(TransactionKind.Expense));
    }

    [Fact]
    public async Task Defaults_are_not_reseeded_after_all_are_deleted()
    {
        foreach (var category in await _repository.GetAllAsync())
        {
            await _repository.DeleteAsync(category.Id);
        }

        var all = await _repository.GetAllAsync();

        Assert.Equal(DefaultCategories.All.Count, all.Count);
        Assert.All(all, c => Assert.True(c.Deleted));
    }

    [Fact]
    public async Task Add_appends_to_the_end_of_the_same_kind()
    {
        var added = await _repository.AddAsync(TransactionKind.Income, " 副業 ");

        Assert.Equal("副業", added.Name);
        Assert.Equal("副業", (await ActiveNamesAsync(TransactionKind.Income)).Last());
    }

    [Fact]
    public async Task Duplicate_name_in_same_kind_is_rejected_but_other_kind_is_allowed()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.AddAsync(TransactionKind.Expense, "食費"));

        // 「その他」は支出にも収入にもある
        Assert.Contains("その他", await ActiveNamesAsync(TransactionKind.Expense));
        Assert.Contains("その他", await ActiveNamesAsync(TransactionKind.Income));
    }

    [Fact]
    public async Task Deleted_name_can_be_reused()
    {
        var food = (await _repository.GetAllAsync()).Single(c => c.Name == "食費");
        await _repository.DeleteAsync(food.Id);

        var added = await _repository.AddAsync(TransactionKind.Expense, "食費");

        Assert.NotEqual(food.Id, added.Id);
    }

    [Fact]
    public async Task Rename_bumps_version()
    {
        var food = (await _repository.GetAllAsync()).Single(c => c.Name == "食費");
        Clock.Advance(TimeSpan.FromMinutes(1));

        var renamed = await _repository.RenameAsync(food.Id, "食料品");

        Assert.Equal("食料品", renamed.Name);
        Assert.Equal(2, renamed.Version);
        Assert.Equal(food.UpdatedAt.AddMinutes(1), renamed.UpdatedAt);
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.RenameAsync(food.Id, "日用品"));
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.RenameAsync(food.Id, " "));
    }

    [Fact]
    public async Task Move_swaps_with_neighbor_and_stops_at_edges()
    {
        var goods = (await _repository.GetAllAsync()).Single(c => c.Name == "日用品");

        await _repository.MoveAsync(goods.Id, -1);
        Assert.Equal(["日用品", "食費"], (await ActiveNamesAsync(TransactionKind.Expense)).Take(2));

        await _repository.MoveAsync(goods.Id, -1);
        Assert.Equal(["日用品", "食費"], (await ActiveNamesAsync(TransactionKind.Expense)).Take(2));

        await _repository.MoveAsync(goods.Id, 1);
        Assert.Equal(["食費", "日用品"], (await ActiveNamesAsync(TransactionKind.Expense)).Take(2));
    }

    [Fact]
    public async Task Move_skips_deleted_categories()
    {
        var all = await _repository.GetAllAsync();
        var food = all.Single(c => c.Name == "食費");
        var goods = all.Single(c => c.Name == "日用品");
        var housing = all.Single(c => c.Name == "住居費");
        await _repository.DeleteAsync(goods.Id);

        await _repository.MoveAsync(housing.Id, -1);

        Assert.Equal(["住居費", "食費"], (await ActiveNamesAsync(TransactionKind.Expense)).Take(2));
        Assert.NotEqual(food.Id, housing.Id);
    }

    [Fact]
    public async Task Deleted_category_is_kept_for_history()
    {
        var food = (await _repository.GetAllAsync()).Single(c => c.Name == "食費");

        await _repository.DeleteAsync(food.Id);

        var deleted = (await _repository.GetAllAsync()).Single(c => c.Id == food.Id);
        Assert.True(deleted.Deleted);
        Assert.DoesNotContain("食費", await ActiveNamesAsync(TransactionKind.Expense));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _repository.RenameAsync(food.Id, "食料品"));
    }
}
