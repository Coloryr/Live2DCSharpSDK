using System.Numerics;
using System.Runtime.InteropServices;
using Live2DCSharpSDK.Framework;
using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Model;
using Live2DCSharpSDK.Framework.Rendering;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Live2DCSharpSDK.Vulkan;

public class CubismRenderer_Vulkan : CubismRenderer
{
    public delegate void FvkCmdSetCullMode(CommandBuffer commandBuffer, CullModeFlags cullMode);

    public static Device s_device;
    public static PhysicalDevice s_physicalDevice;
    public static CommandPool s_commandPool;
    public static Queue s_queue;
    public static Extent2D s_swapchainExtent;
    public static Format s_swapchainImageFormat;
    public static Format s_imageFormat;
    public static Image s_swapchainImage;
    public static ImageView s_swapchainImageView;
    public static Format s_depthFormat;
    public static bool s_useRenderTarget = false;
    public static Image s_renderTargetImage;
    public static ImageView s_renderTargetImageView;
    public static CubismPipeline_Vulkan cubismPipeline_Vulkan;

    /// <summary>
    /// レンダラを作成するための各種設定
    /// モデルを読み込む前に一度だけ呼び出す
    /// </summary>
    /// <param name="device">論理デバイス</param>
    /// <param name="physicalDevice">物理デバイス</param>
    /// <param name="commandPool">コマンドプール</param>
    /// <param name="queue">キュー</param>
    /// <param name="extent">描画解像度</param>
    /// <param name="depthFormat">深度フォーマット</param>
    /// <param name="surfaceFormat">フレームバッファのフォーマット</param>
    /// <param name="swapchainImage"></param>
    /// <param name="swapchainImageView">スワップチェーンのイメージビュー</param>
    /// <param name="dFormat"></param>
    public static void InitializeConstantSettings(Device device, PhysicalDevice physicalDevice,
                                           CommandPool commandPool, Queue queue,
                                           Extent2D extent, Format depthFormat, Format surfaceFormat,
                                           Image swapchainImage,
                                           ImageView swapchainImageView,
                                           Format dFormat)
    {
        s_device = device;
        s_physicalDevice = physicalDevice;
        s_commandPool = commandPool;
        s_queue = queue;
        s_swapchainExtent = extent;
        s_swapchainImageFormat = depthFormat;
        s_imageFormat = surfaceFormat;
        s_swapchainImage = swapchainImage;
        s_swapchainImageView = swapchainImageView;
        s_depthFormat = dFormat;
    }

    /// <summary>
    /// レンダーターゲット変更を有効にする
    /// </summary>
    public static void EnableChangeRenderTarget()
    {
        s_useRenderTarget = true;
    }

    /// <summary>
    /// レンダーターゲットを変更した際にレンダラを作成するための各種設定
    /// </summary>
    /// <param name="image">イメージ</param>
    /// <param name="imageview">イメージビュー</param>
    public static void SetRenderTarget(Image image, ImageView imageview)
    {
        s_renderTargetImage = image;
        s_renderTargetImageView = imageview;
    }

    /// <summary>
    /// 行列を更新する
    /// </summary>
    /// <param name="vkMat4">更新する行列</param>
    /// <param name="cubismMat">新しい値</param>
    public static unsafe void UpdateMatrix(float* vkMat4, CubismMatrix44 cubismMat)
    {
        for (int i = 0; i < 16; i++)
        {
            vkMat4[i] = cubismMat.Tr[i];
        }
    }

    /// <summary>
    /// カラーベクトルを更新する
    /// </summary>
    /// <param name="vkVec4">更新するカラーベクトル</param>
    /// <param name="r">新しい値</param>
    /// <param name="g">新しい値</param>
    /// <param name="b">新しい値</param>
    /// <param name="a">新しい値</param>
    public static unsafe void UpdateColor(float* vkVec4, float r, float g, float b, float a)
    {
        vkVec4[0] = r;
        vkVec4[1] = g;
        vkVec4[2] = b;
        vkVec4[3] = a;
    }

    private readonly Vk _vk;

    private FvkCmdSetCullMode vkCmdSetCullModeEXT;

    /// <summary>
    /// クリッピングマスク管理オブジェクト
    /// </summary>
    private CubismClippingManager_Vulkan _clippingManager;
    /// <summary>
    /// 描画オブジェクトのインデックスを描画順に並べたリスト
    /// </summary>
    private int[] _sortedDrawableIndexList = [];
    /// <summary>
    /// マスクテクスチャに描画するためのクリッピングコンテキスト
    /// </summary>
    public CubismClippingContext_Vulkan? ClippingContextBufferForMask { get; set; }
    /// <summary>
    /// 画面上描画するためのクリッピングコンテキスト
    /// </summary>
    public CubismClippingContext_Vulkan? ClippingContextBufferForDraw { get; set; }
    /// <summary>
    /// マスク描画用のフレームバッファ
    /// </summary>
    private CubismOffscreenSurface_Vulkan[] _offscreenFrameBuffers;
    /// <summary>
    /// 頂点バッファ
    /// </summary>
    private CubismBufferVulkan[] _vertexBuffers;
    /// <summary>
    /// 頂点バッファを更新する際に使うステージングバッファ
    /// </summary>
    private List<CubismBufferVulkan> _stagingBuffers = [];
    /// <summary>
    /// インデックスバッファ
    /// </summary>
    private CubismBufferVulkan[] _indexBuffers;
    /// <summary>
    /// ディスクリプタプール
    /// </summary>
    private DescriptorPool _descriptorPool;
    /// <summary>
    /// ディスクリプタセットのレイアウト
    /// </summary>
    private DescriptorSetLayout _descriptorSetLayout;
    /// <summary>
    /// ディスクリプタ管理オブジェクト
    /// </summary>
    private List<Descriptor> _descriptorSets = [];
    /// <summary>
    /// モデルが使うテクスチャ
    /// </summary>
    private List<CubismImageVulkan> _textures = [];
    /// <summary>
    /// オフスクリーンの色情報を保持する深度画像
    /// </summary>
    private CubismImageVulkan _depthImage;
    /// <summary>
    /// クリアカラー
    /// </summary>
    private ClearValue _clearColor;
    /// <summary>
    /// セマフォ
    /// </summary>
    private Semaphore _updateFinishedSemaphore;
    /// <summary>
    /// 更新用コマンドバッファ
    /// </summary>
    private CommandBuffer _updateCommandBuffer;
    /// <summary>
    /// 描画用コマンドバッファ
    /// </summary>
    private CommandBuffer _drawCommandBuffer;

