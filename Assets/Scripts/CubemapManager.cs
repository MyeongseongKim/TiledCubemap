using UnityEngine;
using UnityEngine.InputSystem;
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
            for (int i = 0; i < _lowResCubemap.TileObjects.Length; i++) 
            {
                _lowResCubemap.TileObjects[i].GetComponent<MeshRenderer>().material.SetColor("_Color", Color.grey);
            }
            Debug.Log("Low-res cubemap is Loaded!");
        });
        // await _highResCubemap.LoadCubemapAtOnceAsync(_path, () => {
        //     Debug.Log("High-res cubemap is Loaded!");
        // });
        // await _highResCubemap.LoadCubemapByPriorityAsync(_path, Camera.main, () => {
        //     Debug.Log("High-res cubemap is Loaded!");
        // });
        // _highResCubemap.LoadingCoroutine = StartCoroutine(
        //     _highResCubemap.LoadCubemapInPeripheralAsync(_path, Camera.main, () => {
        //         Debug.Log("High-res cubemap is Loaded!");
        //     })
        // );
    }


    void Update()
    {
        
    }


    public async void LoadHighResCubemapAtOnce(InputAction.CallbackContext context) 
    {
        if (!context.performed) {
            return;   
        }

        if (_highResCubemap.CubemapObject.activeSelf)
        {
            return;
        }
        await _highResCubemap.LoadCubemapAtOnceAsync(_path, () => {
            Debug.Log("High-res cubemap is Loaded!");
        });
    }

    public async void LoadHighResCubemapByPriority(InputAction.CallbackContext context) 
    {
        if (!context.performed) {
            return;   
        }

        if (_highResCubemap.CubemapObject.activeSelf)
        {
            return;
        }
        await _highResCubemap.LoadCubemapByPriorityAsync(_path, Camera.main, () => {
            Debug.Log("High-res cubemap is Loaded!");
        });
    }

    public void LoadHighResCubemapInPeripheral(InputAction.CallbackContext context) 
    {
        if (!context.performed) {
            return;   
        }

        if (_highResCubemap.CubemapObject.activeSelf)
        {
            return;
        }
        _highResCubemap.LoadingCoroutine = StartCoroutine(
            _highResCubemap.LoadCubemapInPeripheralAsync(_path, Camera.main, () => {
                Debug.Log("High-res cubemap is Loaded!");
            })
        );
    }

    public void ClearHighResCubemap(InputAction.CallbackContext context) 
    {
        if (!context.performed) {
            return;   
        }

        if (!_highResCubemap.CubemapObject.activeSelf)
        {
            return;
        }
        _highResCubemap.CancelLoading();
        if (_highResCubemap.LoadingCoroutine != null)
            StopCoroutine(_highResCubemap.LoadingCoroutine);
        _highResCubemap.Clear();
        Debug.Log("High-res cubemap is deleted!");
    }

}
