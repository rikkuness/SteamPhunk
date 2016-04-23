using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using CSML;
using System.Linq;

namespace TerrainStitch
{
	/// <summary>
	/// Terrain stitch editor.
	/// </summary>
	public class TerrainStitchEditor : EditorWindow
	{
		/// <summary>
		/// The first position for terrain tile management.
		/// </summary>
		public Vector2 firstPosition;
		
		/// <summary>
		/// The _terrains to stitch.
		/// </summary>
		Terrain[] _terrains;
		/// <summary>
		/// The _terrain dict holds terrain positions.
		/// </summary>
		Dictionary<int[],Terrain> _terrainDict = null;

		/// <summary>
		/// The testing options show.
		/// </summary>
		bool testingShow = false;

		/// <summary>
		/// The level smooth.
		/// </summary>
		float levelSmooth = 16;

		/// <summary>
		/// The length of the stitch check.
		/// </summary>
		int checkLength = 100;

		/// <summary>
		/// The power of average function.
		/// </summary>
		float power = 7.0f;

		/// <summary>
		/// The selected method.
		/// </summary>
		int selectedMethod = 0;

		/// <summary>
		/// The methods list.
		/// </summary>
		GUIContent[] options = new GUIContent[] {
			new GUIContent ("Average Power"), 
			new GUIContent ("Trend"),
		};


		enum Side
		{
			Left,
			Right,
			Top,
			Bottom
		}

		/// <summary>
		/// Init this instance.
		/// </summary>
		[MenuItem ("Tools/Terrain Stitcher")]
		static void Init ()
		{
			EditorWindow.GetWindow (typeof(TerrainStitchEditor), false, "Terrain Stitcher");
				
		}

		/// <summary>
		/// Raises the GU event.
		/// </summary>
		void OnGUI ()
		{
			
			GUILayout.Label ("Settings", EditorStyles.boldLabel);

			GUIContent method = new GUIContent ("Stitch Method", "Stitching method");
			selectedMethod = EditorGUILayout.Popup (method, selectedMethod, options); 

			EditorGUILayout.Space ();

			if (selectedMethod == 0) {
				levelSmooth = EditorGUILayout.IntSlider ("Smooth level ", (int)levelSmooth, 5, 100);
				power = EditorGUILayout.IntSlider ("Power", (int)power, 1, 7);
				checkLength = EditorGUILayout.IntField ("Average length", checkLength);

			} else {
				checkLength = EditorGUILayout.IntField ("Trend lenght", checkLength);
			}
		
			EditorGUILayout.Space ();
	
			if (GUILayout.Button ("Stitch Selected Terrains")) {
			
				StitchTerrain (true);
			}
			if (GUILayout.Button ("Stitch All Terrains")) {

				StitchTerrain ();
			}
			EditorGUILayout.Space ();


			if (GUILayout.Button ("Create terrain neighbours manager")) {
			
				CreateTerrainNeighbours ();
			}
			EditorGUILayout.Space ();
			EditorGUILayout.Space ();

			testingShow = EditorGUILayout.Foldout (testingShow, "Testing Options - use only on testing scene, will change terrains on scene");
			if (testingShow) {

				if (GUILayout.Button ("Random Noise on Selected Terrains")) {
				
					RandomNoise (true);
				}

				if (GUILayout.Button ("Random Noise on All Terrains")) {
			
					RandomNoise ();
				}
			}

		}


		/// <summary>
		/// Creates the terrain neighbours manager.
		/// </summary>
		void CreateTerrainNeighbours ()
		{
			GameObject go = new GameObject ("_TerrainNeighboursManager");
			TerrainNeighbours terrainNeighbours = go.AddComponent<TerrainNeighbours> ();
			terrainNeighbours.CreateNeighbours ();
		}

		#region randomNoise

		/// <summary>
		/// Randoms the noise on terrains.
		/// </summary>
		/// <param name="selected">If set to <c>true</c> selected.</param>
		public void  RandomNoise (bool selected = false)
		{
			List<Terrain> terrains = new List<Terrain> ();
			if (selected) {
				foreach (var item in Selection.gameObjects) {
					Terrain terrain = item.GetComponent<Terrain> ();
					if (terrain != null)
						terrains.Add (terrain);
				}
			} else
				terrains.AddRange (Terrain.activeTerrains);

			foreach (var t in terrains) {
				Undo.RegisterUndo (t.terrainData, "Noise terrains");
			}

			foreach (var item in terrains) {
				GenerateHeights (item, 5);
			}

		}

