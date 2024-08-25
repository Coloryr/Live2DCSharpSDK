using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Vulkan;

namespace Live2DCSharpSDK.Vulkan;

public class SwapchainSupportDetails
{
    /// <summary>
    /// 基本的な機能
    /// </summary>
    public SurfaceCapabilitiesKHR Capabilities;
    /// <summary>
    /// 利用可能なフォーマット
    /// </summary>
    public SurfaceFormatKHR[] Formats;
    /// <summary>
    /// 利用可能な表示モード
    /// </summary>
    public PresentModeKHR[] PresentModes; 
}
