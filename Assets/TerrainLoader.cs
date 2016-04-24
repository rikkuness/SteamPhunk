using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // used for Sum of array
using CSML;
using TerrainStitch;

[System.Serializable]
public class TerrainTile
{
    public GameObject terrain;
    public Texture2D heightmap;
    public string url;
    public int x;
    public int y;
    public int z;

    public int worldX;
    public int worldZ;

    public TerrainTile(int tilex, int tiley, int wldz, int wldx)  
    {
        x = tilex;
        y = tiley;
        z = 5;              // Hardcoded zoom level
        worldX = wldx;
        worldZ = wldz;
    }
}

public class TerrainLoader : MonoBehaviour {
    public string baseUrl;
    public string accessToken;

    public Texture2D flatTexture;
    public Texture2D flatNormal;
    public Texture2D steepTexture;
    public Texture2D steepNormal;
    public Texture2D baseTexture;
    public Texture2D baseNormal;

    public int tileSize = 256;
    public int terrainSize = 256;
    public int terrainResolution = 2048;
    public int terrainHeight = 100;
    public float intensity = 1f;
    public float smoothing = 0.05f;

    enum Side { Left, Right, Top, Bottom }

    public Vector2 firstPosition;
    float levelSmooth = 25;
    int checkLength = 5;
    float power = 7.0f;
	bool flatTerrain = false;

    public Dictionary<string, TerrainTile> worldTiles = new Dictionary<string, TerrainTile>();

    IEnumerator loadTerrainTile(TerrainTile tile)
    {
        // Create and position GameObject
        var terrainData = new TerrainData();
        terrainData.heightmapResolution = terrainResolution;
        terrainData.alphamapResolution = tileSize;

        // Download the tile heightmap
        tile.url = baseUrl + tile.z + "/" + tile.x + "/" + tile.y + ".png";
        WWW www = new WWW(tile.url);
        while (!www.isDone) { }
        tile.heightmap = new Texture2D(terrainResolution, terrainResolution); //2049
        www.LoadImageIntoTexture(tile.heightmap);
    
        // Multidimensional array of this tiles heights in x/y
        float[,] terrainHeights = terrainData.GetHeights(0, 0, terrainResolution + 1, terrainResolution + 1);

        // Load colors into byte array
        Color[] pixelByteArray = tile.heightmap.GetPixels();

        if (flatTerrain)
        {
            for (int y = 0; y <= tileSize; y++)
            {
                for (int x = 0; x <= tileSize; x++)
                {
                    terrainHeights[y, x] = 0f;
                }
            }
        }
        else
        {
            for (int y = 0; y <= terrainResolution; y++)
            {
                for (int x = 0; x <= terrainResolution; x++)
                {
                    if (x == terrainResolution && y == terrainResolution)
                    {
                        terrainHeights[y, x] = pixelByteArray[(y - 1) * tileSize + (x - 1)].grayscale * intensity;
                    }
                    else if (x == terrainResolution)
                    {
                        terrainHeights[y, x] = pixelByteArray[(y) * tileSize + (x - 1)].grayscale * intensity;
                    }
                    else if (y == terrainResolution)
                    {
                        terrainHeights[y, x] = pixelByteArray[((y - 1) * tileSize) + x].grayscale * intensity;
                    }
                    else
                    {
                        terrainHeights[y, x] = pixelByteArray[y * tileSize + x].grayscale * intensity;
                    }
                }
            }
        }

        // Use the newly populated height data to apply the heightmap
        terrainData.SetHeights(0, 0, terrainHeights);

        // Set terrain size
        terrainData.size = new Vector3(terrainSize, terrainHeight, terrainSize);

        tile.terrain = Terrain.CreateTerrainGameObject(terrainData);
        tile.terrain.transform.position = new Vector3(tile.worldX * terrainSize, 0, tile.worldZ * terrainSize);

        tile.terrain.name = "tile_" + tile.x.ToString() + "_" + tile.y.ToString();

        yield return null;
    }

    void setTextures(TerrainData terrainData)
    {
        var flatSplat = new SplatPrototype();
        var steepSplat = new SplatPrototype();
        var baseSplat = new SplatPrototype();

        baseSplat.texture = baseTexture;
        baseSplat.normalMap = baseNormal;

        flatSplat.texture = flatTexture;
        flatSplat.normalMap = flatNormal;

        steepSplat.texture = steepTexture;
        steepSplat.normalMap = steepNormal;

        terrainData.splatPrototypes = new SplatPrototype[]
        {
            baseSplat,
            flatSplat,
            flatSplat,
            steepSplat
        };

        // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Normalise x/y coordinates to range 0-1 
                float y_01 = (float)y / (float)terrainData.alphamapHeight;
                float x_01 = (float)x / (float)terrainData.alphamapWidth;

                // Sample the height at this location (note GetHeight expects int coordinates corresponding to locations in the heightmap array)
                float height = terrainData.GetHeight(Mathf.RoundToInt(y_01 * terrainData.heightmapHeight), Mathf.RoundToInt(x_01 * terrainData.heightmapWidth));

                // Calculate the normal of the terrain (note this is in normalised coordinates relative to the overall terrain dimensions)
                Vector3 normal = terrainData.GetInterpolatedNormal(y_01, x_01);

                // Calculate the steepness of the terrain
                float steepness = terrainData.GetSteepness(y_01, x_01);

                // Setup an array to record the mix of texture weights at this point
                float[] splatWeights = new float[terrainData.alphamapLayers];

                // CHANGE THE RULES BELOW TO SET THE WEIGHTS OF EACH TEXTURE ON WHATEVER RULES YOU WANT

                // Texture[0] has constant influence
                splatWeights[0] = 0.5f;

                // Texture[1] is stronger at lower altitudes
                splatWeights[1] = Mathf.Clamp01((terrainData.heightmapHeight - height));

                // Texture[2] stronger on flatter terrain
                // Note "steepness" is unbounded, so we "normalise" it by dividing by the extent of heightmap height and scale factor
                // Subtract result from 1.0 to give greater weighting to flat surfaces
                splatWeights[2] = 1.0f - Mathf.Clamp01(steepness * steepness / (terrainData.heightmapHeight / 5.0f));

                // Texture[3] increases with height but only on surfaces facing positive Z axis 
                splatWeights[3] = height * Mathf.Clamp01(normal.z);

                // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                float z = splatWeights.Sum();

                // Loop through each terrain texture
                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {

                    // Normalize so that sum of all texture weights = 1
                    splatWeights[i] /= z;

                    // Assign this point to the splatmap array
                    splatmapData[x, y, i] = splatWeights[i];
                }
            }
        }

        // Finally assign the new splatmap to the terrainData:
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }


    void loadAllTerrain()
    {
        foreach(TerrainTile tile in worldTiles.Values)
        {
            StartCoroutine(loadTerrainTile(tile));
        }
    }

    void loadTilesAround(int z, int x, int margin)
    {
        for(int tilex = x - margin; tilex < x + margin; tilex++)
        {
            for (int tilez = z - margin; tilez < z + margin; tilez++)
            {
                worldTiles[tilex.ToString() + "_" + tilez.ToString()] = new TerrainTile(
                    tilez, tilex, tilez + z, tilex + x);
            }
        }
    }

    // Use this for initialization
    void Start()
    {
        loadTilesAround(12, 8, 2);

        // Initial tile loading
        loadAllTerrain();

        TerrainStitchEditor t = new TerrainStitchEditor();
        t.StitchTerrain();

        foreach(TerrainTile tile in worldTiles.Values)
        {
            setTextures(tile.terrain.GetComponent<Terrain>().terrainData);
        }
    }
}