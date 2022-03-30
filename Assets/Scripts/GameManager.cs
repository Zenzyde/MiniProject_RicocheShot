using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	[SerializeField][Tooltip("Minimum scale time will be lerped towards (preferably 0 to 0.5 range)")] private float MinTimeScale;
	[SerializeField][Tooltip("Maximum scale time will be lerped towards (preferably 0.5 to 1 range)")] private float MaxTimeScale;
	[SerializeField][Tooltip("How long should a round last")] private float RoundDuration;
	[SerializeField][Tooltip("How fast does time scale/lerp to minimum time scale")] float TimeScalingEnterDuration;
	[SerializeField][Tooltip("How fast does time scale/lerp to maximum time scale")] float TimeScalingExitDuration;
	[SerializeField][Header("Point system settings")] PointSystem PointSystem;
	[SerializeField][Tooltip("Pillar spawner instance")] PillarSpawner_TopDown PillarSpawner;

	// Singleton-instance
	private static GameManager instance;
	public static GameManager INSTANCE { get { return instance; } }

	// Event to notify systems when a shot is fired by 'ShotController'
	public delegate void OnShotRegistered(RaycastHit hit, List<Vector3> waypoints);
	public static event OnShotRegistered OnShotRegisteredEvent;

	// Property which enalbes player to shoot bullet and enables camera to follow bullet correctly
	public bool ShootingRestricted { get; private set; }
	// Flag to indicate whether time is currently being scaled or not, enables routine to not be called multiple times while they're running
	private bool ScalingTime;
	// Flag to indicate that pillars are currently being destroyed/set inactive
	private bool DestroyingPillars;
	// Flag which enables game systems as soon as initial camera intro sequence is done
	private bool AllowGameToRun;
	// Flag to indicate whether or not to release bullet-time time-scaling
	private bool SkipTimeScaling;

	// Cached current bullet instance property
	public Bullet CurrentBullet { get; private set; }

	// Simulation speed property to enable camera and bullet to move at proper speeds
	public float SIMULATION_SPEED { get; private set; } = 1.0f;

	// List of pillars to destroy/deactivate, provided by bullet
	private List<Pillar> PillarsToDestroy = new List<Pillar>();

	// Cached wait-instance for pillar-destroying sequence
	private WaitForSecondsRealtime wait = new WaitForSecondsRealtime(.15f);

	// Start is called before the first frame update
	void Awake()
	{
		if (instance == null)
		{
			// Initialize basic settings and pointsystem
			Cursor.lockState = CursorLockMode.Confined;
			instance = this;
			PointSystem.Initialize(RoundDuration);
			ShootingRestricted = true;
		}
	}

	void Update()
	{
		// If intro camera sequence is not done, exit
		if (!AllowGameToRun)
			return;

		// If time scaling should be skipped, restore time
		if (SkipTimeScaling)
		{
			ScalingTime = false;
			RestoreTime();
		}

		// Update pointsystem
		PointSystem.Update();

		// Initiate next round if time has run out
		if (PointSystem.SIMULATED_TIME <= float.Epsilon)
			EndRound();

		// Toggle time scaling skip if right mouse is clicked
		if (Input.GetMouseButtonDown(1))
			SkipTimeScaling = !SkipTimeScaling;

		// Quit game if desired
		if (Input.GetKeyDown(KeyCode.Escape))
			QuitGame();
	}

	// Camera intro is done, allow game to run
	public void StartGame() => AllowGameToRun = true;

	// Call to intialize a bullet and let camera follow bullet
	public void FireShotEvent(RaycastHit hit, List<Vector3> waypoints) => OnShotRegisteredEvent?.Invoke(hit, waypoints);

	// Call to initiate bullet-time slowing (assuming time is not already scaling or if player has toggled to skip bullet-time)
	public void SlowTime(bool restore = false)
	{
		if (ScalingTime || SkipTimeScaling)
			return;
		StartCoroutine(ISlowTime(restore));
	}

	// Call to initiate bullet-time slowing (assuming time is not already scaling or if player has toggled to skip bullet-time)
	// This method is specific for initial bullet-time intro when bullet is first fired
	public void SlowTime(float enterDuration, float transitionDuration, float exitDuration)
	{
		if (ScalingTime || SkipTimeScaling)
			return;
		StartCoroutine(ISlowTime(enterDuration, transitionDuration, exitDuration));
	}

	IEnumerator ISlowTime(bool restore = false)
	{
		// Set flag to ensure time scaling won't be interrupted
		ScalingTime = true;

		float StartTimeScale = SIMULATION_SPEED;
		float CurrentTimeScaleDuration = 0;

		// Lerp the simulation speed based on entering duration -- used by bullet and camera for movement
		while (CurrentTimeScaleDuration < TimeScalingEnterDuration)
		{
			CurrentTimeScaleDuration += Time.unscaledDeltaTime;
			SIMULATION_SPEED = Mathf.Lerp(StartTimeScale, MinTimeScale, (CurrentTimeScaleDuration / TimeScalingEnterDuration));
			yield return null;
		}

		// If flag is set, go directly to restoring time
		if (restore)
			StartCoroutine(IRestoreTime());
		// Else, set flag to false to allow time to be scaled at a later time
		else
			ScalingTime = false;
	}

	// Specifically called by bullet when shot
	IEnumerator ISlowTime(float enterDuration, float transitionDuration, float exitDuration)
	{
		// Set flag to ensure time scaling won't be interrupted
		ScalingTime = true;

		float StartTimeScale = SIMULATION_SPEED;
		float CurrentTimeScaleDuration = 0;

		// Lerp the simulation speed based on entering duration -- used by bullet and camera for movement
		while (CurrentTimeScaleDuration < enterDuration)
		{
			CurrentTimeScaleDuration += Time.unscaledDeltaTime;
			SIMULATION_SPEED = Mathf.Lerp(StartTimeScale, MinTimeScale, (CurrentTimeScaleDuration / enterDuration));
			yield return null;
		}

		// Wait for transition duration to finish
		yield return new WaitForSecondsRealtime(transitionDuration);

		// Go directly towards restoring time
		StartCoroutine(IRestoreTime(exitDuration));
	}

	// Call to initiate bullet-time restoring (assuming time is not already scaling)
	public void RestoreTime()
	{
		if (ScalingTime)
			return;
		StartCoroutine(IRestoreTime());
	}

	IEnumerator IRestoreTime()
	{
		float StartTimeScale = SIMULATION_SPEED;
		float CurrentTimeScaleDuration = 0;

		// Lerp the simulation speed based on exiting duration -- used by bullet and camera for movement
		while (CurrentTimeScaleDuration < TimeScalingExitDuration)
		{
			CurrentTimeScaleDuration += Time.unscaledDeltaTime;
			SIMULATION_SPEED = Mathf.Lerp(StartTimeScale, MaxTimeScale, (CurrentTimeScaleDuration / TimeScalingExitDuration));
			yield return null;
		}

		// Unset flag to enable further scaling
		ScalingTime = false;
	}

	// Specifically called by bullet when shot
	IEnumerator IRestoreTime(float exitDuration)
	{
		float StartTimeScale = SIMULATION_SPEED;
		float CurrentTimeScaleDuration = 0;

		// Lerp the simulation speed based on exiting duration -- used by bullet and camera for movement
		while (CurrentTimeScaleDuration < exitDuration)
		{
			CurrentTimeScaleDuration += Time.unscaledDeltaTime;
			SIMULATION_SPEED = Mathf.Lerp(StartTimeScale, MaxTimeScale, (CurrentTimeScaleDuration / exitDuration));
			yield return null;
		}

		// Unset flag to enable further scaling
		ScalingTime = false;
	}

	// Assign a new bullet instance to the game systems
	public void AssignNewBullet(Bullet bullet) => CurrentBullet = bullet;

	// Destroy the current bullet instance, called when bullet is done bullet-bouncing
	void DestroyBullet() => Destroy(CurrentBullet.gameObject);

	// Called by camera after bullet is done bullet-bouncing to allow player to see pillars sink
	public void DestroyHits()
	{
		if (DestroyingPillars)
			return;
		StartCoroutine(IDestroyHits());
	}

	IEnumerator IDestroyHits()
	{
		// Set flag to make sure pillars only sink once before round reset
		DestroyingPillars = true;

		// Loop through and call sink on each pillar
		for (int i = 0; i < PillarsToDestroy.Count; i++)
		{
			Pillar pillar = PillarsToDestroy[i];
			pillar.Sink();
			yield return wait;
		}

		// Notify poinsystem to add point buffer to total
		PointSystem.AddPointsToTotal(this);

		// Clear list for next round/shot
		PillarsToDestroy.Clear();

		// Unset flag to allow sequence to be run after next shot/round
		DestroyingPillars = false;
	}

	// Called by bullet when colliding with pillar to update pointsbuffer
	public void AddPointsToBuffer(PillarType pillarType) => PointSystem.AddPointsToBuffer(pillarType);

	// Restricts player from shooting a new bullet until the bullet has been destroyed
	// Also controls whether camera follows bullet or not
	public void SetShootingRestriction(bool state)
	{
		// Set restriction flag
		ShootingRestricted = state;
		if (!ShootingRestricted && CurrentBullet != null)
		{
			// Bullet is done bullet-bouncing, reset time scaling skip
			SkipTimeScaling = false;
			// Add hit pillars for deactivation
			PillarsToDestroy.AddRange(CurrentBullet.HitPillars);
			// Destroy bullet instance
			DestroyBullet();
		}
	}

	// Called when time has run out for a round by point system
	public void EndRound()
	{
		// Unset shooting restriction
		SetShootingRestriction(false);

		// Call pillar spawner to regenerate pillars
		PillarSpawner.GeneratePillars();

		// Reinitialize point system
		PointSystem.Initialize(RoundDuration);
	}

	void QuitGame()
	{
		// Is this comment really needed......it closes the game/application xD
		Application.Quit();
	}
}
