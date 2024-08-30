using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Live2DCSharpSDK.Framework;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Live2DCSharpSDK.Vulkan;

public class PipelineResource(Vk vk, Device device)
{
    /// <summary>
    /// normal, add, multi, maskそれぞれのパイプライン
    /// </summary>
    private Pipeline[] _pipeline;
    /// <summary>
    /// normal, add, multi, maskそれぞれのパイプラインレイアウト
    /// </summary>
    private PipelineLayout[] _pipelineLayout;

    /// <summary>
    /// シェーダーモジュールを作成する
    /// </summary>
    /// <param name="device">論理デバイス</param>
    /// <param name="filename">ファイル名</param>
    /// <returns></returns>
    public unsafe ShaderModule CreateShaderModule(Device device, string filename)
    {
        var assm = Assembly.GetExecutingAssembly();
        string name = "Live2DCSharpSDK.Vulkan.spv." + filename;
        using var item = assm.GetManifestResourceStream(name)!;
        if (item == null)
        {
            CubismLog.Error("failed to open file!");
        }
        using var mem = new MemoryStream();
        item!.CopyTo(mem);
        var data = mem.ToArray();

        var createInfo = new ShaderModuleCreateInfo
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (uint)data.Length
        };

        ShaderModule shaderModule;

        fixed (byte* codePtr = data)
        {
            createInfo.PCode = (uint*)codePtr;

            if (vk.CreateShaderModule(device, ref createInfo, null, out shaderModule) != Result.Success)
            {
                CubismLog.Error("failed to create shader module!");
            }
        }

