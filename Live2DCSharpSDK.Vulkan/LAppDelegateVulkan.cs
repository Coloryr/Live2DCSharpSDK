using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Live2DCSharpSDK.App;
using Live2DCSharpSDK.Framework.Core;
using Live2DCSharpSDK.Framework.Model;
using Live2DCSharpSDK.Framework.Rendering;

namespace Live2DCSharpSDK.Vulkan;

public class LAppDelegateVulkan : LAppDelegate
{
    private VulkanApi _api;

    public bool _framebufferResized = false;

    public LAppDelegateVulkan(VulkanApi api, LogFunction log) : base(log)
    {
        _api = api;
    }

    public override CubismRenderer CreateRenderer(CubismModel model)
    {
        
    }

    public override TextureInfo CreateTexture(LAppModel model, int index, int width, int height, nint data)
    {
        
    }

    public override void GetWindowSize(out int width, out int height)
    {
        
    }

    public override void RunInit()
    {
        
    }
}
