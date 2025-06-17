using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerGem : MonoBehaviour
{
    public Transform m_ExplosionPrefab;
    private Gem m_GemScript;
    private SoundPlayer m_sndPlayer;

    void Awake()
    {
        foreach (ParticleSystem emitter in GetComponentsInChildren<ParticleSystem>())
            emitter.Play();

        m_GemScript = GetComponent<Gem>();
        m_sndPlayer = GameObject.Find("SoundPlayer").GetComponent<SoundPlayer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Explode(Cell cell)
    {
        if (cell == null || cell.Gem == null)
            return;

        Gem gem = cell.Gem;

        if (!gem.CanExplode())
            return;

        Cell top = cell.Top;

        // отправить в полет блоки чуть повыше, которые были задеты взрывом
        while (top != null && top.Gem != null)
        {
            top.Gem.StartCoroutine(
                top.Gem.ApplyForceOverTime(new Vector3(0, 4.6f), 0.03f));

            top = top.Top;
        }

        gem.Gather();
        Destroy(gem.gameObject);
    }

    bool m_Exploded = false;

    public void OnGather()
    {
        if (m_Exploded)
            return;

        m_sndPlayer.Explosion();

        m_Exploded = true;

        Transform explosion = Instantiate(m_ExplosionPrefab);
        Destroy(explosion.gameObject, 0.5f);

        Cell cell = m_GemScript.Cell;
        explosion.transform.position = cell.transform.position;

        if (cell.Top)
        {
            Explode(cell.Top);
            Explode(cell.Top.Left);
            Explode(cell.Top.Right);
        }

        if (cell.Bottom)
        {
            Explode(cell.Bottom);
            Explode(cell.Bottom.Left);
            Explode(cell.Bottom.Right);
        }

        if (cell.Left)
            Explode(cell.Left);

        if (cell.Right)
            Explode(cell.Right);

        Destroy(gameObject);
    }
}
