using Live2DCSharpSDK.App;
using Silk.NET.Vulkan;

namespace Live2DCSharpSDK.Vulkan;

public class LAppViewVulkan(LAppDelegateVulkan lapp) : LAppView(lapp)
{
    public override void RenderPost()
    {

    }

    public override unsafe void RenderPre()
    {
        var vkManager = lapp.VulkanManager;
        var commandBuffer = vkManager.BeginSingleTimeCommands();

        var color = lapp.BGColor;

        var colorAttachment = new RenderingAttachmentInfo
        {
            SType = StructureType.RenderingAttachmentInfoKhr,
            ImageView = vkManager.GetSwapchainImageView(),
            ImageLayout = ImageLayout.AttachmentOptimal,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new()
            {
                Color = new(color.R, color.G, color.B, color.A)
            }
        };

        var renderingInfo = new RenderingInfo
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new(new(0, 0), vkManager.SwapchainManager.Extent),
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachment
        };

        lapp._vk.CmdBeginRendering(commandBuffer, &renderingInfo);
        lapp._vk.CmdEndRendering(commandBuffer);
        vkManager.SubmitCommand(commandBuffer, true);
    }
}
