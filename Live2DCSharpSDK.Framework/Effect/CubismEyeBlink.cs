﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Effect;

enum EyeState : int
{
    /// <summary>
    /// 初期状態
    /// </summary>
    EyeState_First = 0,
    /// <summary>
    /// まばたきしていない状態
    /// </summary>
    EyeState_Interval,
    /// <summary>
    /// まぶたが閉じていく途中の状態
    /// </summary>
    EyeState_Closing,
    /// <summary>
    /// まぶたが閉じている状態
    /// </summary>
    EyeState_Closed,
    /// <summary>
    /// まぶたが開いていく途中の状態
    /// </summary>
    EyeState_Opening
};

/// <summary>
/// 自動まばたき機能を提供する。
/// </summary>
public class CubismEyeBlink
{
    /// <summary>
    /// IDで指定された目のパラメータが、0のときに閉じるなら true 、1の時に閉じるなら false 。
    /// </summary>
    public const bool CloseIfZero = true;

    /// <summary>
    /// 現在の状態
    /// </summary>
    private EyeState _blinkingState;
    /// <summary>
    /// 操作対象のパラメータのIDのリスト
    /// </summary>
    private List<string> ParameterIds { get; set; }
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

    private Random random = new();

    /// <summary>
    /// インスタンスを作成する。
    /// </summary>
    /// <param name="modelSetting">モデルの設定情報</param>
    public CubismEyeBlink(ICubismModelSetting modelSetting)
    {
        _blinkingState = EyeState.EyeState_First;
        _blinkingIntervalSeconds = 4.0f;
        _closingSeconds = 0.1f;
        _closedSeconds = 0.05f;
        _openingSeconds = 0.15f;

        if (modelSetting == null)
        {
            return;
        }

        for (int i = 0; i < modelSetting.GetEyeBlinkParameterCount(); ++i)
        {
            ParameterIds.Add(modelSetting.GetEyeBlinkParameterId(i));
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
        float t = 0.0f;

        switch (_blinkingState)
        {
            case EyeState.EyeState_Closing:
                t = ((_userTimeSeconds - _stateStartTimeSeconds) / _closingSeconds);

                if (t >= 1.0f)
                {
                    t = 1.0f;
                    _blinkingState = EyeState.EyeState_Closed;
                    _stateStartTimeSeconds = _userTimeSeconds;
                }

                parameterValue = 1.0f - t;

                break;
            case EyeState.EyeState_Closed:
                t = ((_userTimeSeconds - _stateStartTimeSeconds) / _closedSeconds);

                if (t >= 1.0f)
                {
                    _blinkingState = EyeState.EyeState_Opening;
                    _stateStartTimeSeconds = _userTimeSeconds;
                }

                parameterValue = 0.0f;

                break;
            case EyeState.EyeState_Opening:
                t = ((_userTimeSeconds - _stateStartTimeSeconds) / _openingSeconds);

                if (t >= 1.0f)
                {
                    t = 1.0f;
                    _blinkingState = EyeState.EyeState_Interval;
                    _nextBlinkingTime = DeterminNextBlinkingTiming();
                }

                parameterValue = t;

                break;
            case EyeState.EyeState_Interval:
                if (_nextBlinkingTime < _userTimeSeconds)
                {
                    _blinkingState = EyeState.EyeState_Closing;
                    _stateStartTimeSeconds = _userTimeSeconds;
                }

                parameterValue = 1.0f;

                break;
            case EyeState.EyeState_First:
            default:
                _blinkingState = EyeState.EyeState_Interval;
                _nextBlinkingTime = DeterminNextBlinkingTiming();

                parameterValue = 1.0f;

                break;
        }

        if (!CloseIfZero)
        {
            parameterValue = -parameterValue;
        }

        foreach (var item in ParameterIds)
        {
            model->SetParameterValue(item, parameterValue);
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
