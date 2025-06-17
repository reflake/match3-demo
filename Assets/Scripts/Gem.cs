using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Теперь уже правда не самоцветы а фрукты
// но не могу поменять полностью код
public class Gem : MonoBehaviour
{
    #region "Classes/Types"

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

    #endregion
    #region "Fields"

    public Gems m_GemType = Gems.Unknown;
    public int m_Worth = 1;

    private Player m_Ply;
    private Animator m_Animator;
    private Collider2D m_Collider;
    private IdleChecker m_IdleChecker;
    private SpriteRenderer m_Sprite;
    private Rigidbody2D m_Rig;

    private Vector3 m_rootPosition;

    public bool Gathered { get; private set; } = false;

    public bool Streak { get; private set; } = false;

    public bool Swapping { get; private set; } = false;

    public bool IsIdle { get {
            return m_IdleChecker.m_Idle &&
                m_Rig.bodyType == RigidbodyType2D.Kinematic;
        }
    }

    public Cell m_Cell = null;
    public Cell Cell {
        get { return m_Cell; }
        set {
            if (m_Cell != null && m_Cell.Gem == this)
                m_Cell.Gem = null;

            m_Cell = value;

            if (value != null)
            {
                value.Gem = this;
                Column = value.Column;
            }
        }
    }

    public int Column { get; set; }

    private SoundPlayer m_sndPlayer;

    private float m_LandTime;

    private GameObject m_Field;

    public bool TurnTag { get; set; } = false;

    #endregion
    #region "Functions/Methods"

    protected void Awake()
    {
        m_Ply = GameObject.FindGameObjectWithTag("Player").
            GetComponent<Player>();

        m_Animator = GetComponent<Animator>();

        m_IdleChecker = m_Animator.GetBehaviour<IdleChecker>();
        m_IdleChecker.m_GemScript = this;

        m_Sprite = GetComponentInChildren<SpriteRenderer>();

        m_Rig = GetComponent<Rigidbody2D>();

        m_sndPlayer = GameObject.Find("SoundPlayer").GetComponent<SoundPlayer>();

        m_Field = GameObject.Find("Field");
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
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
            if (transform.localPosition.y + m_Rig.velocity.y * Time.fixedDeltaTime <
                Cell.transform.localPosition.y)
                Land();

            Bottomtest();
        }
        else
            m_Rig.bodyType = RigidbodyType2D.Dynamic;
    }

    void Bottomtest()
    {
        if (Gathered)
            return;

        if (Cell.Bottom != null &&
            Cell.Bottom.Gem == null)
        {
            m_Rig.bodyType = RigidbodyType2D.Dynamic;

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
            m_Rig.velocity = velocity;

            time -= Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        } while (time >= 0.0f);
    }

    void Land()
    {
        // уже приземлились и неподвижны
        if (m_Rig.bodyType == RigidbodyType2D.Kinematic)
            return;

        m_Rig.bodyType = RigidbodyType2D.Kinematic;
        m_Rig.velocity = Vector2.zero;

        transform.localPosition = Cell.transform.localPosition;

        if (m_sndPlayer)
            m_sndPlayer.Land();

        m_LandTime = Time.time;
    }

    // приземлился ли самоцвет недавно
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
            m_Rig.bodyType == RigidbodyType2D.Kinematic;
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
            m_Rig.bodyType == RigidbodyType2D.Kinematic;
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

        m_sndPlayer.Turn();

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

        m_Animator.Play(animation, -1, 0);
        m_IdleChecker.m_Idle = false;

        Swapping = true;
    }

    public bool IsMatch(Gem g)
    {
        if (g == null)
            return false;

        if (m_GemType == Gems.Hyperstone)
            return true;

        if (g.m_GemType == Gems.Hyperstone)
            return true;

        return m_GemType == g.m_GemType;
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

        SendMessage("OnGather", SendMessageOptions.DontRequireReceiver);

        Gathered = true;

        m_Animator.Play("Gather", -1, 0);

        Destroy(gameObject, 6.0f / 60.0f);

        m_Field.SendMessage("OnGemGathered", this, SendMessageOptions.DontRequireReceiver);
    }

    public void MergeGems(Gem[] otherGems)
    {
        this.m_Rig.simulated = false;

        StartCoroutine(MergeEffect(otherGems, this));
    }

    public IEnumerator Clear()
    {
        float t = 1.0f;
        Gathered = true;
        m_Rig.bodyType = RigidbodyType2D.Dynamic;
        m_Rig.simulated = false;
        m_Animator.enabled = false;

        do
        {
            t -= Time.fixedDeltaTime;

            if (Random.Range(0, 200) == 0)
                m_sndPlayer.Land();

            // потрясти немного самоцвет
            m_Sprite.transform.localPosition = Random.insideUnitCircle * 0.075f;

            yield return new WaitForFixedUpdate();

        } while (t >= 0.0f);

        GetComponent<Collider2D>().enabled = false;
        m_Rig.constraints = RigidbodyConstraints2D.FreezeRotation;
        m_Rig.simulated = true;
        m_Rig.velocity = new Vector2(0, 9) + Random.insideUnitCircle * 3.34f;
        m_Rig.gravityScale = 2.7f;
    }

    public Gem[] GetGemsAround()
    {
        List<Gem> gems = new List<Gem>();
        System.Action<Cell> f = (Cell cell) =>
        {
            if (cell != null && cell.Gem != null)
                gems.Add(cell.Gem);
        };


        f(Cell.Top);
        f(Cell.Left);
        f(Cell.Bottom);
        f(Cell.Right);

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

        this.m_Rig.simulated = true;
    }

    private IEnumerator TransitionEffect(Vector3 newPosition, float duration)
    {
        Vector3 startPosition = transform.localPosition;
        float t = 0.0f;

        m_Rig.simulated = false;

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

    #endregion
}

