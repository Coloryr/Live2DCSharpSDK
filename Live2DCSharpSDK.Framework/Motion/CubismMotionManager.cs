using Live2DCSharpSDK.Framework.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Motion;

/// <summary>
/// モーションの管理を行うクラス。
/// </summary>
public class CubismMotionManager : CubismMotionQueueManager
{
    /// <summary>
    /// 現在再生中のモーションの優先度
    /// </summary>
    private int _currentPriority;
    /// <summary>
    /// 再生予定のモーションの優先度。再生中は0になる。モーションファイルを別スレッドで読み込むときの機能。
    /// </summary>
    private int _reservePriority;                 

    /// <summary>
    /// 再生中のモーションの優先度の取得する。
    /// </summary>
    /// <returns>モーションの優先度</returns>
    public int GetCurrentPriority()
    {
        return _currentPriority;
    }

    /// <summary>
    /// 予約中のモーションの優先度を取得する。
    /// </summary>
    /// <returns>モーションの優先度</returns>
    public int GetReservePriority()
    {
        return _reservePriority;
    }

    /// <summary>
    /// 予約中のモーションの優先度を設定する。
    /// </summary>
    /// <param name="val">優先度</param>
    public void SetReservePriority(int val)
    {
        _reservePriority = val;
    }

    /// <summary>
    /// 優先度を設定してモーションを開始する。
    /// </summary>
    /// <param name="motion">モーション</param>
    /// <param name="autoDelete">再生が終了したモーションのインスタンスを削除するならtrue</param>
    /// <param name="priority">優先度</param>
    /// <returns>開始したモーションの識別番号を返す。個別のモーションが終了したか否かを判定するIsFinished()の引数で使用する。開始できない時は「-1」</returns>
    public object StartMotionPriority(ACubismMotion motion, bool autoDelete, int priority)
    {
        if (priority == _reservePriority)
        {
            _reservePriority = 0;           // 予約を解除
        }

        _currentPriority = priority;        // 再生中モーションの優先度を設定

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
            _currentPriority = 0;           // 再生中モーションの優先度を解除
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
        if ((priority <= _reservePriority) || (priority <= _currentPriority))
        {
            return false;
        }

        _reservePriority = priority;

        return true;
    }
}
