using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(LineRenderer))]
public class ProjectileShooter : MonoBehaviour
{
	[SerializeField] private int maxBounces = 5;
	[SerializeField] private float sphereCastRadius = .15f, sphereCastDistance = 50f;

	private PlayerManager playerManager = new PlayerManager();

	private LineRenderer line;
	private MaterialPropertyBlock mpb;

	private PlayerUI uI;

	void Awake()
	{
		line = GetComponent<LineRenderer>();
		if (!line)
			line = gameObject.AddComponent<LineRenderer>();

		mpb = new MaterialPropertyBlock();
		uI = GetComponent<PlayerUI>();
	}

	// Update is called once per frame
	void Update()
	{
		if (uI.EndedRound())
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		VisualizeProjectile();
		if (Input.GetMouseButtonDown(0) && !uI.EndingRound())
		{
			ShootProjectile();
		}
	}

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		Ray ray = new Ray(transform.position + transform.right * .5f - transform.up * .5f, transform.forward);

		Vector3 nextOrigin, nextDirection;

		for (int i = 0; i <= maxBounces; i++)
		{
			if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, sphereCastDistance))
			{
				nextOrigin = hit.point;
				nextDirection = Vector3.Reflect(ray.direction, hit.normal);

				if (hit.transform.GetComponent<RandomizeTarget>())
				{
					Gizmos.color = Color.yellow;
					Gizmos.DrawLine(ray.origin, nextOrigin);
					return;
				}
				else
				{
					Gizmos.color = Color.red;
					Gizmos.DrawLine(ray.origin, nextOrigin);
				}

				if (i == maxBounces)
				{
					Gizmos.color = Color.green;
					Gizmos.DrawSphere(hit.point, .25f);
				}
				else
				{
					ray.origin = nextOrigin;
					ray.direction = nextDirection;
				}
			}
		}
	}
#endif

	void ShootProjectile()
	{
		Ray ray = new Ray(transform.position + transform.right * .5f - transform.up * .5f, transform.forward);

		Vector3 nextOrigin, nextDirection;

		int points = 0, destroyMult = 0;
		bool hitTarget = false;

		for (int i = 1; i <= maxBounces + 1; i++)
		{
			if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, sphereCastDistance))
			{
				if (hit.transform.parent != null && hit.transform.parent.GetComponent<InitiateTargetPillar>() &&
					!hit.transform.GetComponent<RandomizeTarget>())
				{
					break;
				}
				points++;
				destroyMult++;
				if (hit.transform.GetComponent<RandomizeTarget>())
				{
					hitTarget = true;
					break;
				}
				else if (hit.transform.tag == "Destructible")
				{
					Destroy(hit.transform.gameObject);
				}

				nextOrigin = hit.point;
				nextDirection = Vector3.Reflect(ray.direction, hit.normal);
				ray.origin = nextOrigin;
				ray.direction = nextDirection;
			}
		}

		playerManager.score += points * destroyMult;
		uI.SetPlayerScore(playerManager.score);

		if (hitTarget)
		{
			uI.EndRound();
		}
	}

	void VisualizeProjectile()
	{
		Ray ray = new Ray(transform.position + transform.right * .5f - transform.up * .5f, transform.forward);

		Vector3 nextOrigin, nextDirection;

		line.positionCount = 1;

		line.SetPosition(0, ray.origin);

		for (int i = 1; i <= maxBounces + 1; i++)
		{
			if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, sphereCastDistance))
			{
				line.positionCount++;
				line.SetPosition(i, hit.point);
				if (hit.transform.GetComponent<RandomizeTarget>())
				{
					mpb.SetColor("_EmissionColor", Color.yellow * 2f);
					line.SetPropertyBlock(mpb);
					break;
				}
				else if (hit.transform.parent != null && hit.transform.parent.GetComponent<InitiateTargetPillar>())
				{
					mpb.SetColor("_EmissionColor", Color.cyan * 2f);
					line.SetPropertyBlock(mpb);
					break;
				}
				mpb.SetColor("_EmissionColor", Color.red * 2f);
				line.SetPropertyBlock(mpb);
				nextOrigin = hit.point;
				nextDirection = Vector3.Reflect(ray.direction, hit.normal);
				ray.origin = nextOrigin;
				ray.direction = nextDirection;
			}
		}
	}
}
