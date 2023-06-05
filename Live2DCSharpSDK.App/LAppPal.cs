﻿namespace Live2DCSharpSDK.App;

/// <summary>
/// プラットフォーム依存機能を抽象化する Cubism Platform Abstraction Layer.
/// ファイル読み込みや時刻取得等のプラットフォームに依存する関数をまとめる
/// </summary>
public static class LAppPal
{
    private static float s_currentFrame;
    private static float s_lastFrame;
    private static float s_deltaTime;

    /// <summary>
    /// デルタ時間（前回フレームとの差分）を取得する
    /// </summary>
    /// <returns>デルタ時間[ms]</returns>
    public static float GetDeltaTime()
    {
        return s_deltaTime;
    }

    /// <summary>
    /// 
    /// </summary>
    public static void UpdateTime(float time)
    {
        s_currentFrame = time;
        s_deltaTime = s_currentFrame - s_lastFrame;
        s_lastFrame = s_currentFrame;
    }

    /// <summary>
    /// メッセージを出力する
    /// </summary>
    /// <param name="message">文字列</param>
    public static void PrintLog(string message)
    {
        Console.WriteLine(message);
    }
}