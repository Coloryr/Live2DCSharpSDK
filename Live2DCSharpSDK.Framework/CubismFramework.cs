using Live2DCSharpSDK.Framework.Core;
using Live2DCSharpSDK.Framework.Id;
using Live2DCSharpSDK.Framework.Rendering;

namespace Live2DCSharpSDK.Framework;

/// <summary>
/// Live2D Cubism Original Workflow SDKのエントリポイント
/// 利用開始時はCubismFramework::Initialize()を呼び、CubismFramework::Dispose()で終了する。
/// </summary>
public static class CubismFramework
{
    /// <summary>
    /// メッシュ頂点のオフセット値
    /// </summary>
    public const int VertexOffset = 0;
    /// <summary>
    /// メッシュ頂点のステップ値
    /// </summary>
    public const int VertexStep = 2;

    private static bool s_isStarted = false;
    private static bool s_isInitialized = false;
    private static ICubismAllocator s_allocator;
    private static Option s_option;
    private static CubismIdManager s_cubismIdManager;

    /// <summary>
    /// Cubism FrameworkのAPIを使用可能にする。
    /// APIを実行する前に必ずこの関数を実行すること。
    /// 引数に必ずメモリアロケータを渡してください。
    /// 一度準備が完了して以降は、再び実行しても内部処理がスキップされます。
    /// </summary>
    /// <param name="allocator">ICubismAllocatorクラスのインスタンス</param>
    /// <param name="option">Optionクラスのインスタンス</param>
    /// <returns>準備処理が完了したらtrueが返ります。</returns>
    public static bool StartUp(ICubismAllocator allocator, Option option)
    {
        if (s_isStarted)
        {
            CubismLog.CubismLogInfo("CubismFramework::StartUp() is already done.");
            return s_isStarted;
        }

        s_option = option;
        if (s_option != null)
        {
            CubismCore.SetLogFunction(s_option.LogFunction);
        }

        if (allocator == null)
        {
            CubismLog.CubismLogWarning("CubismFramework::StartUp() failed, need allocator instance.");
            s_isStarted = false;
        }
        else
        {
            s_allocator = allocator;
            s_isStarted = true;
        }

        //Live2D Cubism Coreバージョン情報を表示
        if (s_isStarted)
        {
            var version = CubismCore.GetVersion();

            uint major = (version & 0xFF000000) >> 24;
            uint minor = (version & 0x00FF0000) >> 16;
            uint patch = version & 0x0000FFFF;
            uint vesionNumber = version;

            CubismLog.CubismLogInfo($"Live2D Cubism Core version: {major:##}.{minor:#}.{patch:####} ({vesionNumber})");
        }

        CubismLog.CubismLogInfo("CubismFramework::StartUp() is complete.");

        return s_isStarted;
    }

    /// <summary>
    /// StartUp()で初期化したCubismFrameworkの各パラメータをクリアします。
    /// Dispose()したCubismFrameworkを再利用する際に利用してください。
    /// </summary>
    public static void CleanUp()
    {
        s_isStarted = false;
        s_isInitialized = false;
        s_allocator = null;
        s_option = null;
        s_cubismIdManager = null;
    }

    /// <summary>
    /// Cubism FrameworkのAPIを使用する準備が完了したかどうか？
    /// </summary>
    /// <returns>APIを使用する準備が完了していればtrueが返ります。</returns>
    public static bool IsStarted()
    {
        return s_isStarted;
    }

    /// <summary>
    /// Cubism Framework内のリソースを初期化してモデルを表示可能な状態にします。
    /// 再度Initialize()するには先にDispose()を実行する必要があります。
    /// </summary>
    public static void Initialize()
    {
        if (!s_isStarted)
        {
            CubismLog.CubismLogWarning("CubismFramework is not started.");
            return;
        }

        // --- s_isInitializedによる連続初期化ガード ---
        // 連続してリソース確保が行われないようにする。
        // 再度Initialize()するには先にDispose()を実行する必要がある。
        if (s_isInitialized)
        {
            CubismLog.CubismLogWarning("CubismFramework::Initialize() skipped, already initialized.");
            return;
        }

        s_cubismIdManager = new CubismIdManager();

        s_isInitialized = true;

        CubismLog.CubismLogInfo("CubismFramework::Initialize() is complete.");
    }

    /// <summary>
    /// Cubism Framework内の全てのリソースを解放します。
    /// ただし、外部で確保されたリソースについては解放しません。
    /// 外部で適切に破棄する必要があります。
    /// </summary>
    public static void Dispose()
    {
        if (!s_isStarted)
        {
            CubismLog.CubismLogWarning("CubismFramework is not started.");
            return;
        }

        // --- s_isInitializedによる未初期化解放ガード ---
        // Dispose()するには先にInitialize()を実行する必要がある。
        if (!s_isInitialized) // false...リソース未確保の場合
        {
            CubismLog.CubismLogWarning("CubismFramework::Dispose() skipped, not initialized.");
            return;
        }

        s_isInitialized = false;

        CubismLog.CubismLogInfo("CubismFramework::Dispose() is complete.");
    }

    /// <summary>
    /// Cubism Frameworkのリソース初期化が既に行われているかどうか？
    /// </summary>
    /// <returns>リソース確保が完了していればtrueが返ります。</returns>
    public static bool IsInitialized()
    {
        return s_isInitialized;
    }

    /// <summary>
    /// Core APIにバインドしたログ関数を実行する
    /// </summary>
    /// <param name="data">ログメッセージ</param>
    public static void CoreLogFunction(string data)
    {
        var fuc = CubismCore.GetLogFunction();
        if (fuc == null)
        {
            return;
        }

        fuc(data);
    }

    /// <summary>
    /// 現在のログ出力レベル設定の値を返す。
    /// </summary>
    /// <returns>現在のログ出力レベル設定の値</returns>
    public static Option.LogLevel GetLoggingLevel()
    {
        if (s_option != null)
            return s_option.LoggingLevel;

        return Option.LogLevel.LogLevel_Off;
    }

    /// <summary>
    /// IDマネージャのインスタンスを取得する。
    /// </summary>
    /// <returns>CubismIdManagerクラスのインスタンス</returns>
    public static CubismIdManager GetIdManager()
    {
        return s_cubismIdManager;
    }

    public static IntPtr Allocate(int size)
        => s_allocator.Allocate(size);
    public static IntPtr AllocateAligned(int size, int alignment)
        => s_allocator.AllocateAligned(size, alignment);
    public static void Deallocate(IntPtr address)
        => s_allocator.Deallocate(address);
    public static void DeallocateAligned(IntPtr address)
        => s_allocator.Deallocate(address);
}
