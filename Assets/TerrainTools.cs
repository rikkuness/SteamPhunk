using System;
using UnityEngine;
using System.Collections;

[System.Serializable]
public static class TerrainTools
{
    static double[] smoothModTable = null;
    static double[] smoothDYTable = null;
    static bool memoizationTablesFilled = false;

    static Vector3 prevSize = new Vector3(0, 0, 0);
    static int prevHeight = 0;
    static int prevWidth = 0;
    static int prevNumSamples = 0;

    delegate float GetYMod(int domTerrain, int terrainToMod, double dY, int curCellX, int maxCellsX, int curCellY, int maxCellsY);

    public static void StitchTerrains(Terrain terrain1, Terrain terrain2, int numSamples, int domTerrain)
    {
        var dX = terrain2.transform.position.x - terrain1.transform.position.x;
        var dZ = terrain2.transform.position.z - terrain1.transform.position.z;

        var height = terrain1.terrainData.heightmapHeight;
        var width = terrain1.terrainData.heightmapWidth;

        if (height != terrain2.terrainData.heightmapHeight || width != terrain2.terrainData.heightmapWidth || terrain1.terrainData.size != terrain2.terrainData.size)
        {
            return;
        }

        if (height != prevHeight || width != prevWidth || terrain1.terrainData.size != prevSize || numSamples != prevNumSamples)
        {
            prevHeight = height;
            prevWidth = width;
            prevSize = terrain1.terrainData.size;
            prevNumSamples = numSamples;

            memoizationTablesFilled = false;
            smoothModTable = new double[height];
            smoothDYTable = new double[numSamples];
        }

        GetYMod getYMod;
        if (memoizationTablesFilled)
        {
            getYMod = getYModMemoized;
        }
        else
        {
            getYMod = getYModDynamic;
        }

        var heights1 = terrain1.terrainData.GetHeights(0, 0, width, height);
        var heights2 = terrain2.terrainData.GetHeights(0, 0, width, height);

        if (Mathf.Abs(dX) > Mathf.Abs(dZ))
        {
            var xDir = terrain2.transform.position.x > terrain1.transform.position.x ? 1 : -1;

            terrain2.transform.position = terrain1.transform.position;
            terrain2.transform.position = new Vector3(terrain1.transform.position.x + terrain1.terrainData.size.x * xDir, terrain2.transform.position.y, terrain2.transform.position.z);

            for (int z = 0; z < height; z++)
            {
                var dY = 0.0;
                if (xDir == 1)
                {
                    dY = heights2[z, 0] - heights1[z, width - 1];
                }
                else
                {
                    dY = heights2[z, width - 1] - heights1[z, 0];
                }

                for (int i = 0; i < numSamples; i++)
                {
                    if (xDir == 1)
                    {
                        heights1[z, width - 1 - i] = heights1[z, width - 1 - i] + getYMod.Invoke(domTerrain, 1, dY, z, height, i, numSamples);
                        heights2[z, i] = heights2[z, i] - getYMod.Invoke(domTerrain, 2, dY, z, height, i, numSamples);
                    }
                    else
                    {
                        heights1[z, i] = heights1[z, i] + getYMod.Invoke(domTerrain, 1, dY, z, height, i, numSamples);
                        heights2[z, width - 1 - i] = heights2[z, width - 1 - i] - getYMod.Invoke(domTerrain, 2, dY, z, height, i, numSamples);
                    }
                }
            }
        }
        else
        {
            var zDir = terrain2.transform.position.z > terrain1.transform.position.z ? 1 : -1;

            terrain2.transform.position = terrain1.transform.position;
            terrain2.transform.position = new Vector3(terrain2.transform.position.x, terrain2.transform.position.y, terrain1.transform.position.z + terrain1.terrainData.size.z * zDir);

            for (int x = 0; x < height; x++)
            {
                var dY = 0.0;
                if (zDir == 1)
                {
                    dY = heights2[0, x] - heights1[width - 1, x];
                }
                else
                {
                    dY = heights2[width - 1, x] - heights1[0, x];
                }
                for (int i = 0; i < numSamples; i++)
                {
                    if (zDir == 1)
                    {
                        heights1[width - 1 - i, x] = heights1[width - 1 - i, x] + getYMod.Invoke(domTerrain, 1, dY, x, height, i, numSamples);
                        heights2[i, x] = heights2[i, x] - getYMod.Invoke(domTerrain, 2, dY, x, height, i, numSamples);
                    }
                    else
                    {
                        heights1[i, x] = heights1[i, x] + getYMod.Invoke(domTerrain, 1, dY, x, height, i, numSamples);
                        heights2[width - 1 - i, x] = heights2[width - 1 - i, x] - getYMod.Invoke(domTerrain, 2, dY, x, height, i, numSamples);
                    }
                }
            }
        }

        memoizationTablesFilled = true;

        terrain1.terrainData.SetHeights(0, 0, heights1);
        terrain1.Flush();
        terrain2.terrainData.SetHeights(0, 0, heights2);
        terrain2.Flush();
    }

