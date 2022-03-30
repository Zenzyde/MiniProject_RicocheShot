using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pillar : MonoBehaviour
{
	[SerializeField][Tooltip("How long does it take for pillar to sink")] float SinkDuration;
	[SerializeField][Tooltip("How far should pillar sink")] Vector3 SinkOffset;
	[SerializeField][Tooltip("What type of pillar is this")] PillarType _PillarType;

	// Getter for pillar type
	public PillarType PillarType => _PillarType;

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		// If in editor, draw sink-path
		Gizmos.color = Color.green;
		Gizmos.DrawLine(transform.position, transform.position + SinkOffset);
	}
#endif

	public void Sink()
	{
		// Enable pillar-sinking after bullet bouncing
		StartCoroutine(ISink());
	}

	IEnumerator ISink()
	{
		// Cache positions for sinking-lerp
		Vector3 startPos = transform.position;
		Vector3 endPos = transform.position + SinkOffset;

		// Start the sinking!!
		float currentSink = 0.0f;
		while (currentSink < SinkDuration)
		{
			currentSink += Time.deltaTime;
			transform.position = Vector3.Lerp(startPos, endPos, currentSink / SinkDuration);
			yield return null;
		}

		// Important: deactive object so that 'PillarSpawner_Topdown' can delete it in-between rounds
		gameObject.SetActive(false);
	}
}

public enum PillarType
{
	Target, Regular
}