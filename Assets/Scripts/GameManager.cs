using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using System;
using DG.Tweening;
using Random = UnityEngine.Random;

[System.Serializable] // сделать видимым в инспекторе
public class Vector2IntEvent : UnityEvent<Vector2Int> { }
[System.Serializable]
public class Vector2Int2Event : UnityEvent<Vector2Int, Vector2Int> { }
[System.Serializable]
public class Vector2IntListEvent : UnityEvent<List<Vector2Int>> { }
[System.Serializable]
public class BombExplodedEvent : UnityEvent<Vector2Int, List<Vector2Int>> { }

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
    public BombExplodedEvent OnBombExploded;        //бомба взорвалась и задела клетки!

    // Данные игры
    private Cell[,] cells;
    private GameObject[,] cellObjects;
    public List<(int, int)> cellsToDelete = new List<(int, int)>();
    private bool isAnimating = false; // блокировка кликов и действий

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
        CreateVisualBoardWithoutMatches();
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
    private void SyncCellsWithObjects()
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (cellObjects[i, j] != null)
                {
                    Cell cell = cellObjects[i, j].GetComponent<Cell>();
                    cells[i, j] = cell;
                    // координаты уже обновлены в ShuffleObjects
                }
                else
                {
                    cells[i, j] = null;
                }
            }
        }
    }
    void OnCellClickedEvent(Vector2Int position)
    {
        if (isAnimating) return; // если идёт анимация – игнорируем клики
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
    }

    private IEnumerator AnimatedSwap(int r1, int c1, int r2, int c2, float duration = 0.15f)
    {
        // Получаем объекты
        GameObject obj1 = cellObjects[r1, c1];
        GameObject obj2 = cellObjects[r2, c2];
        if (obj1 == null || obj2 == null) yield break;

        // Запоминаем начальные позиции
        Vector3 pos1 = obj1.transform.position;
        Vector3 pos2 = obj2.transform.position;

        // Запускаем анимацию перемещения
        obj1.transform.DOMove(pos2, duration).SetEase(Ease.InOutQuad);
        obj2.transform.DOMove(pos1, duration).SetEase(Ease.InOutQuad);

        // Ждём окончания анимации
        yield return new WaitForSeconds(duration);

        // Теперь объекты физически на новых местах, обновляем данные
        // Меняем местами в массивах (как в SwapCellObjects)
        Cell tempCell = cells[r1, c1];
        cells[r1, c1] = cells[r2, c2];
        cells[r2, c2] = tempCell;

        GameObject tempObj = cellObjects[r1, c1];
        cellObjects[r1, c1] = cellObjects[r2, c2];
        cellObjects[r2, c2] = tempObj;

        // Обновляем координаты у объектов Cell
        cells[r1, c1].row = r1; cells[r1, c1].col = c1;
        cells[r2, c2].row = r2; cells[r2, c2].col = c2;

        // (Необязательно) точная установка позиции, на случай если DOMove не довело до конца
        cellObjects[r1, c1].transform.position = new Vector3(c1 * cellSpacing, r1 * cellSpacing, 0);
        cellObjects[r2, c2].transform.position = new Vector3(c2 * cellSpacing, r2 * cellSpacing, 0);
    }
    private void ShuffleObjects()
    {
        // Собираем все объекты в список
        List<GameObject> objectsList = new List<GameObject>();
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (cellObjects[i, j] != null)
                    objectsList.Add(cellObjects[i, j]);
            }
        }

        // Перемешиваем список
        for (int i = 0; i < objectsList.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, objectsList.Count);
            GameObject temp = objectsList[i];
            objectsList[i] = objectsList[randomIndex];
            objectsList[randomIndex] = temp;
        }

        // Раскладываем объекты обратно в массив по порядку
        int index = 0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (index < objectsList.Count)
                {
                    cellObjects[i, j] = objectsList[index];
                    // Обновляем координаты в компоненте Cell
                    Cell cell = cellObjects[i, j].GetComponent<Cell>();
                    if (cell != null)
                    {
                        cell.row = i;
                        cell.col = j;
                    }
                    index++;
                }
                else
                {
                    cellObjects[i, j] = null;
                }
            }
        }
    }

    private Vector3 FindObjectPosition(GameObject obj)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (cellObjects[i, j] == obj)
                {
                    return new Vector3(j * cellSpacing, i * cellSpacing, 0);
                }
            }
        }
        return Vector3.zero;
    }
    private IEnumerator ShuffleBoardCoroutine()
    {
        isAnimating = true;

        // 1. Сохраняем все объекты и их текущие позиции
        List<GameObject> allObjects = new List<GameObject>();
        Vector3 centerPos = new Vector3((columns - 1) * cellSpacing / 2f,
                                        (rows - 1) * cellSpacing / 2f, 0);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (cellObjects[i, j] != null)
                {
                    allObjects.Add(cellObjects[i, j]);
                    // Отключаем коллайдеры, чтобы игрок не кликал во время анимации
                    Collider2D col = cellObjects[i, j].GetComponent<Collider2D>();
                    if (col != null) col.enabled = false;
                }
            }
        }

        // 2. Анимация СБОРКИ в центр
        foreach (var obj in allObjects)
        {
            obj.transform.DOMove(centerPos, 0.4f)
                .SetEase(Ease.InQuad);
        }

        yield return new WaitForSeconds(0.5f); // Ждём, пока все соберутся

        // 3. ВСТРЯСКА (трясём каждый объект)
        foreach (var obj in allObjects)
        {
            obj.transform.DOShakePosition(0.3f, 0.3f, 10, 90, false, true)
                .SetEase(Ease.OutQuad);
        }

        // Даём немного времени на встряску
        yield return new WaitForSeconds(0.4f);

        // 4. Перемешиваем ТИПЫ фишек (логика)
        ShuffleTypes();

        // 5. Анимация РАЗБРОСА по новым позициям
        // Для каждого объекта определяем новую позицию на основе обновлённых массивов
        foreach (var obj in allObjects)
        {
            // Находим, где сейчас лежит этот объект в массиве (после перемешивания типов позиции не изменились)
            // Но нам нужно, чтобы объект переместился на новое место, соответствующее новому типу?
            // В нашей логике мы просто перемешали типы, но объекты остались на своих местах.
            // Однако для эффекта "разброса" мы хотим, чтобы объекты физически перелетели на другие места,
            // т.е. мы должны перемешать сами объекты между ячейками.
            // Проще: мы можем перемешать объекты в массивах, а затем анимировать их перемещение.
        }

        // Поскольку мы перемешали только типы, а объекты остались на месте, то разброс не даст эффекта.
        // Поэтому нам нужно также перемешать объекты между ячейками, чтобы они поменялись местами.
        // Сделаем это:
        ShuffleObjects();
        SyncCellsWithObjects();
        // Теперь объекты привязаны к новым позициям, анимируем их перемещение из центра в новые позиции.
        foreach (var obj in allObjects)
        {
            Vector3 targetPos = FindObjectPosition(obj);
            float delay = UnityEngine.Random.Range(0f, 0.1f);
            obj.transform.DOMove(targetPos, 0.4f)
                .SetDelay(delay)
                .SetEase(Ease.OutBack); // пружинистый эффект
        }

        yield return new WaitForSeconds(0.6f); // Ждём завершения разброса

        // 6. Включаем коллайдеры обратно
        foreach (var obj in allObjects)
        {
            Collider2D col = obj.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }

        // 7. Проверяем наличие матчей после перемешивания
        List<Vector2Int> matches = FindAllMatches();
        if (matches.Count > 0)
        {
            Debug.Log("Перемешивание создало матчи!");
            ProcessMatchesAfterSwap(); // запускаем каскад
        }
        else
        {
            // Если матчей нет – пробуем ещё раз или перегенерируем
            Debug.Log("Матчей не появилось, перегенерируем поле.");
            ClearBoard();
            CreateVisualBoardWithoutMatches();
        }

        isAnimating = false;
    }
    private void ShuffleTypes()
    {
        // Собираем все типы в список
        List<GemType> allTypes = new List<GemType>();
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (cells[i, j] != null)
                    allTypes.Add(cells[i, j].Type);
            }
        }

        // Перемешиваем список (Fisher-Yates)
        for (int i = 0; i < allTypes.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, allTypes.Count);
            GemType temp = allTypes[i];
            allTypes[i] = allTypes[randomIndex];
            allTypes[randomIndex] = temp;
        }

        // Раздаём перемешанные типы обратно ячейкам
        int index = 0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (cells[i, j] != null)
                {
                    cells[i, j].Type = allTypes[index];
                    // Обновляем спрайт в соответствии с новым типом
                    Sprite newSprite = gemSprites[(int)allTypes[index]];
                    cells[i, j].SetSpriteAndType(newSprite, allTypes[index]);
                    index++;
                }
            }
        }
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
        // Ждём, пока анимации удаления завершатся
        yield return new WaitForSeconds(0.5f); // можно подобрать точнее

        // Удаляем ссылки на уничтоженные объекты (сами объекты уже Destroy)
        foreach (var pos in positions)
        {
            // Объект мог быть уже уничтожен анимацией, просто очищаем ссылки
            if (cellObjects[pos.x, pos.y] != null)
            {
                // На всякий случай, если анимация не сработала
                Destroy(cellObjects[pos.x, pos.y]);
                cellObjects[pos.x, pos.y] = null;
                cells[pos.x, pos.y] = null;
            }
            else
            {
                // Объект уже уничтожен, но ссылки могли остаться — чистим
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
                Debug.Log("⚠️ Нет ходов! Запускаем анимацию перемешивания...");
                ResetSelection();
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }
    
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
