using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
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

    public int tileSize = 256;
    public int terrainSize = 256;
    public int terrainResolution = 2048;
    public int terrainHeight = 100;

    public int tileMargin;
    public int startX;
    public int startY;

	public bool flatTerrain = false;

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
                        terrainHeights[y, x] = pixelByteArray[(y - 1) * tileSize + (x - 1)].grayscale;
                    }
                    else if (x == terrainResolution)
                    {
                        terrainHeights[y, x] = pixelByteArray[(y) * tileSize + (x - 1)].grayscale;
                    }
                    else if (y == terrainResolution)
                    {
                        terrainHeights[y, x] = pixelByteArray[((y - 1) * tileSize) + x].grayscale;
                    }
                    else
                    {
                        terrainHeights[y, x] = pixelByteArray[y * tileSize + x].grayscale;
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

    void loadAllTerrain()
    {
        
        foreach(TerrainTile tile in worldTiles.Values)
        {
            StartCoroutine(loadTerrainTile(tile));
        }
    }

    void loadTilesAround(int z, int x, int margin)
    {
        for(int tilex = x - margin; tilex <= x + margin; tilex++)
        {
            for (int tilez = z - margin; tilez <= z + margin; tilez++)
            {
                worldTiles[tilex.ToString() + "_" + tilez.ToString()] = new TerrainTile(
                    tilez, tilex, z - tilez, -(x - tilex));
            }
        }
    }
    
    // Use this for initialization
    void Start()
    {
        loadTilesAround(startX, startY, tileMargin);

        // Initial tile loading
        loadAllTerrain();

        TerrainStitchEditor t = new TerrainStitchEditor();
        t.StitchTerrain();

        foreach(TerrainTile tile in worldTiles.Values)
        {
            GetComponent<TerrainTextures>().setTextures(tile.terrain.GetComponent<Terrain>().terrainData);
        }
        
    }
}