using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PillarSpawner_TopDown : MonoBehaviour
{
	[SerializeField][Tooltip("Minimum radius between pillars")] private float MinRadius = 1;
	[SerializeField][Tooltip("Maximum radius between pillars")] private float MaxRadius = 1.5f;
	[SerializeField][Tooltip("How big of an area are pillars allowed to spawn in")] private Vector2 RegionSize = Vector2.one;
	[SerializeField][Tooltip("How many times to try and spawn a pillar before trying another position")] private int RejectionSamples = 20;
	[SerializeField][Tooltip("Gizmo sphere radius")] private float DisplayRadius = 1;
	[SerializeField][Tooltip("Where should the spawnarea start")] private Vector3 RegionOffset = Vector3.one;
	[SerializeField][Tooltip("Basic pillar object to spawn")] private GameObject PoleObject;
	[SerializeField][Tooltip("Special pillar object to spawn")] private GameObject TargetPoleObject;
	[SerializeField] private GameObject PillarObstacle;
	[SerializeField] private int MaxNumObstacles;

	// List of sampled points to spawn pillars at
	private List<Vector2> points;
	// List of currently active pillars, used for regenerating pillars when a round is over
	private List<GameObject> currentPillars = new List<GameObject>();
	private List<GameObject> targetPillars = new List<GameObject>();
	// Cached list of possible rotations for pillars
	private float[] yRotations = new float[] { 0, 22.5f, 45, 67.5f };

	List<GameObject> currentObstacles = new List<GameObject>();

#if UNITY_EDITOR
	void OnValidate()
	{
		// Call to get the sampled position for debugging/visualizing in editor
		points = PoissonDiscSampler.SamplePoissonDiscPositions(MinRadius, MaxRadius, RegionSize, RejectionSamples);
	}

	void OnDrawGizmos()
	{
		// Draw spawning/sampling area
		Gizmos.DrawWireCube(new Vector3(RegionOffset.x + RegionSize.x / 2, RegionOffset.y, RegionOffset.z + RegionSize.y / 2), new Vector3(RegionSize.x, 0, RegionSize.y));
		if (points.Count > 0)
		{
			// Draw each sampled/spawning point
			foreach (Vector2 point in points)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawSphere(new Vector3(RegionOffset.x + point.x, RegionOffset.y, RegionOffset.z + point.y), DisplayRadius);
			}
		}
	}
#endif

	void Start()
	{
		// Generate first set of pillars at game start
		GeneratePillars();
		GeneratePillarObstacles();
	}

	public void GeneratePillarObstacles()
	{
		if (currentObstacles.Count > 0)
		{
			for (int i = currentObstacles.Count - 1; i >= 0; i--)
			{
				Destroy(currentObstacles[i]);
			}
			currentObstacles.Clear();
		}

#if UNITY_STANDALONE
		// Assuming not in editor, sample points for spawning
		points = PoissonDiscSampler.SamplePoissonDiscPositions(MinRadius, MaxRadius, RegionSize, RejectionSamples);
#endif

		if (points.Count > 0)
		{
			int currentNumObstacles = 0;

			// Go through list of positions and spawn pillars
			foreach (Vector2 point in points)
			{
				Vector3 pointPos = new Vector3(RegionOffset.x + point.x, 0, RegionOffset.z + point.y);
				Quaternion rotation = Quaternion.Euler(0, yRotations[Random.Range(0, yRotations.Length)], 0);
				bool spawnObstacle = Random.value >.9f;
				if (spawnObstacle && currentNumObstacles < MaxNumObstacles)
				{
					GameObject obstacle = Instantiate(PillarObstacle, new Vector3(RegionOffset.x + point.x, RegionOffset.y, RegionOffset.z + point.y), rotation);
					currentObstacles.Add(obstacle);
					currentNumObstacles++;
				}
			}
		}
	}

	public void GeneratePillars()
	{
		// If we've already generated a set of pillars, remov them and generate new ones
		if (currentPillars.Count > 0)
		{
			for (int i = 0; i < currentPillars.Count; i++)
			{
				Destroy(currentPillars[i]);
			}
			currentPillars.Clear();
			targetPillars.Clear();
		}

#if UNITY_STANDALONE
		// Assuming not in editor, sample points for spawning
		points = PoissonDiscSampler.SamplePoissonDiscPositions(MinRadius, MaxRadius, RegionSize, RejectionSamples);
#endif
		if (points.Count > 0)
		{
			// Go through list of positions and spawn pillars
			foreach (Vector2 point in points)
			{
				// Vector3 pointPos = new Vector3(RegionOffset.x + point.x, 0, RegionOffset.z + point.y);
				Quaternion rotation = Quaternion.Euler(0, yRotations[Random.Range(0, yRotations.Length)], 0);
				bool spawnTarget = Random.value >.9f && targetPillars.Count < GameManager.INSTANCE.NumObjectivePillarsToSpawn;
				if (spawnTarget)
				{
					GameObject target = Instantiate(TargetPoleObject, new Vector3(RegionOffset.x + point.x, RegionOffset.y, RegionOffset.z + point.y), rotation);
					currentPillars.Add(target);
					targetPillars.Add(target);
				}
				else
				{
					GameObject pillar = Instantiate(PoleObject, new Vector3(RegionOffset.x + point.x, RegionOffset.y, RegionOffset.z + point.y), rotation);
					currentPillars.Add(pillar);
				}
			}
		}
	}
}