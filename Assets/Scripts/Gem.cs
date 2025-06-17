using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public enum Gems
{
    Unknown = 0,
    Apple,
    Orange,
    Grape,
    Banana,
    Cherry,
    Watermelon,
    Pineapple,
    Hyperstone,
}

public class Gem : MonoBehaviour
{
    [SerializeField] private Gems gemType = Gems.Unknown;
    [SerializeField] private int worth = 1;
    [SerializeField] private Cell cell = null;

    public bool Gathered { get; private set; } = false;

    public bool Streak { get; private set; } = false;

    public bool Swapping { get; private set; } = false;

    public bool IsIdle { get {
            return _idleChecker.m_Idle &&
                _rig.bodyType == RigidbodyType2D.Kinematic;
        }
    }

    public Cell Cell {
        get { return cell; }
        set {
            if (cell != null && cell.Gem == this)
                cell.Gem = null;

            cell = value;

            if (value != null)
            {
                value.Gem = this;
                Column = value.Column;
            }
        }
    }

    public int Column { get; set; }

    public bool TurnTag { get; set; } = false;
    
    public int Worth { get => worth; set => worth = value; }
    public Gems GemType => gemType;

    private Animator _animator;
    private Collider2D m_Collider;
    private IdleChecker _idleChecker;
    private Rigidbody2D _rig;
    private SoundPlayer _sndPlayer;
    private GameObject _field;

    private Vector3 _rootPosition;
    private float m_LandTime;

    protected void Awake()
    {
        _animator = GetComponent<Animator>();

        _idleChecker = _animator.GetBehaviour<IdleChecker>();
        _idleChecker.m_GemScript = this;

        _rig = GetComponent<Rigidbody2D>();
        _sndPlayer = FindFirstObjectByType<SoundPlayer>();
        _field = GameObject.Find("Field");
    }

    void FixedUpdate()
    {
        if (Swapping)
        {
            if (IsIdle)
                Swapping = false;

            return;
        }

        if (Gathered)
            return;

        if (Cell != null)
        {
            if (transform.localPosition.y + _rig.linearVelocity.y * Time.fixedDeltaTime <
                Cell.transform.localPosition.y)
                Land();

            Bottomtest();
        }
        else
            _rig.bodyType = RigidbodyType2D.Dynamic;
    }

    void Bottomtest()
    {
        if (Gathered)
            return;

        if (Cell.Bottom != null &&
            Cell.Bottom.Gem == null)
        {
            _rig.bodyType = RigidbodyType2D.Dynamic;

            if (Cell.Top != null &&
                Cell.Top.Gem != null)
                Cell.Top.Gem.Bottomtest();

            Cell = null;
        }
    }

    public IEnumerator ApplyForceOverTime(Vector3 velocity, float time)
    {
        do
        {
            _rig.linearVelocity = velocity;

            time -= Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        } while (time >= 0.0f);
    }

    void Land()
    {
        // уже приземлились и неподвижны
        if (_rig.bodyType == RigidbodyType2D.Kinematic)
            return;

        _rig.bodyType = RigidbodyType2D.Kinematic;
        _rig.linearVelocity = Vector2.zero;

        transform.localPosition = Cell.transform.localPosition;

        if (_sndPlayer)
            _sndPlayer.Land();

        m_LandTime = Time.time;
    }

    public bool LandedLately()
    {
        return Time.time < m_LandTime + 0.05f;
    }

    public bool CanSwap()
    {
        return Cell != null && 
            !Swapping &&
            !Gathered &&
            !Streak &&
            !IsCollapsing() &&
            _rig.bodyType == RigidbodyType2D.Kinematic;
    }

    public bool CanGather()
    {
        return IsIdle && !Gathered;
    }

    public bool CanExplode()
    {
        return !Gathered && !Streak;
    }

    public bool CanStreak()
    {
        return !Gathered &&
            _rig.bodyType == RigidbodyType2D.Kinematic;
    }

    public void SetStreak()
    {
        Streak = true;
    }

    public void Swap(Gem second, bool badmove)
    {
        if (second == null)
            return;

        int moveh = second.Cell.Column - Cell.Column;
        int movev = second.Cell.Row - Cell.Row;

        if (!badmove)
        {
            Cell tmp = Cell;

            Cell = second.Cell;
            second.Cell = tmp;

            Cell.Gem = this;
            second.Cell.Gem = second;

            Vector3 tmp3 = transform.localPosition;

            transform.localPosition = second.transform.localPosition;
            second.transform.localPosition = tmp3;
        }

        _sndPlayer.Turn();

        this.TurnTag = true;
        second.TurnTag = true;

        Move(moveh, movev, badmove);
        second.Move(-moveh, -movev, badmove);
    }

