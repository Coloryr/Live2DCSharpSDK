namespace Live2DCSharpSDK.Test;

using Live2DCSharpSDK.App;
using Live2DCSharpSDK.Core;
using Live2DCSharpSDK.Framework;
using Live2DCSharpSDK.Framework.Model;
using System.Runtime.InteropServices;

public class Tests
{
    private void CoreLog(string data)
    {
        Console.WriteLine(data);
    }

    [SetUp]
    public void Setup()
    {

    }

    [Test]
    public void GetVersion()
    {
        var version = CubismCore.csmGetVersion();

        uint major = (version & 0xFF000000) >> 24;
        uint minor = (version & 0x00FF0000) >> 16;
        uint patch = version & 0x0000FFFF;
        uint vesionNumber = version;

        Console.WriteLine($"Live2D Cubism Core version: {major:##}.{minor:#}.{patch:####} ({vesionNumber})");

        Console.WriteLine($"Moc Version:{CubismCore.csmGetLatestMocVersion()}");
        Assert.Pass();
    }

    [Test]
    public unsafe void LoadModel()
    {
        var data = File.ReadAllBytes("E:\\code\\Live2DCSharpSDK\\Resources\\Haru\\Haru.moc3");
        var all = new LAppAllocator();
        var ptr = all.AllocateAligned(data.Length, csmEnum.csmAlignofMoc);
        Marshal.Copy(data, 0, ptr, data.Length);
        var version = CubismCore.csmGetMocVersion(ptr, data.Length);
        Assert.That(version, Is.Not.Zero);
        Console.WriteLine($"Model Version:{version}");

        var moc = CubismCore.csmReviveMocInPlace(ptr, data.Length);
        var mocptr = new IntPtr(moc);
        Assert.That(mocptr, Is.Not.EqualTo(IntPtr.Zero));
        Console.WriteLine($"Model Address:{mocptr}");

        all.DeallocateAligned(ptr);
    }

    [Test]
    public void FrameworkInit()
    {
        CubismFramework.StartUp(new LAppAllocator(), new Option()
        {
            LogFunction = CoreLog,
            LoggingLevel = Option.LogLevel.LogLevel_Verbose
        });
        Assert.That(CubismFramework.IsStarted(), Is.True);

        CubismFramework.Initialize();
        Assert.That(CubismFramework.IsInitialized(), Is.True);
    }

    [Test]
    public unsafe void CreateMoc()
    {
        FrameworkInit();

        var data = File.ReadAllBytes("E:\\code\\Live2DCSharpSDK\\Resources\\Haru\\Haru.moc3");
        var moc = CubismMoc.Create(data);
        Assert.That(moc, Is.Not.Null);

        var model = moc.CreateModel();
        Assert.That(model, Is.Not.Null);

        model.Initialize();
        Assert.That(new IntPtr(model.GetModel()), Is.Not.EqualTo(IntPtr.Zero));
    }
}