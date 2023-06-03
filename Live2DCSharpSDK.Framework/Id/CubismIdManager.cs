using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Id;

public class CubismIdManager
{
    private readonly List<string> _ids = new();

    public void RegisterIds(List<string> list)
    {
        list.ForEach((item) => 
        {
            RegisterId(item);
        });
    }

    public string RegisterId(string id)
    {
        if (_ids.Contains(id))
            return id;

        _ids.Add(id);

        return id;
    }

    public string? GetId(string item)
    {
        if (_ids.Contains(item))
        {
            return item;
        }

        return null;
    }
}
