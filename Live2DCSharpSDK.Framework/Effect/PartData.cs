﻿using Live2DCSharpSDK.Framework.Model;

namespace Live2DCSharpSDK.Framework.Effect;


/// <summary>
/// パーツにまつわる諸々のデータを管理する。
/// </summary>
public record PartData
{
    /// <summary>
    /// パーツID
    /// </summary>
    public required string PartId { get; set; }
    /// <summary>
    /// パラメータのインデックス
    /// </summary>
    public int ParameterIndex { get; set; }
    /// <summary>
    ///  パーツのインデックス
    /// </summary>
    public int PartIndex { get; set; }
    /// <summary>
    ///  連動するパラメータ
    /// </summary>
    public readonly List<PartData> Link = [];

    /// <summary>
    /// 初期化する。
    /// </summary>
    /// <param name="model">初期化に使用するモデル</param>
    public void Initialize(CubismModel model)
    {
        ParameterIndex = model.GetParameterIndex(PartId);
        PartIndex = model.GetPartIndex(PartId);

        model.SetParameterValue(ParameterIndex, 1);
    }
}
