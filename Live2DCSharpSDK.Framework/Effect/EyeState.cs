using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Effect;

public enum EyeState
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
