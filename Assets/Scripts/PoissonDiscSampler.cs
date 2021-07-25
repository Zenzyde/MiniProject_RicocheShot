using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Written based on this video by Sebastian Lague: https://www.youtube.com/watch?v=7WcmyxyFO7o
public static class PoissonDiscSampler
{
	/// <summary>
	/// Radius => minimum radius between positions,
	/// SampleRegionSize => the available area to work with,
	/// NumSamplesBeforeRejection => number of attempts for placing individual position
	/// </summary>
	/// <param name="radius"></param>
	/// <param name="sampleRegionSize"></param>
	/// <param name="numSamplesBeforeRejection"></param>
	/// <returns></returns>
	public static List<Vector2> SamplePoissonDiscPositions(float radius, Vector2 sampleRegionSize, int numSamplesBeforeRejection = 20)
	{
		//Size of a single cell in the grid-area available
		float cellSize = radius / Mathf.Sqrt(2);

		//The available grid-area where positions can be placed.
		//SampleRegionSize / CellSize => amount of rows and columns in the grid
		int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];

		//The final list of positions to be returned
		List<Vector2> points = new List<Vector2>();

		//List of temporary, possible positions to be synced to "points" list later
		List<Vector2> spawnPoints = new List<Vector2>();

		//First entry, which happens to be the middle of the sample region, don't know how important this is
		spawnPoints.Add(sampleRegionSize / 2);

		//While there are still temporary, possible positions remaining to check/verify for sync
		while (spawnPoints.Count > 0)
		{
			//Get random possible position
			int spawnIndex = Random.Range(0, spawnPoints.Count);
			Vector2 spawnCenter = spawnPoints[spawnIndex];

			//Prepare check for accepted possible position
			bool candidateAccepted = false;

			//Try for X-amount of attempts to successfully place the possible position
			for (int i = 0; i < numSamplesBeforeRejection; i++)
			{
				//Get a random angle for a direction in polar coords
				float angle = Random.value * Mathf.PI * 2;
				//Get a direction from the angle for the position of the possible position
				Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				//Calculate the possible/candidate position from the direction
				Vector2 candidate = spawnCenter + direction * Random.Range(radius, radius * 2);
				//Check that the candidate/possible position is acceptable
				if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid))
				{
					//Add the accepted position to the final list
					points.Add(candidate);
					//Add the accepted position to the temporary list
					spawnPoints.Add(candidate);
					//Mark the position in the grid with the length of the final list to distinguish claimed & non-claimed positions
					//(int)Candidate/CellSize => probably converts the candidate position to the grid-format
					grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
					//Set that a candidate has been accepted & break out of the for-loop
					candidateAccepted = true;
					break;
				}
			}

			//If the candidate wasn't accepted, remove it from the temporary list
			if (!candidateAccepted)
			{
				spawnPoints.RemoveAt(spawnIndex);
			}
		}
		return points;
	}

	/// <summary>
	/// MinRadius => minimum radius between positions,
	/// MaxRadius => maximum radius between positions,
	/// SampleRegionSize => the available area to work with,
	/// NumSamplesBeforeRejection => number of attempts for placing individual position
	/// </summary>
	/// <param name="radius"></param>
	/// <param name="sampleRegionSize"></param>
	/// <param name="numSamplesBeforeRejection"></param>
	/// <returns></returns>
	public static List<Vector2> SamplePoissonDiscPositions(float minRadius, float maxRadius, Vector2 sampleRegionSize, int numSamplesBeforeRejection = 20)
	{
		//Size of a single cell in the grid-area available
		float cellSize = minRadius / Mathf.Sqrt(2);

		//The available grid-area where positions can be placed.
		//SampleRegionSize / CellSize => amount of rows and columns in the grid
		int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];

		//The final list of positions to be returned
		List<Vector2> points = new List<Vector2>();

		//List of temporary, possible positions to be synced to "points" list later
		List<Vector2> spawnPoints = new List<Vector2>();

		//First entry, which happens to be the middle of the sample region, don't know how important this is
		spawnPoints.Add(sampleRegionSize / 2);

		//While there are still temporary, possible positions remaining to check/verify for sync
		while (spawnPoints.Count > 0)
		{
			//Get random possible position
			int spawnIndex = Random.Range(0, spawnPoints.Count);
			Vector2 spawnCenter = spawnPoints[spawnIndex];

			//Prepare check for accepted possible position
			bool candidateAccepted = false;

			//Try for X-amount of attempts to successfully place the possible position
			for (int i = 0; i < numSamplesBeforeRejection; i++)
			{
				//Get a random angle for a direction in polar coords
				float angle = Random.value * Mathf.PI * 2;
				//Get a direction from the angle for the position of the possible position
				Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				//Calculate the possible/candidate position from the direction
				float radius = Random.Range(minRadius, maxRadius);
				Vector2 candidate = spawnCenter + direction * Random.Range(radius, radius * 2);
				//Check that the candidate/possible position is acceptable
				if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid))
				{
					//Add the accepted position to the final list
					points.Add(candidate);
					//Add the accepted position to the temporary list
					spawnPoints.Add(candidate);
					//Mark the position in the grid with the length of the final list to distinguish claimed & non-claimed positions
					//(int)Candidate/CellSize => probably converts the candidate position to the grid-format
					grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
					//Set that a candidate has been accepted & break out of the for-loop
					candidateAccepted = true;
					break;
				}
			}

			//If the candidate wasn't accepted, remove it from the temporary list
			if (!candidateAccepted)
			{
				spawnPoints.RemoveAt(spawnIndex);
			}
		}
		return points;
	}

	static bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius, List<Vector2> points, int[,] grid)
	{
		//Check that the candidate is within the available region area
		if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
		{
			//Get the Cell positions in grid-format probably
			int cellX = (int)(candidate.x / cellSize);
			int cellY = (int)(candidate.y / cellSize);

			//Calculate start & end search positions around the candidate
			//0, Cell +- 2 & Length - 1 => probably making sure to never go onto the candidate or go outside the available region area
			int searchStartX = Mathf.Max(0, cellX - 2);
			int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
			int searchStartY = Mathf.Max(0, cellY - 2);
			int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

			//Loop through & check the smaller area around the candidate in the given radius
			for (int x = searchStartX; x <= searchEndX; x++)
			{
				for (int y = searchStartY; y <= searchEndY; y++)
				{
					//Get the index of the current position in the grid
					int pointIndex = grid[x, y] - 1;
					//Check if the current position is occupied (i think?)
					if (pointIndex != -1)
					{
						//Calculate the distance from the candidate to the current neighbouring cell
						float squareDistance = (candidate - points[pointIndex]).sqrMagnitude;
						//If current neighbour cell is too close and is occupied, it's invalid
						if (squareDistance < radius * radius)
						{
							return false;
						}
					}
				}
			}
			//No neighbouring cells occupied, candidate position is valid
			return true;
		}
		//Candidate position is outside the available region area somehow, not valid
		return false;
	}
}
