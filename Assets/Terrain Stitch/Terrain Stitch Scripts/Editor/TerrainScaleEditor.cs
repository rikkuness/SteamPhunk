using UnityEngine;
using UnityEditor;
namespace TerrainStitch
{
/// <summary>
/// Terrain scale editor.
/// </summary>
	public class TerrainScaleEditor : EditorWindow
	{
		/// <summary>
		/// The new height.
		/// </summary>
		float newHeight = 600f;
		/// <summary>
		/// The terrain.
		/// </summary>
		Terrain terrain;
	
		/// <summary>
		/// Init this instance.
		/// </summary>
		[MenuItem ("Tools/Terrain Scaler")]
		static void Init ()
		{
			EditorWindow.GetWindow (typeof(TerrainScaleEditor), false, "Terrain Scaler");
		}

		/// <summary>
		/// Raises the GU event.
		/// </summary>
		void OnGUI ()
		{
				

			GUILayout.Label ("Base Settings", EditorStyles.boldLabel);

			terrain = (Terrain)EditorGUILayout.ObjectField ("Terrain to change", terrain, typeof(Terrain), true);
			newHeight = EditorGUILayout.FloatField ("New height", newHeight);

			EditorGUILayout.Space ();

			if (GUILayout.Button ("Rescale terrain")) {
				RescaleTerrain ();
			}

		}
		/// <summary>
		/// Rescales the terrain.
		/// </summary>
		void RescaleTerrain ()
		{
			TerrainData terrainData = terrain.terrainData;
			float[,] heights = terrainData.GetHeights (0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
			Vector3 terrainSize = terrainData.size;
			float scale = (float)terrainSize.y / (float)newHeight;
			terrainSize.y = newHeight;
			terrainData.size = terrainSize;
			terrain.Flush ();
			for (int i = 0; i < heights.GetLength(0); i++) {
				for (int j = 0; j < heights.GetLength (1); j++) {
					heights [i, j] = heights [i, j] * scale;
				}
			}
			terrainData.SetHeights (0, 0, heights);
			terrain.terrainData = terrainData;
			terrain.Flush ();
		}
	}
}