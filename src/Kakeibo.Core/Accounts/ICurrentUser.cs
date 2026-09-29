namespace Kakeibo.Core.Accounts;

/// <summary>
/// 現在の利用者を表す。ログイン実装後は、JWT から取り出した user_id を返す実装に差し替える。
/// </summary>
public interface ICurrentUser
{
    string UserId { get; }
}

/// <summary>
/// ログイン機能ができるまでの仮の利用者。
/// </summary>
public sealed class LocalUser : ICurrentUser
{
    public const string LocalUserId = "local";

    public string UserId => LocalUserId;
}
