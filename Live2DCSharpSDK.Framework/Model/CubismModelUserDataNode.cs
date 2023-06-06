﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Model;

/// <summary>
/// Jsonから読み込んだユーザデータを記録しておくための構造体
/// </summary>
public record CubismModelUserDataNode
{
    /// <summary>
    /// ユーザデータターゲットタイプ
    /// </summary>
    public required string TargetType { get; set; }
    /// <summary>
    /// ユーザデータターゲットのID
    /// </summary>
    public required string TargetId { get; set; }
    /// <summary>
    /// ユーザデータ
    /// </summary>
    public required string Value { get; set; }
}