		/// <summary>
		/// Generates the heights.
		/// </summary>
		/// <param name="terrain">Terrain.</param>
		/// <param name="tileSize">Tile size.</param>
		public void GenerateHeights (Terrain terrain, float tileSize)
		{
			Vector2 randomMove = Random.insideUnitCircle * 1000;
			float[,] heights = new float[terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight];
		
			for (int i = 0; i < terrain.terrainData.heightmapWidth; i++) {
				for (int k = 0; k < terrain.terrainData.heightmapHeight; k++) {
					heights [i, k] = Mathf.PerlinNoise (((float)i / (float)terrain.terrainData.heightmapWidth) * tileSize + randomMove.x, ((float)k / (float)terrain.terrainData.heightmapHeight) * tileSize + randomMove.y) / 10.0f;
				}
			}
		
			terrain.terrainData.SetHeights (0, 0, heights);
		}

		#endregion

		#region TerrainStitching

		/// <summary>
		/// Stitchs the terrain.
		/// </summary>
		/// <param name="selected">If set to <c>true</c> usese selected terrains.</param>
		public void StitchTerrain (bool selected = false)
		{
			if (_terrainDict == null)
				_terrainDict = new Dictionary<int[], Terrain> (new IntArrayComparer ());
			else {
				_terrainDict.Clear ();
			}

			List<Terrain> terrains = new List<Terrain> ();
			if (selected) {
				foreach (var item in Selection.gameObjects) {
					Terrain terrain = item.GetComponent<Terrain> ();
					if (terrain != null)
						terrains.Add (terrain);
				}
			} else
				terrains.AddRange (Terrain.activeTerrains);

			_terrains = terrains.ToArray ();

			foreach (var t in terrains) {
				Undo.RegisterUndo (t.terrainData, "Stitch terrains");
			}


			if (_terrains.Length > 0) {

				firstPosition = new Vector2 (_terrains [0].transform.position.x, _terrains [0].transform.position.z);

				int sizeX = (int)_terrains [0].terrainData.size.x;
				int sizeZ = (int)_terrains [0].terrainData.size.z;

				foreach (var terrain in _terrains) {

					int[] posTer = new int[] {
						(int)(Mathf.RoundToInt ((terrain.transform.position.x - firstPosition.x) / sizeX)),
						(int)(Mathf.RoundToInt ((terrain.transform.position.z - firstPosition.y) / sizeZ))
					};
					_terrainDict.Add (posTer, terrain);


					
				}
				//Checks neighbours and stitches them
				foreach (var item in _terrainDict) {
					int[] posTer = item.Key;
					Terrain top = null;
					Terrain left = null;
					Terrain right = null;
					Terrain bottom = null;
					_terrainDict.TryGetValue (new int[] {
						posTer [0],
						posTer [1] + 1
					}, out top);
					_terrainDict.TryGetValue (new int[] {
						posTer [0] - 1,
						posTer [1]
					}, out left);
					_terrainDict.TryGetValue (new int[] {
						posTer [0] + 1,
						posTer [1]
					}, out right);
					_terrainDict.TryGetValue (new int[] {
						posTer [0],
						posTer [1] - 1
					}, out bottom);

					item.Value.SetNeighbors (left, top, right, bottom);

					item.Value.Flush ();

					if (selectedMethod == 0 || checkLength == 0) {
					
						if (right != null) {

							StitchTerrains (item.Value, right, Side.Right);
						}

						if (top != null) {
							StitchTerrains (item.Value, top, Side.Top);
						}


					} else {

						if (top != null)
							StitchTerrainsTrend (item.Value, top, Side.Top);

						if (right != null)
							StitchTerrainsTrend (item.Value, right, Side.Right);
					}

				}
			
				//Repairs corners
				foreach (var item in _terrainDict) {
					int[] posTer = item.Key;
					Terrain top = null;
					Terrain left = null;
					Terrain right = null;
					Terrain bottom = null;
					_terrainDict.TryGetValue (new int[] {
						posTer [0],
						posTer [1] + 1
					}, out top);
					_terrainDict.TryGetValue (new int[] {
						posTer [0] - 1,
						posTer [1]
					}, out left);
					_terrainDict.TryGetValue (new int[] {
						posTer [0] + 1,
						posTer [1]
					}, out right);
					_terrainDict.TryGetValue (new int[] {
						posTer [0],
						posTer [1] - 1
					}, out bottom);


					int temptLength = checkLength;
					checkLength = 0;
				
					if (right != null) {
						StitchTerrains (item.Value, right, Side.Right, false);
					}
									
					if (top != null) {
						StitchTerrains (item.Value, top, Side.Top, false);				
					}	
									
					checkLength = temptLength;

					if (right != null && bottom != null) {
						Terrain rightBottom = null;
						_terrainDict.TryGetValue (new int[] {
							posTer [0] + 1,
							posTer [1] - 1
						}, out rightBottom);
						if (rightBottom != null)
							StitchTerrainsRepair (item.Value, right, bottom, rightBottom);
					}

				}

			}
		}

