using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class Timer : MonoBehaviour
{
    public float m_Duration;
    public float RemainingTime { get; private set; }

    private bool m_Suspended = true;

    public TextMeshProUGUI m_Text;
    public UnityEngine.UI.Image m_ProgressImage;

    public System.Action Callback { get; set; } = null;

    void Start()
    {
        
    }

    public void SetTimer()
    {
        m_Suspended = false;
        RemainingTime = m_Duration;
    }

    public void Resume()
    {
        m_Suspended = false;
    }

    public void Suspend()
    {
        m_Suspended = true;
    }

    void Update()
    {
        if (!m_Suspended)
        {
            RemainingTime -= Time.deltaTime;

            if (RemainingTime <= 0.0f)
            {
                RemainingTime = 0.0f;
                Suspend();

                if (Callback != null)
                    Callback();
            }

            m_ProgressImage.rectTransform.sizeDelta = 
                new Vector2(RemainingTime / m_Duration * 595.0f ,34.0f);

            int secondsLeft = (int)Mathf.Ceil(RemainingTime);
            int minutesLeft = secondsLeft / 60;
            secondsLeft = secondsLeft % 60;

            m_Text.text = minutesLeft.ToString() + ":" + secondsLeft.ToString("00");
        }
    }
}
