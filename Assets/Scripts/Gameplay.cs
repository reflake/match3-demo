using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

using UnityEngine.UI;

public class Gameplay : MonoBehaviour
{
    public SoundPlayer m_AudioPlayer;
    public Transform m_HintPrefab;
    public Gem[] m_PowerGems;
    public Gem m_Hyperstone;

    Coroutine m_HintTimer;

    private Field m_Field;

    int m_SpeedLevel = 0;
    float m_LastStreakTime;

    Transform[] m_TempHint = new Transform[2];

    public TextMeshProUGUI m_ScoreCounter = null;
    public TextMeshProUGUI m_GemCounter = null;

    public Transform m_Curtain;

    private int m_Score = 0;
    private int m_GemCount = 0;

    private bool m_Paused = false;
    private bool m_AnnouncePlaying = false;

    public Transform m_Announcement;
    public TextMeshProUGUI m_AnnounceText;

    public Timer m_GameTimer;

    private void Awake()
    {
        m_Field = GetComponent<Field>();
    }

    public void Restart()
    {
        m_Score = 0;
        m_GemCount = 0;

        m_Field.Clear(true);
        m_Field.ReFill();
        m_Field.SetFreeze(false);

        ResetHintTimer();

        m_GameTimer.SetTimer();
        m_GameTimer.Callback = () => this.OnTimeUp();
    }

    // Start is called before the first frame update
    void Start()
    {
        Restart();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetButtonDown("Restart"))
        {
            Restart();
        }

