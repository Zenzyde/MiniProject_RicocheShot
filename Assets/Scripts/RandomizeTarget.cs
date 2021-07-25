using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeTarget : MonoBehaviour
{
	[SerializeField] private float yRange;
	[SerializeField] private Vector3 centre;
	[SerializeField] private LayerMask defaultMask, targetMask;

	private Vector3 gizmoPos;
	private ProjectileShooter player;

	void Awake() => player = FindObjectOfType<ProjectileShooter>();

	// Start is called before the first frame update
	void Start()
	{
		gizmoPos = transform.localPosition;

		Vector3 position = transform.localPosition;
		position.y += Random.Range(-yRange, yRange);
		transform.localPosition = position;
	}

	void Update()
	{
		Vector3 worldPos = transform.TransformPoint(transform.localPosition);
		Ray ray = new Ray(worldPos, (player.transform.position - worldPos).normalized);
		if (Physics.Raycast(ray, out RaycastHit hit, 100f))
		{
			if (hit.transform == player.transform)
			{
				gameObject.layer = defaultMask;
			}
			else if (hit.transform != player.transform)
			{
				gameObject.layer = targetMask;
			}
		}
	}

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawLine(
			gizmoPos + centre + Vector3.down * yRange / 2,
			gizmoPos + centre + Vector3.up * yRange / 2);
	}
#endif
}
