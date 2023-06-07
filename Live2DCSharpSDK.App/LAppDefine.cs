using Live2DCSharpSDK.Framework;

namespace Live2DCSharpSDK.App;

/// <summary>
/// アプリケーションクラス。
/// Cubism SDK の管理を行う。
/// </summary>
public class LAppDefine
{
    // 画面
    public const float ViewScale = 1.0f;
    public const float ViewMaxScale = 2.0f;
    public const float ViewMinScale = 0.8f;

    public const float ViewLogicalLeft = -1.0f;
    public const float ViewLogicalRight = 1.0f;
    public const float ViewLogicalBottom = -1.0f;
    public const float ViewLogicalTop = -1.0f;

    public const float ViewLogicalMaxLeft = -2.0f;
    public const float ViewLogicalMaxRight = 2.0f;
    public const float ViewLogicalMaxBottom = -2.0f;
    public const float ViewLogicalMaxTop = 2.0f;

    // 相対パス
    public const string ResourcesPath = "Resources/";

    // モデルの後ろにある背景の画像ファイル
    public const string BackImageName = "back_class_normal.png";
    // 歯車
    public const string GearImageName = "icon_gear.png";
    // 終了ボタン
    public const string PowerImageName = "close.png";

    // モデル定義------------------------------------------
    // モデルを配置したディレクトリ名の配列
    // ディレクトリ名とmodel3.jsonの名前を一致させておくこと
    public static readonly string[] ModelDir = new[]
    {
        "Haru",
        "Hiyori",
        "Mark",
        "Natori",
        "Rice",
        "Mao"
    };
    public static readonly int ModelDirSize = ModelDir.Length;

    // 外部定義ファイル(json)と合わせる
    public const string MotionGroupIdle = "Idle"; // アイドリング
    public const string MotionGroupTapBody = "TapBody"; // 体をタップしたとき

    // 外部定義ファイル(json)と合わせる
    public const string HitAreaNameHead = "Head";
    public const string HitAreaNameBody = "Body";

    // MOC3の整合性検証オプション
    public const bool MocConsistencyValidationEnable = true;

    // デバッグ用ログの表示オプション
    public const bool DebugLogEnable = true;
    public const bool DebugTouchLogEnable = false;

    // Frameworkから出力するログのレベル設定
    public const Option.LogLevel CubismLoggingLevel = Option.LogLevel.LogLevel_Verbose;

    // デフォルトのレンダーターゲットサイズ
    public const int RenderTargetWidth = 1920;
    public const int RenderTargetHeight = 1080;
}
