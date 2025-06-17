using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    private AudioSource m_AudioSource;

    public AudioClip m_FailSnd;
    public AudioClip m_GoodSnd;
    public AudioClip m_LandSnd;
    public AudioClip[] m_SpeedSnd;
    public AudioClip m_ExplosionSnd;
    public AudioClip m_PowerGemSnd;
    public AudioClip m_TurnSnd;
    public AudioClip m_SelectSnd;
    public AudioClip m_RewindSnd;
    public AudioClip m_TimeUpSnd;
    public AudioClip m_GoSnd;

    private float m_lastLandSound = 0.0f;

    void Awake()
    {
        m_AudioSource = GetComponent<AudioSource>();
    }

    public void Rewind()
    {
        Play(m_RewindSnd, 0.0f);
    }

    public void Select()
    {
        Play(m_SelectSnd, 0.0f);
    }

    public void Turn()
    {
        Play(m_TurnSnd, 0.0f);
    }

    public void Speed(int level)
    {
        level = Mathf.Clamp(level, 0, m_SpeedSnd.Length - 1);

        Play(m_SpeedSnd[level], 0.0f);
    }

    public void Fail(float delay)
    {
        Play(m_FailSnd, delay);
    }

    public void Good(float delay)
    {
        Play(m_GoodSnd, delay);
    }

    public void Explosion()
    {
        Play(m_ExplosionSnd, 0.0f);
    }

    public void PowerGem()
    {
        Play(m_PowerGemSnd, 0.0f);
    }

    public void TimeUp()
    {
        Play(m_TimeUpSnd, 0.0f);
    }

    public void Go()
    {
        Play(m_GoSnd, 0.0f);
    }

    public void Land()
    {
        if (m_lastLandSound > Time.time)
            return;

        Play(m_LandSnd, 0.0f);

        m_lastLandSound = Time.time + .02f;
    }

    public void Play(AudioClip clip, float delay)
    {
        if (clip == null)
            return;

        StartCoroutine(PlayEffect(clip, delay));
    }

    private IEnumerator PlayEffect(AudioClip clip, float delay)
    {
        if (delay > 0.0f)
            yield return new WaitForSeconds(delay);

        m_AudioSource.PlayOneShot(clip);

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
