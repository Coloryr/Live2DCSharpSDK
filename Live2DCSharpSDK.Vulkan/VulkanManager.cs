using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Live2DCSharpSDK.Framework;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Live2DCSharpSDK.Vulkan;

public class VulkanManager(LAppDelegateVulkan lapp, Vk vk, VulkanApi api)
{
    public static readonly string[] deviceExtensions =
    [
        "VK_KHR_swapchain",
        "VK_EXT_extended_dynamic_state"
    ];

    public static readonly string[] validationLayers = ["VK_LAYER_KHRONOS_validation"];

    /// <summary>
    /// 検証レイヤーを有効にするか
    /// </summary>
    public const bool EnableValidationLayers = true;

    /// <summary>
    /// イメージフォーマット
    /// </summary>
    public const Format SurfaceFormat = Format.R8G8B8A8Unorm;
    private KhrSwapchain _khrs;

    public QueueFamilyIndices Indices = new();

    /// <summary>
    /// ライブラリを初期化するインスタンス
    /// </summary>
    private Instance _instance;
    private KhrSurface khrSurface;
    /// <summary>
    /// ウィンドウシステムに描画ために必要なサーフェス
    /// </summary>
    public SurfaceKHR Surface { get; private set; }
    /// <summary>
    /// 物理デバイス
    /// </summary>
    public PhysicalDevice PhysicalDevice { get; private set; }
    /// <summary>
    /// 論理デバイス
    /// </summary>
    public Device Device => _device;

    private Device _device;

    /// <summary>
    /// 描画コマンドを積むキュー
    /// </summary>
    public Queue GraphicQueue => _graphicQueue;
    private Queue _graphicQueue;

    /// <summary>
    /// 表示コマンドに使用するキュー
    /// </summary>
    public Queue PresentQueue => _presentQueue;
    private Queue _presentQueue;
    /// <summary>
    /// コマンドバッファの作成に必要なコマンドプール
    /// </summary>
    public CommandPool CommandPool => _commandPool;

    private CommandPool _commandPool;
    /// <summary>
    /// セマフォ
    /// </summary>
    private Semaphore _imageAvailableSemaphore;
    /// <summary>
    /// スワップチェーンの管理を行うスワップチェーンマネージャー
    /// </summary>
    public SwapchainManager SwapchainManager { get; private set; }
    /// <summary>
    /// ウィンドウサイズが変更されたかのフラグ
    /// </summary>
    public bool IsSwapchainInvalid { get; set; }
    /// <summary>
    /// デバッグメッセージを出力するオブジェクト
    /// </summary>
    private DebugUtilsMessengerEXT _debugMessenger;
    /// <summary>
    ///  現在のイメージインデックス
    /// </summary>
    private uint _imageIndex = 0;
    /// <summary>
    /// 深度フォーマット
    /// </summary>
    public Format DepthFormat { get; private set; }
    /// <summary>
    /// フレームバッファのサイズが変わったか
    /// </summary>
    public bool FramebufferResized { get; set; }

    private ExtDebugUtils? debugUtils;
    private DebugUtilsMessengerEXT debugMessenger;

    /// <summary>
    /// 検証レイヤーのサポートを確認する
    /// </summary>
    /// <returns></returns>
    public unsafe bool CheckValidationLayerSupport()
    {
        uint layerCount = 0;
        vk.EnumerateInstanceLayerProperties(ref layerCount, null);

        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* availableLayersPtr = availableLayers)
        {
            vk.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
        }

        var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();

