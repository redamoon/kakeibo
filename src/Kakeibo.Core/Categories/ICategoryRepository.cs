using Kakeibo.Core.Transactions;

namespace Kakeibo.Core.Categories;

public interface ICategoryRepository
{
    /// <summary>
    /// 論理削除済みも含めた全カテゴリを、種類・並び順の順に返す。
    /// カテゴリが1件もなければ、先に標準のカテゴリを登録する。
    /// </summary>
    Task<IReadOnlyList<Category>> GetAllAsync();

    Task<Category> AddAsync(TransactionKind kind, string name);

    Task<Category> RenameAsync(Guid id, string name);

    /// <summary>同じ種類の中で、1つ上(-1)または1つ下(+1)と入れ替える。端なら何もしない。</summary>
    Task MoveAsync(Guid id, int offset);

    /// <summary>論理削除する。過去の明細には名前が残り、入力の選択肢からは消える。</summary>
    Task DeleteAsync(Guid id);
}
