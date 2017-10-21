using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Generate the mesh that is underwater
public class ModifyBoatMesh {

	// The boat transform needed to get the global postion of a vertice
	private Transform boatTrans;
	// Coords of all vertices in the original boat
	Vector3[] boatVertices;
	// Positions in allVerticesArray, such as 0, 3, 5 to build triangles
	int[] boatTriangles;

	// Cache so we only need to transform from local to global once
	public Vector3[] boatVerticesGlobal;
	// Find all the distances to water once, some triangle share vertices
	float[] allDistancesToWater;

	// The triangles beloging to the part of the boat that is underwater
	public List<TriangleData> underWaterTriangleData = new List<TriangleData>();

	public ModifyBoatMesh(GameObject boatObj)
	{
		// Get the transform
		boatTrans = boatObj.transform;

		// Init the arrays and lists
		boatVertices = boatObj.GetComponent<MeshFilter> ().mesh.vertices;
		boatTriangles = boatObj.GetComponent<MeshFilter> ().mesh.triangles;
		boatVerticesGlobal = new Vector3[boatVertices.Length];
		allDistancesToWater = new float[boatVertices.Length];
	}

	// Generate the underwater mesh
	public void GenerateUnderwaterMesh()
	{
		// Reset
		underWaterTriangleData.Clear ();

		// Find all distances to water
		for (int j = 0; j < boatVertices.Length; j++)
		{
			// use Global coords
			Vector3 globalPos = boatTrans.TransformPoint (boatVertices [j]);

			// Save the global position so we only calc once
			boatVerticesGlobal [j] = globalPos;
			allDistancesToWater [j] = WaterController.current.DistanceToWater (globalPos, Time.time);
		}

		// Add triangles below water
		AddTriangles ();
	}

	// Add all triangles that is part of the underwater mesh
	private void AddTriangles ()
	{
		// List to store data we need to sort based on distance to water
		List<VertexData> vertexData = new List<VertexData> ();

		// Add init data
		vertexData.Add (new VertexData ());
		vertexData.Add (new VertexData ());
		vertexData.Add (new VertexData ());

		// Loop through all triangle (3 vertex at a time = 1 triangle)
		int i = 0;
		while (i < boatTriangles.Length) {
			// Loop through the 3 vertices
			for (int x = 0; x < 3; x++) {
				// Save data
				vertexData [x].distance = allDistancesToWater [boatTriangles [i]];
				vertexData [x].index = x;
				vertexData [x].globalVertexPos = boatVerticesGlobal [boatTriangles [i]];

				i++;
			}

			// All are above water
			if (vertexData [0].distance > 0f && vertexData [1].distance > 0f && vertexData [2].distance > 0f) {
				continue;
			}

			// Create triangles that are below water

			// Check if all below
			if (vertexData [0].distance < 0f && vertexData [1].distance < 0f && vertexData [2].distance < 0f) {
				Vector3 p1 = vertexData [0].globalVertexPos;
				Vector3 p2 = vertexData [1].globalVertexPos;
				Vector3 p3 = vertexData [2].globalVertexPos;

				// Save Triangle
				underWaterTriangleData.Add (new TriangleData (p1, p2, p3));
			} else {
				// One or Two are below water
				// Sort vertices
				vertexData.Sort ((x, y) => x.distance.CompareTo (y.distance));
				vertexData.Reverse ();

				// Check one is above and the rest below
				if (vertexData [0].distance > 0f && vertexData [1].distance < 0f && vertexData [2].distance < 0f) {
					AddTrianglesOneAboveWater (vertexData);
				} else if (vertexData [0].distance > 0f && vertexData [1].distance > 0f && vertexData [2].distance < 0f) {
					AddTrianglesTwoAboveWater (vertexData);
				}
			}
		}
	}

