using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeletePillar : MonoBehaviour
{
	void Awake()
	{
		foreach (Collider collider in Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius * transform.localScale.magnitude))
		{
			Pillar pillar = collider.GetComponent<Pillar>();
			if (collider.transform != transform && pillar != null && pillar.PillarType == PillarType.Regular && !collider.GetComponent<DeletePillar>())
			{
				Destroy(collider.gameObject);
			}
		}
	}
}