        return validationLayers.All(availableLayerNames.Contains);
    }

    /// <summary>
    /// 必要な拡張機能を取得する
    /// </summary>
    /// <returns>必要な拡張の配列</returns>
    public unsafe string[] GetRequiredExtensions()
    {
        var glfwExtensions = api.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

        if (EnableValidationLayers)
        {
            return [.. extensions, ExtDebugUtils.ExtensionName];
        }

        return extensions;
    }

    private unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        CubismLog.Debug($"validation layer:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));

        return Vk.False;
    }

    /// <summary>
    /// デバッグメッセージを有効にする
    /// </summary>
    /// <param name="createInfo">デバッグメッセンジャーオブジェクトの情報</param>
    public unsafe void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
    }

    /// <summary>
    /// インスタンスを生成する
    /// </summary>
    public unsafe void CreateInstance()
    {
        //検証レイヤーが有効のときに使えるか確認
        if (EnableValidationLayers && !CheckValidationLayerSupport())
        {
            CubismLog.Error("validation layers requested, but not available!");
        }

        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)SilkMarshal.StringToPtr("Live2D"),
            PEngineName = (byte*)SilkMarshal.StringToPtr("Live2D"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version13
        };

        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        var extensions = GetRequiredExtensions();
        createInfo.EnabledExtensionCount = (uint)extensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);
        createInfo.EnabledLayerCount = 0;
        createInfo.PNext = null;

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);

            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.PNext = &debugCreateInfo;
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        if (vk.CreateInstance(ref createInfo, null, out _instance) != Result.Success)
        {
            throw new Exception("failed to create instance!");
        }

        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
#if DEBUG
        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
