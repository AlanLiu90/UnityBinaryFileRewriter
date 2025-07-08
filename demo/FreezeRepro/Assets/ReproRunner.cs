using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ReproRunner : MonoBehaviour
{
    public Text timer;
    public GameObject failIndicator;

    private DateTime _startTime;

    private bool _started;
    private int _loadCount;

    public void Start()
    {
        Application.targetFrameRate = 120;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.runInBackground = true;
    }

    public void StartRepro()
    {
        _started = true;
        _startTime = DateTime.UtcNow;
        StartCoroutine(DoThings());
    }

    private IEnumerator DoThings()
    {
        while (true)
        {
            _loadCount++;
            TimeSpan timeSinceStarted = DateTime.UtcNow - _startTime;
            timer.text = timeSinceStarted.ToString(@"hh\:mm\:ss");
            
            // it doesn't have to be unloaded and loaded the same frame like here, just in quick succession.
            for (int i = 0; i < 30; i++)
            {
                UnloadBundle();
                LoadBundle();
            }
            yield return null;
        }
    }

    public void Update()
    {
        // if (_started)
        // {
        //     _loadCount++;
        //     counter.SetText(_loadCount.ToString());
        //     TimeSpan timeSinceStarted = DateTime.Now - _startTime;
        //     timer.SetText(timeSinceStarted.ToString(@"hh\:mm\:ss"));
        //     
        //     // it doesn't have to be unloaded and loaded the same frame like here, just in quick succession.
        //     for (int i = 0; i < 3; i++)
        //     {
        //         UnloadBundle();
        //         LoadBundle();
        //     }
        // }
    }

    public string assetName = "ui_icon_ability_LivingLightning";
    public string bundleName = "ability_icon";

    private AssetBundle loadedBundle;
    
    public void LoadBundle()
    {
        loadedBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, bundleName));

        if (loadedBundle == null)
        {
            Debug.LogError("Failed to load bundle");
            failIndicator.SetActive(true);
            return;
        }
        
        Debug.Log("Bundle loaded");

        var allAssets = loadedBundle.LoadAllAssets();
        // Sprite asset = loadedBundle.LoadAsset<Sprite>(assetName);

        // if (asset == null)
        // {
        //      failIndicator.SetActive(true);
        //     Debug.LogError("Failed to load asset");
        //     return;
        // }
        Debug.Log($"{allAssets.Length} assets loaded");
        if (allAssets == null || allAssets.Length == 0)
        {
            failIndicator.SetActive(true);
        }
    }

    public void UnloadBundle()
    {
        if (loadedBundle == null)
        {
            return;
        }

        loadedBundle.Unload(true);
        Debug.Log("Bundle unloaded");
    }
}
