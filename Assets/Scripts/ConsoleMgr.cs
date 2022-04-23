using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ConsoleMgr : MonoBehaviour
{
    public static ConsoleMgr instance;
    public TMP_Text log;



    private void Awake()
    {
        instance = this;
    }

    public void Log(string text)
    {
        log.text += "[" + TimeToText(Time.realtimeSinceStartup) + "]: " + text + "\r\n";
    }

    private string TimeToText(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time % 60F);
        int milliseconds = Mathf.FloorToInt((time * 100F) % 100F);

        return minutes.ToString("00") + ":" + seconds.ToString("00") + ":" + milliseconds.ToString("00");
    }
}
