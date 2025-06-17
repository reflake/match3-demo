using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class Field : MonoBehaviour
{
    #region "Classes/Types"

    public class Streak : HashSet<Gem>
    {
        public bool IsMatch()
        {
            foreach (Gem gem1 in this)
            {
                if (gem1 == null)
                    return false;

                foreach (Gem gem2 in this)
                {
                    if (gem2 == null)
                        return false;

                    if (!gem1.IsMatch(gem2))
                        return false;
                }
            }

            return true;
        }

        public bool CanGather()
        {
            foreach (Gem gem in this)
                if (gem != null && !gem.CanGather())
                    return false;

            return true;
        }

        public bool IsRemoved()
        {
            foreach (Gem gem in this)
                if (gem == null)
                    return true;

            return false;
        }

        public bool GemMatches(Gem gem)
        {
            foreach (Gem gem2 in this)
                if (gem2 != null && !gem.IsMatch(gem2))
                    return false;

            return true;
        }

        public Gem GetCausedGem()
        {
            List<Gem> landed = new List<Gem>();

            foreach (Gem gem in this)
            {
                if (gem.TurnTag)
                    return gem;

                if (gem.LandedLately())
                    landed.Add(gem);
            }

            if (landed.Count > 0)
                return landed[Random.Range(0, landed.Count)];

            foreach (Gem gem in this)
                return gem;

            return null;
        }
    }

    // класс для нахождения решений
    class Solver
    {
        private Cell[,] cells;
        private int rows;
        private int columns;

        private List<string[]> Patterns = new List<string[]>();

        public Solver(Cell[,] cells, int rows, int columns)
        {
            this.cells = cells;
            this.rows = rows;
            this.columns = columns;

            // патерны http://ajc.su/wp-content/uploads/bot/patterns.png

            #region Patterns
            Patterns.Add(new string[] {
                "X ",
                "X ",
                " <",
            });

            Patterns.Add(new string[] {
                "X ",
                " <",
                "X ",
            });

            Patterns.Add(new string[] {
                " <",
                "X ",
                "X ",
            });

            Patterns.Add(new string[] {
                " X",
                " X",
                "> ",
            });

            Patterns.Add(new string[] {
                " X",
                "> ",
                " X",
            });
            
            Patterns.Add(new string[] {
                "> ",
                " X",
                " X",
            });

            Patterns.Add(new string[] {
                " oo",
                "^  ",
            });

            Patterns.Add(new string[] {
                "o o",
                " ^ ",
            });

            Patterns.Add(new string[] {
                "oo ",
                "  ^",
            });

            Patterns.Add(new string[] {
                "v  ",
                " oo",
            });

            Patterns.Add(new string[] {
                " v ",
                "o o",
            });

            Patterns.Add(new string[] {
                "  v",
                "oo ",
            });

            Patterns.Add(new string[] {
                "> oo",
            });

            Patterns.Add(new string[] {
                "oo <",
            });

            Patterns.Add(new string[] {
                "v",
                " ",
                "X",
                "X",
            });

            Patterns.Add(new string[] {
                "X",
                "X",
                " ",
                "^",
            });
            #endregion
        }

        // находит все возможные ходы на поле
        //
        // возвращает список из пар самоцветов, которые
        //   нужно поменять местами, чтобы составить ряд
        public (Gem, Gem)[] FindSolutions()
        {
            List<(Gem, Gem)> solutions = new List<(Gem, Gem)>();

            for(int i=0; i<rows; i++)
                for(int j=0; j<columns; j++)
                    solutions.AddRange(
                        CheckPatterns(i, j)
                    );

            return solutions.ToArray();
        }

        private (Gem, Gem)[] CheckPatterns(int row, int column)
        {
            List<(Gem, Gem)> solutions = new List<(Gem, Gem)>();

            // пройдем по всем паттернам и проверим совпадает ли
            // ячейка хотя бы с одним из них
            foreach(string[] pattern in Patterns)
            {
                (Gem g1, Gem g2) solution;
                solution = CheckPattern(pattern, row, column);

                if (solution.g1 != solution.g2)
                    solutions.Add(solution);
            }

            return solutions.ToArray();
        }

        private (Gem, Gem) CheckPattern(string[] pattern, int row, int column)
        {
            // достаточно ли оставшихся строк
            // для проверки паттерна
            if (rows < row + pattern.Length)
                return (null, null);

            Streak combo = new Streak();
            (Gem, Gem) aSolution = (null, null);

            // проходимся по строкам
            for (int i = 0; i < pattern.Length; i++)
            {
                string rowPattern = pattern[i];

                if (columns < column + rowPattern.Length)
                    return (null, null);

                for (int j = 0; j < rowPattern.Length; j++)
                {
                    Cell cell = cells[row + i, column + j];

                    if (rowPattern[j] == ' ')
                        continue;
                    else if (
                        cell.Gem != null &&
                        !cell.Gem.Streak &&
                        !cell.Gem.Gathered &&
                        combo.GemMatches(cell.Gem))

                        combo.Add(cell.Gem);
                    else
                        return (null, null);

                    switch (rowPattern[j])
                    {
                        case '^':
                            aSolution = (cell.Gem, cell.Top.Gem);
                            break;
                        case 'v':
                            aSolution = (cell.Gem, cell.Bottom.Gem);
                            break;
                        case '<':
                            aSolution = (cell.Gem, cell.Left.Gem);
                            break;
                        case '>':
                            aSolution = (cell.Gem, cell.Right.Gem);
                            break;
                    }
                }
            }

            return aSolution;
        }

    }

    #endregion
    #region "Fields/Props"

    public int m_Rows = 8;
    public int m_Columns = 8;
    public int m_ChainMinimalLength = 3;
    public SoundPlayer m_AudioPlayer;
    public Gem[] m_BasicGems;

    private Cell[,] cells;

    public Cell m_CellPrefab;

    int GemsCount {
        get { return m_BasicGems.Length; }
    }

    bool m_Freeze = true;

    private List<Streak> m_Streaks = new List<Streak>();
    private List<Streak> m_GatheredStreaks = new List<Streak>();

    private Solver m_Solver;

    private (Gem, Gem)[] m_Solutions;

    #endregion
    #region "Functions/Methods"

    private void Awake()
    {
        cells = new Cell[m_Rows, m_Columns];

        m_Solver = new Solver(cells, m_Rows, m_Columns);

        // разместим клетки которые смогут занять самоцветы
        for (int i = 0; i < m_Rows; i++)
        {
            for (int j = 0; j < m_Columns; j++)
            {
                Cell c = Instantiate(m_CellPrefab, transform);
                c.transform.localPosition = CellLocation(i, j);
                c.Row = i;
                c.Column = j;

                cells[i, j] = c;

                if (i > 0)
                {
                    cells[i, j].Top = cells[i - 1, j];
                    cells[i - 1, j].Bottom = cells[i, j];
                }

                if (j > 0)
                {
                    cells[i, j].Left = cells[i, j - 1];
                    cells[i, j - 1].Right = cells[i, j];
                }
            }
        }
    }

    void Start()
    {
        // раскомментировать чтоб включить бота
        //LaunchBot(0.05f);
    }

    void Update()
    {
        if (m_Freeze)
            return;

        UpdateStreaks();

        Cascade();

        Peace();
    }

    public void SetFreeze(bool value)
    {
        m_Freeze = value;
    }

    void LaunchBot(float delay)
    {
        StartCoroutine(BotLogic(delay));
    }

    // меняет местами столбцы и строки
    void Transpose()
    {
        Cell[,] newField = new Cell[m_Columns, m_Rows];
        int tmp;

        for (int i = 0; i < m_Rows; i++)
        {
            for (int j = 0; j < m_Columns; j++)
            {
                newField[j, i] = cells[i, j];

                Cell g = cells[i, j];

                tmp = g.Row;
                g.Row = g.Column;
                g.Column = tmp;
            }
        }

        tmp = m_Rows;
        m_Rows = m_Columns;
        m_Columns = tmp;

        cells = newField;
    }

    public void Clear(bool forceClear)
    {
        for (int i = 0; i < m_Rows; i++)
            for (int j = 0; j < m_Columns; j++)
                cells[i, j].Gem = null;

        if (forceClear)
        {
            foreach (GameObject g in GameObject.FindGameObjectsWithTag("Gem"))
                Destroy(g);
        }
        else
        {
            foreach (GameObject g in GameObject.FindGameObjectsWithTag("Gem"))
            {
                Gem gem = g.GetComponent<Gem>();

                gem.StartCoroutine(gem.Clear());
            }
        }
    }

    public void ReFill()
    {
        // генерирует самоцветы на полях
        // до тех пор пока на поле не будет
        // совпадающих рядов и будет по-крайней мере
        // 3 решения (хода)
        do
        {
            for (int i = 0; i < m_Rows; i++)
            {
                for (int j = 0; j < m_Columns; j++)
                    cells[i, j].Gem = GetRandomGem();
            }

        } while (HaveStreaks() || m_Solver.FindSolutions().Length < 3);

        for (int i = 0; i < m_Rows; i++)
        {
            for (int j = 0; j < m_Columns; j++)
            {
                Cell cell = cells[i, j];
                Gem gem;

                gem = Instantiate(
                    cell.Gem,
                    transform);

                cell.Gem = null;

                gem.transform.localPosition =
                    cell.transform.localPosition + new Vector3(0, m_Rows + (m_Rows - i) * 0.45f);
            }
        }
    }

    // заполняет пустые поля блоками сверху
    void Cascade()
    {
        // найдем как много блоков осталось
        // а еще посчитаем в каких они столбцах
        int counter = 0;
        int[] columnCounter = new int[m_Columns];
        foreach (GameObject gobj in GameObject.FindGameObjectsWithTag("Gem"))
        {
            Gem gem = gobj.GetComponent<Gem>();

            if (gem == null)
                continue;

            counter++;
            columnCounter[gem.Column]++;
        }

        int emptyCells = m_Rows * m_Columns - counter;

        if (emptyCells <= 0)
            return;

        for(int j=0; j<m_Columns; j++)
        {
            for(int i=columnCounter[j]; i<m_Rows; i++)
            {
                Gem gem = Instantiate(
                    GetRandomGem(),//gemArray[index++],
                    transform
                );

                gem.transform.localPosition =
                    CellLocation(-m_Rows + i, j) + new Vector3(0, (m_Rows - i - 1) * 1.1f);

                gem.Column = j;
            }
        }
    }

    // дает любой базовый самоцвет
    Gem GetRandomGem()
    {
        return m_BasicGems[Random.Range(0, GemsCount)];
    }

    // поиск совпадающих рядов
    void UpdateStreaks()
    {
        // проходим по строкам
        // потом транспонируем поле (строки станут столбами)
        // чтобы пройти по столбцам
        // потом снова транспонируем
        for (int k = 0; k < 2; k++)
        {
            for (int i = 0; i < m_Rows; i++)
            {
                foreach(Streak newStreak in GetRowStreaks(i))
                {
                    Streak sameStreak = m_Streaks.Find(
                        (Streak a) => a.SetEquals(newStreak)
                    );

                    if (sameStreak == null)
                        m_Streaks.Add(newStreak);
                }
            }
                
            // транспонируя поле два раза мы вернем его к 
            // обратонму состоянию
            Transpose();
        }

        if (m_Streaks.Count == 0)
            return;

        m_Streaks.RemoveAll(
            (a) => TryGatherStreak(a)
        );

        if (m_GatheredStreaks.Count > 0)
            SendMessage("OnGetStreaks", m_GatheredStreaks, SendMessageOptions.DontRequireReceiver);

        foreach (Streak s in m_GatheredStreaks)
            foreach (Gem g in s)
                g.Gather();

        m_GatheredStreaks.Clear();
    }

    public (Gem, Gem)[] GetSolutions()
    {
        return m_Solver.FindSolutions();
    }

    // возвращает если есть совпадающий ряд
    List<Streak> GetRowStreaks(int i)
    {
        List<Streak> streaks = new List<Streak>();
        Streak newStreak = new Streak();

        for (int j = 0; j < m_Columns; j++)
        {
            if (cells[i, j].Gem == null ||
               !cells[i, j].Gem.CanStreak())
            {
                newStreak = new Streak();
                continue;
            }

            if (newStreak.GemMatches(cells[i, j].Gem))
                newStreak.Add(cells[i, j].Gem);
            else
            {
                newStreak = new Streak();
                newStreak.Add(cells[i, j].Gem);
            }

            if (newStreak.Count == m_ChainMinimalLength)
                streaks.Add(newStreak);
        }

        streaks.RemoveAll((Streak s) =>
            !s.IsMatch()
        );

        return streaks;
    }

    bool TryGatherStreak(Streak streak)
    {
        foreach (Gem g in streak)
            g.SetStreak();

        if (!streak.CanGather())
            return false;

        m_GatheredStreaks.Add(streak);

        return true;
    }

    // все блоки неподвижны
    void Peace()
    {
        // когда блоки не двигаются и все ячейки заняты
        // самое время проверить остались ли у игрока еще
        // ходы
        m_Solutions = m_Solver.FindSolutions();

        if (m_Solutions.Length > 0)
            return;

        for(int i = 0; i < m_Rows; i++)
        {
            for(int j = 0; j < m_Columns; j++)
            {
                Gem gem = cells[i, j].Gem;

                if (gem == null ||
                gem.Gathered ||
                gem.Streak ||
                gem.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Dynamic)
                {
                    return;
                }
            }
        }

        if (m_Solutions.Length == 0)
            Freeze();
    }

    void Freeze()
    {
        m_Freeze = true;

        SendMessage("OnFreeze", SendMessageOptions.DontRequireReceiver);
    }

    public bool HaveStreaks()
    {
        for (int i = 0; i < m_Rows; i++)
        {
            int counter = 0;
            Gem prevCell = cells[i, 0].Gem;

            for (int j = 0; j < m_Columns; j++)
            {
                Gem gem = cells[i, j].Gem;

                if (prevCell.IsMatch(gem))
                    counter++;
                else
                    counter = 1;

                if (counter == m_ChainMinimalLength)
                    return true;

                prevCell = gem;
            }
        }

        for (int j = 0; j < m_Columns; j++)
        {
            int counter = 0;
            Gem prevCell = cells[0, j].Gem;

            for (int i = 0; i < m_Rows; i++)
            {
                Gem gem = cells[i, j].Gem;

                if (prevCell.IsMatch(gem))
                    counter++;
                else
                    counter = 1;

                if (counter == m_ChainMinimalLength)
                    return true;

                prevCell = gem;
            }
        }

        return false;
    }

    bool GoodMove(Gem first, Gem second)
    {
        foreach ((Gem g1, Gem g2) turn in m_Solutions)
        {
            if (first == turn.g1 && second == turn.g2 ||
                first == turn.g2 && second == turn.g1)
            {
                return true;
            }
        }

        return false;
    }

    // попробовать поменять местами два самоцвета
    // чтобы посмотреть образуют ли пара комбинации
    public void TrySwap(Gem first, Gem second)
    {
        if (first == second)
            return;

        if (!first.IsNear(second))
            return;

        if (!first.CanSwap() || !second.CanSwap())
            return;

        if (m_Solutions.Length == 0)
            return;

        bool goodmove = GoodMove(first, second);

        // временно поменяем местами самоцветы
        // вдруг они образуют комбинации
        first.Swap(second, !goodmove);

        if (!goodmove)
            m_AudioPlayer.Fail(14.0f/60.0f);

        //StartCoroutine(SwapBack(first, second));
    }

    // определяет где на экране должен находится самоцвет
    private Vector3 CellLocation(int i, int j)
    {
        return new Vector3(-m_Columns / 2.0f, m_Rows / 2.0f) +
            new Vector3(j, -i) +
            new Vector3(0.5f, -0.5f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        for (int i = 0; i < m_Rows; i++)
        {
            for (int j = 0; j < m_Columns; j++)
            {
                Gizmos.DrawWireCube(
                    CellLocation(i, j) + transform.position,
                    new Vector3(1, 1)
                );
            }
        }
    }

    #region "Coroutines"

    private IEnumerator BotLogic(float delay)
    {
        do
        {
            if (m_Solutions != null && 
                m_Solutions.Length > 0)
            {
                (Gem, Gem) rndSolution = m_Solutions[Random.Range(0, m_Solutions.Length)];
                TrySwap(rndSolution.Item1, rndSolution.Item2);
            }

            yield return new WaitForSecondsRealtime(delay);
        } while (true);
    }

    /*private IEnumerator SwapBack(Gem first, Gem second)
    {
        yield return new WaitUntil(() =>
            first == null || second == null ||
            !first.Swapping && !second.Swapping
        );

        if (first == null || second == null)
            yield break;

        if (first.Gathered || second.Gathered)
            yield break;

        if (!first.CanSwap() || !second.CanSwap())
            yield break; ;

        first.Swap(second);

        first.TurnTag = false;
        second.TurnTag = false;

        if (m_AudioPlayer != null)
            m_AudioPlayer.Fail(0.0f);
    }*/

    #endregion

    #endregion
}
