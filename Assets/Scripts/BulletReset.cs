using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletReset : MonoBehaviour
{
	void OnTriggerEnter(Collider other)
	{
		if (other.transform.TryGetComponent(out Bullet bullet))
		{
			// If bullet collided with us, it's gone rogue and needs to be reset
			GameManager.INSTANCE.ResetBullet();
		}
	}
}
