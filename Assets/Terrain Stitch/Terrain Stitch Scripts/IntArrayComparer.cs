using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TerrainStitch
{
/// <summary>
/// Int array comparer.
/// </summary>
	public class IntArrayComparer : IEqualityComparer<int[]>
	{
		/// <summary>
		/// Equals the specified x and y.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		public bool Equals (int[] x, int[] y)
		{
			if (x.Length != y.Length) {
				return false;
			}
			for (int i = 0; i < x.Length; i++) {
				if (x [i] != y [i]) {
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Gets the hash code.
		/// </summary>
		/// <returns>The hash code.</returns>
		/// <param name="obj">Object.</param>
		public int GetHashCode (int[] obj)
		{
			int result = 17;
			for (int i = 0; i < obj.Length; i++) {
				unchecked {
					result = result * 23 + obj [i];
				}
			}
			return result;
		}
	}
}