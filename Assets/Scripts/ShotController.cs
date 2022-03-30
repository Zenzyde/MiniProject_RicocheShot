using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotController : MonoBehaviour
{
	[SerializeField] Bullet BulletPrefab;

	void OnEnable()
	{
		// Subscribe to firing event
		GameManager.OnShotRegisteredEvent += ShootProjectile;
	}

	void OnDisable()
	{
		// Making sure to unsubscribe event
		GameManager.OnShotRegisteredEvent -= ShootProjectile;
	}

	void ShootProjectile(RaycastHit userHit, List<Vector3> waypoints)
	{
		// Don't let player shoot if camera is currently following a bullet
		if (GameManager.INSTANCE.ShootingRestricted || userHit.transform == null)
			return;

		// Calculate initial shooting direction based on first hit by "ProjectileVisualizer"
		Vector3 RayDirection = (userHit.point - transform.position).normalized;
		// Project direction on the horizontal plane to flatten it and prevent unwanted shooting-deviation
		RayDirection = Vector3.ProjectOnPlane(RayDirection, Vector3.up);
		Ray Ray = new Ray(transform.position, RayDirection);

		// Make sure there's a bullet prefab available
		if (BulletPrefab)
		{
			// Create bullet and point it towards the first hit
			Bullet bullet = Instantiate(BulletPrefab, transform.position, Quaternion.LookRotation(RayDirection));
			// Make gamemanager aware of the bullet instance
			GameManager.INSTANCE.AssignNewBullet(bullet);
			// Make sure all systems know to not allow shooting while following bullet
			GameManager.INSTANCE.SetShootingRestriction(true);
			// Provide bullet instance with waypoints provided by "ProjectileVisualizer"
			// Also provide callback to the gamemanager to enable all systems to allow shooting the next bullet when current bullet instance--
			// has reached it's final waypoint
			bullet.MoveToWaypoint(waypoints, () => GameManager.INSTANCE.SetShootingRestriction(false));
		}
	}
}
