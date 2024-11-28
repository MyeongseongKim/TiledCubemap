using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Threading;
using System.Threading.Tasks;


public class TiledCubemap
{
    private const int TILE_RESOLUTION = 512;
    public enum DivisionType
    {
        Matrix1x1 = 1,
        Matrix2x2 = 2,
        Matrix4x4 = 4
    }

    private GameObject _cubemapObject;
    public GameObject CubemapObject 
    {
        get { return _cubemapObject;}
    }
    private GameObject[] _tileObjects;
    public GameObject[] TileObjects 
    {
        get { return _tileObjects;}
    }
    private CancellationTokenSource _cts;

    private readonly Shader CUBEMAP_SHADER = Shader.Find("Unlit/TransparentTexture");


    public TiledCubemap(DivisionType type, float size, int queueOffset = 0) 
    {
        int division = (int) type;
        int res = division * TILE_RESOLUTION;

        _cubemapObject = new GameObject();
        _cubemapObject.transform.localScale = size * Vector3.one;

        _tileObjects = new GameObject[6 * division * division];
        float tileSize = 1f / (float) division;
        for (int face = 0; face < 6; face++)
        {
            for (int i = 0; i < division; i++) // row
            {
                for (int j = 0; j < division; j++) // column
                {
                    int index = face * division * division + i * division + j;

                    var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    tile.SetActive(false);

                    var renderer = tile.GetComponent<MeshRenderer>();
                    renderer.material = new Material(CUBEMAP_SHADER);
                    renderer.material.renderQueue += queueOffset;
                    
                    tile.transform.parent = _cubemapObject.transform;
                    tile.name = $"{face}_{res}_{i}_{j}";

                    tile.transform.localScale = Vector3.one * tileSize;
                    switch (face) 
                    {
                        case 0:
                            tile.transform.forward = _cubemapObject.transform.forward;
                            break;
                        case 1:
                            tile.transform.forward = _cubemapObject.transform.right;
                            break;
                        case 2:
                            tile.transform.forward = -_cubemapObject.transform.forward;
                            break;
                        case 3:
                            tile.transform.forward = -_cubemapObject.transform.right;
                            break;
                        case 4:
                            tile.transform.forward = _cubemapObject.transform.up;
                            break;
                        case 5:
                            tile.transform.forward = -_cubemapObject.transform.up;
                            break;
                    }
                    tile.transform.localPosition += 0.5f * tile.transform.forward;
                    tile.transform.localPosition += tileSize * (j - (division - 1) / 2f) * tile.transform.right;
                    tile.transform.localPosition += tileSize * (i - (division - 1) / 2f) * -tile.transform.up;
                
                    _tileObjects[index] = tile;
                }
            }
        }
    }


    public void CancelLoading() 
    {
        _cts.Cancel();
    }


    private async Task LoadTextureAsync(string url, Action<Texture2D> onTextureLoaded, Action onError)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            var response = request.SendWebRequest();
            while (!response.isDone)
            {
                if (_cts.Token.IsCancellationRequested)
                {
                    Debug.LogWarning("Texture loading is cancelled");
                    return;
                }
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Failed request from {url}: {request.error}");
                onError?.Invoke();
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                texture.wrapMode = TextureWrapMode.Clamp;
                onTextureLoaded?.Invoke(texture);
            }
        }
    }
}
