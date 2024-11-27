using UnityEngine;
using System;


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
                    tile.GetComponent<MeshRenderer>().material = new Material(CUBEMAP_SHADER);
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
}
