using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(LineRenderer))]
public class ProjectileVisualizer : MonoBehaviour
{
	[SerializeField][Tooltip("Max distance from camera to detect pillars with raycast")] float RaycastMaxDistance = 10f;
	[SerializeField][Tooltip("How wide of a 'ray' to cast")] float SpherecastRadius = .2f;
	[SerializeField][Tooltip("How many times could a bullet bounce at max")] int MaxBounces = 5;
	[SerializeField][Tooltip("Where the bullet is being shot from")] Transform shotOrigin;

	// The initial raycast hit-point -- used for initial bullet direction at creation
	private Vector3 hitPoint;
	// The waypoints bullets will follow when 'bouncing' between pillars
	private List<Vector3> hitPoints = new List<Vector3>();

	// Line for visualizing the path bullet will follow when shooting
	private LineRenderer line;
	// Using MPB for optimizing setting colour of linerenderer at runtime
	private MaterialPropertyBlock mpb;

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		// Draw bullet-path for confirming bullet follows the path properly
		Gizmos.color = Color.green;
		Gizmos.DrawLine(transform.position, transform.position - Vector3.up * RaycastMaxDistance);
		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(hitPoint, SpherecastRadius);
		Gizmos.color = Color.red;
		for (int i = 0; i < hitPoints.Count - 1; i++)
		{
			Gizmos.DrawLine(hitPoints[i], hitPoints[i + 1]);
		}
	}
#endif

	// Initialize needed components
	void Awake()
	{
		line = GetComponent<LineRenderer>();
		mpb = new MaterialPropertyBlock();
		mpb.SetColor("_EmissionColor", Color.red * 2f);
		line.SetPropertyBlock(mpb);
	}

	void Update()
	{
		// Create camera-to-world ray
		Ray ray = RaycastCameraToWorldPosition();
		// Once we have the ray, cast and try to get a hitresult
		if (GetRaycastResult(ray, out RaycastHit hit))
		{
			// We have an initial hitpoint for a possible bullet!
			hitPoint = hit.point;
		}
		else
		{
			// We didn't hit any pillars, can't shoot bullet
			hitPoint = Vector3.zero;
		}

		// Visualize the possible travel path for a bullet, given that there was a raycast hit
		VisualizeProjectileTrajectory(hit);

		// If we click and have a hitresult confirmed, tell gamemanager to fire!
		if (Input.GetMouseButtonDown(0))
		{
			GameManager.INSTANCE.FireShotEvent(hitPoint != Vector3.zero ? hit : new RaycastHit() { point = ray.GetPoint(RaycastMaxDistance) }, hitPoints);
		}
	}

	Ray RaycastCameraToWorldPosition()
	{
		// Create and return a simple ray from camera to world based on mouse position
		return Camera.main.ScreenPointToRay(Input.mousePosition);
	}

	bool GetRaycastResult(Ray ray, out RaycastHit hit)
	{
		// Perform raycast and return the hitresult
		return Physics.Raycast(ray, out hit, RaycastMaxDistance);
	}

	void VisualizeProjectileTrajectory(RaycastHit visualisation)
	{
		// If there's already an actively travelling bullet, don't visualize it's path
		if (GameManager.INSTANCE.ShootingRestricted | GameManager.INSTANCE.IsPaused || visualisation.point == Vector3.zero)
		{
			line.positionCount = 1;
			line.SetPosition(0, transform.position);
			Ray idleRay = Camera.main.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.z));
			Vector3 shotIdleHit = idleRay.origin + idleRay.direction * 1000;
			Vector3 shotIdleDirection = (shotIdleHit - shotOrigin.position).normalized;
			shotIdleDirection = Vector3.ProjectOnPlane(shotIdleDirection, Vector3.up);
			shotOrigin.rotation = Quaternion.LookRotation(shotIdleDirection);
			return;
		}

		// Clear the previous possible bullet path
		hitPoints.Clear();

		// Create the initial bullet direction from shot-origin to first hit pillar
		Vector3 RayDirection = (visualisation.point - shotOrigin.position).normalized;
		// Flatten raydirection along horizontal plane to circumvent possible travel-problems for bullet
		RayDirection = Vector3.ProjectOnPlane(RayDirection, Vector3.up);
		// Rotate "player" towards shot direction
		shotOrigin.rotation = Quaternion.LookRotation(RayDirection);
		Ray Ray = new Ray(shotOrigin.position, RayDirection);

		// Local variables for calculating reflected vectors and continue bullet-bounce
		Vector3 NextOrigin, NextDirection;

		// Set first point of visualizing line renderer
		line.positionCount = 1;
		line.SetPosition(0, Ray.origin);

		// Visualize all the bounces!
		for (int i = 1; i <= MaxBounces + 1; i++)
		{
			// Perform a spherecast to give bullet-bouncing and the player a bit of lee-way when aiming
			if (Physics.SphereCast(Ray, SpherecastRadius, out RaycastHit hit))
			{
				// set next visualization point
				line.positionCount++;
				line.SetPosition(i, hit.point);

				// Set the hit origin for the next bounce and reflect the next diretion off of the current hit normal
				NextOrigin = hit.point;
				NextDirection = Vector3.Reflect(Ray.direction, hit.normal);
				Ray.origin = NextOrigin;
				Ray.direction = NextDirection;

				// Add the hit origin to the waypoints for a possible bullet
				hitPoints.Add(Ray.origin);
			}
			else
			{
				// If we aimed in a way that didn't give us max amount of bounces, exit out
				return;
			}
		}
	}
}