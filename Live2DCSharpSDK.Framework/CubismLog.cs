namespace Live2DCSharpSDK.Framework;

public static class CubismLog
{
    public static void CubismLogPrintln(LogLevel level, string head, string fmt, params object?[] args)
    {
        string data = $"[CSM] {head} {string.Format(fmt, args)}";
        if (level < CubismFramework.GetLoggingLevel())
            return;

        CubismFramework.CoreLogFunction(data);
    }

    public static void CubismLogVerbose(string fmt, params object?[] args)
    {
        CubismLogPrintln(LogLevel.Verbose, "[V]", fmt, args);
    }

    public static void CubismLogDebug(string fmt, params object?[] args)
    {
        CubismLogPrintln(LogLevel.Debug, "[D]", fmt, args);
    }

    public static void CubismLogInfo(string fmt, params object?[] args)
    {
        CubismLogPrintln(LogLevel.Info, "[I]", fmt, args);
    }

    public static void CubismLogWarning(string fmt, params object?[] args)
    {
        CubismLogPrintln(LogLevel.Warning, "[W]", fmt, args);
    }

    public static void CubismLogError(string fmt, params object?[] args)
    {
        CubismLogPrintln(LogLevel.Error, "[E]", fmt, args);
    }
}
