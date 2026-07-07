using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

[System.Serializable]
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

    [Header("Компоненты (автоматически добавляются)")]
    public BoardManager boardManager;
    public MatchFinder matchFinder;
    public InputHandler inputHandler;
    public AnimationController animationController;

    [Header("События")]
    public Vector2IntEvent OnCellClicked;
    public Vector2Int2Event OnCellsSwapped;
    public Vector2IntListEvent OnMatchFound;
    public Vector2IntListEvent OnCellsToDelete;
    public BombExplodedEvent OnBombExploded;

    [HideInInspector] public bool isAnimating = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // 🔥 Устанавливаем ёмкость DOTween (достаточно для всех анимаций)
            DOTween.SetTweensCapacity(500, 125);

        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 🔥 Ищем существующие компоненты или создаём новые (после того как Instance установлен)
        boardManager = GetComponent<BoardManager>();
        if (boardManager == null)
            boardManager = gameObject.AddComponent<BoardManager>();

        matchFinder = GetComponent<MatchFinder>();
        if (matchFinder == null)
            matchFinder = gameObject.AddComponent<MatchFinder>();

        inputHandler = GetComponent<InputHandler>();
        if (inputHandler == null)
            inputHandler = gameObject.AddComponent<InputHandler>();

        animationController = GetComponent<AnimationController>();
        if (animationController == null)
            animationController = gameObject.AddComponent<AnimationController>();

        // Инициализация события клика
        if (OnCellClicked == null)
            OnCellClicked = new Vector2IntEvent();
        OnCellClicked.AddListener(inputHandler.OnCellClickedEvent);

        // Инициализация игрового поля
        boardManager.InitializeBoard();
        boardManager.CreateVisualBoardWithoutMatches();
    }

    public void ProcessMatchesAfterSwap()
    {
        List<Vector2Int> matches = matchFinder.FindAllMatches();
        if (matches.Count == 0) return;

        OnMatchFound?.Invoke(matches);
        foreach (var pos in matches)
        {
            boardManager.cells[pos.x, pos.y].OnMatch();
        }
        OnCellsToDelete?.Invoke(matches);

        StartCoroutine(DeleteAndRefill(matches));
    }

    private IEnumerator DeleteAndRefill(List<Vector2Int> positions)
    {
        yield return new WaitForSeconds(0.5f);

        foreach (var pos in positions)
        {
            if (boardManager.cellObjects[pos.x, pos.y] != null)
            {
                Destroy(boardManager.cellObjects[pos.x, pos.y]);
                boardManager.cellObjects[pos.x, pos.y] = null;
                boardManager.cells[pos.x, pos.y] = null;
            }
            else
            {
                boardManager.cellObjects[pos.x, pos.y] = null;
                boardManager.cells[pos.x, pos.y] = null;
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
            for (int col = 0; col < boardManager.columns; col++)
            {
                for (int row = boardManager.rows - 1; row > 0; row--)
                {
                    if (boardManager.cells[row, col] == null && boardManager.cells[row - 1, col] != null)
                    {
                        boardManager.cells[row, col] = boardManager.cells[row - 1, col];
                        boardManager.cellObjects[row, col] = boardManager.cellObjects[row - 1, col];
                        boardManager.cells[row - 1, col] = null;
                        boardManager.cellObjects[row - 1, col] = null;

                        boardManager.cells[row, col].row = row;
                        boardManager.cells[row, col].col = col;

                        float targetY = row * boardManager.cellSpacing;
                        animationController.AnimateFall(boardManager.cellObjects[row, col].transform,
                            new Vector3(col * boardManager.cellSpacing, targetY, 0), 0.2f);

                        moved = true;
                    }
                }
            }
            if (moved) yield return new WaitForSeconds(0.1f);
        }

        // Новые фишки сверху
        for (int col = 0; col < boardManager.columns; col++)
        {
            for (int row = 0; row < boardManager.rows; row++)
            {
                if (boardManager.cells[row, col] == null)
                {
                    boardManager.CreateNewCell(row, col);
                    boardManager.cellObjects[row, col].transform.position =
                        new Vector3(col * boardManager.cellSpacing, -boardManager.cellSpacing, 0);
                    animationController.AnimateFall(boardManager.cellObjects[row, col].transform,
                        new Vector3(col * boardManager.cellSpacing, row * boardManager.cellSpacing, 0), 0.3f);
                }
            }
        }

        yield return new WaitForSeconds(0.4f);

        // Каскадные матчи
        List<Vector2Int> newMatches = matchFinder.FindAllMatches();
        if (newMatches.Count > 0)
        {
            ProcessMatchesAfterSwap();
        }
        else
        {
            if (!matchFinder.HasValidMoves())
            {
                Debug.Log("⚠️ Нет ходов! Запускаем анимацию перемешивания...");
                inputHandler.ResetSelection();
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(animationController.ShuffleBoardCoroutine());
            }
        }
    }
}