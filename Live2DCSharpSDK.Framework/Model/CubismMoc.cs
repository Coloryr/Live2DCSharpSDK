using Live2DCSharpSDK.Core;
using System.Runtime.InteropServices;

namespace Live2DCSharpSDK.Framework.Model;

/// <summary>
/// Mocデータの管理を行うクラス。
/// </summary>
public class CubismMoc : IDisposable
{
    /// <summary>
    /// Mocデータ
    /// </summary>
    private IntPtr _moc;
    /// <summary>
    /// 読み込んだモデルの.moc3 Version
    /// </summary>
    private uint _mocVersion;

    public CubismModel Model { get; }

    /// <summary>
    /// バッファからMocファイルを読み取り、Mocデータを作成する。
    /// </summary>
    /// <param name="mocBytes"> Mocファイルのバッファ</param>
    /// <param name="shouldCheckMocConsistency">MOCの整合性チェックフラグ(初期値 : false)</param>
    /// <returns></returns>
    public CubismMoc(byte[] mocBytes, bool shouldCheckMocConsistency = false)
    {
        IntPtr alignedBuffer = CubismFramework.AllocateAligned(mocBytes.Length, CsmEnum.csmAlignofMoc);
        Marshal.Copy(mocBytes, 0, alignedBuffer, mocBytes.Length);

        if (shouldCheckMocConsistency)
        {
            // .moc3の整合性を確認
            bool consistency = HasMocConsistency(alignedBuffer, mocBytes.Length);
            if (!consistency)
            {
                CubismFramework.DeallocateAligned(alignedBuffer);

                // 整合性が確認できなければ処理しない
                throw new Exception("Inconsistent MOC3.");
            }
        }

        var moc = CubismCore.csmReviveMocInPlace(alignedBuffer, mocBytes.Length);

        if (new IntPtr(moc) != IntPtr.Zero)
        {
            _moc = moc;
        }
        else
        {
            throw new Exception("MOC3 is null");
        }

        _mocVersion = CubismCore.csmGetMocVersion(alignedBuffer, mocBytes.Length);

        int modelSize = CubismCore.csmGetSizeofModel(_moc);
        IntPtr modelMemory = CubismFramework.AllocateAligned(modelSize, CsmEnum.csmAlignofModel);

        var model = CubismCore.csmInitializeModelInPlace(_moc, modelMemory, modelSize);

        if (model != IntPtr.Zero)
        {
            Model = new CubismModel(model);
            Model.Initialize();
        }
        else
        {
            throw new Exception("MODEL is null");
        }
    }

    /// <summary>
    /// 最新の.moc3 Versionを取得する。
    /// </summary>
    /// <returns></returns>
    public static uint GetLatestMocVersion()
    {
        return CubismCore.GetLatestMocVersion();
    }

    /// <summary>
    /// Checks consistency of a moc.
    /// </summary>
    /// <param name="address">Address of unrevived moc. The address must be aligned to 'csmAlignofMoc'.</param>
    /// <param name="size">Size of moc (in bytes).</param>
    /// <returns>'1' if Moc is valid; '0' otherwise.</returns>
    public static bool HasMocConsistency(IntPtr address, int size)
    {
        return CubismCore.csmHasMocConsistency(address, size) != 0;
    }

    /// <summary>
    /// Checks consistency of a moc.
    /// </summary>
    /// <param name="mocBytes">Mocファイルのバッファ</param>
    /// <param name="size">バッファのサイズ</param>
    /// <returns>'true' if Moc is valid; 'false' otherwise.</returns>
    public static bool HasMocConsistencyFromUnrevivedMoc(byte[] data)
    {
        IntPtr alignedBuffer = CubismFramework.AllocateAligned(data.Length, CsmEnum.csmAlignofMoc);
        Marshal.Copy(data, 0, alignedBuffer, data.Length);

        bool consistency = HasMocConsistency(alignedBuffer, data.Length);

        CubismFramework.DeallocateAligned(alignedBuffer);

        return consistency;
    }

    /// <summary>
    /// 読み込んだモデルの.moc3 Versionを取得する。
    /// </summary>
    /// <returns>読み込んだモデルの.moc3 Version</returns>
    public uint GetMocVersion()
    {
        return _mocVersion;
    }

    /// <summary>
    /// デストラクタ。
    /// </summary>
    public void Dispose()
    {
        Model.Dispose();
        CubismFramework.DeallocateAligned(_moc);
        GC.SuppressFinalize(this);
    }
}
