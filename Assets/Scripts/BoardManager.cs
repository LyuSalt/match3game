using UnityEngine;
using System.Collections;

/// <summary>
/// Управляет созданием, очисткой и синхронизацией игрового поля.
/// </summary>
public class BoardManager : MonoBehaviour
{
    [Header("Настройки поля")]
    public int rows = 5;
    public int columns = 5;
    public GameObject cellPrefab;   // Префаб клетки
    public Sprite[] gemSprites;     // Спрайты для фишек
    public float cellSpacing = 1.5f;

    [HideInInspector] public Cell[,] cells;          // Массив логических клеток
    [HideInInspector] public GameObject[,] cellObjects; // Массив визуальных объектов

    private GameManager gameManager;

    // Awake не использует GameManager.Instance, чтобы избежать ошибок порядка инициализации.
    void Awake()
    {
        // Ссылка на GameManager будет получена в Start
    }

    void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager == null)
            Debug.LogError("GameManager.Instance is null in BoardManager.Start!");
    }

    /// <summary>
    /// Инициализирует пустые массивы под размер поля.
    /// </summary>
    public void InitializeBoard()
    {
        cells = new Cell[rows, columns];
        cellObjects = new GameObject[rows, columns];
    }

    /// <summary>
    /// Заполняет поле случайными фишками с назначением спрайтов и типов.
    /// </summary>
    public void CreateVisualBoard()
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                GameObject cellObj = Instantiate(cellPrefab);
                cellObjects[i, j] = cellObj;
                float posX = j * cellSpacing;
                float posY = i * cellSpacing;
                cellObj.transform.position = new Vector3(posX, posY, 0);
                cellObj.name = $"Cell_{i}_{j}";

                Cell cell = cellObj.GetComponent<Cell>();
                if (cell == null) cell = cellObj.AddComponent<Cell>();
                cell.Initialize(i, j);

                if (gemSprites != null && gemSprites.Length > 0)
                {
                    int randomSpriteIndex = UnityEngine.Random.Range(0, gemSprites.Length);
                    Sprite sprite = gemSprites[randomSpriteIndex];
                    GemType type = (GemType)randomSpriteIndex;
                    cell.SetSpriteAndType(sprite, type);
                }
                cells[i, j] = cell;
            }
        }
    }

    /// <summary>
    /// Создаёт поле без начальных матчей (повторяет генерацию до 50 раз).
    /// </summary>
    public void CreateVisualBoardWithoutMatches()
    {
        // Гарантируем, что gameManager и matchFinder существуют
        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (gameManager == null || gameManager.matchFinder == null)
        {
            Debug.LogError("MatchFinder не инициализирован в BoardManager!");
            // Попытка найти/создать MatchFinder
            if (gameManager != null && gameManager.matchFinder == null)
            {
                gameManager.matchFinder = gameManager.GetComponent<MatchFinder>();
                if (gameManager.matchFinder == null)
                    gameManager.matchFinder = gameManager.gameObject.AddComponent<MatchFinder>();
            }
            else
                return; // Если всё ещё null, выходим, чтобы избежать ошибки
        }

        int attempts = 0;
        while (attempts < 50)
        {
            CreateVisualBoard();
            if (gameManager.matchFinder.FindAllMatches().Count == 0) break;
            ClearBoard();
            attempts++;
        }
        Debug.Log($"Поле готово за {attempts} попыток");
    }

    /// <summary>
    /// Полностью очищает поле (удаляет все объекты).
    /// </summary>
    public void ClearBoard()
    {
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < columns; j++)
            {
                if (cellObjects[i, j] != null)
                {
                    Destroy(cellObjects[i, j]);
                    cellObjects[i, j] = null;
                    cells[i, j] = null;
                }
            }
    }

    /// <summary>
    /// Синхронизирует массивы cells и cellObjects после перетасовки объектов.
    /// </summary>
    public void SyncCellsWithObjects()
    {
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < columns; j++)
            {
                if (cellObjects[i, j] != null)
                {
                    Cell cell = cellObjects[i, j].GetComponent<Cell>();
                    cells[i, j] = cell;
                }
                else
                    cells[i, j] = null;
            }
    }

    /// <summary>
    /// Создаёт новую клетку с случайным типом и спрайтом.
    /// </summary>
    public void CreateNewCell(int row, int col)
    {
        GameObject cellObj = Instantiate(cellPrefab);
        cellObjects[row, col] = cellObj;
        cellObj.name = $"Cell_{row}_{col}";
        Cell cell = cellObj.GetComponent<Cell>();
        if (cell == null) cell = cellObj.AddComponent<Cell>();
        cell.Initialize(row, col);

        if (gemSprites != null && gemSprites.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, gemSprites.Length);
            Sprite sprite = gemSprites[randomIndex];
            GemType type = (GemType)randomIndex;
            cell.SetSpriteAndType(sprite, type);
        }
        cells[row, col] = cell;
    }

    /// <summary>
    /// Меняет местами клетки (обновляет массивы и позиции объектов).
    /// </summary>
    public void SwapCellObjects(int row1, int col1, int row2, int col2)
    {
        Cell buf = cells[row1, col1];
        cells[row1, col1] = cells[row2, col2];
        cells[row2, col2] = buf;

        GameObject buf1 = cellObjects[row1, col1];
        cellObjects[row1, col1] = cellObjects[row2, col2];
        cellObjects[row2, col2] = buf1;

        cells[row1, col1].row = row1; cells[row1, col1].col = col1;
        cells[row2, col2].row = row2; cells[row2, col2].col = col2;

        cellObjects[row1, col1].transform.position = new Vector3(col1 * cellSpacing, row1 * cellSpacing, 0);
        cellObjects[row2, col2].transform.position = new Vector3(col2 * cellSpacing, row2 * cellSpacing, 0);
    }
}
