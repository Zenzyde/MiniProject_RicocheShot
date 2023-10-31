using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Get enough total style points -> make up for any missed "stars" needed to get to next "round"
// Objectives hit text
// Hidden total points text for game
// Possibly hidden total rounds lasted
// Possibly hidden total "stars" earned
// Visible total points per round, resets each round
// Bullets remaining text
// Extra objective slider

// Points additions system ideas:
// 1: Queue-system
// Each shot is saved with its own buffer and multiplier, which is reset text-wise in-between each shot, and then added together to total at the end of a round
// 2: 

// Represent score text as:
// * Current shot score: #1(pillar points) x#2(num pillars hit)
// * Current round score: #1(total of shot score - queue-system)
// * Extra objective earned: determined by current round score
[System.Serializable]
public class PointSystem
{
	[SerializeField][Tooltip("How many points should a pillar type give")] PillarPoint[] PillarPoints;
	[SerializeField] Text NumObjectivePillarsHitText;
	[SerializeField][Tooltip("Text object for total amount of points in a game")] Text TotalPointsGainedInGameText;
	[SerializeField][Tooltip("Text object for amount of points per bullet shot")] Text TotalPointsInRoundText;
	[SerializeField] Text BulletsRemainingText;
	[SerializeField] Text RoundEndStatusText;
	[SerializeField] Text ExtraObjectivesEarnedText;
	[SerializeField] Text CurrentBulletScoreText;
	[SerializeField] GameObject RoundEndPanel;
	[SerializeField] GameObject CurrentBulletScorePanel;
	[SerializeField] Vector2 CurrentBulletScoreOffset;

	public int ExtraObjectivePointsGatheredForRound { get; private set; } = 0;

	// Total gathered points this game
	private int TotalPointsForGame;
	// Total gathered points for current bullet shot
	private int PointsBuffer;
	// Total gathered points for current round
	private int TotalPointsForRound;
	// Total bonus for amount of pillars per current bullet shot
	private float CurrentShotMultiplier = 1;
	// Current round tracker
	private int CurrentRound;
	private float PointsNeededForExtraObjective = 1;

	// Dictionary for optimized point lookup, a bit overkill but fun and easy to use
	private Dictionary<PillarType, int> PillarPointDict = new Dictionary<PillarType, int>();

	// First-time initialization flag
	private bool Initialized;

	// Cached waiting time for points-counting routine
	private WaitForSecondsRealtime wait = new WaitForSecondsRealtime(.15f);

	private Queue<float> PointBufferQueue = new Queue<float>();

	public void Initialize(int numBullets, int numObjectivePillars)
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

			// Initialize first round to -1 because "CalculateNextPointLimit" increases "CurrentRound" automatically
			CurrentRound = 0;