        if (Input.GetButtonDown("Cancel"))
        {
            SetPause(!m_Paused);
        }
    }

    // механика подсказок
    //
    // показывается каждые 9 сек., пока игрок не сделает ход
    #region Hint Mechanic

    void ShowOneHint()
    {
        (Gem, Gem)[] solutions = m_Field.GetSolutions();

        // а есть ли решения?
        if (solutions.Length == 0)
            return;

        // любое из решений
        int i = Random.Range(0, solutions.Length);

        (Gem g1, Gem g2) turn = solutions[i];

        Vector3 diff = turn.g2.transform.position - turn.g1.transform.position;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        Transform hint = Instantiate(m_HintPrefab, turn.g1.transform);
        hint.position = turn.g1.transform.position;
        hint.rotation = Quaternion.Euler(0, 0, angle);

        m_TempHint[0] = hint;

        StartCoroutine(RemoveHint(turn.g1, hint));

        hint = Instantiate(m_HintPrefab, turn.g2.transform);
        hint.position = turn.g2.transform.position;
        hint.rotation = Quaternion.Euler(0, 0, angle - 180);

        m_TempHint[1] = hint;

        StartCoroutine(RemoveHint(turn.g2, hint));
    }

    IEnumerator RemoveHint(Gem gem, Transform helper)
    {
        float lifeTime = Time.time + 9.0f;

        yield return new WaitUntil(() =>
            gem == null ||
            gem.Swapping ||
            gem.Gathered ||
            Time.time > lifeTime);

        if (helper.gameObject != null)
            Destroy(helper.gameObject);
    }

    void ResetHintTimer()
    {
        if (m_HintTimer != null)
            StopCoroutine(m_HintTimer);

        m_HintTimer = StartCoroutine(HintTimer());
    }

    IEnumerator HintTimer()
    {
        yield return new WaitForSeconds(15.0f);

        ShowOneHint();

        ResetHintTimer();
    }
    
    #endregion

    void ShowAnnouncement(string text)
    {
        m_AnnouncePlaying = true;

        Time.timeScale = 0.0f;

        m_Announcement.gameObject.SetActive(true);
        m_Announcement.GetComponent<Animator>().Play("AnnouncementShow", 0, 0.0f);

        m_AnnounceText.text = text;

        StartCoroutine(AnnouncementTimer());
    }

    IEnumerator AnnouncementTimer()
    {
        yield return new WaitForSecondsRealtime(3.0f);

        m_Announcement.gameObject.SetActive(false);

        m_AnnouncePlaying = false;

        if (!m_Paused)
            Time.timeScale = 1.0f;
    }

    void SetPause(bool value)
    {
        if (value == true)
            Time.timeScale = 0.0f;
        else if (!m_AnnouncePlaying)
            Time.timeScale = 1.0f;

        m_Paused = value;
        m_Curtain.gameObject.SetActive(value);
    }

    // звук который проигрывается когда находятся
    // совпадающие ряды
    void MatchSound()
    {
        if (Time.time - m_LastStreakTime > 3.0f)
            m_SpeedLevel = 0;

        if (m_AudioPlayer)
        {
            if (m_SpeedLevel > 0)
                m_AudioPlayer.Speed(m_SpeedLevel);
            else
                m_AudioPlayer.Good(0.0f);
        }

        m_SpeedLevel++;

        m_LastStreakTime = Time.time;
    }

    // вызывается когда время уже вышло
    void OnTimeUp()
    {
        ShowAnnouncement("T<color=#FF4130>I</color>ME UP");

        m_Field.SetFreeze(true);

        m_AudioPlayer.TimeUp();

        StartCoroutine(GameOver());
    }

    IEnumerator GameOver()
    {
        yield return new WaitForSeconds(1.0f);

        m_Field.Clear(false);
    }

    // не осталось больше шагов
    void OnFreeze()
    {
        // тогда нужно все перемешать
        ShowAnnouncement("BRE<color=#46DAF9>A</color>K TIME");

        m_GameTimer.Suspend();

        StartCoroutine(BreakLoop());
    }

    IEnumerator BreakLoop()
    {
        yield return new WaitForSeconds(0.5f);

        m_AudioPlayer.Rewind();

        AddScore(10000);

        Gem first = null, second = null;
        Gem[] gems;

        float breakTime = Time.time + 3.0f;

        do
        {
            yield return new WaitForSeconds(0.11f);

            foreach (GameObject gem in GameObject.FindGameObjectsWithTag("Gem"))
            {
                if (Random.Range(0, 16) == 0)
                {
                    first = gem.GetComponent<Gem>();

                    if (!first.IsIdle)
                    {
                        first = null;
                        continue;
                    } 
                    else
                        break;
                }
            }

            if (first == null)
                continue;

            gems = first.GetGemsAround();

            second = gems[Random.Range(0, gems.Length)];

            if (second == null || !second.IsIdle)
                continue;

            first.Swap(second, false);

        } while (breakTime > Time.time || 
            m_Field.GetSolutions().Length < 3 || 
            m_Field.HaveStreaks());

        m_Field.SetFreeze(false);

        m_GameTimer.Resume();
        
    }

    void OnGemGathered(Gem gem)
    {
        m_GemCount += gem.m_Worth;

        if (m_GemCounter == null)
            return;

        m_GemCounter.text = m_GemCount.ToString();
    }

    void AddScore(int amount)
    {
        m_Score += amount;

        if (m_ScoreCounter == null)
            return;

        m_ScoreCounter.text = m_Score.ToString();
    }

    void OnGetStreaks(List<Field.Streak> gatherStreaks)
    {
        ResetHintTimer();

        MatchSound();

        gatherStreaks.RemoveAll((streak) =>
        {
            if (streak.IsRemoved())
                return false;

            AddScore(streak.Count * streak.Count * 5 * m_SpeedLevel);

            // четыре самоцвета объединяются в
            // один заряженный (который взрывается)
            if (streak.Count >= 4)
            {
                Gem root = streak.GetCausedGem();

                Gem powerGem = null;

                foreach (Gem g in m_PowerGems)
                    if (g.m_GemType == root.m_GemType)
                        powerGem = g;

                if (powerGem == null)
                    return false;

                Gem[] otherGems = new Gem[streak.Count];
                int i = 0;
                int totalWorth = 0;

                foreach (Gem gem in streak)
                {
                    otherGems[i++] = gem;
                    totalWorth += gem.m_Worth;
                }

                powerGem = Instantiate(powerGem, transform);

                powerGem.GetComponent<Gem>().Cell = root.Cell;
                powerGem.transform.localPosition = root.transform.localPosition;
                powerGem.m_Worth = totalWorth;

                //Destroy(root);
                powerGem.GetComponent<Gem>().MergeGems(otherGems);

                m_AudioPlayer.PowerGem();

                // DO NOT GATHER
                return true;
            }

            return false;
        });
    }

    void OnGameOver()
    {
        GameObject.Find("GameOverSign")
            .GetComponent<Rigidbody2D>().simulated = true;
    }

}
