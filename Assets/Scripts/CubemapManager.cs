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


    void Start()
    {
        _path = _remoteDir + _envName;
        Debug.Log(_path);

        _lowResCubemap = new TiledCubemap(TiledCubemap.DivisionType.Matrix1x1, SIZE + 0.01f, -1);
        _lowResCubemap.CubemapObject.name = "Cubemap2K";
        _highResCubemap = new TiledCubemap(TiledCubemap.DivisionType.Matrix4x4, SIZE);
        _highResCubemap.CubemapObject.name = "Cubemap8K";

        _lowResCubemap.LoadingCoroutine = StartCoroutine(
            _lowResCubemap.LoadCubemapAtOnceAsync(_path, () => {
                Debug.Log("Low-res cubemap is Loaded!");
            })
        );
        // _highResCubemap.LoadingCoroutine = StartCoroutine(
        //     _highResCubemap.LoadCubemapAtOnceAsync(_path, () => {
        //         Debug.Log("High-res cubemap is Loaded!");
        //     })
        // );
        // _highResCubemap.LoadingCoroutine = StartCoroutine(
        //     _highResCubemap.LoadCubemapByPriorityAsync(_path, Camera.main, () => {
        //         Debug.Log("High-res cubemap is Loaded!");
        //     })
        // );
        _highResCubemap.LoadingCoroutine = StartCoroutine(
            _highResCubemap.LoadCubemapInPeripheralAsync(_path, Camera.main, () => {
                Debug.Log("High-res cubemap is Loaded!");
            })
        );
    }


    void Update()
    {
        
    }
}