			// Set initialized flag, we should not re-initialize these values
			Initialized = true;
		}

		// setup variables and text
		PointsBuffer = 0;
		CurrentShotMultiplier = 1;
		TotalPointsGainedInGameText.gameObject.SetActive(false);
		TotalPointsInRoundText.text = string.Format("Current score: {0}", PointsBuffer);
		NumObjectivePillarsHitText.text = string.Format("Targets hit: {0}/{1}", 0, numObjectivePillars);

		BulletsRemainingText.text = string.Format("Bullets remaining: x{0}", numBullets);
		ExtraObjectivesEarnedText.gameObject.SetActive(false);
		ExtraObjectivePointsGatheredForRound = 0;
		RoundEndStatusText.gameObject.SetActive(false);
		RoundEndPanel.SetActive(false);
		CurrentBulletScorePanel.SetActive(false);
		TotalPointsForRound = 0;

		// Calculate point limit for new round
		CalculateNextPointLimit();
	}

	public void Update()
	{
		if (GameManager.INSTANCE.AllowGameToRun)
			FormatText();
	}

	public void AddPointsToBuffer(PillarType pillarType)
	{
		// If the supplied pillar type exists, increase points buffer and increase pillar-hit-multiplier
		if (PillarPointDict.TryGetValue(pillarType, out int point))
		{
			PointsBuffer += point;
			if (point > 0)
				CurrentShotMultiplier += 1f;
		}
	}

	public void ResetShotMultiplier()
	{
		PointBufferQueue.Enqueue(PointsBuffer * CurrentShotMultiplier);
		TotalPointsForRound += (int) (PointsBuffer * CurrentShotMultiplier);
		// PointsBuffer = (int) (PointsBuffer * CurrentShotMultiplier);
		PointsBuffer = 0;
		CurrentShotMultiplier = 1;
	}

	public void AddPointsToTotal(MonoBehaviour caller)
	{
		// Bullet has been destroyed and camera is back to topdown view, enable point count
		// caller.StartCoroutine(ICountUpPoints());

		// Add pointsbuffer to total and reset pointsbuffer
		// int extraPointEarned = 0;
		// while (PointsBuffer > 0)
		// {
		// 	TotalPointsForGame++;
		// 	PointsBuffer--;
		// 	if (extraPointEarned >= PointsNeededForExtraObjective)
		// 	{
		// 		ExtraObjectivePointsGatheredForRound++;
		// 		ExtraObjectivesEarnedText.gameObject.SetActive(true);
		// 		ExtraObjectivesEarnedText.text = string.Format("Extra targets earned: {0}", ExtraObjectivePointsGatheredForRound);
		// 		extraPointEarned = 0;
		// 	}
		// 	else
		// 	{
		// 		extraPointEarned++;
		// 	}
		// }

		int extraPointEarned = 0;
		while (PointBufferQueue.Count > 0)
		{
			float score = PointBufferQueue.Dequeue();
			TotalPointsForGame += (int) score;
			if (extraPointEarned >= PointsNeededForExtraObjective)
			{
				ExtraObjectivePointsGatheredForRound++;
				ExtraObjectivesEarnedText.gameObject.SetActive(true);
				ExtraObjectivesEarnedText.text = string.Format("Extra targets earned: {0}", ExtraObjectivePointsGatheredForRound);
				extraPointEarned = 0;
			}
			else
			{
				extraPointEarned += (int) score;
			}
		}
	}

	IEnumerator ICountUpPoints()
	{
		// Add pointsbuffer to total and reset pointsbuffer
		int extraPointEarned = 0;
		while (PointsBuffer > 0)
		{
			TotalPointsForGame++;
			PointsBuffer--;
			extraPointEarned++;
			if (extraPointEarned >= PointsNeededForExtraObjective)
			{
				ExtraObjectivePointsGatheredForRound++;
				ExtraObjectivesEarnedText.text = string.Format("Extra targets earned: {0}", ExtraObjectivePointsGatheredForRound);
				extraPointEarned = 0;
			}
			yield return null;
		}
	}

	void CalculateNextPointLimit()
	{
		CurrentRound++;
		PointsNeededForExtraObjective = 75 * Mathf.CeilToInt(CurrentRound * 1.2f);
	}

	void FormatText()
	{
		int bullets = GameManager.INSTANCE.NumRemainingBullets;
		int objectivesHit = GameManager.INSTANCE.NumObjectivePillarsToSpawn - GameManager.INSTANCE.NumRemainingObjectivePillars;
		// Handle end of round/game over cases and show text depending on win or loss
		if (GameManager.INSTANCE.IsPaused)
		{
			RoundEndPanel.SetActive(true);
			CurrentBulletScorePanel.SetActive(false);
			TotalPointsGainedInGameText.gameObject.SetActive(true);
			RoundEndStatusText.gameObject.SetActive(true);
			if (!GameManager.INSTANCE.PlayerPassedRound())
			{
				TotalPointsGainedInGameText.color = Color.red;
				TotalPointsGainedInGameText.text = string.Format("Round {0}. Total score: {1}", CurrentRound, TotalPointsForGame);
				RoundEndStatusText.color = Color.red;
				RoundEndStatusText.text = string.Format("You missed some pillars - Game over.\nExiting game in: {0:0.00}", GameManager.INSTANCE.RoundPauseTime);
			}
			else
			{
				TotalPointsGainedInGameText.color = Color.green;
				TotalPointsGainedInGameText.text = string.Format("Round {0}. Total score: {1}", CurrentRound, TotalPointsForGame);
				RoundEndStatusText.color = Color.green;
				RoundEndStatusText.text = string.Format("Round passed!\nNext Round in: {0:0.00}", GameManager.INSTANCE.RoundPauseTime);
			}
			NumObjectivePillarsHitText.text = string.Format("Targets hit: {0}/{1}", objectivesHit, GameManager.INSTANCE.NumObjectivePillarsToSpawn);
			TotalPointsInRoundText.text = string.Format("Current score: {0}", TotalPointsForRound);
			BulletsRemainingText.text = string.Format("Bullets remaining: x{0}", bullets);
			CurrentBulletScoreText.text = string.Empty;
		}
		else
		{
			RoundEndPanel.SetActive(false);
			CurrentBulletScorePanel.SetActive(true);
			// Format and update generic text objects
			TotalPointsGainedInGameText.gameObject.SetActive(false);
			RoundEndStatusText.gameObject.SetActive(false);
			NumObjectivePillarsHitText.text = string.Format("Targets hit: {0}/{1}", objectivesHit, GameManager.INSTANCE.NumObjectivePillarsToSpawn);
			BulletsRemainingText.text = string.Format("Bullets remaining: x{0}", bullets);
			TotalPointsInRoundText.text = string.Format("Current score: {0}", TotalPointsForRound);
			CurrentBulletScoreText.text = string.Format("Bullet Score: {0}\nUnique hit bonus: x{1}", PointsBuffer, CurrentShotMultiplier);
			CurrentBulletScorePanel.transform.position = GameManager.INSTANCE.GetBulletScreenPosition() + CurrentBulletScoreOffset;
		}
	}
}

[System.Serializable]
public class PillarPoint
{
	[Tooltip("Which type of pillar is this")] public PillarType PillarType;
	[Tooltip("How many points is this pillar worth")] public int PointWorth;
}