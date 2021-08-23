using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
	[SerializeField] private Text totalScore, timeLoss;

	private float timeElapsed;
	private float score;
	private bool endingRound = false, doneWithCountdown = false;

	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		if (!endingRound)
			timeElapsed += Time.deltaTime;
		totalScore.text = string.Format("Score: {0:0}", score);
		timeLoss.text = string.Format("Time elapsed: {0:0.00}", timeElapsed);
	}

	public void SetPlayerScore(int score)
	{
		if (!endingRound)
			this.score = score;
	}

	public void EndRound()
	{
		if (endingRound)
			return;
		endingRound = true;
		StartCoroutine(IEndRound());
	}

	IEnumerator IEndRound()
	{
		while (timeElapsed > 0.0f)
		{
			score -= 2 * Time.deltaTime;
			timeElapsed -= 2 * Time.deltaTime;
			yield return null;
		}
		if (score < 0.0f)
			score = 0.0f;
		if (timeElapsed < 0.0f)
			timeElapsed = 0.0f;
		yield return new WaitForSeconds(3f);
		doneWithCountdown = true;
	}

	public bool EndedRound() => timeElapsed <= 0.0f && endingRound && doneWithCountdown;
	public bool EndingRound() => timeElapsed > 0.0f && endingRound && !doneWithCountdown;
}
