using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using System;

[System.Serializable] // сделать видимым в инспекторе
public class Vector2IntEvent : UnityEvent<Vector2Int> { }
[System.Serializable]
public class Vector2Int2Event : UnityEvent<Vector2Int, Vector2Int> { }
[System.Serializable]
public class Vector2IntListEvent : UnityEvent<List<Vector2Int>> { }


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Настройки поля")]
    public int rows = 5;
    public int columns = 5;
    public GameObject cellPrefab;

    [Header("Спрайты для фишек")]
    public Sprite[] gemSprites;

    [Header("Визуальные настройки")]
    public float cellSpacing = 1.5f;

    //объявление полей-событий. Создаём кнопки событий
    [Header("События")]
    public Vector2IntEvent OnCellClicked;           // Клик [i,j]
    public Vector2Int2Event OnCellsSwapped;         // Обмен [pos1,pos2]
    public Vector2IntListEvent OnMatchFound;        // Матч!
    public Vector2IntListEvent OnCellsToDelete;     // Удалить клетки

    // Данные игры
    private Cell[,] cells;
    private GameObject[,] cellObjects;
    public List<(int, int)> cellsToDelete = new List<(int, int)>();

    // Для выбора клеток
    private Vector2Int firstSelectedPos = new Vector2Int(-1, -1); // первая выбранная клетка, пока тут несуществующие координаты
    private GameObject firstSelectedObject = null; //визуальный объект по этим координатам

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // gameObject - то, на чём висит скрипт
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("Игра началась!");
        InitializeBoard();
        CreateVisualBoard();
        if (OnCellClicked == null)
        {
            OnCellClicked = new Vector2IntEvent();
            OnCellClicked.AddListener(OnCellClickedEvent);
        }
    
    }
    void CreateVisualBoardWithoutMatches()
    {
        int attempts = 0;
        while (attempts < 50)
        {
            CreateVisualBoard();
            if (FindAllMatches().Count == 0) break;  // ✅ без матчей!
            ClearBoard();
            attempts++;
        }
        Debug.Log($"Поле готово за {attempts} попыток");
    }
    void InitializeBoard()
    {
        cells = new Cell[rows, columns];
        cellObjects = new GameObject[rows, columns];
    }

    void CreateVisualBoard()
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
                if (cell == null)
                    cell = cellObj.AddComponent<Cell>();

                cell.Initialize(i, j); 

                if (gemSprites != null && gemSprites.Length > 0)
                {
                    int randomSpriteIndex = UnityEngine.Random.Range(0, gemSprites.Length);
                    //cell.SetSprite(gemSprites[randomSpriteIndex]);
                    Sprite sprite = gemSprites[randomSpriteIndex];
                    GemType type = (GemType)randomSpriteIndex;   // индекс → enum

                    cell.SetSpriteAndType(sprite, type);
                }
                cells[i, j] = cell;
            }
        }
    }

    void OnCellClickedEvent(Vector2Int position)
    {
        int row = position.x, col = position.y;

        // ✅ ПРОВЕРКА: поле готово?
        if (cells[row, col] == null || cellObjects[row, col] == null)
        {
            Debug.LogWarning($"Клетка [{row},{col}] не готова!");
            return;
        }

        Debug.Log($"Клик по клетке: [{row},{col}]");

        if (firstSelectedPos.x == -1) // Первый выбор
        {
            firstSelectedPos = position;

            // ✅ ПРОВЕРКА: объект живой?
            firstSelectedObject = cellObjects[row, col];
            if (firstSelectedObject != null)
                firstSelectedObject.transform.localScale = Vector3.one * 1.2f;

            Debug.Log($"Выбрана первая клетка: [{row},{col}]");
        }
        else // Второй выбор
        {
            // ✅ СБРОС ПЕРВОЙ выделенной
            if (firstSelectedObject != null)
            {
                firstSelectedObject.transform.localScale = Vector3.one;
                firstSelectedObject = null;
            }

            if (IsAdjacent(firstSelectedPos.x, firstSelectedPos.y, row, col))
            {
                Debug.Log("Клетки соседние! Меняем...");

                // ✅ ПРОВЕРКА перед свапом
                if (cells[firstSelectedPos.x, firstSelectedPos.y] != null &&
                    cells[row, col] != null)
                {
                    SwapCellObjects(firstSelectedPos.x, firstSelectedPos.y, row, col);
                    bool ok = CheckAfterSwap(firstSelectedPos, position);

                    if (ok)
                    {
                        OnCellsSwapped?.Invoke(firstSelectedPos, position);
                        ProcessMatchesAfterSwap();
                    }
                    else
                    {
                        Debug.Log("Нет совпадений, откат");
                        SwapCellObjects(firstSelectedPos.x, firstSelectedPos.y, row, col);
                    }
                }
            }

            firstSelectedPos = new Vector2Int(-1, -1);  // ✅ СБРОС
        }
        //int row = position.x, col = position.y;


        //if (cells[row, col] == null || cellObjects[row, col] == null)
        //    return;

        //Debug.Log($"Клик по клетке: [{row},{col}]");

        //if (firstSelectedPos.x == -1) // Первый выбор
        //{
        //    firstSelectedPos = position;
        //    firstSelectedObject = cellObjects[row, col];
        //    firstSelectedObject.transform.localScale = Vector3.one * 1.2f;
        //    Debug.Log($"Выбрана первая клетка: [{row},{col}]");
        //}
        //else // Второй выбор
        //{
        //    Debug.Log($"Выбрана вторая клетка: [{row},{col}]");
        //    firstSelectedObject.transform.localScale = Vector3.one;

        //    if (IsAdjacent(firstSelectedPos.x, firstSelectedPos.y, row, col))
        //    { 
        //        Debug.Log("Клетки соседние! Меняем местами...");
        //        // 1. меняем местами
        //        SwapCellObjects(firstSelectedPos.x, firstSelectedPos.y, row, col);
        //        // 2. проверяем, случилось ли что-то полезное
        //        bool ok = CheckAfterSwap(firstSelectedPos, position);

        //        if (ok)
        //        {
        //            // ход корректный: можно оповестить, что свап принят
        //            OnCellsSwapped?.Invoke(firstSelectedPos, position);
        //            ProcessMatchesAfterSwap(); // ✅ ОСНОВНОЙ ВЫЗОВ!
        //        }
        //        else
        //        {
        //            // хода нет – откатываем назад
        //            Debug.Log("Совпадений нет, откатываем ход");
        //            SwapCellObjects(firstSelectedPos.x, firstSelectedPos.y, row, col);
        //        }
        //    }
        //    else
        //    {
        //        Debug.Log("Клетки НЕ соседние!");
        //    }

        //    firstSelectedPos = new Vector2Int(-1, -1);
        //    firstSelectedObject = null;
        //}
    }

    
    bool IsAdjacent(int row1, int col1, int row2, int col2)
    {
        int rowDiff = Mathf.Abs(row1 - row2);
        int colDiff = Mathf.Abs(col1 - col2);
        return (rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1);
    }

    void SwapCellObjects(int row1, int col1, int row2, int col2)
    {
        // Меняем ссылки
        Cell buf = cells[row1, col1];
        cells[row1, col1] = cells[row2, col2];
        cells[row2, col2] = buf;

        GameObject buf1 = cellObjects[row1, col1];
        cellObjects[row1, col1] = cellObjects[row2, col2];
        cellObjects[row2, col2] = buf1;

        // Обновляем координаты
        cells[row1, col1].row = row1; cells[row1, col1].col = col1;
        cells[row2, col2].row = row2; cells[row2, col2].col = col2;

        // Перепозиционируем
        cellObjects[row1, col1].transform.position = new Vector3(col1 * cellSpacing, row1 * cellSpacing, 0);
        cellObjects[row2, col2].transform.position = new Vector3(col2 * cellSpacing, row2 * cellSpacing, 0);

        Debug.Log($"Клетки [{row1},{col1}] ↔ [{row2},{col2}] поменяны!");
    }

    bool CheckAfterSwap(Vector2Int aPos, Vector2Int bPos)
    {
        return FindAllMatches().Count > 0;
        //bool matchA = CheckMatchesAt(aPos.x, aPos.y);
        //bool matchB = CheckMatchesAt(bPos.x, bPos.y);

        // сюда позже можно добавить проверку бомб:
        // bool bombA = IsBombAt(aPos.x, aPos.y);
        // bool bombB = IsBombAt(bPos.x, bPos.y);

        //return matchA || matchB;
    }
    // ✅ ПОИСК ВСЕХ МАТЧЕЙ НА ДОСКЕ
    public List<Vector2Int> FindAllMatches()
    {
        List<Vector2Int> matches = new List<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector2Int pos = new Vector2Int(row, col);
                if (visited.Contains(pos)) continue;

                List<Vector2Int> group = GetMatchGroup(row, col);
                if (group.Count >= 3)
                {
                    foreach (var p in group)
                        visited.Add(p);
                    matches.AddRange(group);
                }
            }
        }
        return matches;
    }

    private List<Vector2Int> GetMatchGroup(int centerRow, int centerCol)
    {
        List<Vector2Int> group = new List<Vector2Int>();
        GemType centerType = cells[centerRow, centerCol].Type;
        group.Add(new Vector2Int(centerRow, centerCol));

        CheckHorizontalLine(centerRow, centerCol, centerType, group);
        CheckVerticalLine(centerRow, centerCol, centerType, group);

        return group;
    }

    private void CheckHorizontalLine(int row, int startCol, GemType type, List<Vector2Int> group)
    {
        int leftCount = 0, rightCount = 0;
        for (int c = startCol - 1; c >= 0; c--)
        {
            if (cells[row, c].Type == type) leftCount++;
            else break;
        }
        for (int c = startCol + 1; c < columns; c++)
        {
            if (cells[row, c].Type == type) rightCount++;
            else break;
        }
        if (leftCount + rightCount + 1 >= 3)
        {
            for (int c = startCol - leftCount; c <= startCol + rightCount; c++)
                group.Add(new Vector2Int(row, c));
        }
    }

    private void CheckVerticalLine(int startRow, int col, GemType type, List<Vector2Int> group)
    {
        int upCount = 0, downCount = 0;
        for (int r = startRow - 1; r >= 0; r--)
        {
            if (cells[r, col].Type == type) upCount++;
            else break;
        }
        for (int r = startRow + 1; r < rows; r++)
        {
            if (cells[r, col].Type == type) downCount++;
            else break;
        }
        if (upCount + downCount + 1 >= 3)
        {
            for (int r = startRow - upCount; r <= startRow + downCount; r++)
                group.Add(new Vector2Int(r, col));
        }
    }
    bool HasValidMoves()
    {
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                // Проверяем соседей вверх/вправо
                if (row < rows - 1 && IsSwapValid(row, col, row + 1, col)) return true;
                if (col < columns - 1 && IsSwapValid(row, col, row, col + 1)) return true;
            }
        return false;
    }

    bool IsSwapValid(int r1, int c1, int r2, int c2)
    {
        GemType t1 = cells[r1, c1].Type, t2 = cells[r2, c2].Type;
        cells[r1, c1].Type = t2; cells[r2, c2].Type = t1;  // свап
        bool match = FindAllMatches().Count > 0;
        cells[r1, c1].Type = t1; cells[r2, c2].Type = t2;  // откат
        return match;
    }

    // ✅ ГЛАВНЫЙ МЕТОД: обработка матчей после свапа
    public void ProcessMatchesAfterSwap()
    {
        List<Vector2Int> matches = FindAllMatches();
        if (matches.Count == 0) return;

        Debug.Log($"🎉 Найдено {matches.Count} совпадений!");
        OnMatchFound?.Invoke(matches);

        cellsToDelete.Clear();
        foreach (var pos in matches)
        {
            cellsToDelete.Add((pos.x, pos.y));
            cells[pos.x, pos.y].OnMatch();
        }
        OnCellsToDelete?.Invoke(matches);

        StartCoroutine(DeleteAndRefill(matches));
    }

    private IEnumerator DeleteAndRefill(List<Vector2Int> positions)
    {
        yield return new WaitForSeconds(0.3f);

        foreach (var pos in positions)
        {
            if (cellObjects[pos.x, pos.y] != null)
            {
                Destroy(cellObjects[pos.x, pos.y]);
                cellObjects[pos.x, pos.y] = null;
                cells[pos.x, pos.y] = null;
            }
        }

        yield return StartCoroutine(FillEmptyCells());
    }

    private IEnumerator FillEmptyCells()
    {
        // Гравитация
        bool moved = true;
        while (moved)
        {
            moved = false;
            for (int col = 0; col < columns; col++)
            {
                for (int row = rows - 1; row > 0; row--)
                {
                    if (cells[row, col] == null && cells[row - 1, col] != null)
                    {
                        cells[row, col] = cells[row - 1, col];
                        cellObjects[row, col] = cellObjects[row - 1, col];
                        cells[row - 1, col] = null;
                        cellObjects[row - 1, col] = null;

                        cells[row, col].row = row;
                        cells[row, col].col = col;

                        float targetY = row * cellSpacing;
                        StartCoroutine(AnimateFall(cellObjects[row, col].transform,
                            new Vector3(col * cellSpacing, targetY, 0), 0.2f));

                        moved = true;
                    }
                }
            }
            if (moved) yield return new WaitForSeconds(0.1f);
        }

        // Новые фишки сверху
        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (cells[row, col] == null)
                {
                    CreateNewCell(row, col);
                    cellObjects[row, col].transform.position =
                        new Vector3(col * cellSpacing, -cellSpacing, 0);
                    StartCoroutine(AnimateFall(cellObjects[row, col].transform,
                        new Vector3(col * cellSpacing, row * cellSpacing, 0), 0.3f));
                }
            }
        }

        yield return new WaitForSeconds(0.4f);

        // Каскадные матчи
        List<Vector2Int> newMatches = FindAllMatches();
        if (newMatches.Count > 0)
        {
            ProcessMatchesAfterSwap(); // каскады
        }
        else
        {
            if (!HasValidMoves())
            {
                Debug.Log("⚠️ Нет ходов! Перегенерируем...");
                ResetSelection();
                yield return new WaitForSeconds(0.5f);
                ClearBoard();
                CreateVisualBoardWithoutMatches();  // ← без матчей!
            }
            // Нет ходов → полная перегенерация!
            //Debug.Log(" Мёртвое поле! Перегенерируем...");

            //ClearBoard();
            //CreateVisualBoard();
        }
    }
    //bool CheckMatchesAt(int row, int col)
    //{
    //    GemType center = cells[row, col].Type;

    //    bool found = false;

    //    // --- горизонталь ---
    //    // [col-2, col-1, col]
    //    if (col >= 2 &&
    //        cells[row, col - 1].Type == center &&
    //        cells[row, col - 2].Type == center)
    //        found = true;

    //    // [col-1, col, col+1]
    //    if (col >= 1 && col + 1 < columns &&
    //        cells[row, col - 1].Type == center &&
    //        cells[row, col + 1].Type == center)
    //        found = true;

    //    // [col, col+1, col+2]
    //    if (col + 2 < columns &&
    //        cells[row, col + 1].Type == center &&
    //        cells[row, col + 2].Type == center)
    //        found = true;

    //    // --- вертикаль ---
    //    // [row-2, row-1, row]
    //    if (row >= 2 &&
    //        cells[row - 1, col].Type == center &&
    //        cells[row - 2, col].Type == center)
    //        found = true;

    //    // [row-1, row, row+1]
    //    if (row >= 1 && row + 1 < rows &&
    //        cells[row - 1, col].Type == center &&
    //        cells[row + 1, col].Type == center)
    //        found = true;

    //    // [row, row+1, row+2]
    //    if (row + 2 < rows &&
    //        cells[row + 1, col].Type == center &&
    //        cells[row + 2, col].Type == center)
    //        found = true;

    //    return found;
    //}
    private void CreateNewCell(int row, int col)
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

    private IEnumerator AnimateFall(Transform obj, Vector3 targetPos, float duration)
    {
        Vector3 startPos = obj.position;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.position = Vector3.Lerp(startPos, targetPos, t * t);
            yield return null;
        }
        obj.position = targetPos;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) ResetSelection();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClearBoard();
            CreateVisualBoard();
        }
    }

    void ResetSelection()
    {
        //if (firstSelectedObject != null)
        //{
        //    firstSelectedObject.transform.localScale = Vector3.one;
        //    firstSelectedPos = new Vector2Int(-1, -1);
        //    firstSelectedObject = null;

        //}
        if (firstSelectedObject != null)
        {
            // ✅ ПРОВЕРКА живой ли объект перед масштабированием
            try
            {
                firstSelectedObject.transform.localScale = Vector3.one;
            }
            catch (MissingReferenceException)
            {
                // Игнорируем — объект уже уничтожен
            }
            catch (NullReferenceException)
            {
                // Дополнительная защита
            }

            firstSelectedObject = null;  // Очищаем ссылку
        }

        // ✅ Сброс позиции первой выбранной клетки
        firstSelectedPos = new Vector2Int(-1, -1);
    }

    void ClearBoard()
    {
        ResetSelection();
        cellsToDelete.Clear();
        for (int i = 0; i < rows; i++)
        {
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
    }
}
