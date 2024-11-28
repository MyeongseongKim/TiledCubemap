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
    private Coroutine _loadingCoroutine;
    public Coroutine LoadingCoroutine 
    {
        get { return _loadingCoroutine; }
        set { _loadingCoroutine = value; }
    }
    private CancellationTokenSource _cts;

    private readonly Shader CUBEMAP_SHADER = Shader.Find("Unlit/TransparentTexture");

    private static readonly float PERIPHERAL_FOV = Mathf.Deg2Rad * 120f;
    private const int MAX_TASK_COUNT = 8;


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

    public IEnumerator LoadCubemapInPeripheralAsync(string path, Camera camera, Action onComplete) 
    {
        _cts = new CancellationTokenSource();

        List<GameObject> unloadedTiles = _tileObjects.ToList();
        while (unloadedTiles.Count > 0) 
        {
            if (_cts.Token.IsCancellationRequested)
            {
                Debug.LogWarning("Cubemap loading is cancelled");
                yield break;
            }

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

            if (tilesToLoad.Count > 0) 
            {
                Task loadingTask = LoadTilesAsync(tilesToLoad, path, null);
                while (!loadingTask.IsCompleted) 
                {
                    yield return null;
                }
                unloadedTiles = unloadedTiles.Except(tilesToLoad).ToList();
            }

            yield return null;
        }

        onComplete?.Invoke();
    }

    public IEnumerator LoadCubemapByPriorityAsync(string path, Camera camera, Action onComplete) 
    {
        _cts = new CancellationTokenSource();

        List<GameObject> unloadedTiles = _tileObjects.ToList();
        while (unloadedTiles.Count > 0) 
        {
            if (_cts.Token.IsCancellationRequested)
            {
                Debug.LogWarning("Cubemap loading is cancelled");
                yield break;
            }

            SortTiles(unloadedTiles, camera);
            var tile = unloadedTiles[0];
            string url = $"{path}_{tile.name}.jpg";

            Task loadingTask = LoadTilesAsync(new List<GameObject>{tile}, path, null);
            while (!loadingTask.IsCompleted) 
            {
                yield return null;
            }

            unloadedTiles.Remove(tile);
        }

        onComplete?.Invoke();
    }

    public IEnumerator LoadCubemapAtOnceAsync(string path, Action onComplete) 
    {
        _cts = new CancellationTokenSource();
        
        Task loadingTask = LoadTilesAsync(_tileObjects, path);
        while (!loadingTask.IsCompleted) 
        {
            if (_cts.Token.IsCancellationRequested)
            {
                Debug.LogWarning("Cubemap loading is cancelled");
                yield break;
            }

            yield return null;
        }

        onComplete?.Invoke();
    }


    public async Task LoadTilesAsync(IEnumerable<GameObject> tiles, string path, Action onComplete = null)
    {
        List<Task> loadTasks = new List<Task>();
        foreach (var tile in tiles)
        {
            if (_cts.Token.IsCancellationRequested)
            {
                Debug.LogWarning("Texture loading is cancelled");
                return;
            }

            string url = $"{path}_{tile.name}.jpg";
            loadTasks.Add(GetTextureFromWebRequestAsync(url, _cts,
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

        onComplete?.Invoke();
    }

    public static async Task<Texture2D> GetTextureFromWebRequestAsync(string url, CancellationTokenSource cts = null, Action<Texture2D> onComplete = null, Action onError = null) 
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            var response = request.SendWebRequest();
            while (!response.isDone)
            {
                if (cts.Token.IsCancellationRequested)
                {
                    Debug.LogWarning("Texture loading cancelled");
                    return null;
                }
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Error: " + request.error);
                onError?.Invoke();
                return null;
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                texture.wrapMode = TextureWrapMode.Clamp;
                onComplete?.Invoke(texture);
                return texture;
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

    private float GetTileWeight(GameObject tile, Camera camera) 
    {
        Vector3 look = camera.transform.forward;
        Vector3 pseudoNormal = (tile.transform.position - camera.transform.position).normalized;
        float weight = Vector3.Dot(look, pseudoNormal);
        return weight;
    }
}
