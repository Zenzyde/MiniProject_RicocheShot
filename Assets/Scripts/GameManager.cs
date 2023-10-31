using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	[SerializeField][Header("Point system settings")] PointSystem PointSystem;
	[SerializeField][Tooltip("Pillar spawner instance")] PillarSpawner_TopDown PillarSpawner;
	[SerializeField] int MaxNumBullets;
	[SerializeField] int MaxNumObjectivePillars;
	[SerializeField] float RoundPauseDuration;

	// Singleton-instance
	private static GameManager instance;
	public static GameManager INSTANCE { get { return instance; } }

	// Event to notify systems when a shot is fired by 'ShotController'
	public delegate void OnShotRegistered(RaycastHit hit, List<Vector3> waypoints);
	public static event OnShotRegistered OnShotRegisteredEvent;

	// Property which enables player to shoot bullet and enables camera to follow bullet correctly
	public bool ShootingRestricted { get; private set; }
	// Flags which enables game systems as soon as initial camera intro sequence is done
	public bool IsPaused { get; private set; }
	public bool AllowGameToRun { get; private set; }
	// Flag to indicate that pillars are currently being destroyed/set inactive
	private bool DestroyingPillars;

	// Cached current bullet instance property
	public Bullet CurrentBullet { get; private set; }

	public int NumRemainingBullets { get; private set; }

	public int NumRemainingObjectivePillars { get; private set; }
	public int NumObjectivePillarsToSpawn { get; private set; }

	public float RoundPauseTime { get; private set; }
	public float GameQuitTime { get; private set; }

	// List of pillars to destroy/deactivate, provided by bullet
	private List<Pillar> PillarsToDestroy = new List<Pillar>();

	// Cached wait-instance for pillar-destroying sequence
	private WaitForSecondsRealtime wait = new WaitForSecondsRealtime(.15f);

	// Scaling sequence tracker, for canceling any active routine and restoring time if bullet goes rogue
	private Coroutine ActiveScalingRoutine;

	// Start is called before the first frame update
	void Awake()
	{
		if (instance == null)
		{
			// Initialize basic settings and pointsystem
			Cursor.lockState = CursorLockMode.Confined;
			instance = this;
			PointSystem.Initialize(MaxNumBullets, MaxNumObjectivePillars);
			ShootingRestricted = true;
			NumRemainingBullets = MaxNumBullets;
			NumRemainingObjectivePillars = MaxNumObjectivePillars;
			NumObjectivePillarsToSpawn = MaxNumObjectivePillars;
		}
	}

	void Update()
	{
		// Quit game if desired
		if (Input.GetKeyDown(KeyCode.Escape))
			QuitGame(true);

		// If intro camera sequence is not done, exit
		if (!AllowGameToRun)
			return;

		// Update pointsystem
		PointSystem.Update();
	}

	// Camera intro is done, allow game to run
	public void StartGame()
	{
		AllowGameToRun = true;
		IsPaused = false;
	}

	// Call to intialize a bullet and let camera follow bullet
	public void FireShotEvent(RaycastHit hit, List<Vector3> waypoints)
	{
		OnShotRegisteredEvent?.Invoke(hit, waypoints);
	}

	// Assign a new bullet instance to the game systems
	public void AssignNewBullet(Bullet bullet)
	{
		NumRemainingBullets--;
		CurrentBullet = bullet;
	}

	// Destroy the current bullet instance, called when bullet is done bullet-bouncing
	void DestroyBullet() => Destroy(CurrentBullet.gameObject);

	// Called by camera after bullet is done bullet-bouncing to allow player to see pillars sink
	public void DestroyHits()
	{
		if (DestroyingPillars)
			return;

		// Set flag to make sure pillars only sink once before round reset
		DestroyingPillars = true;

		StartCoroutine(IDestroyHits());
	}

	IEnumerator IDestroyHits()
	{
		// Loop through and call sink on each pillar
		for (int i = 0; i < PillarsToDestroy.Count; i++)
		{
			Pillar pillar = PillarsToDestroy[i];
			if (pillar.PillarType == PillarType.Target && NumRemainingObjectivePillars > 0)
			{
				NumRemainingObjectivePillars--;
			}
			if (pillar.gameObject.activeInHierarchy)
				pillar.Sink();
			yield return wait;
		}

		if (NumRemainingBullets <= 0 || NumRemainingObjectivePillars <= 0)
		{
			//! Not working correctly currently, system gets stuck because it's still processing sequentially -- if the extra points for the round is a big number it takes time to countdown and therefore a long time before next round starts!

			// Notify pointsystem to add point buffer to total
			PointSystem.AddPointsToTotal(this);

			// Initiate next round if time has run out and player passed round
			if (PlayerPassedRound())
				EndRound();
			// End game if time ran out and player did not pass round
			else
				QuitGame(false);
		}

		// Clear list for next round/shot
		PillarsToDestroy.Clear();

		// Unset flag to allow sequence to be run after next shot/round
		DestroyingPillars = false;
	}

	// Called by bullet when colliding with pillar to update pointsbuffer
	public void AddPointsToBuffer(PillarType pillarType) => PointSystem.AddPointsToBuffer(pillarType);

	public void ResetShotMultiplier() => PointSystem.ResetShotMultiplier();

	// Restricts player from shooting a new bullet until the bullet has been destroyed
	// Also controls whether camera follows bullet or not
	public void SetShootingRestriction(bool state)
	{
		// Set restriction flag
		ShootingRestricted = state;
		if (!ShootingRestricted && CurrentBullet != null)
		{
			// Add hit pillars for deactivation
			PillarsToDestroy.AddRange(CurrentBullet.HitPillars);
			// Destroy bullet instance
			DestroyBullet();
			// Destroy hit targets
			DestroyHits();
		}
	}

	void SetPauseState(bool state, float pauseDuration = 0, System.Action callback = null)
	{
		if (pauseDuration > 0)
			StartCoroutine(ISetPauseState(state, pauseDuration, callback));
		else
		{
			IsPaused = state;
		}
	}

	IEnumerator ISetPauseState(bool state, float duration, System.Action callback)
	{
		IsPaused = state;
		RoundPauseTime = duration;
		while (RoundPauseTime > 0.0f)
		{
			yield return null;
			RoundPauseTime -= Time.deltaTime;
		}
		RoundPauseTime = 0.0f;
		callback.Invoke();
		IsPaused = !state;
	}

	// Has player passed and gets to advance to next round?
	public bool PlayerPassedRound() => NumRemainingObjectivePillars <= 0 || NumRemainingObjectivePillars - PointSystem.ExtraObjectivePointsGatheredForRound <= 0;

	// Called when time has run out for a round by point system
	public void EndRound()
	{
		SetPauseState(true, RoundPauseDuration, () =>
		{
			// Call pillar spawner to regenerate pillars
			PillarSpawner.GeneratePillars();

			// Reinitialize point system
			PointSystem.Initialize(MaxNumBullets, MaxNumObjectivePillars);

			// Reset bullets & pillars
			NumRemainingBullets = MaxNumBullets;
			NumRemainingObjectivePillars = MaxNumObjectivePillars;
		});
	}

	// Bullet has gone rogue and collided with a wall, needs reset!
	public void ResetBullet()
	{
		// Stop any currently running time scaling routine for safety
		if (ActiveScalingRoutine != null)
			StopCoroutine(ActiveScalingRoutine);

		// Call to destroy bullet, prepare for camera transition, destroy pillars and other cleanup
		SetShootingRestriction(false);
	}

	public Vector2 GetBulletScreenPosition()
	{
		if (CurrentBullet != null)
			return CurrentBullet.GetScreenPosition();
		return new Vector2(-1000, -1000);
	}

	public void QuitGame(bool forceQuit)
	{
		if (forceQuit)
		{
			// Close the game/application
			Application.Quit();
		}
		else
		{
			SetPauseState(true, RoundPauseDuration, () => Application.Quit());
		}
	}
}