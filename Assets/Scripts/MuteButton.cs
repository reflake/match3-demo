using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuteButton : MonoBehaviour
{
    bool m_Mute = false;
    public AudioSource m_SoundPlayer;
    Animator m_Animator;
    AudioSource m_ClickAudio;

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
        m_ClickAudio = GetComponent<AudioSource>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (m_Animator)
            m_Animator.SetBool("mute", m_Mute);
    }

    void ToggleMute()
    {
        m_Mute = !m_Mute;

        if (m_SoundPlayer)
        m_SoundPlayer.mute = m_Mute;
    }

    private void OnMouseOver()
    {
        if (m_Animator)
            m_Animator.SetBool("hover", true);
    }

    private void OnMouseExit()
    {
        if (m_Animator)
            m_Animator.SetBool("hover", false);
    }

    private void OnMouseDown()
    {
        ToggleMute();

        if (m_ClickAudio)
            m_ClickAudio.Play();
    }
}