    public CubismRenderer_Vulkan(Vk vk, CubismModel model) : base(model)
    {
        _vk = vk;

        cubismPipeline_Vulkan = new(vk, s_device);
    }

    /// <summary>
    /// スワップチェーンを再作成したときに変更されるリソースを更新する
    /// </summary>
    /// <param name="extent">クリッピングマスクバッファのサイズ</param>
    /// <param name="image"></param>
    /// <param name="imageView">イメージビュー</param>
    public static void UpdateSwapchainVariable(Extent2D extent, Image image, ImageView imageView)
    {
        s_swapchainExtent = extent;
        s_swapchainImage = image;
        s_swapchainImageView = imageView;
    }

    /// <summary>
    /// スワップチェーンイメージを更新する
    /// </summary>
    /// <param name="image">イメージ</param>
    /// <param name="imageView">イメージビュー</param>
    public static void UpdateRendererSettings(Image image, ImageView imageView)
    {
        s_swapchainImage = image;
        s_swapchainImageView = imageView;
    }

    public static Viewport GetViewport(float width, float height, float minDepth, float maxDepth)
    {
        return new()
        {
            Width = width,
            Height = height,
            MinDepth = minDepth,
            MaxDepth = maxDepth
        };
    }

    public static Rect2D GetScissor(float offsetX, float offsetY, float width, float height)
    {
        return new()
        {
            Offset = new()
            {
                X = (int)offsetX,
                Y = (int)offsetY
            },
            Extent = new()
            {
                Height = (uint)height,
                Width = (uint)width
            }
        };
    }

    /// <summary>
    /// コマンドの記録を開始する。
    /// </summary>
    /// <returns>記録を開始したコマンドバッファ</returns>
    public CommandBuffer BeginSingleTimeCommands()
    {
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = s_commandPool,
            CommandBufferCount = 1
        };

        _vk.AllocateCommandBuffers(s_device, ref allocInfo, out var commandBuffer);

        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        _vk.BeginCommandBuffer(commandBuffer, ref beginInfo);

