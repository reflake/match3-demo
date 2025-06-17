using UnityEngine;

[RequireComponent(typeof(Gem), typeof(SoundPlayer))]
public class PowerGem : MonoBehaviour
{
    [SerializeField] private Transform explosionPrefab;
    
    private Gem _gem;
    private SoundPlayer _sndPlayer;
    private bool _exploded = false;

    void Awake()
    {
        foreach (ParticleSystem emitter in GetComponentsInChildren<ParticleSystem>())
            emitter.Play();

        _gem = GetComponent<Gem>();
        _sndPlayer = FindAnyObjectByType<SoundPlayer>();
    }

    private void Explode(Cell cell)
    {
        if (cell == null || cell.Gem == null)
            return;

        Gem gem = cell.Gem;

        if (!gem.CanExplode())
            return;

        Cell top = cell.Top;

        // Explosion knockbacks the gem on top
        while (top != null && top.Gem != null)
        {
            top.Gem.StartCoroutine(
                top.Gem.ApplyForceOverTime(new Vector3(0, 4.6f), 0.03f));

            top = top.Top;
        }

        gem.Gather();
        Destroy(gem.gameObject);
    }

    // Triggered by Gem class
    public void OnGather()
    {
        if (_exploded)
            return;

        _sndPlayer.Explosion();

        _exploded = true;

        Transform explosion = Instantiate(explosionPrefab);
        Destroy(explosion.gameObject, 0.5f);

        Cell cell = _gem.Cell;
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