	// Build the new triangles where on of the old vertices is above water
	private void AddTrianglesOneAboveWater(List<VertexData> vertexData)
	{
		// H is always at position 0
		Vector3 H = vertexData [0].globalVertexPos;

		// Left of H is M
		// Right of H is L

		// Find the index of M
		int M_index = vertexData [0].index - 1;
		if (M_index < 0)
		{
			M_index = 2;
		}

		// We also need the heights to water
		float h_H = vertexData [0].distance;
		float h_M = 0f;
		float h_L = 0f;

		Vector3 M = Vector3.zero;
		Vector3 L = Vector3.zero;

		// This means M is at position 1 in the list
		if(vertexData[1].index == M_index)
		{
			M = vertexData [1].globalVertexPos;
			L = vertexData [2].globalVertexPos;

			h_M = vertexData [1].distance;
			h_L = vertexData [2].distance;
		} else {
			M = vertexData [2].globalVertexPos;
			L = vertexData [1].globalVertexPos;

			h_M = vertexData [2].distance;
			h_L = vertexData [1].distance;
		}

		// Now we can calculate where we should cut the triangle to form 2 new triangles
		// because the resutling area will always form a square

		// Point I_M
		Vector3 MH = H - M;
		float t_M = -h_M / (h_H - h_M);
		Vector3 MI_M = t_M * MH;
		Vector3 I_M = MI_M + M;

		// Point I_L
		Vector3 LH = H - L;
		float t_L = -h_L / (h_H - h_L);
		Vector3 LI_L = t_L * LH;
		Vector3 I_L = LI_L + L;

		// Save the data, normal, area etc
		// 2 triangles are below the water
		underWaterTriangleData.Add (new TriangleData(M, I_M, I_L));
		underWaterTriangleData.Add (new TriangleData(M, I_L, L));
	}

	// Build the new triangles where two of the old vertices are above water
	private void AddTrianglesTwoAboveWater(List<VertexData> vertexData)
	{
		// H and M are above water
		// H is after the vertix that is underwater, which is L
		// L is last in the sorted list
		Vector3 L = vertexData [2].globalVertexPos;

		// Find H
		int H_index = vertexData [2].index + 1;
		if (H_index > 2)
		{
			H_index = 0;
		}

		// We also need the heights to water
		float h_L = vertexData [2].distance;
		float h_M = 0f;
		float h_H = 0f;

		Vector3 M = Vector3.zero;
		Vector3 H = Vector3.zero;

		// This means H is at position 1 in the list
		if(vertexData[1].index == H_index)
		{
			M = vertexData [0].globalVertexPos;
			H = vertexData [1].globalVertexPos;

			h_M = vertexData [0].distance;
			h_H = vertexData [1].distance;
		} else {
			M = vertexData [1].globalVertexPos;
			H = vertexData [0].globalVertexPos;

			h_M = vertexData [1].distance;
			h_H = vertexData [0].distance;
		}

		// now cut
		// Point J_M
		Vector3 LM = M - L;
		float t_M = -h_L / (h_M - h_L);
		Vector3 LJ_M = t_M * LM;
		Vector4 J_M = LJ_M + L;

		// Point J_H
		Vector3 LH = H - L;
		float t_H = -h_L / (h_H - h_L);
		Vector3 LJ_H = t_H * LH;
		Vector3 J_H = LJ_H + L;

		// Save the data
		// 1 triangle below water
		underWaterTriangleData.Add (new TriangleData (L, J_H, J_M));
	}

	// Helper class to store triangle data
	private class VertexData
	{
		// The distance to water from this vertex
		public float distance;
		// An index to create clockwise triangles
		public int index;
		// The global Vector3 position of this vertex
		public Vector3 globalVertexPos;
	}

	// Display the underwater mesh
	public void DisplayMesh(Mesh mesh, string name, List<TriangleData> trianglesData)
	{
		List<Vector3> vertices = new List<Vector3> ();
		List<int> triangles = new List<int> ();

		// Build mesh
		for (int i = 0; i < trianglesData.Count; i++)
		{
			// From global to local
			Vector3 p1 = boatTrans.InverseTransformPoint (trianglesData [i].p1);
			Vector3 p2 = boatTrans.InverseTransformPoint (trianglesData [i].p2);
			Vector3 p3 = boatTrans.InverseTransformPoint (trianglesData [i].p3);

			vertices.Add (p1);
			triangles.Add (vertices.Count - 1);
			vertices.Add (p2);
			triangles.Add (vertices.Count - 1);
			vertices.Add (p3);
			triangles.Add (vertices.Count - 1);
		}

		// Remove old mesh
		mesh.Clear ();

		// give it a name
		mesh.name = name;

		// Add new vertices and triangles
		mesh.vertices = vertices.ToArray ();
		mesh.triangles = triangles.ToArray ();
		mesh.RecalculateBounds ();
	}
}
