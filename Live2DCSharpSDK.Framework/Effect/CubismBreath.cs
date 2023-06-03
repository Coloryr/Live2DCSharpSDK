﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Effect;

public record BreathParameterData
{
    /// <summary>
    /// 呼吸をひもづけるパラメータIDs
    /// </summary>
    public string ParameterId { get; set; }
    /// <summary>
    /// 呼吸を正弦波としたときの、波のオフセット
    /// </summary>
    public float Offset { get; set; }
    /// <summary>
    /// 呼吸を正弦波としたときの、波の高さ
    /// </summary>
    public float Peak { get; set; }
    /// <summary>
    /// 呼吸を正弦波としたときの、波の周期
    /// </summary>
    public float Cycle { get; set; }
    /// <summary>
    /// パラメータへの重み
    /// </summary>
    public float Weight { get; set; }
}

/// <summary>
/// 呼吸機能を提供する。
/// </summary>
public class CubismBreath
{
    /// <summary>
    /// 呼吸にひもづいているパラメータのリスト
    /// </summary>
    private List<BreathParameterData> Parameters { get; set; }
    /// <summary>
    /// 積算時間[秒]
    /// </summary>
    private float _currentTime;

    /// <summary>
    /// モデルのパラメータを更新する。
    /// </summary>
    /// <param name="model">対象のモデル</param>
    /// <param name="deltaTimeSeconds">デルタ時間[秒]</param>
    public void UpdateParameters(CubismModel model, float deltaTimeSeconds)
    {
        _currentTime += deltaTimeSeconds;

        float t = _currentTime * 2.0f * 3.14159f;

        foreach(var item in Parameters)
        {
            model->AddParameterValue(item.ParameterId, item.Offset + (item.Peak * MathF.Sin(t / item.Cycle)), item.Weight);
        }
    }
}
