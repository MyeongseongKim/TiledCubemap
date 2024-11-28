using UnityEngine;
using System;


public class CubemapManager : MonoBehaviour
{
    [SerializeField] private string _remoteDir;
    [SerializeField] private string _envName;
    private string _path;

    private TiledCubemap _lowResCubemap;
    private TiledCubemap _highResCubemap;
    private const float SIZE = 100f;


    async void Start()
    {
        _path = _remoteDir + _envName;
        Debug.Log(_path);

        _lowResCubemap = new TiledCubemap(TiledCubemap.Resolution.Low, SIZE + 0.01f, -1);
        _lowResCubemap.CubemapObject.name = "Cubemap2K";
        _highResCubemap = new TiledCubemap(TiledCubemap.Resolution.High, SIZE);
        _highResCubemap.CubemapObject.name = "Cubemap8K";

        await _lowResCubemap.LoadCubemapAtOnceAsync(_path, () => {
            Debug.Log("Low-res cubemap is Loaded!");
        });
        // await _highResCubemap.LoadCubemapAtOnceAsync(_path, () => {
        //     Debug.Log("High-res cubemap is Loaded!");
        // });
        await _highResCubemap.LoadCubemapByPriorityAsync(_path, Camera.main, () => {
            Debug.Log("High-res cubemap is Loaded!");
        });
    }


    void Update()
    {
        
    }
}
