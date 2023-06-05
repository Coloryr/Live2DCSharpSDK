namespace Live2DCSharpSDK.Framework.Motion;

/// <summary>
/// CubismMotionQueueManagerで再生している各モーションの管理クラス。
/// </summary>
public class CubismMotionQueueEntry
{
    /// <summary>
    /// 自動削除
    /// </summary>
    public bool _autoDelete;
    /// <summary>
    /// モーション
    /// </summary>
    public ACubismMotion _motion;

    /// <summary>
    /// 有効化フラグ
    /// </summary>
    private bool _available;
    /// <summary>
    /// 終了フラグ
    /// </summary>
    private bool _finished;
    /// <summary>
    /// 開始フラグ（0.9.00以降）
    /// </summary>
    private bool _started;
    /// <summary>
    /// モーション再生開始時刻[秒]
    /// </summary>
    private float _startTimeSeconds;
    /// <summary>
    /// フェードイン開始時刻（ループの時は初回のみ）[秒]
    /// </summary>
    private float _fadeInStartTimeSeconds;
    /// <summary>
    /// 終了予定時刻[秒]
    /// </summary>
    private float _endTimeSeconds;
    /// <summary>
    /// 時刻の状態[秒]
    /// </summary>
    private float _stateTimeSeconds;
    /// <summary>
    /// 重みの状態
    /// </summary>
    private float _stateWeight;
    /// <summary>
    /// 最終のMotion側のチェックした時間
    /// </summary>
    private float _lastEventCheckSeconds;
    private float _fadeOutSeconds;
    private bool _IsTriggeredFadeOut;

    /// <summary>
    /// インスタンスごとに一意の値を持つ識別番号
    /// </summary>
    public object _motionQueueEntryHandle;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    public CubismMotionQueueEntry()
    {
        _available = true;
        _startTimeSeconds = -1.0f;
        _endTimeSeconds = -1.0f;
        _motionQueueEntryHandle = this;
    }

    /// <summary>
    /// フェードアウトの開始を設定する。
    /// </summary>
    /// <param name="fadeOutSeconds">フェードアウトにかかる時間[秒]</param>
    public void SetFadeout(float fadeOutSeconds)
    {
        _fadeOutSeconds = fadeOutSeconds;
        _IsTriggeredFadeOut = true;
    }

    /// <summary>
    /// フェードアウトを開始する。
    /// </summary>
    /// <param name="fadeOutSeconds">フェードアウトにかかる時間[秒]</param>
    /// <param name="userTimeSeconds">デルタ時間の積算値[秒]</param>
    public void StartFadeout(float fadeOutSeconds, float userTimeSeconds)
    {
        float newEndTimeSeconds = userTimeSeconds + fadeOutSeconds;
        _IsTriggeredFadeOut = true;

        if (_endTimeSeconds < 0.0f || newEndTimeSeconds < _endTimeSeconds)
        {
            _endTimeSeconds = newEndTimeSeconds;
        }
    }

    /// <summary>
    /// モーションが終了したかどうか。
    /// </summary>
    /// <returns>true    モーションが終了した
    /// false   終了していない</returns>
    public bool IsFinished()
    {
        return _finished;
    }

    /// <summary>
    /// モーションが開始したかどうか。
    /// </summary>
    /// <returns>true    モーションが開始した
    /// false   終了していない</returns>
    public bool IsStarted()
    {
        return _started;
    }

    /// <summary>
    /// モーションの開始時刻を取得する。
    /// </summary>
    /// <returns>モーションの開始時刻[秒]</returns>
    public float GetStartTime()
    {
        return _startTimeSeconds;
    }

    /// <summary>
    /// フェードインの開始時刻を取得する。
    /// </summary>
    /// <returns>フェードインの開始時刻[秒]</returns>
    public float GetFadeInStartTime()
    {
        return _fadeInStartTimeSeconds;
    }

    /// <summary>
    /// フェードインの終了時刻を取得する。
    /// </summary>
    /// <returns>フェードインの終了時刻[秒]</returns>
    public float GetEndTime()
    {
        return _endTimeSeconds;
    }

    /// <summary>
    /// モーションの開始時刻を設定する。
    /// </summary>
    /// <param name="startTime">モーションの開始時刻[秒]</param>
    public void SetStartTime(float startTime)
    {
        _startTimeSeconds = startTime;
    }

    /// <summary>
    /// フェードインの開始時刻を設定する。
    /// </summary>
    /// <param name="startTime">フェードインの開始時刻[秒]</param>
    public void SetFadeInStartTime(float startTime)
    {
        _fadeInStartTimeSeconds = startTime;
    }

    /// <summary>
    /// フェードインの終了時刻を設定する。
    /// </summary>
    /// <param name="endTime">フェードインの終了時刻[秒]</param>
    public void SetEndTime(float endTime)
    {
        _endTimeSeconds = endTime;
    }

    /// <summary>
    /// モーションの終了を設定する。
    /// </summary>
    /// <param name="f">trueならモーションの終了</param>
    public void IsFinished(bool f)
    {
        _finished = f;
    }

    /// <summary>
    /// モーションの開始を設定する。
    /// </summary>
    /// <param name="f">trueならモーションの開始</param>
    public void IsStarted(bool f)
    {
        _started = f;
    }

    /// <summary>
    /// モーションの有効・無効を取得する。
    /// </summary>
    /// <returns>true    モーションは有効
    /// false   モーションは無効</returns>
    public bool IsAvailable()
    {
        return _available;
    }

    /// <summary>
    /// モーションの有効・無効を設定する。
    /// </summary>
    /// <param name="v">trueならモーションは有効</param>
    public void IsAvailable(bool v)
    {
        _available = v;
    }

    /// <summary>
    /// モーションの状態を設定する。
    /// </summary>
    /// <param name="timeSeconds">現在時刻[秒]</param>
    /// <param name="weight">モーションの重み</param>
    public void SetState(float timeSeconds, float weight)
    {
        _stateTimeSeconds = timeSeconds;
        _stateWeight = weight;
    }

    /// <summary>
    /// モーションの現在時刻を取得する。
    /// </summary>
    /// <returns>モーションの現在時刻[秒]</returns>
    public float GetStateTime()
    {
        return _stateTimeSeconds;
    }

    /// <summary>
    /// モーションの重みを取得する。
    /// </summary>
    /// <returns>モーションの重み</returns>
    public float GetStateWeight()
    {
        return _stateWeight;
    }

    /// <summary>
    /// 最後にイベントの発火をチェックした時間を取得する。
    /// </summary>
    /// <returns>最後にイベントの発火をチェックした時間[秒]</returns>
    public float GetLastCheckEventTime()
    {
        return _lastEventCheckSeconds;
    }

    /// <summary>
    /// 最後にイベントをチェックした時間を設定する。
    /// </summary>
    /// <param name="checkTime">最後にイベントをチェックした時間[秒]</param>
    public void SetLastCheckEventTime(float checkTime)
    {
        _lastEventCheckSeconds = checkTime;
    }

    /// <summary>
    /// モーションがフェードアウトが開始しているかを取得する。
    /// </summary>
    /// <returns>フェードアウトが開始しているか</returns>
    public bool IsTriggeredFadeOut()
    {
        return _IsTriggeredFadeOut;
    }

    /// <summary>
    /// モーションのフェードアウト時間を取得する。
    /// </summary>
    /// <returns>フェードアウト開始[秒]</returns>
    public float GetFadeOutSeconds()
    {
        return _fadeOutSeconds;
    }
}