        return commandBuffer;
    }

    /// <summary>
    /// コマンドを実行する。
    /// </summary>
    /// <param name="commandBuffer">コマンドバッファ</param>
    /// <param name="signalUpdateFinishedSemaphore">フェンス</param>
    /// <param name="waitUpdateFinishedSemaphore"></param>
    public unsafe void SubmitCommand(CommandBuffer commandBuffer, Semaphore? signalUpdateFinishedSemaphore = null,
        Semaphore? waitUpdateFinishedSemaphore = null)
    {
        _vk.EndCommandBuffer(commandBuffer);
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };
        if (waitUpdateFinishedSemaphore is { } sem1)
        {
            var waitStages = new PipelineStageFlags[]
            {
                PipelineStageFlags.VertexInputBit, PipelineStageFlags.ColorAttachmentOutputBit
            };

            submitInfo.WaitSemaphoreCount = 1;
            submitInfo.PWaitSemaphores = &sem1;
            fixed (PipelineStageFlags* ptr = waitStages)
            {
                submitInfo.PWaitDstStageMask = ptr;
                _vk.QueueSubmit(s_queue, 1, ref submitInfo, new Fence());
            }
            _vk.QueueWaitIdle(s_queue);
        }
        else if (signalUpdateFinishedSemaphore is { } sem2)
        {
            submitInfo.SignalSemaphoreCount = 1;
            submitInfo.PSignalSemaphores = &sem2;
            _vk.QueueSubmit(s_queue, 1, ref submitInfo, new Fence());
        }
        else
        {
            _vk.QueueSubmit(s_queue, 1, ref submitInfo, new Fence());
            _vk.QueueWaitIdle(s_queue);
            _vk.FreeCommandBuffers(s_device, s_commandPool, 1, ref commandBuffer);
        }
    }

    /// <summary>
    /// コマンドバッファを作成する。
    /// </summary>
    public unsafe void CreateCommandBuffer()
    {
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = s_commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };

        if (_vk.AllocateCommandBuffers(s_device, ref allocInfo, out _updateCommandBuffer) != Result.Success
            || _vk.AllocateCommandBuffers(s_device, ref allocInfo, out _drawCommandBuffer) != Result.Success)
        {
            CubismLog.Error("[Live2D Vulkan]failed to allocate command buffers!");
        }
    }

    /// <summary>
    /// 空の頂点バッファを作成する。
    /// </summary>
    public void CreateVertexBuffer()
    {
        int drawableCount = Model.GetDrawableCount();
        _vertexBuffers = new CubismBufferVulkan[drawableCount];

        for (int drawAssign = 0; drawAssign < drawableCount; drawAssign++)
        {
            int vcount = Model.GetDrawableVertexCount(drawAssign);
            if (vcount != 0)
            {
                //頂点データは初期化できない

                var bufferSize = (ulong)(ModelVertex.Size * vcount); // 総長 構造体サイズ*個数
                var stagingBuffer = new CubismBufferVulkan(_vk);
                stagingBuffer.CreateBuffer(s_device, s_physicalDevice, bufferSize, BufferUsageFlags.TransferSrcBit,
                                          MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

                stagingBuffer.Map(s_device, bufferSize);
                _stagingBuffers.Add(stagingBuffer);

                var vertexBuffer = new CubismBufferVulkan(_vk);
                vertexBuffer.CreateBuffer(s_device, s_physicalDevice, bufferSize,
                                           BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit,
                                           MemoryPropertyFlags.DeviceLocalBit);
                _vertexBuffers[drawAssign] = vertexBuffer;
            }
        }
    }

    /// <summary>
    /// インデックスバッファを作成する。
    /// </summary>
    public unsafe void CreateIndexBuffer()
    {
        int drawableCount = Model.GetDrawableCount();
        _indexBuffers = new CubismBufferVulkan[drawableCount];

        for (int drawAssign = 0; drawAssign < drawableCount; drawAssign++)
        {
            int icount = Model.GetDrawableVertexIndexCount(drawAssign);
            if (icount != 0)
            {
                var bufferSize = (ulong)(2 * icount);
                var indices = Model.GetDrawableVertexIndices(drawAssign);

                var stagingBuffer = new CubismBufferVulkan(_vk);
                stagingBuffer.CreateBuffer(s_device, s_physicalDevice, bufferSize, BufferUsageFlags.TransferSrcBit,
                                           MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                stagingBuffer.Map(s_device, bufferSize);
                stagingBuffer.MemCpy(indices, (long)bufferSize);
                stagingBuffer.UnMap(s_device);

                var indexBuffer = new CubismBufferVulkan(_vk);
                indexBuffer.CreateBuffer(s_device, s_physicalDevice, bufferSize,
                                         BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit,
                                         MemoryPropertyFlags.DeviceLocalBit);

                var commandBuffer = BeginSingleTimeCommands();

                var copyRegion = new BufferCopy
                {
                    Size = bufferSize
                };
                _vk.CmdCopyBuffer(commandBuffer, stagingBuffer.Buffer, indexBuffer.Buffer, 1, ref copyRegion);
                SubmitCommand(commandBuffer);
                _indexBuffers[drawAssign] = indexBuffer;
                stagingBuffer.Destroy(s_device);
            }
        }
    }

    /// <summary>
    /// ユニフォームバッファとディスクリプタセットを作成する。
    /// ディスクリプタセットレイアウトはユニフォームバッファ1つとモデル用テクスチャ1つとマスク用テクスチャ1つを指定する。
    /// </summary>
    public unsafe void CreateDescriptorSets()
    {
        // ディスクリプタプールの作成
        int drawableCount = Model.GetDrawableCount();
        int textureCount = 2;
        int drawModeCount = 2; // マスクされる描画と通常の描画
        int descriptorSetCount = drawableCount * drawModeCount;

        var poolSizes = new DescriptorPoolSize[]
        {
            new()
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = (uint)descriptorSetCount
            },
            new()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = (uint)(descriptorSetCount * textureCount) // drawableCount * 描画方法の数 * テクスチャの数
            }
        };

        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 2
        };
        fixed (DescriptorPoolSize* ptr = poolSizes)
        {
            poolInfo.PPoolSizes = ptr;
            poolInfo.MaxSets = (uint)descriptorSetCount;
            if (_vk.CreateDescriptorPool(s_device, ref poolInfo, null, out _descriptorPool) != Result.Success)
            {
                CubismLog.Error("failed to create descriptor pool!");
            }
        }

        // ディスクリプタセットレイアウトの作成
        var bindings = new DescriptorSetLayoutBinding[]
        {
            new()
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType =  DescriptorType.UniformBuffer,
                PImmutableSamplers = null,
                StageFlags = ShaderStageFlags.All
            },
            new()
            {
                Binding = 1,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                PImmutableSamplers = null,
                StageFlags = ShaderStageFlags.FragmentBit
            },
            new()
            {
                Binding = 2,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                PImmutableSamplers = null,
                StageFlags = ShaderStageFlags.FragmentBit
            }
        };

        var layoutInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 3
        };
        fixed (DescriptorSetLayoutBinding* ptr = bindings)
        {
            layoutInfo.PBindings = ptr;
            if (_vk.CreateDescriptorSetLayout(s_device, ref layoutInfo, null, out _descriptorSetLayout) != Result.Success)
            {
                CubismLog.Error("failed to create descriptor set layout!");
            }
        }

        _descriptorSets.Clear();

        for (int drawAssign = 0; drawAssign < drawableCount; drawAssign++)
        {
            var desc = new Descriptor()
            {
                UniformBuffer = new CubismBufferVulkan(_vk)
            };

            desc.UniformBuffer.CreateBuffer(s_device, s_physicalDevice, ModelUBOInfo.Size,
                                       BufferUsageFlags.UniformBufferBit,
                                      MemoryPropertyFlags.HostVisibleBit |
                                      MemoryPropertyFlags.HostCoherentBit);
            desc.UniformBuffer.Map(s_device, ulong.MaxValue);
            _descriptorSets.Add(desc);
        }

        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _descriptorPool,
            DescriptorSetCount = (uint)descriptorSetCount
        };
        var layouts = new DescriptorSetLayout[descriptorSetCount];
        for (int i = 0; i < descriptorSetCount; i++)
        {
            layouts[i] = _descriptorSetLayout;
        }
        fixed (DescriptorSetLayout* ptr = layouts)
        {
            allocInfo.PSetLayouts = ptr;
            var descriptorSets = new DescriptorSet[descriptorSetCount];
            if (_vk.AllocateDescriptorSets(s_device, &allocInfo, descriptorSets) != Result.Success)
            {
                CubismLog.Error("failed to allocate descriptor sets!");
            }

            for (int drawAssign = 0; drawAssign < drawableCount; drawAssign++)
            {
                _descriptorSets[drawAssign].DescriptorSet = descriptorSets[drawAssign * 2];
                _descriptorSets[drawAssign].DescriptorSetMasked = descriptorSets[drawAssign * 2 + 1];
            }
        }
    }

    /// <summary>
    /// 深度バッファを作成する。
    /// </summary>
    public void CreateDepthBuffer()
    {
        _depthImage.CreateImage(s_device, s_physicalDevice, (int)s_swapchainExtent.Width, (int)s_swapchainExtent.Height,
        1, s_depthFormat, ImageTiling.Optimal,
         ImageUsageFlags.DepthStencilAttachmentBit);
        _depthImage.CreateView(s_device, s_depthFormat,
             ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit, 1);
    }

    /// <summary>
    /// レンダラーを初期化する。
    /// </summary>
    public unsafe void InitializeRenderer()
    {
        vkCmdSetCullModeEXT = Marshal.GetDelegateForFunctionPointer<FvkCmdSetCullMode>(_vk.GetDeviceProcAddr(s_device, "vkCmdSetCullModeEXT"));

        var semaphoreInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo
        };
        _vk.CreateSemaphore(s_device, ref semaphoreInfo, null, out _updateFinishedSemaphore);

        CreateCommandBuffer();
        CreateVertexBuffer();
        CreateIndexBuffer();
        CreateDescriptorSets();
        CreateDepthBuffer();
        cubismPipeline_Vulkan.CreatePipelines(_descriptorSetLayout);
    }

    /// <summary>
    /// レンダラの初期化処理を実行する
    /// <para></para>
    /// 引数に渡したモデルからレンダラの初期化処理に必要な情報を取り出すことができる
    /// </summary>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="maskBufferCount">マスクバッファの数</param>
    public void Initialize(CubismModel model, int maskBufferCount)
    {
        if (s_device.Handle == 0)
        {
            CubismLog.Error("Device has not been set.");
            return;
        }

        if (model.IsUsingMasking())
        {
            //モデルがマスクを使用している時のみにする
            _clippingManager = new CubismClippingManager_Vulkan();
            _clippingManager.Initialize(model, maskBufferCount);
        }

        int count = model.GetDrawableCount();
        _sortedDrawableIndexList = new int[count];
        for (int a = 0; a < count; a++)
        {
            _sortedDrawableIndexList[a] = 0;
        }

        Initialize(model, maskBufferCount); // 親クラスの処理を呼ぶ

        // 1未満は1に補正する
        if (maskBufferCount < 1)
        {
            maskBufferCount = 1;
            CubismLog.Warning("The number of render textures must be an integer greater than or equal to 1. Set the number of render textures to 1.");
        }
        if (model.IsUsingMasking())
        {
            var bufferWidth = (int)_clippingManager.ClippingMaskBufferSize.X;
            var bufferHeight = (int)_clippingManager.ClippingMaskBufferSize.Y;

            _offscreenFrameBuffers = new CubismOffscreenSurface_Vulkan[maskBufferCount];
            // バックバッファ分確保
            for (int i = 0; i < maskBufferCount; i++)
            {
                _offscreenFrameBuffers[i] = new(_vk);
                _offscreenFrameBuffers[i].CreateOffscreenSurface(s_device, s_physicalDevice, bufferWidth, bufferHeight,
                    s_imageFormat, s_depthFormat);
            }
        }

        InitializeRenderer();
    }

    /// <summary>
    /// 頂点バッファを更新する。
    /// </summary>
    /// <param name="drawAssign">描画インデックス</param>
    /// <param name="vcount">頂点数</param>
    /// <param name="varray">頂点配列</param>
    /// <param name="uvarray">uv配列</param>
    /// <param name="commandBuffer">コマンドバッファ</param>
    public unsafe void CopyToBuffer(int drawAssign, int vcount, float* varray, float* uvarray,
                      CommandBuffer commandBuffer)
    {
        var vertices = new ModelVertex[vcount];

        for (int ct = 0, ct1 = 0; ct < vcount; ct++, ct1 += 2)
        {
            ModelVertex vertex;
            // モデルデータからのコピー
            vertex.pos.X = varray[ct1 + 0];
            vertex.pos.Y = varray[ct1 + 1];
            vertex.texCoord.X = uvarray[ct1 + 0];
            vertex.texCoord.Y = uvarray[ct1 + 1];
            vertices[ct] = vertex;
        }
        var bufferSize = ModelVertex.Size * vertices.Length;

        fixed (void* ptr = vertices)
        {
            _stagingBuffers[drawAssign].MemCpy(ptr, bufferSize);
        }

        var copyRegion = new BufferCopy
        {
            Size = (ulong)bufferSize
        };

        _vk.CmdCopyBuffer(commandBuffer, _stagingBuffers[drawAssign].Buffer, _vertexBuffers[drawAssign].Buffer, 1,
                        ref copyRegion);
    }

    /// <summary>
    /// ディスクリプタセットを更新する
    /// </summary>
    /// <param name="descriptor">1つのシェーダーが使用するディスクリプタセットとUBO</param>
    /// <param name="textureIndex">テクスチャインデックス</param>
    /// <param name="isMasked"></param>
    public unsafe void UpdateDescriptorSet(Descriptor descriptor, int textureIndex, bool isMasked)
    {
        var uniformBuffer = descriptor.UniformBuffer.Buffer;
        // descriptorが更新されていない最初の1回のみ行う
        DescriptorSet descriptorSet;
        if (isMasked && !descriptor.IsDescriptorSetMaskedUpdated)
        {
            descriptorSet = descriptor.DescriptorSetMasked;
            descriptor.IsDescriptorSetMaskedUpdated = true;
        }
        else if (!descriptor.IsDescriptorSetUpdated)
        {
            descriptorSet = descriptor.DescriptorSet;
            descriptor.IsDescriptorSetUpdated = true;
        }
        else
        {
            return;
        }

        var descriptorWrites = new WriteDescriptorSet[3];

        var uniformBufferInfo = new DescriptorBufferInfo
        {
            Buffer = uniformBuffer,
            Offset = 0,
            Range = ulong.MaxValue
        };
        var ubo = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = descriptorSet,
            DstBinding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            PBufferInfo = &uniformBufferInfo
        };
        descriptorWrites[0] = ubo;

        //テクスチャ1はキャラクターのテクスチャ、テクスチャ2はマスク用のオフスクリーンに使用するテクスチャ
        var imageInfo1 = new DescriptorImageInfo
        {
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            ImageView = _textures[textureIndex].View,
            Sampler = _textures[textureIndex].Sampler
        };
        var image1 = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = descriptorSet,
            DstBinding = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            PImageInfo = &imageInfo1
        };
        descriptorWrites[1] = image1;

        if (isMasked)
        {
            var imageInfo2 = new DescriptorImageInfo
            {
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = _offscreenFrameBuffers[ClippingContextBufferForDraw!.
                BufferIndex].GetTextureView(),
                Sampler = _offscreenFrameBuffers[ClippingContextBufferForDraw.
                BufferIndex].GetTextureSampler()
            };
            var image2 = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = descriptorSet,
                DstBinding = 2,
                DescriptorType = DescriptorType.CombinedImageSampler,
                DescriptorCount = 1,
                PImageInfo = &imageInfo2
            };
            descriptorWrites[2] = image2;
        }

        fixed (WriteDescriptorSet* ptr = descriptorWrites)
        {
            _vk.UpdateDescriptorSets(s_device, (uint)(isMasked ? 3 : 2), ptr, 0, null);
        }
    }

    /// <summary>
    /// メッシュ描画を実行する
    /// </summary>
    /// <param name="model">描画対象のモデル</param>
    /// <param name="index">描画オブジェクトのインデックス</param>
    /// <param name="cmdBuffer">フレームバッファ関連のコマンドバッファ</param>
    public unsafe void ExecuteDrawForDraw(CubismModel model, int index, CommandBuffer cmdBuffer)
    {
        // パイプラインレイアウト設定用のインデックスを取得
        int blendIndex;
        int shaderIndex;
        bool masked = ClippingContextBufferForDraw != null; // この描画オブジェクトはマスク対象か
        bool invertedMask = model.GetDrawableInvertedMask(index);
        int offset = (masked ? (invertedMask ? 2 : 1) : 0) + (IsPremultipliedAlpha ? 3 : 0);

        switch (model.GetDrawableBlendMode(index))
        {
            default:
                shaderIndex = ShaderNames.ShaderNames_Normal + offset;
                blendIndex = Blend.Blend_Normal;
                break;

            case CubismBlendMode.Additive:
                shaderIndex = ShaderNames.ShaderNames_Add + offset;
                blendIndex = Blend.Blend_Add;
                break;

            case CubismBlendMode.Multiplicative:
                shaderIndex = ShaderNames.ShaderNames_Mult + offset;
                blendIndex = Blend.Blend_Mult;
                break;
        }

        var descriptor = _descriptorSets[index];
        var ubo = new ModelUBO();
        if (masked)
        {
            // クリッピング用行列の設定
            UpdateMatrix(ubo.ClipMatrix, ClippingContextBufferForDraw!.MatrixForDraw); // テクスチャ座標の変換に使用するのでy軸の向きは反転しない

            // カラーチャンネルの設定
            SetColorChannel(ubo, ClippingContextBufferForDraw);
        }

        // MVP行列の設定
        UpdateMatrix(ubo.ProjectionMatrix, GetMvpMatrix());

        // 色定数バッファの設定
        var baseColor = GetModelColorWithOpacity(model.GetDrawableOpacity(index));
        var multiplyColor = model.GetMultiplyColor(index);
        var screenColor = model.GetScreenColor(index);
        SetColorUniformBuffer(ubo, baseColor, multiplyColor, screenColor);

        // ディスクリプタにユニフォームバッファをコピー
        descriptor.UniformBuffer.MemCpy(&ubo, ModelUBOInfo.Size);

        // 頂点バッファの設定
        BindVertexAndIndexBuffers(index, cmdBuffer);

        // テクスチャインデックス取得
        int textureIndex = model.GetDrawableTextureIndex(index);

        // ディスクリプタセットのバインド
        UpdateDescriptorSet(descriptor, textureIndex, masked);
        var descriptorSet = masked ? _descriptorSets[index].DescriptorSetMasked
                                                 : _descriptorSets[index].DescriptorSet;
        _vk.CmdBindDescriptorSets(cmdBuffer, PipelineBindPoint.Graphics,
                                    cubismPipeline_Vulkan.GetPipelineLayout(shaderIndex, blendIndex), 0, 1,
                                    ref descriptorSet, 0, null);

        // パイプラインのバインド
        _vk.CmdBindPipeline(cmdBuffer, PipelineBindPoint.Graphics,
                          cubismPipeline_Vulkan.GetPipeline(shaderIndex, blendIndex));

        // 描画
        _vk.CmdDrawIndexed(cmdBuffer, (uint)model.GetDrawableVertexIndexCount(index), 1, 0, 0, 0);
    }

    /// <summary>
    /// マスク描画を実行する
    /// </summary>
    /// <param name="model">描画対象のモデル</param>
    /// <param name="index">描画オブジェクトのインデックス</param>
    /// <param name="cmdBuffer">フレームバッファ関連のコマンドバッファ</param>
    public unsafe void ExecuteDrawForMask(CubismModel model, int index, CommandBuffer cmdBuffer)
    {
        int shaderIndex = ShaderNames.ShaderNames_SetupMask;
        int blendIndex = Blend.Blend_Mask;

        var descriptor = _descriptorSets[index];
        var ubo = new ModelUBO();

        // クリッピング用行列の設定
        UpdateMatrix(ubo.ClipMatrix, ClippingContextBufferForMask!.MatrixForMask);

        // カラーチャンネルの設定
        SetColorChannel(ubo, ClippingContextBufferForMask);

        // 色定数バッファの設定
        var rect = ClippingContextBufferForMask.LayoutBounds;
        var baseColor = new CubismTextureColor(rect.X * 2.0f - 1.0f, rect.Y * 2.0f - 1.0f, rect.GetRight() * 2.0f - 1.0f, rect.GetBottom() * 2.0f - 1.0f);
        var multiplyColor = model.GetMultiplyColor(index);
        var screenColor = model.GetScreenColor(index);
        SetColorUniformBuffer(ubo, baseColor, multiplyColor, screenColor);

        // ディスクリプタにユニフォームバッファをコピー
        descriptor.UniformBuffer.MemCpy(&ubo, ModelUBOInfo.Size);

        // 頂点バッファの設定
        BindVertexAndIndexBuffers(index, cmdBuffer);

        // テクスチャインデックス取得
        int textureIndex = model.GetDrawableTextureIndex(index);

        // ディスクリプタセットのバインド
        UpdateDescriptorSet(descriptor, textureIndex, false);
        _vk.CmdBindDescriptorSets(cmdBuffer, PipelineBindPoint.Graphics,
                                cubismPipeline_Vulkan.GetPipelineLayout(shaderIndex, blendIndex), 0, 1,
                                ref _descriptorSets[index].DescriptorSet, 0, null);

        // パイプラインのバインド
        _vk.CmdBindPipeline(cmdBuffer, PipelineBindPoint.Graphics,
                          cubismPipeline_Vulkan.GetPipeline(shaderIndex, blendIndex));

        // 描画
        _vk.CmdDrawIndexed(cmdBuffer, (uint)model.GetDrawableVertexIndexCount(index), 1, 0, 0, 0);
    }

    /// <summary>
    /// [オーバーライド]
    /// <para></para>
    /// 描画オブジェクト（アートメッシュ）を描画する。
    /// </summary>
    /// <param name="model">描画対象のモデル</param>
    /// <param name="index">描画メッシュのインデックス</param>
    /// <param name="commandBuffer">コマンドバッファ</param>
    /// <param name="updateCommandBuffer">更新用コマンドバッファ</param>
    public unsafe void DrawMeshVulkan(CubismModel model, int index,
                        CommandBuffer commandBuffer, CommandBuffer updateCommandBuffer)
    {
        if (s_device.Handle == 0)
        {
            // デバイス未設定
            return;
        }
        if (model.GetDrawableVertexIndexCount(index) == 0)
        {
            // 描画物無し
            return;
        }
        if (model.GetDrawableOpacity(index) <= 0.0f && ClippingContextBufferForMask == null)
        {
            // 描画不要なら描画処理をスキップする
            return;
        }

        int textureIndex = model.GetDrawableTextureIndices(index);
        if (_textures[textureIndex].Sampler.Handle == 0 || _textures[textureIndex].View.Handle == 0)
        {
            return;
        }

        // 裏面描画の有効・無効
        if (IsCulling)
        {
            vkCmdSetCullModeEXT(commandBuffer, CullModeFlags.BackBit);
        }
        else
        {
            vkCmdSetCullModeEXT(commandBuffer, CullModeFlags.None);
        }

        // 頂点バッファにコピー
        CopyToBuffer(index, model.GetDrawableVertexCount(index),
                     model.GetDrawableVertices(index),
                     (float*)model.GetDrawableVertexUvs(index),
                     updateCommandBuffer);

        if (ClippingContextBufferForMask != null) // マスク生成時
        {
            ExecuteDrawForMask(model, index, commandBuffer);
        }
        else
        {
            ExecuteDrawForDraw(model, index, commandBuffer);
        }

        ClippingContextBufferForDraw = null;
        ClippingContextBufferForMask = null;
    }

    /// <summary>
    /// レンダリング開始
    /// </summary>
    /// <param name="drawCommandBuffer">コマンドバッファ</param>
    /// <param name="isResume">レンダリング再開かのフラグ</param>
    public unsafe void BeginRendering(CommandBuffer drawCommandBuffer, bool isResume)
    {
        var clearValue = new ClearValue[2];
        clearValue[0].Color = _clearColor.Color;
        clearValue[1].DepthStencil = new(1.0f, 0);

        var colorAttachment = new RenderingAttachmentInfo
        {
            SType = StructureType.RenderingAttachmentInfoKhr
        };
        if (s_useRenderTarget)
        {
            colorAttachment.ImageView = s_renderTargetImageView;
            if (isResume)
            {
                colorAttachment.LoadOp = AttachmentLoadOp.Load;
            }
            else
            {
                colorAttachment.LoadOp = AttachmentLoadOp.Clear;
            }
        }
        else
        {
            colorAttachment.ImageView = s_swapchainImageView;
            colorAttachment.LoadOp = AttachmentLoadOp.Load;
        }
        colorAttachment.ImageLayout = ImageLayout.AttachmentOptimal;
        colorAttachment.StoreOp = AttachmentStoreOp.Store;
        colorAttachment.ClearValue = clearValue[0];

        var depthStencilAttachment = new RenderingAttachmentInfo
        {
            SType = StructureType.RenderingAttachmentInfoKhr,
            ImageView = _depthImage.View,
            ImageLayout = ImageLayout.DepthStencilAttachmentOptimal,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            ClearValue = new(new(1.0f), new(0))
        };

        var renderingInfo = new RenderingInfo();
        renderingInfo.SType = StructureType.RenderingInfo;
        renderingInfo.RenderArea = new()
        {
            Offset = new(),
            Extent = s_swapchainExtent
        };
        renderingInfo.LayerCount = 1;
        renderingInfo.ColorAttachmentCount = 1;
        renderingInfo.PColorAttachments = &colorAttachment;
        renderingInfo.PDepthAttachment = &depthStencilAttachment;

        _vk.CmdBeginRendering(drawCommandBuffer, &renderingInfo);
    }

    /// <summary>
    /// レンダリング終了
    /// </summary>
    /// <param name="drawCommandBuffer">コマンドバッファ</param>
    public unsafe void EndRendering(CommandBuffer drawCommandBuffer)
    {
        _vk.CmdEndRendering(drawCommandBuffer);

        // レイアウト変更
        if (s_useRenderTarget)
        {
            var memoryBarrier = new ImageMemoryBarrier();
            memoryBarrier.SType = StructureType.ImageMemoryBarrier;
            memoryBarrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
            memoryBarrier.OldLayout = ImageLayout.Undefined;
            memoryBarrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
            memoryBarrier.Image = s_renderTargetImage;
            memoryBarrier.SubresourceRange = new()
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            };

            _vk.CmdPipelineBarrier(drawCommandBuffer, PipelineStageFlags.ColorAttachmentOutputBit,
                               PipelineStageFlags.BottomOfPipeBit, 0, 0, null, 0, null, 1, ref memoryBarrier);
        }
        else
        {
            var memoryBarrier = new ImageMemoryBarrier();
            memoryBarrier.SType = StructureType.ImageMemoryBarrier;
            memoryBarrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
            memoryBarrier.OldLayout = ImageLayout.Undefined;
            memoryBarrier.NewLayout = ImageLayout.PresentSrcKhr;
            memoryBarrier.Image = s_swapchainImage;
            memoryBarrier.SubresourceRange = new()
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            };

            _vk.CmdPipelineBarrier(drawCommandBuffer, PipelineStageFlags.ColorAttachmentOutputBit,
                                 PipelineStageFlags.BottomOfPipeBit, 0, 0, null, 0, null, 1, ref memoryBarrier);
        }
    }

    public void BindTexture(CubismImageVulkan image)
    {
        _textures.Add(image);
    }

    /// <summary>
    /// クリッピングマスクバッファのサイズを設定する
    /// </summary>
    /// <param name="width">クリッピングマスクバッファの横幅</param>
    /// <param name="height">クリッピングマスクバッファの立幅</param>
    public void SetClippingMaskBufferSize(float width, float height)
    {
        // インスタンス破棄前にレンダーテクスチャの数を保存
        var renderTextureCount = _clippingManager.RenderTextureCount;

        //FrameBufferのサイズを変更するためにインスタンスを破棄・再作成する
        _clippingManager.Dispose();

        _clippingManager = new CubismClippingManager_Vulkan();

        _clippingManager.SetClippingMaskBufferSize(width, height);

        _clippingManager.Initialize(Model, renderTextureCount);
    }

    /// <summary>
    /// クリッピングマスクバッファのサイズを取得する
    /// </summary>
    /// <returns>クリッピングマスクバッファのサイズ</returns>
    public Vector2 GetClippingMaskBufferSize()
    {
        return _clippingManager.ClippingMaskBufferSize;
    }

    public CubismOffscreenSurface_Vulkan GetMaskBuffer(int index)
    {
        return _offscreenFrameBuffers[index];
    }

    private unsafe void SetColorUniformBuffer(ModelUBO ubo, CubismTextureColor baseColor,
                                                  CubismTextureColor multiplyColor, CubismTextureColor screenColor)
    {
        UpdateColor(ubo.BaseColor, baseColor.R, baseColor.G, baseColor.B, baseColor.A);
        UpdateColor(ubo.MultiplyColor, multiplyColor.R, multiplyColor.G, multiplyColor.B, multiplyColor.A);
        UpdateColor(ubo.ScreenColor, screenColor.R, screenColor.G, screenColor.B, screenColor.A);
    }

    private void BindVertexAndIndexBuffers(int index, CommandBuffer cmdBuffer)
    {
        var vertexBuffers = new Buffer[] { _vertexBuffers[index].Buffer };
        var offsets = new ulong[] { 0 };
        _vk.CmdBindVertexBuffers(cmdBuffer, 0, 1, vertexBuffers, offsets);
        _vk.CmdBindIndexBuffer(cmdBuffer, _indexBuffers[index].Buffer, 0, IndexType.Uint16);
    }

    private unsafe void SetColorChannel(ModelUBO ubo, CubismClippingContext_Vulkan contextBuffer)
    {
        int channelIndex = contextBuffer.LayoutChannelIndex;
        var colorChannel = contextBuffer.Manager.GetChannelFlagAsColor(channelIndex);
        UpdateColor(ubo.ChannelFlag, colorChannel.R, colorChannel.G, colorChannel.B, colorChannel.A);
    }

    public unsafe override void Dispose()
    {
        // オフスクリーンを作成していたのなら開放
        for (int i = 0; i < _offscreenFrameBuffers.Length; i++)
        {
            _offscreenFrameBuffers[i].DestroyOffscreenSurface(s_device);
        }

        _offscreenFrameBuffers = [];

        _depthImage.Destroy(s_device);
        _vk.DestroySemaphore(s_device, _updateFinishedSemaphore, null);
        _vk.DestroyDescriptorPool(s_device, _descriptorPool, null);
        _vk.DestroyDescriptorSetLayout(s_device, _descriptorSetLayout, null);

        for (int i = 0; i < _vertexBuffers.Length; i++)
        {
            _vertexBuffers[i].Destroy(s_device);
        }

        for (int i = 0; i < _stagingBuffers.Count; i++)
        {
            _stagingBuffers[i].Destroy(s_device);
        }

        for (int i = 0; i < _indexBuffers.Length; i++)
        {
            _indexBuffers[i].Destroy(s_device);
        }

        for (int i = 0; i < _descriptorSets.Count; i++)
        {
            _descriptorSets[i].UniformBuffer.Destroy(s_device);
        }

        _clippingManager.Dispose();
    }

    /// <summary>
    /// モデルを描画する実際の処理
    /// </summary>
    protected unsafe override void DoDrawModel()
    {
        //------------ クリッピングマスク・バッファ前処理方式の場合 ------------
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo
        };
        _vk.BeginCommandBuffer(_updateCommandBuffer, ref beginInfo);
        _vk.BeginCommandBuffer(_drawCommandBuffer, ref beginInfo);

        if (_clippingManager != null)
        {
            // サイズが違う場合はここで作成しなおし
            for (int i = 0; i < _clippingManager.RenderTextureCount; ++i)
            {
                if (_offscreenFrameBuffers[i].BufferWidth != _clippingManager.ClippingMaskBufferSize.X
                    || _offscreenFrameBuffers[i].BufferHeight != _clippingManager.ClippingMaskBufferSize.Y)
                {
                    _offscreenFrameBuffers[i].DestroyOffscreenSurface(s_device);
                    _offscreenFrameBuffers[i].CreateOffscreenSurface(
                        s_device, s_physicalDevice,
                        (int)_clippingManager.ClippingMaskBufferSize.X,
                        (int)_clippingManager.ClippingMaskBufferSize.Y,
                        s_imageFormat, s_depthFormat
                    );
                }
            }
            if (UseHighPrecisionMask)
            {
                _clippingManager.SetupMatrixForHighPrecision(Model, false);
            }
            else
            {
                _clippingManager.SetupClippingContext(Model, _drawCommandBuffer, _updateCommandBuffer, this);
            }
        }
        SubmitCommand(_updateCommandBuffer, _updateFinishedSemaphore);
        SubmitCommand(_drawCommandBuffer, null, _updateFinishedSemaphore);

        // スワップチェーンを再作成した際に深度バッファのサイズを更新する
        if (_depthImage.Width != s_swapchainExtent.Width || _depthImage.Height != s_swapchainExtent.Height)
        {
            _depthImage.Destroy(s_device);
            CreateDepthBuffer();
        }

        var drawableCount = Model.GetDrawableCount();
        var renderOrder = Model.GetDrawableRenderOrders();
        // インデックスを描画順でソート
        for (int i = 0; i < drawableCount; ++i)
        {
            int order = renderOrder[i];
            _sortedDrawableIndexList[order] = i;
        }

        //描画
        _vk.BeginCommandBuffer(_updateCommandBuffer, &beginInfo);
        _vk.BeginCommandBuffer(_drawCommandBuffer, &beginInfo);
        BeginRendering(_drawCommandBuffer, false);

        for (int i = 0; i < drawableCount; ++i)
        {
            int drawableIndex = _sortedDrawableIndexList[i];
            // Drawableが表示状態でなければ処理をパスする
            if (!Model.GetDrawableDynamicFlagIsVisible(drawableIndex))
            {
                continue;
            }

            // クリッピングマスクをセットする
            var clipContext = (_clippingManager != null) ? _clippingManager.ClippingContextListForDraw[drawableIndex]
                as CubismClippingContext_Vulkan : null;

            if (clipContext != null && UseHighPrecisionMask) // マスクを書く必要がある
            {
                if (clipContext.IsUsing) // 書くことになっていた
                {
                    // 一旦オフスクリーン描画に移る
                    EndRendering(_drawCommandBuffer);

                    // 描画順を考慮して今までに積んだコマンドを実行する
                    SubmitCommand(_updateCommandBuffer, _updateFinishedSemaphore);
                    SubmitCommand(_drawCommandBuffer, null, _updateFinishedSemaphore);
                    _vk.BeginCommandBuffer(_updateCommandBuffer, &beginInfo);
                    _vk.BeginCommandBuffer(_drawCommandBuffer, &beginInfo);

                    CubismOffscreenSurface_Vulkan currentHighPrecisionMaskColorBuffer = _offscreenFrameBuffers[clipContext.BufferIndex];
                    currentHighPrecisionMaskColorBuffer.BeginDraw(_drawCommandBuffer, 1.0f, 1.0f, 1.0f, 1.0f);

                    // 生成したFrameBufferと同じサイズでビューポートを設定
                    var viewport1 = GetViewport(_clippingManager!.ClippingMaskBufferSize.X, _clippingManager.ClippingMaskBufferSize.Y,
                        0.0f, 1.0f);
                    _vk.CmdSetViewport(_drawCommandBuffer, 0, 1, ref viewport1);

                    var rect1 = GetScissor(0.0f, 0.0f, _clippingManager.ClippingMaskBufferSize.X, _clippingManager.ClippingMaskBufferSize.Y);
                    _vk.CmdSetScissor(_drawCommandBuffer, 0, 1, ref rect1);

                    int clipDrawCount = clipContext.ClippingIdCount;
                    for (int ctx = 0; ctx < clipDrawCount; ctx++)
                    {
                        int clipDrawIndex = clipContext.ClippingIdList[ctx];

                        // 頂点情報が更新されておらず、信頼性がない場合は描画をパスする
                        if (!Model.GetDrawableDynamicFlagVertexPositionsDidChange(clipDrawIndex))
                        {
                            continue;
                        }

                        IsCulling = Model.GetDrawableCulling(clipDrawIndex);

                        ClippingContextBufferForMask = clipContext;
                        DrawMeshVulkan(Model, clipDrawIndex, _drawCommandBuffer, _updateCommandBuffer);
                    }
                    // --- 後処理 ---
                    currentHighPrecisionMaskColorBuffer.EndDraw(_drawCommandBuffer); // オフスクリーン描画終了
                    ClippingContextBufferForMask = null;
                    SubmitCommand(_updateCommandBuffer, _updateFinishedSemaphore);
                    SubmitCommand(_drawCommandBuffer, null, _updateFinishedSemaphore);
                    _vk.BeginCommandBuffer(_updateCommandBuffer, &beginInfo);
                    _vk.BeginCommandBuffer(_drawCommandBuffer, &beginInfo);

                    // 描画再開
                    BeginRendering(_drawCommandBuffer, true);
                }
            }

            // ビューポートを設定する
            var viewport = GetViewport(s_swapchainExtent.Width, s_swapchainExtent.Height, 0.0f, 1.0f);
            _vk.CmdSetViewport(_drawCommandBuffer, 0, 1, &viewport);

            var rect = GetScissor(0.0f, 0.0f, s_swapchainExtent.Width, s_swapchainExtent.Height);
            _vk.CmdSetScissor(_drawCommandBuffer, 0, 1, &rect);

            // クリッピングマスクをセットする
            ClippingContextBufferForDraw = clipContext;
            IsCulling = Model.GetDrawableCulling(drawableIndex);
            DrawMeshVulkan(Model, drawableIndex, _drawCommandBuffer, _updateCommandBuffer);
        }

        EndRendering(_drawCommandBuffer);
        SubmitCommand(_updateCommandBuffer, _updateFinishedSemaphore);
        SubmitCommand(_drawCommandBuffer, null, _updateFinishedSemaphore);
    }

    public override unsafe void DrawMesh(int textureNo, int indexCount, int vertexCount, ushort* indexArray, float* vertexArray, float* uvArray, float opacity, CubismBlendMode colorBlendMode, bool invertedMask)
    {

    }

    /// <summary>
    /// モデル描画直前のステートを保持する
    /// </summary>
    protected override void SaveProfile()
    {

    }

    /// <summary>
    /// モデル描画直前のステートを保持する
    /// </summary>
    protected override void RestoreProfile()
    {

    }
}
