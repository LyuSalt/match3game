using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.Rendering.DebugUI.Table;

public class GameManager : MonoBehaviour
{
    [Header("Настройки поля")]
    public int rows = 5;
    public int columns = 5;
    public GameObject cellPrefab;
    
    [Header("Спрайты для фишек")]
    public Sprite[] gemSprites; // набор (массив) спрайтов
    
    [Header("Визуальные настройки")]
    public float cellSpacing = 1.5f; // расстояние между фишками на поле
    
    // Данные игры
    private Cell[,] cells;// таблица, отвечающая за логику игры
    private GameObject[,] cellObjects; //та же таблица, хранит ссылки на реальные GameObject‑ы на сцене (фишки), у которых есть Transform, SpriteRenderer, Collider 
    private List<(int, int)> cellsToDelete = new List<(int, int)>();
    
    // Для выбора клеток
    private Cell firstSelectedCell = null; //ссылка на объект Cell который кликнули
    private GameObject firstSelectedObject = null; // ссылка на gameobject, который кликнули
    
    void Start()
    {
        Debug.Log("Игра началась!");
        InitializeBoard();
        CreateVisualBoard();
    }
    
    void InitializeBoard() // резервируем память под два "поля" в массивах. Создает пустые поля с ячейками заданного размера
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
                // 1. Создаём объект фишки
                GameObject cellObj = Instantiate(cellPrefab); // создаем объект по шаблону префаба
                cellObjects[i, j] = cellObj;
                
                // 2. Позиционируем. Это масштабируемость поля. Насколько клетки близко\далеко расположены друг от друга
                float posX = j * cellSpacing; // координату умножаем на расстояние (на 1.5)
                float posY = i * cellSpacing;
                cellObj.transform.position = new Vector3(posX, posY, 0); // передаем в transform расположение объекта после вычислений
                cellObj.name = $"Cell_{i}_{j}"; // фиксируем уникальное имя объекта
                
                // 3. Добавляем компонент Cell и инициализируем
                Cell cell = cellObj.GetComponent<Cell>(); // Создаем переменную типа Cell, в ней будет храниться ссылка на скрипт cellObj, затем проверяем, есть ли на объекте скрипт Cell
                if (cell == null) // есть ссылка или нет?
                    cell = cellObj.AddComponent<Cell>(); // если нет, то добавляем на него компонент(скрипт)
                //тут он будет добавляться, так как у префаба нет скрипта Cell

                cell.Initialize(this, i, j); //инициализация объекта. Метод скрипта Cell.
                
                // 4. Устанавливаем случайный спрайт из ассетов
                if (gemSprites != null && gemSprites.Length > 0)
                {
                    int randomSpriteIndex = Random.Range(0, gemSprites.Length);
                    cell.SetSprite(gemSprites[randomSpriteIndex]); //Устанавливаем спрайт (через cell)
                }
                
                // 5. Сохраняем ссылку
                cells[i, j] = cell; //кладём эту же ссылку в глобальный массив
            }
        }
    }
    
    public void MarkCellToDelete(int row, int col)
    {
        cellsToDelete.Add((row, col));
        Debug.Log($"Клетка [{row},{col}] помечена на удаление");
    }
    
    public void OnCellClicked(int row, int col)
    {
        Debug.Log($"Клик по клетке: {row}, {col}");
        
        if (firstSelectedCell == null) //если клукнули на клетку и она не пуста
        {
            // Первый выбор
            firstSelectedCell = cells[row, col]; //переприсваиваем ссылки
            firstSelectedObject = cellObjects[row, col];
            
            // Подсвечиваем (немного увеличиваем)
            firstSelectedObject.transform.localScale = Vector3.one * 1.2f;
            
            Debug.Log($"Выбрана первая клетка: [{row},{col}]");
        }
        else
        {
            // Второй выбор
            Debug.Log($"Выбрана вторая клетка: [{row},{col}]");
            
            // Сбрасываем подсветку первой клетки
            firstSelectedObject.transform.localScale = Vector3.one;
            
            // Проверяем, соседние ли клетки
            if (IsAdjacent(firstSelectedCell.row, firstSelectedCell.col, row, col))
            {
                Debug.Log("Клетки соседние! Меняем местами...");
                
                // Меняем местами спрайты
                SwapCellObject(firstSelectedCell.row, firstSelectedCell.col, row, col);
            }
            else
            {
                Debug.Log("Клетки НЕ соседние!");
            }
            
            // Сбрасываем выбор
            firstSelectedCell = null;
            firstSelectedObject = null;
        }
    }
    
    bool IsAdjacent(int row1, int col1, int row2, int col2)
    {
        int rowDiff = Mathf.Abs(row1 - row2);
        int colDiff = Mathf.Abs(col1 - col2);
        
        // Соседние по горизонтали или вертикали
        return (rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1);
    }
    
    void SwapCellObject(int row1, int col1, int row2, int col2)
    {
        //// Получаем спрайт-рендереры
        //SpriteRenderer sr1 = cellObjects[row1, col1].GetComponent<SpriteRenderer>(); //компонент, который отвечает за ОТОБРАЖЕНИЕ спрайта на экране
        //SpriteRenderer sr2 = cellObjects[row2, col2].GetComponent<SpriteRenderer>();

        //// Меняем спрайты местами
        //Sprite tempSprite = sr1.sprite;
        //sr1.sprite = sr2.sprite;
        //sr2.sprite = tempSprite;
        Cell buf = null;
        buf = cells[row1, col1];
        cells[row1, col1] = cells[row2,col2];
        cells[row2, col2] = buf;
        buf = null;

        GameObject buf1 = null;
        buf1 = cellObjects[row1, col1];
        cellObjects[row1, col1] = cellObjects[row2, col2];
        cellObjects[row2, col2] = buf1;
        buf1 = null;

        // Обновляем координаты
        cells[row1, col1].row = row1; cells[row1, col1].col = col1;
        cells[row2, col2].row = row2; cells[row2, col2].col = col2;

        // Перепозиционируем объекты
        cellObjects[row1, col1].transform.position = new Vector3(col1 * cellSpacing, row1 * cellSpacing, 0); //cellSpacing - расстояние между фишками на поле
        cellObjects[row2, col2].transform.position = new Vector3(col2 * cellSpacing, row2 * cellSpacing, 0);

        Debug.Log($"Объекты клеток [{row1},{col1}] и [{row2},{col2}] поменяны местами");
    }
    
    void Update()
    {
        // Клавиша R для сброса
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetSelection();
        }
        
        // Клавиша Space для создания нового поля
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClearBoard();
            CreateVisualBoard();
        }
    }
    
    void ResetSelection()
    {
        if (firstSelectedObject != null)
        {
            firstSelectedObject.transform.localScale = Vector3.one;
            firstSelectedCell = null;
            firstSelectedObject = null;
            Debug.Log("Выбор сброшен");
        }
    }
    
    void ClearBoard()
    {
        // Удаляем все фишки
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (cellObjects[i, j] != null)
                    Destroy(cellObjects[i, j]);
            }
        }
    }
}