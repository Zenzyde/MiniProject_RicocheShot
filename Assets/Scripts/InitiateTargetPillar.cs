using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitiateTargetPillar : MonoBehaviour
{
	[SerializeField] Vector3 min, max;

	// Start is called before the first frame update
	void Start()
	{
		Relocate();
	}

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(min, .15f);
		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(max, .15f);
	}
#endif

	// Update is called once per frame
	void Update()
	{

	}

	void Relocate()
	{
		transform.position = Vector3.Lerp(min, max, Random.value);
	}
}
