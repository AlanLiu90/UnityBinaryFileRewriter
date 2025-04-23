#define NO_COUNT

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class InstantiateByAsset : MonoBehaviour
{
    public string text;

#if !NO_COUNT
    public int count;
#endif

    void Start()
    {
#if !NO_COUNT
        Debug.Log($"[InstantiateByAsset] text:{text}, 这个脚本通过挂载到资源的方式实例化, count: {count}");
#else
        Debug.Log($"[InstantiateByAsset] text:{text}, 这个脚本通过挂载到资源的方式实例化");
#endif
    }
}
