using Live2DCSharpSDK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework;

/// <summary>
/// CubismFrameworkに設定するオプション要素を定義するクラス
/// </summary>
public class Option
{
    /// <summary>
    /// ログ出力のレベル
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 詳細ログ
        /// </summary>
        LogLevel_Verbose = 0,
        /// <summary>
        /// デバッグログ
        /// </summary>
        LogLevel_Debug,
        /// <summary>
        /// Infoログ
        /// </summary>
        LogLevel_Info,
        /// <summary>
        /// 警告ログ
        /// </summary>
        LogLevel_Warning,
        /// <summary>
        /// エラーログ
        /// </summary>
        LogLevel_Error,
        /// <summary>
        /// ログ出力無効
        /// </summary>
        LogLevel_Off
    };

    /// <summary>
    /// ログ出力の関数ポイ
    /// </summary>
    public CubismCore.csmLogFunction LogFunction;
    /// <summary>
    /// ログ出力レベル設定
    /// </summary>
    public LogLevel LoggingLevel;                  
}
