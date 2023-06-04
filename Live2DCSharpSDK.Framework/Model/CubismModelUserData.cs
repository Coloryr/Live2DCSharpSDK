using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Model;

/// <summary>
/// Jsonから読み込んだユーザデータを記録しておくための構造体
/// </summary>
public record CubismModelUserDataNode
{
    /// <summary>
    /// ユーザデータターゲットタイプ
    /// </summary>
    public string TargetType;
    /// <summary>
    /// ユーザデータターゲットのID
    /// </summary>
    public string TargetId;
    /// <summary>
    /// ユーザデータ
    /// </summary>
    public string Value;          
}

public record CubismModelUserDataObj
{
    public record MetaObj
    { 
        public int UserDataCount { get; set; }
        public int TotalUserDataSize { get; set; }
    }

    public record UserDataObj
    { 
        public string Target { get; set; }
        public string Id { get; set; }
        public string Value { get; set; }
    }

    public MetaObj Meta { get; set; }
    public List<UserDataObj> UserData { get; set; }
}

/// <summary>
/// ユーザデータをロード、管理、検索インターフェイス、解放までを行う。
/// </summary>
public class CubismModelUserData
{
    public const string ArtMesh = "ArtMesh";
    public const string Meta = "Meta";
    public const string UserDataCount = "UserDataCount";
    public const string TotalUserDataSize = "TotalUserDataSize";
    public const string UserData = "UserData";
    public const string Target = "Target";
    public const string Id = "Id";
    public const string Value = "Value";

    /// <summary>
    /// ユーザデータ構造体配列
    /// </summary>
    private readonly List<CubismModelUserDataNode> _userDataNodes = new();
    /// <summary>
    /// 閲覧リスト保持
    /// </summary>
    private readonly List<CubismModelUserDataNode> _artMeshUserDataNodes = new();

    private CubismModelUserDataObj Obj;

    /// <summary>
    /// userdata3.jsonをパースする。
    /// </summary>
    /// <param name="data">userdata3.jsonが読み込まれいるバッファ</param>
    public CubismModelUserData(string data)
    {
        Obj = JsonConvert.DeserializeObject<CubismModelUserDataObj>(data)!;

        string typeOfArtMesh = CubismFramework.GetIdManager().GetId(ArtMesh);

        int nodeCount = Obj.Meta.UserDataCount;

        for (int i = 0; i < nodeCount; i++)
        {
            var node = Obj.UserData[i];
            CubismModelUserDataNode addNode = new()
            {
                TargetId = CubismFramework.GetIdManager().GetId(node.Id),
                TargetType = CubismFramework.GetIdManager().GetId(node.Target),
                Value = CubismFramework.GetIdManager().GetId(node.Value)
            };
            _userDataNodes.Add(addNode);

            if (addNode.TargetType == typeOfArtMesh)
            {
                _artMeshUserDataNodes.Add(addNode);
            }
        }
    }

    public List<CubismModelUserDataNode> GetArtMeshUserDatas()
    {
        return _artMeshUserDataNodes;
    }
}
