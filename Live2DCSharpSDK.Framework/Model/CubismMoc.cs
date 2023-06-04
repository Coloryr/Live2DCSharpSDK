using Live2DCSharpSDK.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Model;

using csmVersion = UInt32;
using csmFlags = Byte;
using csmMocVersion = UInt32;
using csmParameterType = Int32;

/// <summary>
/// Mocデータの管理を行うクラス。
/// </summary>
public unsafe class CubismMoc : IDisposable
{
    /// <summary>
    /// Mocデータ
    /// </summary>
    private csmMoc* _moc;
    /// <summary>
    /// Mocデータから作られたモデルの個数
    /// </summary>
    private int _modelCount;
    /// <summary>
    /// 読み込んだモデルの.moc3 Version
    /// </summary>
    private csmMocVersion _mocVersion;

    /// <summary>
    /// バッファからMocファイルを読み取り、Mocデータを作成する。
    /// </summary>
    /// <param name="mocBytes"> Mocファイルのバッファ</param>
    /// <param name="shouldCheckMocConsistency">MOCの整合性チェックフラグ(初期値 : false)</param>
    /// <returns></returns>
    public static CubismMoc? Create(byte[] mocBytes, bool shouldCheckMocConsistency = false)
    {
        CubismMoc? cubismMoc = null;

        IntPtr alignedBuffer = CubismFramework.AllocateAligned(mocBytes.Length, csmEnum.csmAlignofMoc);
        Marshal.Copy(mocBytes, 0, alignedBuffer, mocBytes.Length);

        if (shouldCheckMocConsistency)
        {
            // .moc3の整合性を確認
            bool consistency = HasMocConsistency(alignedBuffer, mocBytes.Length);
            if (!consistency)
            {
                CubismFramework.DeallocateAligned(alignedBuffer);

                // 整合性が確認できなければ処理しない
                CubismLog.CubismLogError("Inconsistent MOC3.");
                return cubismMoc;
            }
        }

        var moc = CubismCore.csmReviveMocInPlace(alignedBuffer, mocBytes.Length);
        csmMocVersion version = CubismCore.csmGetMocVersion(alignedBuffer, mocBytes.Length);

        if (new IntPtr(moc) != IntPtr.Zero)
        {
            cubismMoc = new CubismMoc(moc);
            cubismMoc._mocVersion = version;
        }

        return cubismMoc;
    }

    /// <summary>
    /// 最新の.moc3 Versionを取得する。
    /// </summary>
    /// <returns></returns>
    public static csmMocVersion GetLatestMocVersion()
    {
        return CubismCore.csmGetLatestMocVersion();
    }

    /// <summary>
    /// Checks consistency of a moc.
    /// </summary>
    /// <param name="address">Address of unrevived moc. The address must be aligned to 'csmAlignofMoc'.</param>
    /// <param name="size">Size of moc (in bytes).</param>
    /// <returns>'1' if Moc is valid; '0' otherwise.</returns>
    public static bool HasMocConsistency(IntPtr address, int size)
    {
        csmParameterType isConsistent = CubismCore.csmHasMocConsistency(address, size);
        return isConsistent != 0 ? true : false;
    }

    /// <summary>
    /// Checks consistency of a moc.
    /// </summary>
    /// <param name="mocBytes">Mocファイルのバッファ</param>
    /// <param name="size">バッファのサイズ</param>
    /// <returns>'true' if Moc is valid; 'false' otherwise.</returns>
    public static bool HasMocConsistencyFromUnrevivedMoc(byte[] data)
    {
        IntPtr alignedBuffer = CubismFramework.AllocateAligned(data.Length, csmEnum.csmAlignofMoc);
        Marshal.Copy(data, 0, alignedBuffer, data.Length);

        bool consistency = HasMocConsistency(alignedBuffer, data.Length);

        CubismFramework.DeallocateAligned(alignedBuffer);

        return consistency;
    }

    public CubismMoc(csmMoc* moc)
    {
        _moc = moc;
    }

    /// <summary>
    /// 読み込んだモデルの.moc3 Versionを取得する。
    /// </summary>
    /// <returns>読み込んだモデルの.moc3 Version</returns>
    public csmMocVersion GetMocVersion()
    {
        return _mocVersion;
    }

    /// <summary>
    /// モデルを削除する。
    /// </summary>
    /// <param name="model">対象のモデル</param>
    public void DeleteModel(CubismModel model)
    {
        model.Dispose();
        --_modelCount;
    }

    /// <summary>
    /// モデルを作成する。
    /// </summary>
    /// <returns>Mocデータから作成されたモデル</returns>
    public CubismModel CreateModel()
    {
        CubismModel cubismModel = null;
        int modelSize = CubismCore.csmGetSizeofModel(_moc);
        IntPtr modelMemory = CubismFramework.AllocateAligned(modelSize, csmEnum.csmAlignofModel);

        var model = CubismCore.csmInitializeModelInPlace(_moc, modelMemory, modelSize);

        if (new IntPtr(model) != IntPtr.Zero)
        {
            cubismModel = new CubismModel(model);
            cubismModel.Initialize();

            ++_modelCount;
        }

        return cubismModel;
    }

    /// <summary>
    /// デストラクタ。
    /// </summary>
    public void Dispose()
    {
        CubismFramework.DeallocateAligned(new IntPtr(_moc));
    }
}
