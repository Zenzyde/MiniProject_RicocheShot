using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class CameraController : MonoBehaviour
{
	[SerializeField][Tooltip("How long should intro camera-elevation sequence take")] float MoveDuration;
	[SerializeField][Tooltip("How long should initial bullet-following sequence take")] float BulletFollowIntroDuration;
	[SerializeField][Tooltip("How fast camera will move when far away from and following bullet")] float BulletFollowSpeedFAR;
	[SerializeField][Tooltip("How fast camera will move when close toand following bullet")] float BulletFollowSpeedNEAR;
	[SerializeField][Tooltip("How long should exit sequence after following bullet take")] float BulletFollowExitDuration;
	[SerializeField][Tooltip("How fast should camera rotate to look at bullet when following")] float RotationSpeed;
	[SerializeField][Tooltip("Size of movement, affects CameraIntro animation cuvres")] Vector3 MoveStrength;
	[SerializeField][Tooltip("The exact point away from the bullet the camera will try to follow")] Vector3 BulletFollowOffset;
	[SerializeField][Tooltip("Camera intro animation curve, x-axis")] AnimationCurve CameraIntroCurveX;
	[SerializeField][Tooltip("Camera intro animation curve, y-axis")] AnimationCurve CameraIntroCurveY;
	[SerializeField][Tooltip("Camera intro animation curve, z-axis")] AnimationCurve CameraIntroCurveZ;

	// Flag which enables normal movement only after intro is done
	private bool IntroDone = false;

	// Cached topdown position for exit after bullet-following
	private Vector3 TopDownPos;
	// Cached bullet position for exit after bullet-following
	private Vector3 LastBulletFollowPos;

	// Cached topdown rotation for exit after bullet-following
	private Quaternion TopDownRot;
	// Cached bullet rotation for exit after bullet-following
	private Quaternion LastBulletFollowRot;

	// The actual speed of the camera when following bullet, will be lerped
	private float CurrentFollowSpeed;
	// The percentual progression for initial bullet-follow intro sequence
	private float CurrentFollowDuration;
	// The percentual progression for initial bullet-follow exit sequence
	private float CurrentFollowExitDuration;

	// Current active bullet instance
	private Bullet bullet;

	// Update is called once per frame
	void Awake()
	{
		// Perform initial topdown movement sequence
		StartCoroutine(AnimateCameraIntro());
	}

	void FixedUpdate()
	{
		// Exit if initial animation is not done yet
		if (!IntroDone)
			return;
		// Handle camera bullet-following movement
		MoveCamera();
	}

	IEnumerator AnimateCameraIntro()
	{
		float currentDuration = 0.0f;
		Vector3 origin = transform.position;
		while (currentDuration < MoveDuration)
		{
			Vector3 current = transform.position;

			// Calculate next position based on percentage-duration and animation curve strength, using animation curves
			current.x = origin.x + CameraIntroCurveX.Evaluate(currentDuration / MoveDuration) * MoveStrength.x;
			current.y = origin.y + CameraIntroCurveY.Evaluate(currentDuration / MoveDuration) * MoveStrength.y;
			current.z = origin.z + CameraIntroCurveZ.Evaluate(currentDuration / MoveDuration) * MoveStrength.z;

			transform.position = current;
			currentDuration += Time.deltaTime;
			yield return null;
		}
		// Cache rotation and position for later when moving from and to bullet-following
		TopDownPos = transform.position;
		TopDownRot = transform.rotation;
		// Let gamemanager notify systems to start game
		GameManager.INSTANCE.StartGame();
		// Tell gamemanager to remove shooting restriction
		GameManager.INSTANCE.SetShootingRestriction(false);
		// Let regular camera movement let loose!
		IntroDone = true;
	}

	void MoveCamera()
	{
		// If a bullet has been shot, follow it!
		if (GameManager.INSTANCE.ShootingRestricted)
		{
			// Reset follow exit duration percetage if camera should follow bullet
			CurrentFollowExitDuration = 0.0f;

			// Handle percentage calculation based on simulated time
			if (CurrentFollowDuration < BulletFollowIntroDuration)
				CurrentFollowDuration += Time.fixedDeltaTime * GameManager.INSTANCE.SIMULATION_SPEED;
			float perc = (CurrentFollowDuration / BulletFollowIntroDuration);
			// Lerp actual follow speed based on percentage -> move slower the closer camera is to bullet
			CurrentFollowSpeed = Mathf.Lerp(BulletFollowSpeedFAR, BulletFollowSpeedNEAR, perc);

			// Cache the current bullet if it's not already cached
			if (bullet == null)
				bullet = GameManager.INSTANCE.CurrentBullet;

			// Update bullet follow position for exit-lerp
			LastBulletFollowPos = transform.position;
			// Lerp camera to folow bullet based on simulated time and lerped speed
			transform.position = Vector3.Lerp(
				transform.position, bullet.transform.position + BulletFollowOffset, CurrentFollowSpeed * Time.fixedDeltaTime * GameManager.INSTANCE.SIMULATION_SPEED);

			// Calculate look direction, cache it for exit lerp and constantly lerp camera rotation to look at bullet
			Quaternion look = Quaternion.LookRotation((bullet.transform.position - transform.position).normalized);
			LastBulletFollowRot = look;
			transform.rotation = Quaternion.Slerp(transform.rotation, look, RotationSpeed * Time.fixedDeltaTime * GameManager.INSTANCE.SIMULATION_SPEED);
		}
		// If bullet has been destroyed
		else
		{
			// Reset follow enter duration if camera should return to topdown view
			CurrentFollowDuration = 0.0f;

			// Handle percentage calculation based on simulated time
			if (CurrentFollowExitDuration < BulletFollowExitDuration)
				CurrentFollowExitDuration += Time.fixedDeltaTime * GameManager.INSTANCE.SIMULATION_SPEED;
			float perc = (CurrentFollowExitDuration / BulletFollowExitDuration);

			// If we've not reset lastbulletfollowposition, meaning camera is not yet at topdown view position
			if (LastBulletFollowPos != Vector3.zero)
			{
				// Lerp position and rotation to initial topdown view
				transform.position = Vector3.Lerp(LastBulletFollowPos, TopDownPos, perc);
				transform.rotation = Quaternion.Lerp(LastBulletFollowRot, TopDownRot, perc);

				// Lerp is done
				if (perc >= 1.0f)
				{
					// Let the player see all pillars that were hit, sink
					GameManager.INSTANCE.DestroyHits();
					// Reset lastbulletfollowposition to keep the camera from redoing this entire sequence
					LastBulletFollowPos = Vector3.zero;
				}
			}
		}
	}
}
