using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


public class TiledCubemap
{
    private const int TILE_RESOLUTION = 512;
    public enum Resolution
    {
        Low = 512,
        Mid = 1024,
        High = 2048
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
    private Coroutine _loadingCoroutine;
    public Coroutine LoadingCoroutine 
    {
        get { return _loadingCoroutine; }
        set { _loadingCoroutine = value; }
    }
    private CancellationTokenSource _cts;

    private readonly Shader CUBEMAP_SHADER = Shader.Find("Custom/CubemapShader");

    private static readonly float PERIPHERAL_FOV = Mathf.Deg2Rad * 120f;
    private const int MAX_TASK_COUNT = 8;


    public TiledCubemap(Resolution res, float size, int queueOffset = 0) 
    {
        int division = (int) res / TILE_RESOLUTION;

        _cubemapObject = new GameObject();
        _cubemapObject.SetActive(false);
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
                    tile.name = $"{face}_{(int) res}_{i}_{j}";

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


    public void Clear()
    {
        foreach (var tile in _tileObjects) 
        {
            tile.SetActive(false);
            tile.GetComponent<Renderer>().material.mainTexture = null;
        }
        _cubemapObject.SetActive(false);
    }

    public void CancelLoading() 
    {
        _cts.Cancel();
    }

    public IEnumerator LoadCubemapInPeripheralAsync(string path, Camera camera, Action onCubemapLoaded) 
    {
        _cubemapObject.SetActive(true);
        _cts = new CancellationTokenSource();

        List<GameObject> unloadedTiles = _tileObjects.ToList();
        while (unloadedTiles.Count > 0) 
        {
            SortTiles(unloadedTiles, camera);
            List<GameObject> tilesToLoad = new List<GameObject>();
            
            int length = MAX_TASK_COUNT;
            if (unloadedTiles.Count < MAX_TASK_COUNT) 
            {
                length = unloadedTiles.Count;
            }

            for (int i = 0; i < length; i++) 
            {
                var tile = unloadedTiles[i];
                if (GetTileWeight(tile, camera) > Mathf.Cos(PERIPHERAL_FOV * 0.5f)) 
                {
                    tilesToLoad.Add(unloadedTiles[i]);
                }
            }

            bool isLoaded = false;
            LoadTilesAsync(tilesToLoad, path, () => {
                isLoaded = true;
            });
            while (!isLoaded)
            {
                yield return null;
            }

            unloadedTiles = unloadedTiles.Except(tilesToLoad).ToList();

            yield return null;
        }

        onCubemapLoaded?.Invoke();
    }

    public async Task LoadCubemapByPriorityAsync(string path, Camera camera, Action onCubemapLoaded) 
    {
        _cubemapObject.SetActive(true);
        _cts = new CancellationTokenSource();

        while (GetTileIndexToLoad(camera) != -1)
        {
            if (_cts.Token.IsCancellationRequested)
            {
                Debug.LogWarning("Cubemap loading is cancelled");
                return;
            }

            int index = GetTileIndexToLoad(camera);
            var tile = _tileObjects[index];
            string url = $"{path}_{tile.name}.jpg";

            await LoadTextureAsync(
                url, 
                texture => {
                    tile.GetComponent<MeshRenderer>().material.mainTexture = texture;
                    tile.SetActive(true);
                },
                () => {
                    Debug.LogWarning($"Failed to load tile {tile.name} of {_cubemapObject.name}");    
                }
            );
        }

        onCubemapLoaded?.Invoke();
    }

    public async Task LoadCubemapAtOnceAsync(string path, Action onCubemapLoaded)
    {
        _cubemapObject.SetActive(true);
        _cts = new CancellationTokenSource();

        await LoadTilesAsync(_tileObjects, path, onCubemapLoaded);
    }


    public async Task LoadTilesAsync(IEnumerable<GameObject> tiles, string path, Action onCubemapLoaded)
    {
        List<Task> loadTasks = new List<Task>();
        foreach (var tile in tiles)
        {
            if (_cts.Token.IsCancellationRequested)
            {
                Debug.LogWarning("Cubemap loading is cancelled");
                return;
            }

            string url = $"{path}_{tile.name}.jpg";
            loadTasks.Add(LoadTextureAsync(
                url, 
                texture => {
                    tile.GetComponent<MeshRenderer>().material.mainTexture = texture;
                    tile.SetActive(true);
                },
                () => {
                    Debug.LogWarning($"Failed to load tile {tile.name} of {_cubemapObject.name}");    
                }
            ));
        }
        await Task.WhenAll(loadTasks);

        onCubemapLoaded?.Invoke();
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


    private void SortTiles(List<GameObject> tiles, Camera camera) 
    {
        tiles.Sort((tileA, tileB) =>
        {
            float weightA = GetTileWeight(tileA, camera);
            float weightB = GetTileWeight(tileB, camera);

            return weightB.CompareTo(weightA);
        });
    }


    private int GetTileIndexToLoad(Camera camera) 
    {
        Vector3 look = camera.transform.forward;

        int index = -1;
        float maxWeight = -1f;
        for (int i = 0; i < _tileObjects.Length; i++) 
        {
            if (!_tileObjects[i].activeSelf) 
            {
                Vector3 pseudoNormal = (_tileObjects[i].transform.position - camera.transform.position).normalized;
                float weight = Vector3.Dot(look, pseudoNormal);

                if (index < 0 || weight > maxWeight) 
                {
                    index = i;
                    maxWeight = weight;
                }
            }
        }
        return index;
    }

    private float GetTileWeight(GameObject tile, Camera camera) 
    {
        Vector3 look = camera.transform.forward;
        Vector3 pseudoNormal = (tile.transform.position - camera.transform.position).normalized;
        float weight = Vector3.Dot(look, pseudoNormal);
        return weight;
    }
}
