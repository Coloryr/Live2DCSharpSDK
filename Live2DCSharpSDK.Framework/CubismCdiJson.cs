using Newtonsoft.Json;

namespace Live2DCSharpSDK.Framework;

public record ModelObj
{
    public record Parameter
    {
        public string Id { get; set; }
        public string GroupId { get; set; }
        public string Name { get; set; }
    }

    public record Part
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public List<Parameter> Parameters { get; set; }
    public List<Parameter> ParameterGroups { get; set; }
    public List<Part> Parts { get; set; }
}

public class CubismCdiJson
{
    public const string Version = "Version";
    public const string Parameters = "Parameters";
    public const string ParameterGroups = "ParameterGroups";
    public const string Parts = "Parts";
    public const string Id = "Id";
    public const string GroupId = "GroupId";
    public const string Name = "Name";

    public ModelObj Obj { get; }

    public CubismCdiJson(string buffer)
    {
        Obj = JsonConvert.DeserializeObject<ModelObj>(buffer)!;

        if (!IsValid())
        {
            CubismLog.CubismLogError("[CubismJsonHolder] Invalid Json document.");
        }
    }

    // パラメータについて
    public int GetParametersCount()
    {
        if (!IsExistParameters()) return 0;
        return Obj.Parameters.Count;
    }

    public string GetParametersId(int index)
    {
        return Obj.Parameters[index].Id;
    }

    public string GetParametersGroupId(int index)
    {
        return Obj.Parameters[index].GroupId;
    }

    public string GetParametersName(int index)
    {
        return Obj.Parameters[index].Name;
    }

    // パラメータグループについて
    public int GetParameterGroupsCount()
    {
        if (!IsExistParameterGroups()) return 0;
        return Obj.ParameterGroups.Count;
    }

    public string GetParameterGroupsId(int index)
    {
        return Obj.ParameterGroups[index].Id;
    }

    public string GetParameterGroupsGroupId(int index)
    {
        return Obj.ParameterGroups[index].GroupId;
    }

    public string GetParameterGroupsName(int index)
    {
        return Obj.ParameterGroups[index].Name;
    }

    // パーツについて
    public int GetPartsCount()
    {
        if (!IsExistParts()) return 0;
        return Obj.Parts.Count;
    }

    public string GetPartsId(int index)
    {
        return Obj.Parts[index].Id;
    }

    public string GetPartsName(int index)
    {
        return Obj.Parts[index].Name;
    }

    /// <summary>
    /// CubismJsonの有効性チェック
    /// </summary>
    /// <returns>true  -> Jsonファイルが正常に読み込めた
    /// false -> Jsonファイルが読み込めなかった。もしくは、存在しない</returns>
    public bool IsValid()
    {
        return Obj != null;
    }

    /// <summary>
    /// パラメータのキーが存在するかどうかを確認する
    /// </summary>
    /// <returns>true  -> キーが存在する
    /// false -> キーが存在しない</returns>
    private bool IsExistParameters()
    {
        return Obj.Parameters != null;
    }

    /// <summary>
    /// パラメータグループのキーが存在するかどうかを確認する
    /// </summary>
    /// <returns>true  -> キーが存在する
    /// false -> キーが存在しない</returns>
    private bool IsExistParameterGroups()
    {
        return Obj.ParameterGroups != null;
    }

    /// <summary>
    /// パーツのキーが存在するかどうかを確認する
    /// </summary>
    /// <returns>true  -> キーが存在する
    /// false -> キーが存在しない</returns>
    private bool IsExistParts()
    {
        return Obj.Parts != null;
    }
}
