using UnityEngine;
using System; // Добавлено для Action (используется в лямбда-выражениях)

public class InputHandler : MonoBehaviour
{
    // Ленивые свойства для доступа к компонентам GameManager
    private BoardManager Board => GameManager.Instance.boardManager;
    private MatchFinder MatchFinder => GameManager.Instance.matchFinder;
    private AnimationController AnimCtrl => GameManager.Instance.animationController;
    private GameManager Game => GameManager.Instance;

    private Vector2Int firstSelectedPos = new Vector2Int(-1, -1);
    private GameObject firstSelectedObject = null;
    private bool isProcessing = false; // Защита от повторных кликов во время анимации

    public void OnCellClickedEvent(Vector2Int position)
    {
        if (Game == null || Game.isAnimating || isProcessing) return;

        int row = position.x, col = position.y;
        if (Board == null || Board.cells == null || Board.cellObjects == null)
        {
            Debug.LogError("BoardManager или его массивы не инициализированы!");
            return;
        }

        if (Board.cells[row, col] == null || Board.cellObjects[row, col] == null)
        {
            Debug.LogWarning($"Клетка [{row},{col}] не готова!");
            return;
        }

        if (firstSelectedPos.x == -1) // Первый выбор
        {
            firstSelectedPos = position;
            firstSelectedObject = Board.cellObjects[row, col];
            if (firstSelectedObject != null)
                firstSelectedObject.transform.localScale = Vector3.one * 1.2f;
        }
        else // Второй выбор
        {
            if (firstSelectedObject != null)
            {
                firstSelectedObject.transform.localScale = Vector3.one;
                firstSelectedObject = null;
            }

            if (IsAdjacent(firstSelectedPos.x, firstSelectedPos.y, row, col))
            {
                if (Board.cells[firstSelectedPos.x, firstSelectedPos.y] != null &&
                    Board.cells[row, col] != null)
                {
                    // Блокируем повторные клики
                    isProcessing = true;

                    // Сохраняем координаты для callback
                    Vector2Int posA = firstSelectedPos;
                    Vector2Int posB = new Vector2Int(row, col);

                    // Анимируем свап и после завершения проверяем матчи
                    AnimCtrl.AnimatedSwap(posA.x, posA.y, posB.x, posB.y, () =>
                    {
                        // Проверяем, есть ли матчи после обмена
                        bool ok = MatchFinder.CheckAfterSwap(posA, posB);
                        if (ok)
                        {
                            Game.OnCellsSwapped?.Invoke(posA, posB);
                            Game.ProcessMatchesAfterSwap();
                        }
                        else
                        {
                            Debug.Log("Нет совпадений, откат");
                            // Откат – меняем обратно с callback для разблокировки
                            AnimCtrl.AnimatedSwap(posA.x, posA.y, posB.x, posB.y, () =>
                            {
                                isProcessing = false;
                            });
                            // Не сбрасываем isProcessing здесь, потому что откат асинхронный
                            return;
                        }
                        isProcessing = false;
                    });

                    // Сбрасываем выделение (но isProcessing останется true до завершения)
                    firstSelectedPos = new Vector2Int(-1, -1);
                    return; // выходим, чтобы не сбросить isProcessing преждевременно
                }
            }
            firstSelectedPos = new Vector2Int(-1, -1);
        }
    }

    public void ResetSelection()
    {
        if (firstSelectedObject != null)
        {
            try { firstSelectedObject.transform.localScale = Vector3.one; }
            catch { }
            firstSelectedObject = null;
        }
        firstSelectedPos = new Vector2Int(-1, -1);
    }

    private bool IsAdjacent(int row1, int col1, int row2, int col2)
    {
        int rowDiff = Mathf.Abs(row1 - row2);
        int colDiff = Mathf.Abs(col1 - col2);
        return (rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1);
    }
}