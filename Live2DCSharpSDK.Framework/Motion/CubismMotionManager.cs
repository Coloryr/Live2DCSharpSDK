using Live2DCSharpSDK.Framework.Model;

namespace Live2DCSharpSDK.Framework.Motion;

/// <summary>
/// モーションの管理を行うクラス。
/// </summary>
public class CubismMotionManager : CubismMotionQueueManager
{
    /// <summary>
    /// 現在再生中のモーションの優先度
    /// </summary>
    public int CurrentPriority { get; private set; }
    /// <summary>
    /// 再生予定のモーションの優先度。再生中は0になる。モーションファイルを別スレッドで読み込むときの機能。
    /// </summary>
    public int ReservePriority { get; set; }

    /// <summary>
    /// 優先度を設定してモーションを開始する。
    /// </summary>
    /// <param name="motion">モーション</param>
    /// <param name="autoDelete">再生が終了したモーションのインスタンスを削除するならtrue</param>
    /// <param name="priority">優先度</param>
    /// <returns>開始したモーションの識別番号を返す。個別のモーションが終了したか否かを判定するIsFinished()の引数で使用する。開始できない時は「-1」</returns>
    public object StartMotionPriority(ACubismMotion motion, bool autoDelete, int priority)
    {
        if (priority == ReservePriority)
        {
            ReservePriority = 0;           // 予約を解除
        }

        CurrentPriority = priority;        // 再生中モーションの優先度を設定

        return StartMotion(motion, autoDelete, _userTimeSeconds);
    }

    /// <summary>
    /// モーションを更新して、モデルにパラメータ値を反映する。
    /// </summary>
    /// <param name="model">対象のモデル</param>
    /// <param name="deltaTimeSeconds">デルタ時間[秒]</param>
    /// <returns>true    更新されている
    /// false   更新されていない</returns>
    public bool UpdateMotion(CubismModel model, float deltaTimeSeconds)
    {
        _userTimeSeconds += deltaTimeSeconds;

        bool updated = DoUpdateMotion(model, _userTimeSeconds);

        if (IsFinished())
        {
            CurrentPriority = 0;           // 再生中モーションの優先度を解除
        }

        return updated;
    }

    /// <summary>
    /// モーションを予約する。
    /// </summary>
    /// <param name="priority">優先度</param>
    /// <returns>true    予約できた
    /// false   予約できなかった</returns>
    public bool ReserveMotion(int priority)
    {
        if ((priority <= ReservePriority) || (priority <= CurrentPriority))
        {
            return false;
        }

        ReservePriority = priority;

        return true;
    }
}
