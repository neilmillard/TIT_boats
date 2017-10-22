using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterController : MonoBehaviour {

	public static WaterController current;

	public bool isMoving;

	// Wave height and speed
	public float scale = 0.1f;
	public float speed = 1.0f;
	// Width between waves
	public float waveDistance = 1f;
	// Noise
	public float noiseStrength = 1f;
	public float noiseWalk = 1f;

	// Use this for initialization
	void Start () {
		current = this;
		isMoving = true;
	}
	
	// Get the y coord from whatever wavetype is used
	public float GetWaveYPos (Vector3 position, float timeSinceStart) 
	{
		if (isMoving)
		{
			return WaveTypes.SinXWave (position, speed, scale, waveDistance, noiseStrength, noiseWalk, timeSinceStart);
		} else {
			return 0f;
		}	
	}

	// Find the distance to the water from a vertex
	// Ensure position is global coords
	// +ve is above water
	// -ve is below water
	public float DistanceToWater(Vector3 position, float timeSinceStart)
	{
		float waterHeight = GetWaveYPos (position, timeSinceStart);
		float distanceToWater = position.y - waterHeight;
		return distanceToWater;
	}
}
