using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Model;

namespace Live2DCSharpSDK.Framework.Motion;

/// <summary>
/// モーション再生終了コールバック関数定義
/// </summary>
public delegate void FinishedMotionCallback(ACubismMotion self);

/// <summary>
/// モーションの抽象基底クラス。MotionQueueManagerによってモーションの再生を管理する。
/// </summary>
public abstract class ACubismMotion
{
    /// <summary>
    /// フェードインにかかる時間[秒]
    /// </summary>
    public float FadeIn { get; set; }
    /// <summary>
    /// フェードアウトにかかる時間[秒]
    /// </summary>
    public float FadeOut { get; set; }
    /// <summary>
    /// モーションの重み
    /// </summary>
    public float Weight { get; set; }
    /// <summary>
    /// モーション再生の開始時刻[秒]
    /// </summary>
    public float Offset { get; set; }

    protected readonly List<string> _firedEventValues = new();

    // モーション再生終了コールバック関数
    protected FinishedMotionCallback? _onFinishedMotion;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    public ACubismMotion()
    {
        FadeIn = -1.0f;
        FadeOut = -1.0f;
        Weight = 1.0f;
    }

    /// <summary>
    /// モデルのパラメータを更新する。
    /// </summary>
    /// <param name="model">対象のモデル</param>
    /// <param name="motionQueueEntry">CubismMotionQueueManagerで管理されているモーション</param>
    /// <param name="userTimeSeconds">デルタ時間の積算値[秒]</param>
    public void UpdateParameters(CubismModel model, CubismMotionQueueEntry motionQueueEntry, float userTimeSeconds)
    {
        if (!motionQueueEntry.Available || motionQueueEntry.Finished)
        {
            return;
        }

        if (!motionQueueEntry.Started)
        {
            motionQueueEntry.Started = true;
            motionQueueEntry.StartTime = userTimeSeconds - Offset;//モーションの開始時刻を記録
            motionQueueEntry.FadeInStart = userTimeSeconds; //フェードインの開始時刻

            float duration = GetDuration();

            if (motionQueueEntry.EndTime < 0)
            {
                //開始していないうちに終了設定している場合がある。
                motionQueueEntry.EndTime = (duration <= 0) ? -1 : motionQueueEntry.StartTime + duration;
                //duration == -1 の場合はループする
            }
        }

        float fadeWeight = Weight; //現在の値と掛け合わせる割合

        //---- フェードイン・アウトの処理 ----
        //単純なサイン関数でイージングする
        float fadeIn = FadeIn == 0.0f ? 1.0f
                           : CubismMath.GetEasingSine((userTimeSeconds - motionQueueEntry.FadeInStart) / FadeIn);

        float fadeOut = (FadeOut == 0.0f || motionQueueEntry.EndTime < 0.0f)  ? 1.0f
                            : CubismMath.GetEasingSine((motionQueueEntry.EndTime - userTimeSeconds) / FadeOut);

        fadeWeight = fadeWeight * fadeIn * fadeOut;

        motionQueueEntry.SetState(userTimeSeconds, fadeWeight);

        if (0.0f > fadeWeight || fadeWeight > 1.0f)
        {
            throw new Exception("fadeWeight out of range");
        }

        //---- 全てのパラメータIDをループする ----
        DoUpdateParameters(model, userTimeSeconds, fadeWeight, motionQueueEntry);

        //後処理
        //終了時刻を過ぎたら終了フラグを立てる（CubismMotionQueueManager）
        if ((motionQueueEntry.EndTime > 0) && (motionQueueEntry.EndTime < userTimeSeconds))
        {
            motionQueueEntry.Finished = true;      //終了
        }
    }

    /// <summary>
    /// モーションの長さを取得する。
    /// 
    /// ループのときは「-1」。
    /// ループではない場合は、オーバーライドする。
    /// 正の値の時は取得される時間で終了する。
    /// 「-1」のときは外部から停止命令が無い限り終わらない処理となる。
    /// </summary>
    /// <returns>モーションの長さ[秒]</returns>
    public virtual float GetDuration()
    {
        return -1.0f;
    }

    /// <summary>
    /// モーションのループ1回分の長さを取得する。
    /// 
    /// ループしない場合は GetDuration()と同じ値を返す。
    /// ループ一回分の長さが定義できない場合（プログラム的に動き続けるサブクラスなど）の場合は「-1」を返す
    /// </summary>
    /// <returns>モーションのループ1回分の長さ[秒]</returns>
    public virtual float GetLoopDuration()
    {
        return -1.0f;
    }

    /// <summary>
    /// イベント発火のチェック。
    /// 入力する時間は呼ばれるモーションタイミングを０とした秒数で行う。
    /// </summary>
    /// <param name="beforeCheckTimeSeconds">前回のイベントチェック時間[秒]</param>
    /// <param name="motionTimeSeconds">今回の再生時間[秒]</param>
    /// <returns></returns>
    public virtual List<string> GetFiredEvent(float beforeCheckTimeSeconds, float motionTimeSeconds)
    {
        return _firedEventValues;
    }

    /// <summary>
    /// モーション再生終了コールバックを登録する。
    /// IsFinishedフラグを設定するタイミングで呼び出される。
    /// 以下の状態の際には呼び出されない:
    ///   1. 再生中のモーションが「ループ」として設定されているとき
    ///   2. コールバックにNULLが登録されているとき
    /// </summary>
    /// <param name="onFinishedMotionHandler">モーション再生終了コールバック関数</param>
    public void SetFinishedMotionHandler(FinishedMotionCallback onFinishedMotionHandler)
    {
        _onFinishedMotion = onFinishedMotionHandler;
    }

    /// <summary>
    /// モーション再生終了コールバックを取得する。
    /// </summary>
    /// <returns>登録されているモーション再生終了コールバック関数。NULLのとき、関数は何も登録されていない。</returns>
    public FinishedMotionCallback? GetFinishedMotionHandler()
    {
        return _onFinishedMotion;
    }

    /// <summary>
    /// 透明度のカーブが存在するかどうかを確認する
    /// </summary>
    /// <returns>true  . キーが存在する
    /// false . キーが存在しない</returns>
    public virtual bool IsExistModelOpacity()
    {
        return false;
    }

    /// <summary>
    /// 透明度のカーブのインデックスを返す
    /// </summary>
    /// <returns>success：透明度のカーブのインデックス</returns>
    public virtual int GetModelOpacityIndex()
    {
        return -1;
    }

    /// <summary>
    /// 透明度のIdを返す
    /// </summary>
    /// <returns>透明度のId</returns>
    public virtual string GetModelOpacityId(int index)
    {
        return null;
    }

    protected virtual float GetModelOpacityValue()
    {
        return 1.0f;
    }

    public abstract void DoUpdateParameters(CubismModel model, float userTimeSeconds, float weight, CubismMotionQueueEntry motionQueueEntry);
}