    private void Move(int moveh, int movev, bool badmove)
    {
        string animation = "Move_";

        if (movev < 0)
            animation += "U";
        else if (movev > 0)
            animation += "D";

        if (moveh < 0)
            animation += "L";
        else if (moveh > 0)
            animation += "R";

        if (badmove)
            animation += "_invalid";

        _animator.Play(animation, -1, 0);
        _idleChecker.m_Idle = false;

        Swapping = true;
    }

    public bool IsMatch(Gem gem)
    {
        if (gem == null)
            return false;

        if (gemType == Gems.Hyperstone)
            return true;

        if (gem.gemType == Gems.Hyperstone)
            return true;

        return gemType == gem.gemType;
    }

    public bool IsNear(Gem gem)
    {
        if (gem == null)
            return false;

        float distance = (transform.position - gem.transform.position).magnitude;

        return Mathf.Approximately(distance, 1.0f);
    }

    public bool IsCollapsing()
    {
        if (Cell == null)
            return true;

        if (Gathered)
            return true;

        Cell current = Cell.Bottom;

        if (current)
        {
            if (current.Gem == null)
                return true;

            return current.Gem.IsCollapsing();
        }

        return false;
    }

    public void Gather()
    {
        if (Gathered)
            return;

        SendMessage(nameof(PowerGem.OnGather), SendMessageOptions.DontRequireReceiver);

        Gathered = true;

        _animator.Play("Gather", -1, 0);

        const float destructionDelay = 1.0f / 10.0f;
        Destroy(gameObject, destructionDelay);

        _field.SendMessage("OnGemGathered", this, SendMessageOptions.DontRequireReceiver);
    }

    public void MergeGems(Gem[] otherGems)
    {
        this._rig.simulated = false;

        StartCoroutine(MergeEffect(otherGems, this));
    }

    public void Clear()
    {
        Gathered = true;
        _rig.bodyType = RigidbodyType2D.Dynamic;
        _rig.simulated = false;
        _animator.enabled = false;

        const float duration = 1.0f;
        var destroySequence = DOTween.Sequence();
        destroySequence
            .Append(transform.DOShakePosition(duration, 0.2f, 120, fadeOut: false))
            .AppendCallback(LaunchGem);

        const float soundRepeatRate = 0.1f;
        
        InvokeRepeating("PlayRandomSound", Random.Range(0.0f, soundRepeatRate), soundRepeatRate);
    }

    void PlayRandomSound()
    {
        // When gem is being destroyed play a random sound every .1s
        if (Random.Range(0, 25) == 0)
            _sndPlayer.Land();
    }

    private void LaunchGem()
    {
        // Stop playing random sound
        CancelInvoke("PlayRandomSound");
        
        GetComponent<Collider2D>().enabled = false;
        _rig.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rig.simulated = true;
        _rig.linearVelocity = new Vector2(0, 9) + Random.insideUnitCircle * 3.34f;
        _rig.gravityScale = 2.7f;
    }

    public Gem[] GetGemsAround()
    {
        List<Gem> gems = new();
            
        void AddGemFromCell(Cell cell)
        {
            if (cell != null && cell.Gem != null)
                gems.Add(cell.Gem);
        }

        AddGemFromCell(Cell.Top);
        AddGemFromCell(Cell.Left);
        AddGemFromCell(Cell.Bottom);
        AddGemFromCell(Cell.Right);

        return gems.ToArray();
    }

    private IEnumerator MergeEffect(Gem[] otherGems, Gem newGem)
    {
        foreach (Gem gem in otherGems)
        {
            gem.Gathered = true;
            gem.StartCoroutine(gem.TransitionEffect(transform.localPosition, 0.09f));
        }

        yield return new WaitForSeconds(0.225f);

        _rig.simulated = true;
    }

    private IEnumerator TransitionEffect(Vector3 newPosition, float duration)
    {
        Vector3 startPosition = transform.localPosition;
        float t = 0.0f;

        _rig.simulated = false;

        do
        {
            t += Time.fixedDeltaTime;

            transform.localPosition = Vector3.Lerp(startPosition, newPosition, t / duration);

            yield return new WaitForFixedUpdate();
        } while (t < duration);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Cell = null;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Gathered)
            return;

        Cell cell = collision.GetComponent<Cell>();

        if (transform.parent != cell.transform.parent)
            transform.parent = cell.transform.parent;

        if (cell.transform.localPosition.y - 0.1f > transform.localPosition.y)
            return;

        if (cell.Gem != null)
            return;

        if (cell == null)
            return;

        if (cell.Bottom != null && cell.Bottom.Gem == null)
            return;

        Cell = cell;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (TurnTag)
            return;

        if (Gathered)
            return;

        Cell cell = collision.GetComponent<Cell>();

        if (cell == null)
            return;

        if (cell == Cell && cell.Gem == this)
            Cell = null;
    }
}