		#endregion

		/// <summary>
		/// Average the specified first and second value.
		/// </summary>
		/// <param name="first">First.</param>
		/// <param name="second">Second.</param>
		float average (float first, float second)
		{

			return Mathf.Pow ((Mathf.Pow (first, power) + Mathf.Pow (second, power)) / 2.0f, 1 / power);
		}

		#region TrendAverage

		/// <summary>
		/// Stitchs the terrains with trend method.
		/// </summary>
		/// <param name="terrain">First Terrain.</param>
		/// <param name="second">Second Terrain.</param>
		/// <param name="side">Side of stitch.</param>
		void StitchTerrainsTrend (Terrain terrain, Terrain second, Side side)
		{
			TerrainData terrainData = terrain.terrainData;
			TerrainData secondData = second.terrainData;
		
		
		
			float[,] heights = terrainData.GetHeights (0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
			float[,] secondHeights = secondData.GetHeights (0, 0, secondData.heightmapWidth, secondData.heightmapHeight);
		
	
		
			if (side == Side.Right) {
			
			
				//int y = heights.GetLength (0) - 1;
				int x = 0;
			
				//int x2 = 0;
				//int y2 = 0;

				string matrixAT = "";
				string matrixATID = "";
				string matrixATOnes = "";



				for (int i = 1; i <= checkLength; i++) {
					matrixAT += (i * i);
					matrixATID += i;
					matrixATOnes += "1";

					matrixAT += ",";
					matrixATID += ",";
					matrixATOnes += ",";

				}
				for (int i = checkLength; i <= checkLength * 2; i++) {
					matrixAT += (i * i);
					matrixATID += i;
					matrixATOnes += "1";
				
					if (i < checkLength * 2) {
						matrixAT += ",";
						matrixATID += ",";
						matrixATOnes += ",";
					}
				}

				Matrix AT = new Matrix (matrixAT + ";" + matrixATID + ";" + matrixATOnes);
					
				Matrix A = AT.Transpose ();
				Matrix ATA = AT * A;
				Matrix ATA1 = ATA.Inverse ();

				for (x = 0; x < heights.GetLength (1); x++) {
					string matrixZ = "";

					for (int i = heights.GetLength (0) - checkLength; i < heights.GetLength (0); i++) {
						matrixZ += heights [x, i] + ";";

					}


					for (int i = 0; i <= checkLength; i++) {
					
						matrixZ += secondHeights [x, i];
						if (i < checkLength) {
							matrixZ += ";";
						}
					}

					Matrix Z = new Matrix (matrixZ);
				
					Matrix ATZ = AT * Z;
					Matrix X = ATA1 * ATZ;




					double trendAverage = checkLength * checkLength * X [1, 1].Re + checkLength * X [2, 1].Re + X [3, 1].Re;

					Matrix sAT = new Matrix ("1," + (checkLength * checkLength) + "," + Mathf.Pow (2 * checkLength, 2) + ";1," + checkLength + "," + (checkLength * 2) + ";1,1,1");

					Matrix sA = sAT.Transpose ();
					Matrix sATA = sAT * sA;
					Matrix sATA1 = sATA.Inverse ();
				
					Matrix sZ = new Matrix (heights [x, heights.GetLength (0) - checkLength] + ";" + trendAverage + ";" + secondHeights [x, checkLength]);

			


					Matrix sATZ = sAT * sZ;
					Matrix sX = sATA1 * sATZ;


					double[] heightTrend = new double[checkLength];
					double[] secondHeightTrend = new double[checkLength + 1];

					for (int i = 1; i <= checkLength; i++) {
						heightTrend [i - 1] = i * i * sX [1, 1].Re + i * sX [2, 1].Re + sX [3, 1].Re;
					}
					int j = 0;
					for (int i = checkLength; i <= checkLength * 2; i++) {
						secondHeightTrend [j] = i * i * sX [1, 1].Re + i * sX [2, 1].Re + sX [3, 1].Re;
						j++;
					}




					for (int i = 0; i < checkLength; i++) {
						heights [x, heights.GetLength (1) - i - 1] = (float)heightTrend [checkLength - i - 1] * (checkLength - i) / checkLength + heights [x, heights.GetLength (1) - i - 1] * i / checkLength;
					
					}


					for (int i = 0; i <= checkLength; i++) {
					
						secondHeights [x, i] = (float)secondHeightTrend [i] * (checkLength - i) / checkLength + secondHeights [x, i] * i / checkLength;

					}
				}



			



			} else {
				if (side == Side.Top) {
				
					int y = 0;
					//int x = heights.GetLength (0) - 1;
				
					//int x2 = 0;
					//int y2 = 0;

				
					string matrixAT = "";
					string matrixATID = "";
					string matrixATOnes = "";
				
				
				
					for (int i = 1; i <= checkLength; i++) {
						matrixAT += (i * i);
						matrixATID += i;
						matrixATOnes += "1";
					
						matrixAT += ",";
						matrixATID += ",";
						matrixATOnes += ",";
					
					}
					for (int i = checkLength; i <= checkLength * 2; i++) {
						matrixAT += (i * i);
						matrixATID += i;
						matrixATOnes += "1";
					
						if (i < checkLength * 2) {
							matrixAT += ",";
							matrixATID += ",";
							matrixATOnes += ",";
						}
					}
				
					Matrix AT = new Matrix (matrixAT + ";" + matrixATID + ";" + matrixATOnes);
				
					Matrix A = AT.Transpose ();
					Matrix ATA = AT * A;
					Matrix ATA1 = ATA.Inverse ();
				
					for (y = 0; y < heights.GetLength (1); y++) {
						string matrixZ = "";
					
						for (int i = heights.GetLength (0) - checkLength; i < heights.GetLength (0); i++) {
							matrixZ += heights [i, y] + ";";
						
						}

						for (int i = 0; i <= checkLength; i++) {
						
							matrixZ += secondHeights [i, y];
							if (i < checkLength) {
								matrixZ += ";";
							}
						}
					
						Matrix Z = new Matrix (matrixZ);
					
						Matrix ATZ = AT * Z;
						Matrix X = ATA1 * ATZ;
					
					
					
					
						double trendAverage = checkLength * checkLength * X [1, 1].Re + checkLength * X [2, 1].Re + X [3, 1].Re;
					
						Matrix sAT = new Matrix ("1," + (checkLength * checkLength) + "," + Mathf.Pow (2 * checkLength, 2) + ";1," + checkLength + "," + (checkLength * 2) + ";1,1,1");

						Matrix sA = sAT.Transpose ();
						Matrix sATA = sAT * sA;
						Matrix sATA1 = sATA.Inverse ();
					
						Matrix sZ = new Matrix (heights [heights.GetLength (0) - checkLength, y] + ";" + trendAverage + ";" + secondHeights [checkLength, y]);
					
					
					
					
						Matrix sATZ = sAT * sZ;
						Matrix sX = sATA1 * sATZ;
					
					
						double[] heightTrend = new double[checkLength];
						double[] secondHeightTrend = new double[checkLength + 1];
					
						for (int i = 1; i <= checkLength; i++) {
							heightTrend [i - 1] = i * i * sX [1, 1].Re + i * sX [2, 1].Re + sX [3, 1].Re;
						}
						int j = 0;

						for (int i = checkLength; i <= checkLength * 2; i++) {
							secondHeightTrend [j] = i * i * sX [1, 1].Re + i * sX [2, 1].Re + sX [3, 1].Re;
							j++;
						}
					
				

						for (int i = 0; i < checkLength; i++) {
							heights [heights.GetLength (0) - i - 1, y] = (float)heightTrend [checkLength - i - 1] * (checkLength - i) / checkLength + heights [heights.GetLength (0) - i - 1, y] * i / checkLength;

						}
					
						for (int i = 0; i <= checkLength; i++) {
						
							secondHeights [i, y] = (float)secondHeightTrend [i] * (checkLength - i) / checkLength + secondHeights [i, y] * i / checkLength;
						
						}
					}

				}
			}
		
		
			terrainData.SetHeights (0, 0, heights);
			terrain.terrainData = terrainData;
		
			secondData.SetHeights (0, 0, secondHeights);
			second.terrainData = secondData;
		
			terrain.Flush ();
			second.Flush ();
		
		
		}

		#endregion

		#region PowerAverage

		/// <summary>
		/// Stitchs the terrains with power average.
		/// </summary>
		/// <param name="terrain">First Terrain.</param>
		/// <param name="second">Second Terrain.</param>
		/// <param name="side">Side of stitch.</param>
		/// <param name="smooth">If set to <c>true</c> smooth.</param>
		void StitchTerrains (Terrain terrain, Terrain second, Side side, bool smooth = true)
		{


			TerrainData terrainData = terrain.terrainData;
			TerrainData secondData = second.terrainData;



			float[,] heights = terrainData.GetHeights (0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
			float[,] secondHeights = secondData.GetHeights (0, 0, secondData.heightmapWidth, secondData.heightmapHeight);



			if (side == Side.Right) {


				int y = heights.GetLength (0) - 1;
				int x = 0;
		
				int y2 = 0;

				for (x = 0; x < heights.GetLength (1); x++) {

					heights [x, y] = average (heights [x, y], secondHeights [x, y2]);

					if (smooth)
						heights [x, y] += Mathf.Abs (heights [x, y - 1] - secondHeights [x, y2 + 1]) / levelSmooth;
			
					secondHeights [x, y2] = heights [x, y];

					for (int i = 1; i < checkLength; i++) {

						heights [x, y - i] = (average (heights [x, y - i], heights [x, y - i + 1]) + Mathf.Abs (heights [x, y - i] - heights [x, y - i + 1]) / levelSmooth) * (checkLength - i) / checkLength + heights [x, y - i] * i / checkLength;
						secondHeights [x, y2 + i] = (average (secondHeights [x, y2 + i], secondHeights [x, y2 + i - 1]) + Mathf.Abs (secondHeights [x, y2 + i] - secondHeights [x, y2 + i - 1]) / levelSmooth) * (checkLength - i) / checkLength + secondHeights [x, y2 + i] * i / checkLength;
					
					}

				}
			} else {
				if (side == Side.Top) {
				
					int y = 0;
					int x = heights.GetLength (0) - 1;
				
					int x2 = 0;
				
					for (y = 0; y < heights.GetLength (1); y++) {

						heights [x, y] = average (heights [x, y], secondHeights [x2, y]);

						if (smooth)
							heights [x, y] += Mathf.Abs (heights [x - 1, y] - secondHeights [x2 + 1, y]) / levelSmooth;


						secondHeights [x2, y] = heights [x, y];
					
						for (int i = 1; i < checkLength; i++) {

							heights [x - i, y] = (average (heights [x - i, y], heights [x - i + 1, y]) + Mathf.Abs (heights [x - i, y] - heights [x - i + 1, y]) / levelSmooth) * (checkLength - i) / checkLength + heights [x - i, y] * i / checkLength;
							secondHeights [x2 + i, y] = (average (secondHeights [x2 + i, y], secondHeights [x2 + i - 1, y]) + Mathf.Abs (secondHeights [x2 + i, y] - secondHeights [x2 + i - 1, y]) / levelSmooth) * (checkLength - i) / checkLength + secondHeights [x2 + i, y] * i / checkLength;
						
						}

					}
				}
			}


			terrainData.SetHeights (0, 0, heights);
			terrain.terrainData = terrainData;

			secondData.SetHeights (0, 0, secondHeights);
			second.terrainData = secondData;

			terrain.Flush ();
			second.Flush ();

	
		}

		#endregion

		#region RepairCorners

		/// <summary>
		/// Stitchs the terrains corners.
		/// </summary>
		/// <param name="terrain11">Terrain11.</param>
		/// <param name="terrain21">Terrain21.</param>
		/// <param name="terrain12">Terrain12.</param>
		/// <param name="terrain22">Terrain22.</param>
		void StitchTerrainsRepair (Terrain terrain11, Terrain terrain21, Terrain terrain12, Terrain terrain22)
		{

			int size = terrain11.terrainData.heightmapHeight - 1;
			int size0 = 0;
			List<float> heights = new List<float> ();


			heights.Add (terrain11.terrainData.GetHeights (size, size0, 1, 1) [0, 0]);
			heights.Add (terrain21.terrainData.GetHeights (size0, size0, 1, 1) [0, 0]);
			heights.Add (terrain12.terrainData.GetHeights (size, size, 1, 1) [0, 0]);
			heights.Add (terrain22.terrainData.GetHeights (size0, size, 1, 1) [0, 0]);


			float[,] height = new float[1, 1];
			height [0, 0] = heights.Max ();

			terrain11.terrainData.SetHeights (size, size0, height);
			terrain21.terrainData.SetHeights (size0, size0, height);
			terrain12.terrainData.SetHeights (size, size, height);
			terrain22.terrainData.SetHeights (size0, size, height);

			terrain11.Flush ();
			terrain12.Flush ();
			terrain21.Flush ();
			terrain22.Flush ();
		
		
		}

		#endregion
	}
}


