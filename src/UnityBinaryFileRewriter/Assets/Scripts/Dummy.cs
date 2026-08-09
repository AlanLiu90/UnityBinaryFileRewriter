using System;
using UnityEngine;

[Serializable]
public class Base
{
    public int m_Data = 1;
}

[Serializable]
public class Apple : Base
{
    public string m_Description = "Ripe";
}

public class Dummy : MonoBehaviour
{
    public string AssetBundleName;

    [SerializeReference]
    public Base Object;

    public void Func1()
    {
        var bundle = AssetBundle.LoadFromFile(AssetBundleName);
        bundle.LoadAllAssets<TextAsset>();
    }
}