#endif
    }

    /// <summary>
    /// デバッグメッセージを有効にする
    /// </summary>
    public unsafe void SetupDebugMessenger()
    {
        if (!EnableValidationLayers) return;

        //TryGetInstanceExtension equivilant to method CreateDebugUtilsMessengerEXT from original tutorial.
        if (!vk.TryGetInstanceExtension(_instance, out debugUtils)) return;

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        if (debugUtils!.CreateDebugUtilsMessenger(_instance, in createInfo, null, out debugMessenger) != Result.Success)
        {
            throw new Exception("failed to set up debug messenger!");
        }
    }

    /// <summary>
    /// デバイスの拡張をチェックする
    /// </summary>
    /// <param name="physicalDevice">物理デバイス</param>
    /// <returns></returns>
    public unsafe bool CheckDeviceExtensionSupport(PhysicalDevice physicalDevice)
    {
        uint extentionsCount = 0;
        vk!.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, ref extentionsCount, null);

        var availableExtensions = new ExtensionProperties[extentionsCount];
        fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
        {
            vk.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, ref extentionsCount, availableExtensionsPtr);
        }

        var availableExtensionNames = availableExtensions.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();

        return deviceExtensions.All(availableExtensionNames.Contains);
    }

    /// <summary>
    /// サーフェスを作る
    /// </summary>
    public unsafe void CreateSurface()
    {
        if (!vk.TryGetInstanceExtension(_instance, out khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        Surface = api.CreateSurface<AllocationCallbacks>(_instance.ToHandle(), null).ToSurface();
    }

    /// <summary>
    /// キューファミリを見つける
    /// </summary>
    /// <param name="device">物理デバイス</param>
    public unsafe void FindQueueFamilies(PhysicalDevice device)
    {
        uint queueFamilityCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
        }

        uint i = 0;
        foreach (var queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                Indices.GraphicsFamily = (int)i;
            }

            khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, Surface, out var presentSupport);

            if (presentSupport)
            {
                Indices.PresentFamily = (int)i;
            }

            if (Indices.IsComplete())
            {
                break;
            }

            i++;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="device">デバイスが使えるか確認する</param>
    /// <returns>物理デバイス</returns>
    public bool IsDeviceSuitable(PhysicalDevice device)
    {
        FindQueueFamilies(device);
        bool extensionsSupported = CheckDeviceExtensionSupport(device);
        bool swapChainAdequate = false;
        //デバイスの拡張機能がサポートされていたら、スワップチェインをサポートしているか確認する
        if (extensionsSupported)
        {
            var swapchainSupport = SwapchainManager.QuerySwapchainSupport(khrSurface, device, Surface);
            swapChainAdequate = swapchainSupport.Formats.Length != 0 && swapchainSupport.PresentModes.Length != 0;
        }

        vk.GetPhysicalDeviceFeatures(device, out var supportedFeatures);

        return Indices.IsComplete() && swapChainAdequate && supportedFeatures.SamplerAnisotropy;
    }

    /// <summary>
    /// 物理デバイスを取得する
    /// </summary>
    public void PickPhysicalDevice()
    {
        var devices = vk.GetPhysicalDevices(_instance);

        foreach (var device in devices)
        {
            if (IsDeviceSuitable(device))
            {
                PhysicalDevice = device;
                break;
            }
        }

        if (PhysicalDevice.Handle == 0)
        {
            throw new Exception("failed to find a suitable GPU!");
        }
    }

    /// <summary>
    /// 論理デバイスを作成する
    /// </summary>
    public unsafe void CreateLogicalDevice()
    {
        FindQueueFamilies(PhysicalDevice);

        var uniqueQueueFamilies = new[] { (uint)Indices.GraphicsFamily, (uint)Indices.PresentFamily };
        uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

        using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
        var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

        float queuePriority = 1.0f;
        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            queueCreateInfos[i] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
        }

        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
            PQueueCreateInfos = queueCreateInfos,
            EnabledExtensionCount = (uint)deviceExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions)
        };

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
        }

        var dynamicRenderingF = new PhysicalDeviceDynamicRenderingFeatures
        {
            SType = StructureType.PhysicalDeviceDynamicRenderingFeaturesKhr,
            DynamicRendering = true
        };

        var dynamicStateF = new PhysicalDeviceExtendedDynamicStateFeaturesEXT
        {
            SType = StructureType.PhysicalDeviceExtendedDynamicStateFeaturesExt,
            ExtendedDynamicState = true,
            PNext = &dynamicRenderingF
        };

        var deviceFeatures2 = new PhysicalDeviceFeatures2
        {
            SType = StructureType.PhysicalDeviceFeatures2,
            PNext = &dynamicStateF
        };
        vk.GetPhysicalDeviceFeatures2(PhysicalDevice, &deviceFeatures2);
        createInfo.PNext = &deviceFeatures2;

        if (vk.CreateDevice(PhysicalDevice, in createInfo, null, out _device) != Result.Success)
        {
            throw new Exception("failed to create logical device!");
        }

        vk.GetDeviceQueue(_device, (uint)Indices.GraphicsFamily, 0, out _graphicQueue);
        vk.GetDeviceQueue(_device, (uint)Indices.PresentFamily, 0, out _presentQueue);

        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }

        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
    }

    /// <summary>
    /// 深度フォーマットを作成する
    /// </summary>
    public void ChooseSupportedDepthFormat()
    {
        Format[] depthFormats =
        [
            Format.D32SfloatS8Uint, Format.D32Sfloat,
            Format.D24UnormS8Uint,
            Format.D16UnormS8Uint, Format.D16Unorm,
        ];

        for (int i = 0; i < depthFormats.Length; i++)
        {
            vk.GetPhysicalDeviceFormatProperties(PhysicalDevice, depthFormats[i], out var formatProps);

            if ((formatProps.OptimalTilingFeatures & FormatFeatureFlags.DepthStencilAttachmentBit)
                == FormatFeatureFlags.DepthStencilAttachmentBit)
            {
                DepthFormat = depthFormats[i];
                return;
            }
        }
        CubismLog.Error("can't find depth format!");
        DepthFormat = depthFormats[0];
    }

    /// <summary>
    /// コマンドプールを作成する
    /// </summary>
    public unsafe void CreateCommandPool()
    {
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = (uint)Indices.GraphicsFamily
        };

        if (vk.CreateCommandPool(_device, ref poolInfo, null, out _commandPool) != Result.Success)
        {
            CubismLog.Error("failed to create graphics command pool!");
        }
    }

    /// <summary>
    /// 同期オブジェクトを作成する
    /// </summary>
    public unsafe void CreateSyncObjects()
    {
        var semaphoreInfo = new SemaphoreCreateInfo()
        {
            SType = StructureType.SemaphoreCreateInfo
        };
        vk.CreateSemaphore(_device, ref semaphoreInfo, null, out _imageAvailableSemaphore);
    }

    /// <summary>
    /// 初期化する
    /// </summary>
    public void Initialize()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();

        if (_khrs is null)
        {
            if (!vk.TryGetDeviceExtension(_instance, _device, out _khrs))
            {
                CubismLog.Error("VK_KHR_swapchain extension not found.");
            }
        }

        ChooseSupportedDepthFormat();
        SwapchainManager = new SwapchainManager(_khrs, khrSurface, api, vk, PhysicalDevice, _device, Surface, Indices.GraphicsFamily,
                                                Indices.PresentFamily);
        CreateCommandPool();
        SwapchainManager.ChangeLayout(_device, _commandPool, _graphicQueue);
        CreateSyncObjects();
    }

    /// <summary>
    /// コマンドの記録を開始する
    /// </summary>
    /// <returns></returns>
    public unsafe CommandBuffer BeginSingleTimeCommands()
    {
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = _commandPool,
            CommandBufferCount = 1
        };

        vk.AllocateCommandBuffers(_device, ref allocInfo, out var commandBuffer);

        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        vk.BeginCommandBuffer(commandBuffer, ref beginInfo);

        return commandBuffer;
    }

    /// <summary>
    /// コマンドを提出する
    /// </summary>
    /// <param name="commandBuffer">コマンドバッファ</param>
    /// <param name="isFirstDraw">最初の描画コマンドか</param>
    public unsafe void SubmitCommand(CommandBuffer commandBuffer, bool isFirstDraw = false)
    {
        vk.EndCommandBuffer(commandBuffer);

        var submitInfo = new SubmitInfo
        {
            SType = StructureType
            .SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };
        var sem = _imageAvailableSemaphore;
        var waitStages = stackalloc PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit };
        if (isFirstDraw)
        {
            submitInfo.WaitSemaphoreCount = 1;
            submitInfo.PWaitSemaphores = &sem;
            submitInfo.PWaitDstStageMask = waitStages;
        }
        vk.QueueSubmit(_graphicQueue, 1, &submitInfo, default);
        // コマンドの実行終了まで待機
        vk.QueueWaitIdle(_graphicQueue);
        vk.FreeCommandBuffers(_device, _commandPool, 1, &commandBuffer);
    }

    /// <summary>
    /// 描画する
    /// </summary>
    public void UpdateDrawFrame()
    {
        Result result = _khrs.AcquireNextImage(_device, SwapchainManager.Swapchain, uint.MaxValue,
        _imageAvailableSemaphore, default,
        ref _imageIndex);
        if (result == Result.ErrorOutOfDateKhr)
        {
            IsSwapchainInvalid = true;
        }
        else if (result != Result.Success && result != Result.SuboptimalKhr)
        {
            CubismLog.Error("failed to acquire swap chain image!");
        }
    }

    /// <summary>
    /// 描画する
    /// </summary>
    public void PostDraw()
    {
        Result result = SwapchainManager.QueuePresent(_presentQueue, _imageIndex);
        if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || FramebufferResized)
        {
            FramebufferResized = false;
            IsSwapchainInvalid = true;
        }
        else if (result != Result.Success)
        {
            CubismLog.Error("failed to present swap chain image!");
        }
    }

    /// <summary>
    /// スワップチェーンを再構成する
    /// </summary>
    public void RecreateSwapchain()
    {
        vk.DeviceWaitIdle(_device);
        SwapchainManager.Cleanup(_device);
        SwapchainManager.CreateSwapchain(PhysicalDevice, _device, Surface, Indices.GraphicsFamily,
                                          Indices.PresentFamily);
        SwapchainManager.ChangeLayout(_device, _commandPool, _graphicQueue);
    }

    /// <summary>
    /// リソースを破棄する
    /// </summary>
    public unsafe void Destroy()
    {
        SwapchainManager.Cleanup(_device);

        vk.DestroySemaphore(_device, _imageAvailableSemaphore, default);

        if (EnableValidationLayers)
        {
            debugUtils!.DestroyDebugUtilsMessenger(_instance, debugMessenger, null);
        }

        vk.DestroyCommandPool(_device, _commandPool, default);
        vk.DestroyDevice(_device, null);
        khrSurface!.DestroySurface(_instance, Surface, null);
        vk.DestroyInstance(_instance, null);
    }

    /// <summary>
    /// スワップチェーンイメージを取得する
    /// </summary>
    /// <returns>スワップチェーンイメージ</returns>
    public Image GetSwapchainImage() { return SwapchainManager.Images[_imageIndex]; }

    /// <summary>
    /// スワップチェーンイメージを取得する
    /// </summary>
    /// <returns>スワップチェーンイメージ</returns>
    public ImageView GetSwapchainImageView() { return SwapchainManager.ImageViews[_imageIndex]; }

    public void Dispose()
    {
        Destroy();
    }
}
