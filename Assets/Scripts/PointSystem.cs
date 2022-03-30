using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

[System.Serializable]
public class PointSystem
{
	[SerializeField][Tooltip("How many points should a pillar type give")] PillarPoint[] PillarPoints;
	[SerializeField][Tooltip("Text object for total amount of points in a game")] Text TotalPointsText;
	[SerializeField][Tooltip("Text object for amount of points per bullet shot")] Text CurrentPointsText;
	[SerializeField][Tooltip("Text object for bonus for amount of pillars hit in a single shot")] Text NumPillarHitBonusText;
	[SerializeField][Tooltip("Text object for remaining simulated time")] Text TimeText;

	// Total gathered points this game
	private int CurrentTotalPoints;
	// Total gathered points for current bullet shot
	private int PointsBuffer;
	// Total bonus for amount of pillars per current bullet shot
	private int CurrentShotMultiplier;

	// Dictionary for optimized point lookup, a bit overkill but fun and easy to use
	private Dictionary<PillarType, int> PillarPointDict = new Dictionary<PillarType, int>();

	// Hold simulated time for displaying how much time is left in a given round
	private float SimulatedTime;

	// First-time initialization flag
	private bool Initialized;

	// Cached waiting time for points-counting routine
	private WaitForSecondsRealtime wait = new WaitForSecondsRealtime(.5f);

	public void Initialize(float RoundDuration)
	{
		// Perform and set first-time initialization
		if (!Initialized)
		{
			// setup point lookup-table
			for (int i = 0; i < PillarPoints.Length; i++)
			{
				PillarPoint pillarPoint = PillarPoints[i];
				PillarPointDict.Add(pillarPoint.PillarType, pillarPoint.PointWorth);
			}
			Initialized = true;
		}

		// setup variables and text
		SimulatedTime = RoundDuration;
		PointsBuffer = 0;
		CurrentShotMultiplier = 0;
		TotalPointsText.text = string.Format("Total score: {000}", CurrentTotalPoints);
		CurrentPointsText.text = string.Format("Current score: {000}", PointsBuffer);
		TimeText.text = string.Format("Time remaining for round: {0:00.00} seconds", SimulatedTime);
		NumPillarHitBonusText.text = string.Empty;
	}

	// Remaining simulated time getter
	public float SIMULATED_TIME => SimulatedTime;

	public void Update()
	{
		// Format and update text objects
		TotalPointsText.text = string.Format("Total score: {000}", CurrentTotalPoints);
		CurrentPointsText.text = string.Format("Current score: {000}", PointsBuffer);
		if (CurrentShotMultiplier > 0)
			NumPillarHitBonusText.text = string.Format("Pillars hit bonus: x{00}", CurrentShotMultiplier);
		else
			NumPillarHitBonusText.text = string.Empty;
		UpdateSimulatedTime();
	}

	public void AddPointsToBuffer(PillarType pillarType)
	{
		// If the supplied pillar type exists, increase points buffer and increase pillar-hit-multiplier
		if (PillarPointDict.TryGetValue(pillarType, out int point))
		{
			PointsBuffer += point;
			CurrentShotMultiplier++;
		}
	}

	public void AddPointsToTotal(MonoBehaviour caller)
	{
		// Bullet has been destroyed and camera is back to topdown view, enable point count
		caller.StartCoroutine(ICountUpPoints());
	}

	IEnumerator ICountUpPoints()
	{
		// Calculate multiplier bonus if applicable
		if (CurrentShotMultiplier > 0)
		{
			int Total = PointsBuffer * CurrentShotMultiplier;
			// Add multiplier bonus to buffer
			while (PointsBuffer < Total)
			{
				PointsBuffer++;
				yield return null;
			}
			CurrentShotMultiplier = 0;
			yield return wait;
		}
		// Multiplier bonus has been calculated and added to buffer, add pointsbuffer to total and reset pointsbuffer
		while (PointsBuffer > 0)
		{
			CurrentTotalPoints++;
			PointsBuffer--;
			yield return null;
		}
	}

	void UpdateSimulatedTime()
	{
		// Countdown remaining simulated time for round, based on scaled simulation time
		SimulatedTime -= Time.deltaTime * GameManager.INSTANCE.SIMULATION_SPEED;
		TimeText.text = string.Format("Time remaining for round: {0:00.00} seconds", SimulatedTime);
		// Keep time above zero to avoid negative-0 error
		if (SimulatedTime <= float.Epsilon)
			SimulatedTime = 0.0f;
	}
}

[System.Serializable]
public class PillarPoint
{
	[Tooltip("Which type of pillar is this")] public PillarType PillarType;
	[Tooltip("How many points is this pillar worth")] public int PointWorth;
}