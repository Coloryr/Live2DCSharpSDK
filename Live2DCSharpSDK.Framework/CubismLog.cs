using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework;

public static class CubismLog
{
    public static void CubismLogPrintln(Option.LogLevel level, string head, string fmt, params object?[] args)
    { 
        string data = $"[CSM] {head} {string.Format(fmt, args)}";
        if (level < CubismFramework.GetLoggingLevel())
            return;

        CubismFramework.CoreLogFunction(data);
    }

    public static void CubismLogVerbose(string fmt, params object?[] args)
    {
        CubismLogPrintln(Option.LogLevel.LogLevel_Verbose, "[V]",fmt, args);
    }

    public static void CubismLogDebug(string fmt, params object?[] args)
    {
        CubismLogPrintln(Option.LogLevel.LogLevel_Debug, "[D]", fmt, args);
    }

    public static void CubismLogInfo(string fmt, params object?[] args)
    {
        CubismLogPrintln(Option.LogLevel.LogLevel_Info, "[I]", fmt, args);
    }

    public static void CubismLogWarning(string fmt, params object?[] args)
    {
        CubismLogPrintln(Option.LogLevel.LogLevel_Warning, "[W]", fmt, args);
    }

    public static void CubismLogError(string fmt, params object?[] args)
    {
        CubismLogPrintln(Option.LogLevel.LogLevel_Error, "[E]", fmt, args);
    }
}
