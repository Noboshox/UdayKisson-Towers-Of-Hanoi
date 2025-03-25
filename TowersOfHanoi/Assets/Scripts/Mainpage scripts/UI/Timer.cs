using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class Timer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;
    float elapsedtime;

    private void Update()
    {
        elapsedtime += Time.deltaTime;
        timerText.text = elapsedtime.ToString();
        int minutes = Mathf.FloorToInt(elapsedtime / 60);
        int seconds = Mathf.FloorToInt(elapsedtime % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

}
