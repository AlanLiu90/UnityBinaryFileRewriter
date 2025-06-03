using UnityEngine;

public class Dummy : MonoBehaviour
{
    public string AssetBundleName;

    public void Func1()
    {
        var bundle = AssetBundle.LoadFromFile(AssetBundleName);
        bundle.LoadAllAssets<TextAsset>();
    }
}
