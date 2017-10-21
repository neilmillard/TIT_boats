using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatPhysics : MonoBehaviour {

	// Drags
	public GameObject underWaterObj;

	// Script that's doing everything needed with the boat mesh, calc submerged, etc.
	private ModifyBoatMesh modifyBoatMesh;

	// Mesh for debugging
	private Mesh underWaterMesh;

	// The boats rigidbody
	private Rigidbody boatRB;

	// The density of the water
	private float rhoWater = 1027f;

	// Use this for initialization
	void Start () {
		// Get the boats rigidbody
		boatRB = gameObject.GetComponent<Rigidbody>();

		// Init the script to modify the boat mesh
		modifyBoatMesh = new ModifyBoatMesh (gameObject);

		// Meshes that are below and above water
		underWaterMesh = underWaterObj.GetComponent<MeshFilter> ().mesh;
	}
	
	// Update is called once per frame
	void Update () {
		// Generate the underwater mesh
		modifyBoatMesh.GenerateUnderwaterMesh ();

		// Display the underwater mesh
		modifyBoatMesh.DisplayMesh (underWaterMesh, "UnderWater Mesh", modifyBoatMesh.underWaterTriangleData);
	}

	void FixedUpdate()
	{
		// Add forces to the part of the boat that is under water
		if (modifyBoatMesh.underWaterTriangleData.Count >0)
		{
			AddUnderWaterForces ();
		}
	}

	// Add all the forces that act on the squares/triangles underwater
	void AddUnderWaterForces()
	{
		// Get all triangles
		List<TriangleData> underWaterTriangleData = modifyBoatMesh.underWaterTriangleData;

		for(int i = 0; i < underWaterTriangleData.Count; i++ )
		{
			// This triangle
			TriangleData triangleData = underWaterTriangleData [i];

			// Calc buoyancy force
			Vector3 buoyancyForce = BuoyancyForce (rhoWater, triangleData);

			// Add the force to the boat
			boatRB.AddForceAtPosition (buoyancyForce, triangleData.center);

			//Debug
			// Normal
			Debug.DrawRay (triangleData.center, triangleData.normal * 3f, Color.white);

			// Buoyance
			Debug.DrawRay (triangleData.center, buoyancyForce.normalized * 3f, Color.blue);
		}

	}

	// The buoyancy force
	private Vector3 BuoyancyForce(float rho, TriangleData triangleData)
	{
		// Buoyancy is a hydrostatic force.
		// F_buoyancy = rho * g * V
		// rho 	- density of the medium you are in
		// g 	- gravity
		// V	- volume of fluid displaced

		// V = z * S * n
		// Z 	- distance to the surface
		// S 	- surface area
		// n	- normal to the surface
		Vector3 buoyancyForce = rho * Physics.gravity.y * triangleData.distanceToSurface * triangleData.area * triangleData.normal;

		// The vertical component of the hydrostatic force don't cancel out, the horizontal force does
		buoyancyForce.x = 0f;
		buoyancyForce.z = 0f;

		return buoyancyForce;
	}
}
