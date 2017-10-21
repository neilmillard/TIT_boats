using System.Collections;
using UnityEngine;

// A struct to have parameters for function methods
public struct TriangleData {
	// The corners of this triangle in global coords
	public Vector3 p1;
	public Vector3 p2;
	public Vector3 p3;

	// The center of the triangle
	public Vector3 center;

	// The distance to the surface from the center of the triangle
	public float distanceToSurface;

	// The normal
	public Vector3 normal;

	// The area
	public float area;

	public TriangleData(Vector3 p1, Vector3 p2, Vector3 p3)
	{
		this.p1 = p1;
		this.p2 = p2;
		this.p3 = p3;

		// Calc center
		this.center = (p1 + p2 + p3) / 3f;

		// Distance to the surface from the center
		this.distanceToSurface = Mathf.Abs (WaterController.current.DistanceToWater (this.center, Time.time));

		// the Normal
		this.normal = Vector3.Cross (p2 - p1, p3 - p1).normalized;

		// Area of the triangle
		float a = Vector3.Distance (p1, p2);
		float c = Vector3.Distance (p3, p1);
		this.area = (a * c * Mathf.Sin (Vector3.Angle (p2 - p1, p3 - p1) * Mathf.Deg2Rad)) / 2f;
	}
}
