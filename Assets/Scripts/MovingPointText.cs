using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPointText : MonoBehaviour
{
	[SerializeField] float VisibleDuration = 1.2f;
	[SerializeField] float MoveSpeed = 2;

	// Start is called before the first frame update
	void Start() => Destroy(gameObject, VisibleDuration);

	// Update is called once per frame
	void Update() => transform.position += Vector3.up * MoveSpeed * Time.deltaTime;
}