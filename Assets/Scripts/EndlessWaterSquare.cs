using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

// Create endless water system, this is one WaterSquare (with the boat in it) with 8 squares around it.
public class EndlessWaterSquare : MonoBehaviour {

	// The object the water will follow
	public GameObject boatObj;
	// The 1 watersquare
	public GameObject waterSqrObj;

	// WaterSquare data
	private float squareWidth = 800f;
	private float innerSquareResolution = 5f;
	private float outerSquareResolution = 25f;

	// List with all the water mesh squares == the entire ocean we can see
	List<WaterSquare> waterSquares = new List<WaterSquare> ();

	// Var for thread
	float secondSinceStart;
	// position of boat
	Vector3 boatPos;
	// position of the ocean, not the same as boat as it follows the resolution of the smallest water square
	Vector3 oceanPos;
	// Has the thread finished
	bool hasThreadUpdatedWater;

	// Use this for initialization
	void Start () {
		// Create the Sea
		CreateEndlessSea ();

		// Init the time
		secondSinceStart = Time.time;

		// Update the water in the thread
		ThreadPool.QueueUserWorkItem (new WaitCallback (UpdateWaterWithThreadPooling));

		// Start the coroutine
		StartCoroutine (UpdateWater ());
	}
	
	// Update is called once per frame
	void Update () {

		// Shader Code
		//UpdateWaterNoThread ();

		// Update as often as possible as we don't know when the thread will run
		secondSinceStart = Time.time;

		// Update pos of boat
		boatPos = boatObj.transform.position;

	}

	// Update the water with no thread to compare
	void UpdateWaterNoThread()
	{
		// Update pos of boat
		boatPos = boatObj.transform.position;

		MoveWaterToBoat ();

		// Add the new position of the ocean to the transform
		transform.position = oceanPos;

		// Update the vertices
		for (int i = 0; i < waterSquares.Count; i++)
		{
			waterSquares [i].MoveSea (oceanPos, Time.time);
		}
	}

	// The loop that gives the update vertices from the thread to the meshes
	IEnumerator UpdateWater()
	{
		while(true)
		{
			// Has the thread finished updating the water?
			if (hasThreadUpdatedWater)
			{
				// Move water to the boat
				transform.position = oceanPos;

				// Add the update vertices
				for (int i = 0; i < waterSquares.Count; i++)
				{
					waterSquares[i].terrainMeshFilter.mesh.vertices = waterSquares[i].vertices;
					waterSquares[i].terrainMeshFilter.mesh.RecalculateNormals ();
				}

				// Stop looping until water updated in thread
				hasThreadUpdatedWater = false;

				// Update the water in the thread
				ThreadPool.QueueUserWorkItem (new WaitCallback (UpdateWaterWithThreadPooling));
			}

			// dont need to update water every frame
			yield return new WaitForSeconds (Time.deltaTime * 3f);
		}
	}

	// The thread that updates the water vertices
	void UpdateWaterWithThreadPooling(object state){
		MoveWaterToBoat ();

		// Loop through all water squares
		for (int i = 0; i < waterSquares.Count; i++)
		{
			// The local center pos of this square
			Vector3 centerPos = waterSquares[i].centerPos;
			// All the vertices in this square
			Vector3[] vertices = waterSquares[i].vertices;

			// Update the vertices
			for (int j = 0; j < vertices.Length; j++)
			{
				// Local pos of vertex
				Vector3 vertexPos = vertices [j];

				// Cant use transformpoint in a thread, so we just add the position of the ocean and water square
				// rotation is always 0 and scale always 1
				Vector3 vertexPosGlobal = vertexPos + centerPos + oceanPos;

				// Get the water height
				vertexPos.y = WaterController.current.GetWaveYPos (vertexPosGlobal, secondSinceStart);

				// Save the new y coord, x and z still local
				vertices[j] = vertexPos;
			}
		}

		hasThreadUpdatedWater = true;

	}

	// Move endless water to boat's position in steps that is the same as the water's resolution
	void MoveWaterToBoat()
	{
		// Roudn to nearest
		float x = innerSquareResolution * (int)Mathf.Round (boatPos.x / innerSquareResolution);
		float z = innerSquareResolution * (int)Mathf.Round (boatPos.z / innerSquareResolution);
		// Should we move?
		if (oceanPos.x != x || oceanPos.z != z)
		{
			// move the sea
			oceanPos = new Vector3 (x, oceanPos.y, z);
		}
	}

	// Init the endless sea
	void CreateEndlessSea()
	{
		// The center piece
		AddWaterPlane (0f, 0f, 0f, squareWidth, innerSquareResolution);

		// The 8 squares around the center
		for (int x = -1; x <=1; x+=1)
		{
			for (int z = -1; z <= 1; z+=1)
			{
				// Ignore center
				if (x == 0 && z == 0)
					continue;

				// The y-pos should be lower to avoid ugly seam
				float yPos = -0.5f;
				AddWaterPlane (x * squareWidth, z * squareWidth, yPos, squareWidth, outerSquareResolution);
			}
		}
	}

	// Add one water plane
	void AddWaterPlane(float xCoord, float zCoord, float yCoord, float squareWidth, float spacing)
	{
		GameObject waterPlane = Instantiate (waterSqrObj, transform.position, transform.rotation) as GameObject;

		waterPlane.SetActive (true);

		// Update position
		Vector3 centerPos = transform.position;
		centerPos.x += xCoord;
		centerPos.y = yCoord;
		centerPos.z += zCoord;

		waterPlane.transform.position = centerPos;

		// Parent it
		waterPlane.transform.parent = transform;

		// Give it moving water
		//WaterSquare newWaterSquare = new WaterSquare (waterPlane, squareWidth, spacing);
		WaterSquare newWaterSquare = waterPlane.AddComponent<WaterSquare>();

		newWaterSquare.Init(waterPlane, squareWidth, spacing);

		waterSquares.Add (newWaterSquare);
	}
}
