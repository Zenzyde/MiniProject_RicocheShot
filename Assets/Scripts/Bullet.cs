using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
	[SerializeField][Tooltip("How fast the bullet should move")] float MoveSpeed;
	[SerializeField][Tooltip("How long should entering bullet-time take")] float BulletTimeEnterDuration;
	[SerializeField][Tooltip("How long should bullet-time take between enter and exit")] float BulletTimeTransitionDuration;
	[SerializeField][Tooltip("How long should exiting bullet-time take")] float BulletTimeExitDuration;
	[SerializeField][Tooltip("How far ahead should raycast look for a pillar to activate bullet time entry")] float RaycastMaxDist;
	[SerializeField][Tooltip("For comparing size with 'ProjectileVisualizer spherecast' to ensure movement and bullet-time behaves correctly")] float SpherecastRadius;

	// Cached rigidbody for movement
	private Rigidbody Rigidbody;

	// Cached list of waypoints for bullet to follow, provided by 'ProjectileVisualizer'
	private List<Vector3> Waypoints = new List<Vector3>();

	// Tracking the current waypoint bullet moves towards
	private int CurrentWaypoint = 0;

	// Flag to indicate whether bullet has notified gamemanager that it's done bouncing
	private bool CalledCallback;
	// Flag for indicating if a call for slowing time has been made
	private bool SlowedTime = false;

	// Cached callback to call when done 'bullet-bouncing'
	private System.Action Callback;

	// List of pillars that were hit during 'bullet-bouncing' for point calculation and pillar-sinking
	public HashSet<Pillar> HitPillars { get; private set; } = new HashSet<Pillar>();

	void Awake()
	{
		// Cache rigidbody and notify gamemanager to perform initial slowing of time for camera intro sequence when shooting bullet
		Rigidbody = GetComponent<Rigidbody>();
		GameManager.INSTANCE.SlowTime(BulletTimeEnterDuration, BulletTimeTransitionDuration, BulletTimeExitDuration);
	}

	void FixedUpdate()
	{
		// There are no waypoints to move towards for whatever reason, don't move
		if (Waypoints.Count <= 0)
			return;

		if (CurrentWaypoint < Waypoints.Count)
		{
			// While we still have waypoints to move towards, move based on simulated time
			Rigidbody.position += MoveSpeed * Time.fixedDeltaTime * transform.forward * GameManager.INSTANCE.SIMULATION_SPEED;

			// If close to a pillar, call to slow down simulated time for bullet-time and set flag
			if (Physics.Raycast(Rigidbody.position, transform.forward, RaycastMaxDist) && !SlowedTime)
			{
				GameManager.INSTANCE.SlowTime();
				SlowedTime = true;
			}
		}
		else if (CurrentWaypoint >= Waypoints.Count && !CalledCallback)
		{
			// No more waypoints to follow, let gamemanager know bullet is done and ready to be destroyed -- call callback
			GameManager.INSTANCE.RestoreTime();
			Callback.Invoke();
			CalledCallback = true;
		}
	}

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		// Draw bullet-time activating raycast
		Gizmos.color = Color.magenta;
		Gizmos.DrawRay(transform.position, transform.forward * RaycastMaxDist);

		if (Waypoints.Count > 0)
		{
			// Assuming there are waypoints to follow, visualize their positions for debugging purposes
			Gizmos.color = Color.cyan;
			for (int i = 0; i < Waypoints.Count; i++)
			{
				Gizmos.DrawSphere(Waypoints[i], .2f);
			}
		}

		// Draw the endpoint of the raycast as a sphere, for debugging purposes
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position + transform.forward * RaycastMaxDist, SpherecastRadius);
	}
#endif

	public void MoveToWaypoint(List<Vector3> waypoints, System.Action callBack)
	{
		// Add waypints to enable bullet to move and cache the callback
		Waypoints.AddRange(waypoints);
		Callback = callBack;
	}

	void OnCollisionEnter(Collision other)
	{
		if (other.transform.TryGetComponent(out Pillar pillar))
		{
			// Set the position of the rigidbody for accuracy, not noticeable fortunately
			Rigidbody.position = Waypoints[CurrentWaypoint];

			// Update the current waypoint bullet moves towards
			CurrentWaypoint++;

			// If we hit a new pillar, add it to the list and register it to the points-text
			if (!HitPillars.Contains(pillar))
			{
				HitPillars.Add(pillar);
				GameManager.INSTANCE.AddPointsToBuffer(pillar.PillarType);
			}

			// Don't execute any further logic if we're at the last waypoint
			if (CurrentWaypoint >= Waypoints.Count)
				return;

			// Calculate and flatten the look direction to the next waypoint
			Vector3 projectedDirection = Vector3.ProjectOnPlane((Waypoints[CurrentWaypoint] - Waypoints[CurrentWaypoint - 1]).normalized, Vector3.up);
			transform.rotation = Quaternion.LookRotation(projectedDirection);

			// We hit a pillar and want to move to the next waypoint, release bullet-timea and call to restore simulated time
			SlowedTime = false;
			GameManager.INSTANCE.RestoreTime();
		}
	}
}
