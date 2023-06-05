using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.App;

enum SelectTarget
{
    /// <summary>
    /// デフォルトのフレームバッファにレンダリング
    /// </summary>
    SelectTarget_None,
    /// <summary>
    /// LAppModelが各自持つフレームバッファにレンダリング
    /// </summary>
    SelectTarget_ModelFrameBuffer,
    /// <summary>
    /// LAppViewの持つフレームバッファにレンダリング
    /// </summary>
    SelectTarget_ViewFrameBuffer,     
};

/// <summary>
/// 描画クラス
/// </summary>
public class LAppView
{

}
