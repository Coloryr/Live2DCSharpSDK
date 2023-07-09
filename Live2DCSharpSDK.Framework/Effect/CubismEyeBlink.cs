﻿//IDで指定された目のパラメータが、0のときに閉じるなら true 、1の時に閉じるなら false 。
//#define CloseIfZero

using Live2DCSharpSDK.Framework.Model;

namespace Live2DCSharpSDK.Framework.Effect;

/// <summary>
/// 自動まばたき機能を提供する。
/// </summary>
public class CubismEyeBlink
{
    /// <summary>
    /// 現在の状態
    /// </summary>
    private EyeState _blinkingState;
    /// <summary>
    /// 操作対象のパラメータのIDのリスト
    /// </summary>
    public readonly List<string> ParameterIds = new();
    /// <summary>
    /// 次のまばたきの時刻[秒]
    /// </summary>
    private float _nextBlinkingTime;
    /// <summary>
    /// 現在の状態が開始した時刻[秒]
    /// </summary>
    private float _stateStartTimeSeconds;
    /// <summary>
    /// まばたきの間隔[秒]
    /// </summary>
    private float _blinkingIntervalSeconds;
    /// <summary>
    /// まぶたを閉じる動作の所要時間[秒]
    /// </summary>
    private float _closingSeconds;
    /// <summary>
    /// まぶたを閉じている動作の所要時間[秒]
    /// </summary>
    private float _closedSeconds;
    /// <summary>
    /// まぶたを開く動作の所要時間[秒]
    /// </summary>
    private float _openingSeconds;
    /// <summary>
    /// デルタ時間の積算値[秒]
    /// </summary>
    private float _userTimeSeconds;

    private readonly Random random = new();

    /// <summary>
    /// インスタンスを作成する。
    /// </summary>
    /// <param name="modelSetting">モデルの設定情報</param>
    public CubismEyeBlink(ModelSettingObj modelSetting)
    {
        _blinkingState = EyeState.First;
        _blinkingIntervalSeconds = 4.0f;
        _closingSeconds = 0.1f;
        _closedSeconds = 0.05f;
        _openingSeconds = 0.15f;

        foreach (var item in modelSetting.Groups)
        {
            if (item.Name == CubismModelSettingJson.EyeBlink)
            {
                foreach (var item1 in item.Ids)
                {
                    if (item1 == null)
                        continue;
                    var item2 = CubismFramework.CubismIdManager.GetId(item1);
                    ParameterIds.Add(item2);
                }
                break;
            }
        }
    }

    /// <summary>
    /// まばたきの間隔を設定する。
    /// </summary>
    /// <param name="blinkingInterval">まばたきの間隔の時間[秒]</param>
    public void SetBlinkingInterval(float blinkingInterval)
    {
        _blinkingIntervalSeconds = blinkingInterval;
    }

    /// <summary>
    /// まばたきのモーションの詳細設定を行う。
    /// </summary>
    /// <param name="closing">まぶたを閉じる動作の所要時間[秒]</param>
    /// <param name="closed">まぶたを閉じている動作の所要時間[秒]</param>
    /// <param name="opening">まぶたを開く動作の所要時間[秒]</param>
    public void SetBlinkingSettings(float closing, float closed, float opening)
    {
        _closingSeconds = closing;
        _closedSeconds = closed;
        _openingSeconds = opening;
    }

    /// <summary>
    /// モデルのパラメータを更新する。
    /// </summary>
    /// <param name="model">対象のモデル</param>
    /// <param name="deltaTimeSeconds">デルタ時間[秒]</param>
    public void UpdateParameters(CubismModel model, float deltaTimeSeconds)
    {
        _userTimeSeconds += deltaTimeSeconds;
        float parameterValue;
        float t;
        switch (_blinkingState)
        {
            case EyeState.Closing:
                t = ((_userTimeSeconds - _stateStartTimeSeconds) / _closingSeconds);

                if (t >= 1.0f)
                {
                    t = 1.0f;
                    _blinkingState = EyeState.Closed;
                    _stateStartTimeSeconds = _userTimeSeconds;
                }

                parameterValue = 1.0f - t;

                break;
            case EyeState.Closed:
                t = ((_userTimeSeconds - _stateStartTimeSeconds) / _closedSeconds);

                if (t >= 1.0f)
                {
                    _blinkingState = EyeState.Opening;
                    _stateStartTimeSeconds = _userTimeSeconds;
                }

                parameterValue = 0.0f;

                break;
            case EyeState.Opening:
                t = ((_userTimeSeconds - _stateStartTimeSeconds) / _openingSeconds);

                if (t >= 1.0f)
                {
                    t = 1.0f;
                    _blinkingState = EyeState.Interval;
                    _nextBlinkingTime = DeterminNextBlinkingTiming();
                }

                parameterValue = t;

                break;
            case EyeState.Interval:
                if (_nextBlinkingTime < _userTimeSeconds)
                {
                    _blinkingState = EyeState.Closing;
                    _stateStartTimeSeconds = _userTimeSeconds;
                }

                parameterValue = 1.0f;

                break;
            case EyeState.First:
            default:
                _blinkingState = EyeState.Interval;
                _nextBlinkingTime = DeterminNextBlinkingTiming();

                parameterValue = 1.0f;

                break;
        }
#if CloseIfZero
        parameterValue = -parameterValue;
#endif

        foreach (var item in ParameterIds)
        {
            model.SetParameterValue(item, parameterValue);
        }
    }

    /// <summary>
    /// 次のまばたきのタイミングを決定する。
    /// </summary>
    /// <returns>次のまばたきを行う時刻[秒]</returns>
    private float DeterminNextBlinkingTiming()
    {
        float r = random.Next() / int.MaxValue;

        return _userTimeSeconds + (r * (2.0f * _blinkingIntervalSeconds - 1.0f));
    }
}
