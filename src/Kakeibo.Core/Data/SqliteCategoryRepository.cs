using Kakeibo.Core.Accounts;
using Kakeibo.Core.Categories;
using Kakeibo.Core.Transactions;

namespace Kakeibo.Core.Data;

public sealed class SqliteCategoryRepository(
    KakeiboDatabase database,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        var rows = await GetRowsAsync();
        return rows.Select(r => r.ToModel()).ToList();
    }

    public async Task<Category> AddAsync(TransactionKind kind, string name)
    {
        name = NormalizeName(name);
        var siblings = await GetActiveSiblingsAsync(kind);
        EnsureUniqueName(siblings, name, exceptId: null);

        var row = new CategoryRow
        {
            Id = Guid.NewGuid().ToString(),
            UserId = currentUser.UserId,
            Kind = (int)kind,
            Name = name,
            SortOrder = siblings.Count == 0 ? 0 : siblings.Max(r => r.SortOrder) + 1,
            UpdatedAt = Now(),
            Version = 1,
        };

        var connection = await database.GetConnectionAsync();
        await connection.InsertAsync(row);
        return row.ToModel();
    }

    public async Task<Category> RenameAsync(Guid id, string name)
    {
        name = NormalizeName(name);
        var row = await FindActiveRowAsync(id);
        var siblings = await GetActiveSiblingsAsync((TransactionKind)row.Kind);
        EnsureUniqueName(siblings, name, exceptId: row.Id);

        row.Name = name;
        Touch(row);

        var connection = await database.GetConnectionAsync();
        await connection.UpdateAsync(row);
        return row.ToModel();
    }

    public async Task MoveAsync(Guid id, int offset)
    {
        var row = await FindActiveRowAsync(id);
        var siblings = await GetActiveSiblingsAsync((TransactionKind)row.Kind);
        var index = siblings.FindIndex(r => r.Id == row.Id);
        var targetIndex = index + offset;
        if (targetIndex < 0 || targetIndex >= siblings.Count)
        {
            return;
        }

        // 並び順を 0 から振り直してから入れ替える。同期で番号が重複していても順序が崩れないようにする
        var ordered = siblings.ToList();
        (ordered[index], ordered[targetIndex]) = (ordered[targetIndex], ordered[index]);

        var connection = await database.GetConnectionAsync();
        await connection.RunInTransactionAsync(tran =>
        {
            for (var i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].SortOrder == i)
                {
                    continue;
                }

                ordered[i].SortOrder = i;
                Touch(ordered[i]);
                tran.Update(ordered[i]);
            }
        });
    }

    public async Task DeleteAsync(Guid id)
    {
        var row = await FindActiveRowAsync(id);
        row.Deleted = true;
        Touch(row);

        var connection = await database.GetConnectionAsync();
        await connection.UpdateAsync(row);
    }

    /// <summary>
    /// 利用者のカテゴリを、論理削除済みも含めて返す。
    /// 1件もなければ(初回起動時)、どの操作よりも先に標準のカテゴリを登録する。
    /// </summary>
    private async Task<List<CategoryRow>> GetRowsAsync()
    {
        var rows = await QueryRowsAsync();
        if (rows.Count > 0)
        {
            return rows;
        }

        var connection = await database.GetConnectionAsync();
        await connection.InsertAllAsync(CategoryRow.CreateDefaults(currentUser.UserId, Now()));
        return await QueryRowsAsync();
    }

    private async Task<List<CategoryRow>> QueryRowsAsync()
    {
        var userId = currentUser.UserId;
        var connection = await database.GetConnectionAsync();
        return await connection.Table<CategoryRow>()
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.Kind)
            .ThenBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync();
    }

    private async Task<List<CategoryRow>> GetActiveSiblingsAsync(TransactionKind kind)
    {
        var rows = await GetRowsAsync();
        return rows.Where(r => r.Kind == (int)kind && !r.Deleted).ToList();
    }

    private async Task<CategoryRow> FindActiveRowAsync(Guid id)
    {
        var key = id.ToString();
        var userId = currentUser.UserId;
        var connection = await database.GetConnectionAsync();
        return await connection.Table<CategoryRow>()
                   .Where(r => r.Id == key && r.UserId == userId && !r.Deleted)
                   .FirstOrDefaultAsync()
               ?? throw new KeyNotFoundException($"カテゴリ {id} が見つかりません。");
    }

    private static string NormalizeName(string name)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("カテゴリ名を入力してください。", nameof(name));
        }

        return name;
    }

    private static void EnsureUniqueName(IEnumerable<CategoryRow> siblings, string name, string? exceptId)
    {
        if (siblings.Any(r => r.Id != exceptId && r.Name == name))
        {
            throw new ArgumentException($"「{name}」はすでにあります。", nameof(name));
        }
    }

    private void Touch(CategoryRow row)
    {
        row.UpdatedAt = Now();
        row.Version++;
    }

    private long Now() => timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
}