    static float getYModDynamic(int domTerrain, int terrainToMod, double dY, int curCellX, int maxCellsX, int curCellY, int maxCellsY)
    {
        double yMod = dY / 2.0f;

        if (terrainToMod == 1)
        {
            if (domTerrain == 1)
            {
                yMod = dY * smoothMod(curCellX, maxCellsX);
            }
            else if (domTerrain == 2)
            {
                yMod = dY * (1.0f - smoothMod(curCellX, maxCellsX));
            }
        }
        else if (terrainToMod == 2)
        {
            if (domTerrain == 1)
            {
                yMod = dY * (1.0f - smoothMod(curCellX, maxCellsX));
            }
            else if (domTerrain == 2)
            {
                yMod = dY * smoothMod(curCellX, maxCellsX);
            }
        }
        else
        {
            Debug.LogError("terrainToMod must be either 1 or 2! (found: " + terrainToMod + ")");
        }

        return (float)(yMod * smoothDY(curCellY, maxCellsY));
    }

    static float getYModMemoized(int domTerrain, int terrainToMod, double dY, int curCellX, int maxCellsX, int curCellY, int maxCellsY)
    {
        double yMod = (double)dY / 2.0f;

        if (terrainToMod == 1)
        {
            if (domTerrain == 1)
            {
                yMod = dY * smoothModTable[curCellX];
            }
            else if (domTerrain == 2)
            {
                yMod = dY * (1.0f - smoothModTable[curCellX]);
            }
        }
        else if (terrainToMod == 2)
        {
            if (domTerrain == 1)
            {
                yMod = dY * (1.0f - smoothModTable[curCellX]);
            }
            else if (domTerrain == 2)
            {
                yMod = dY * smoothModTable[curCellX];
            }
        }
        else
        {
            Debug.LogError("terrainToMod must be either 1 or 2! (found: " + terrainToMod + ")");
        }

        return (float)(yMod * smoothDYTable[curCellY]);
    }

    static double smoothDY(int current, double max)
    {
        double x = 1.0f - (double)current / max - 1;

        var result = (236706659320000.0 * Math.Pow(x, 6) + 99115929736349168000.0 * Math.Pow(x, 5) - 247790818523389713100.0 * Math.Pow(x, 4)
            + 80036273392876468420.0 * Math.Pow(x, 3) + 127736693875550215551.0 * Math.Pow(x, 2) - 713647159857677493.0 * x) / 58384668816317760000.0;
        smoothDYTable[current] = result;
        return result;
    }

    static double smoothMod(int current, double max)
    {
        double x = (double)current / max - 1;

        var result = 1.0E-4 * ((1 * (x - 0.0) * (x - 0.05) * (x - 0.1) * (x - 0.5) * (x - 0.9) * (x - 0.95) * (x - 0.99) * (x - 1.0)) / -1.4317846804800003E-5) +
            0.0050 * ((1 * (x - 0.0) * (x - 0.01) * (x - 0.1) * (x - 0.5) * (x - 0.9) * (x - 0.95) * (x - 0.99) * (x - 1.0)) / 3.074152499999999E-5) +
            0.04 * ((1 * (x - 0.0) * (x - 0.01) * (x - 0.05) * (x - 0.5) * (x - 0.9) * (x - 0.95) * (x - 0.99) * (x - 1.0)) / -9.804240000000002E-5) +
            0.5 * ((1 * (x - 0.0) * (x - 0.01) * (x - 0.05) * (x - 0.1) * (x - 0.9) * (x - 0.95) * (x - 0.99) * (x - 1.0)) / 0.0019448099999999997) +
            0.04 * ((1 * (x - 0.0) * (x - 0.01) * (x - 0.05) * (x - 0.1) * (x - 0.5) * (x - 0.95) * (x - 0.99) * (x - 1.0)) / -9.804239999999985E-5) +
            0.0050 * ((1 * (x - 0.0) * (x - 0.01) * (x - 0.05) * (x - 0.1) * (x - 0.5) * (x - 0.9) * (x - 0.99) * (x - 1.0)) / 3.0741525000000005E-5) +
            1.0E-4 * ((1 * (x - 0.0) * (x - 0.01) * (x - 0.05) * (x - 0.1) * (x - 0.5) * (x - 0.9) * (x - 0.95) * (x - 1.0)) / -1.4317846804800018E-5);
        smoothModTable[current] = result;
        return result;
    }
}
