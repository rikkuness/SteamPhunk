using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TerrainStitch
{

	/// <summary>
	/// Sets Terrain neighbours.
	/// </summary>
	public class TerrainNeighbours : MonoBehaviour
	{

		Terrain[] _terrains;
		Dictionary<int[],Terrain> _terrainDict = null;


		/// <summary>
		/// The first position for terrain tile management.
		/// </summary>
		public Vector2 firstPosition;

		/// <summary>
		/// Start this instance and creates neighbours for scene terrains
		/// </summary>
		void Start ()
		{
			CreateNeighbours ();

		}

		/// <summary>
		/// Sets the neighbours for all terrains in scenes
		/// </summary>
		public void CreateNeighbours ()
		{
			if (_terrainDict == null)
				_terrainDict = new Dictionary<int[], Terrain> (new IntArrayComparer ());
			else {
				_terrainDict.Clear ();
			}
			_terrains = Terrain.activeTerrains;
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
				}
			}
		}

	}

}