        return shaderModule;
    }

    /// <summary>
    /// パイプラインを作成する
    /// </summary>
    /// <param name="vertFileName">Vertexシェーダーのファイル</param>
    /// <param name="fragFileName">Fragmentシェーダーのファイル</param>
    /// <param name="descriptorSetLayout">ディスクリプタセットレイアウト</param>
    public unsafe void CreateGraphicsPipeline(string vertFileName, string fragFileName,
                                    DescriptorSetLayout descriptorSetLayout)
    {
        var vertShaderModule = CreateShaderModule(device, vertFileName);
        var fragShaderModule = CreateShaderModule(device, fragFileName);

        _pipeline = new Pipeline[4];
        _pipelineLayout = new PipelineLayout[4];

        var vertShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var fragShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var shaderStages = stackalloc[]
        {
            vertShaderStageInfo,
            fragShaderStageInfo
        };

        var vertexInputInfo = new PipelineVertexInputStateCreateInfo
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo
        };

        var bindingDescription = ModelVertex.GetBindingDescription();
        var attributeDescriptions = stackalloc VertexInputAttributeDescription[2];
        ModelVertex.GetAttributeDescriptions(attributeDescriptions);
        vertexInputInfo.VertexBindingDescriptionCount = 1;
        vertexInputInfo.VertexAttributeDescriptionCount = 2;
        vertexInputInfo.PVertexBindingDescriptions = &bindingDescription;
        vertexInputInfo.PVertexAttributeDescriptions = attributeDescriptions;

        var inputAssembly = new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false
        };

        var viewportState = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount = 1
        };

        var rasterizer = new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = false,
            RasterizerDiscardEnable = false,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1.0f,
            FrontFace = FrontFace.CounterClockwise
        };

        var multisampling = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit
        };

        // 通常
        var colorBlendAttachment = new PipelineColorBlendAttachmentState
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit
            | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = true,
            SrcColorBlendFactor = BlendFactor.One,
            DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
            ColorBlendOp = BlendOp.Add,
            SrcAlphaBlendFactor = BlendFactor.One,
            DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
            AlphaBlendOp = BlendOp.Add
        };

        var colorBlending = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment
        };

        var pipelineLayoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &descriptorSetLayout
        };

        var dynamicState = stackalloc[]
        {
            DynamicState.Viewport,
            DynamicState.Scissor,
            DynamicState.CullMode
        };

        var dynamicStateCI = new PipelineDynamicStateCreateInfo
        {
            SType = StructureType.PipelineDynamicStateCreateInfo,
            DynamicStateCount = 3,
            PDynamicStates = dynamicState
        };

        var pipelineDepthStencilStateCreateInfo = new PipelineDepthStencilStateCreateInfo
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = true,
            DepthWriteEnable = true,
            DepthCompareOp = CompareOp.LessOrEqual,
            Back = new()
            {
                CompareOp = CompareOp.Always
            }
        };

        if (vk.CreatePipelineLayout(device, ref pipelineLayoutInfo, null,
            out _pipelineLayout[Blend.Normal]) != Result.Success)
        {
            CubismLog.Error("failed to create _pipeline layout!");
        }

        var renderingInfo = new PipelineRenderingCreateInfo
        {
            SType = StructureType.PipelineRenderingCreateInfo,
            ColorAttachmentCount = 1,
            DepthAttachmentFormat = CubismRenderer_Vulkan.DepthFormat
        };
        var format = CubismRenderer_Vulkan.ImageFormat;
        var format1 = CubismRenderer_Vulkan.SwapchainImageFormat;
        if (CubismRenderer_Vulkan.UseRenderTarget)
        {
            renderingInfo.PColorAttachmentFormats = &format;
        }
        else
        {
            renderingInfo.PColorAttachmentFormats = &format1;
        }

        var pipelineInfo = new GraphicsPipelineCreateInfo
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            StageCount = 2,
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multisampling,
            PColorBlendState = &colorBlending,
            Layout = _pipelineLayout[Blend.Normal],
            Subpass = 0,
            BasePipelineHandle = new(),
            PDynamicState = &dynamicStateCI,
            PDepthStencilState = &pipelineDepthStencilStateCreateInfo,
            PNext = &renderingInfo,
            PStages = shaderStages
        };

        if (vk.CreateGraphicsPipelines(device, default, 1, ref pipelineInfo, null, out _pipeline[Blend.Normal]) !=
             Result.Success)
        {
            CubismLog.Error("failed to create graphics _pipeline!");
        }

        // 加算
        colorBlendAttachment.SrcColorBlendFactor = BlendFactor.One;
        colorBlendAttachment.DstColorBlendFactor = BlendFactor.One;
        colorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.Zero;
        colorBlendAttachment.DstAlphaBlendFactor = BlendFactor.One;

        if (vk.CreatePipelineLayout(device, ref pipelineLayoutInfo, null, out _pipelineLayout[Blend.Add]) != Result.Success)
        {
            CubismLog.Error("failed to create _pipeline layout!");
        }

        pipelineInfo.Layout = _pipelineLayout[Blend.Add];

        if (vk.CreateGraphicsPipelines(device, default, 1, ref pipelineInfo, null, out _pipeline[Blend.Add]) !=
             Result.Success)
        {
            CubismLog.Error("failed to create graphics _pipeline!");
        }

        // 乗算
        colorBlendAttachment.SrcColorBlendFactor = BlendFactor.DstColor;
        colorBlendAttachment.DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha;
        colorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.Zero;
        colorBlendAttachment.DstAlphaBlendFactor = BlendFactor.One;

        if (vk.CreatePipelineLayout(device, ref pipelineLayoutInfo, null, out _pipelineLayout[Blend.Mult]) != Result.Success)
        {
            CubismLog.Error("failed to create _pipeline layout!");
        }

        pipelineInfo.Layout = _pipelineLayout[Blend.Mult];

        if (vk.CreateGraphicsPipelines(device, default, 1, ref pipelineInfo, null, out _pipeline[Blend.Mult]) !=
             Result.Success)
        {
            CubismLog.Error("failed to create graphics _pipeline!");
        }

        // マスク
        colorBlendAttachment.SrcColorBlendFactor = BlendFactor.Zero;
        colorBlendAttachment.DstColorBlendFactor = BlendFactor.OneMinusSrcColor;
        colorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.Zero;
        colorBlendAttachment.DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha;

        if (vk.CreatePipelineLayout(device, ref pipelineLayoutInfo, null, out _pipelineLayout[Blend.Mask]) != Result.Success)
        {
            CubismLog.Error("failed to create _pipeline layout!");
        }

        renderingInfo.PColorAttachmentFormats = &format;
        pipelineInfo.Layout = _pipelineLayout[Blend.Mask];

        if (vk.CreateGraphicsPipelines(device, default, 1, ref pipelineInfo, null, out _pipeline[Blend.Mask]) !=
             Result.Success)
        {
            CubismLog.Error("failed to create graphics _pipeline!");
        }

        vk.DestroyShaderModule(device, vertShaderModule, null);
        vk.DestroyShaderModule(device, fragShaderModule, null);

        SilkMarshal.Free((IntPtr)vertShaderStageInfo.PName);
        SilkMarshal.Free((IntPtr)fragShaderStageInfo.PName);
    }

    public unsafe void Release()
    {
        for (int i = 0; i < 4; i++)
        {
            vk.DestroyPipeline(device, _pipeline[i], null);
            vk.DestroyPipelineLayout(device, _pipelineLayout[i], null);
        }
        _pipeline = [];
        _pipelineLayout = [];
    }

    public Pipeline GetPipeline(int index) 
    { 
        return _pipeline[index]; 
    }

    public PipelineLayout GetPipelineLayout(int index) 
    { 
        return _pipelineLayout[index];
    }
}
