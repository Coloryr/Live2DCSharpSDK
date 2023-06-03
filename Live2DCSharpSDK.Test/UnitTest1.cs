namespace Live2DCSharpSDK.Test;

using Live2DCSharpSDK.Core;
using System.Diagnostics;

public class Tests
{
    private void CoreLog(string data)
    {
        Console.WriteLine(data);
    }

    [SetUp]
    public void Setup()
    {
        CubismCore.csmSetLogFunction(CoreLog);
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
        fixed (byte* addr = data)
        {
            var version = CubismCore.csmGetMocVersion(new IntPtr(addr), data.Length);
            Assert.That(version, Is.Not.Zero);
            Console.WriteLine($"Model Version:{version}");

            var moc = CubismCore.csmReviveMocInPlace(new IntPtr(addr), data.Length);
            var mocptr = new IntPtr(moc);
            Assert.That(mocptr, Is.Not.EqualTo(IntPtr.Zero));
            Console.WriteLine($"Model Address:{mocptr}");
        }
    }
}