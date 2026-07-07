using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System;

public class AnimationController : MonoBehaviour
{
    private BoardManager board;
    private MatchFinder matchFinder;
    private GameManager gameManager;

    void Awake()
    {
        board = GameManager.Instance.boardManager;
        matchFinder = GameManager.Instance.matchFinder;
        gameManager = GameManager.Instance;
    }

    public void AnimateFall(Transform obj, Vector3 targetPos, float duration)
    {
        StartCoroutine(AnimateFallCoroutine(obj, targetPos, duration));
    }

    private IEnumerator AnimateFallCoroutine(Transform obj, Vector3 targetPos, float duration)
    {
        if (obj == null) yield break; // объект уже уничтожен

        Vector3 startPos = obj.position;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (obj == null) yield break; // объект мог быть уничтожен во время анимации
            float t = elapsed / duration;
            obj.position = Vector3.Lerp(startPos, targetPos, t * t);
            yield return null;
        }
        if (obj != null)
            obj.position = targetPos;
    }

    public void AnimatedSwap(int r1, int c1, int r2, int c2, Action onComplete, float duration = 0.15f)
    {
        StartCoroutine(AnimatedSwapCoroutine(r1, c1, r2, c2, duration, onComplete));
    }

    private IEnumerator AnimatedSwapCoroutine(int r1, int c1, int r2, int c2, float duration, Action onComplete)
    {
        GameObject obj1 = board.cellObjects[r1, c1];
        GameObject obj2 = board.cellObjects[r2, c2];
        if (obj1 == null || obj2 == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector3 pos1 = obj1.transform.position;
        Vector3 pos2 = obj2.transform.position;

        obj1.transform.DOMove(pos2, duration).SetEase(Ease.InOutQuad);
        obj2.transform.DOMove(pos1, duration).SetEase(Ease.InOutQuad);

        yield return new WaitForSeconds(duration);

        board.SwapCellObjects(r1, c1, r2, c2);
        onComplete?.Invoke();
    }

    // ---------- Перетасовка (Shuffle) ----------
    public IEnumerator ShuffleBoardCoroutine()
    {
        gameManager.isAnimating = true;

        List<GameObject> allObjects = new List<GameObject>();
        for (int i = 0; i < board.rows; i++)
        {
            for (int j = 0; j < board.columns; j++)
            {
                if (board.cellObjects[i, j] != null)
                {
                    allObjects.Add(board.cellObjects[i, j]);
                    Collider2D col = board.cellObjects[i, j].GetComponent<Collider2D>();
                    if (col != null) col.enabled = false;
                }
            }
        }

        // Логическая перетасовка до появления матчей (без анимации)
        int attempts = 0;
        bool hasMatches = false;
        while (!hasMatches && attempts < 100)
        {
            ShuffleTypes();
            ShuffleObjects();
            board.SyncCellsWithObjects();

            List<Vector2Int> matches = matchFinder.FindAllMatches();
            if (matches.Count > 0)
            {
                hasMatches = true;
                break;
            }
            attempts++;
        }

        if (!hasMatches)
        {
            Debug.Log("Не удалось создать матчи, перегенерируем поле.");
            board.ClearBoard();
            board.CreateVisualBoardWithoutMatches();
            gameManager.isAnimating = false;
            yield break;
        }

        // Анимация сжатия
        foreach (var obj in allObjects)
            obj.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack);
        yield return new WaitForSeconds(0.25f);

        // Обновляем позиции и сбрасываем масштаб
        foreach (var obj in allObjects)
        {
            Vector3 targetPos = FindObjectPosition(obj);
            obj.transform.position = targetPos;
            obj.transform.localScale = Vector3.zero;
        }

        // Появление с волной
        float delay = 0f;
        foreach (var obj in allObjects)
        {
            obj.transform.DOScale(Vector3.one, 0.25f)
                .SetDelay(delay)
                .SetEase(Ease.OutBack);
            delay += 0.03f;
        }

        yield return new WaitForSeconds(0.4f + delay);

        // Включаем коллайдеры
        foreach (var obj in allObjects)
        {
            Collider2D col = obj.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }

        // Обрабатываем матчи
        List<Vector2Int> finalMatches = matchFinder.FindAllMatches();
        if (finalMatches.Count > 0)
        {
            gameManager.ProcessMatchesAfterSwap();
        }

        gameManager.isAnimating = false;
    }

    // ---------- Вспомогательные методы (без изменений) ----------
    private void ShuffleObjects()
    {
        List<GameObject> objectsList = new List<GameObject>();
        for (int i = 0; i < board.rows; i++)
            for (int j = 0; j < board.columns; j++)
                if (board.cellObjects[i, j] != null)
                    objectsList.Add(board.cellObjects[i, j]);

        for (int i = 0; i < objectsList.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, objectsList.Count);
            GameObject temp = objectsList[i];
            objectsList[i] = objectsList[randomIndex];
            objectsList[randomIndex] = temp;
        }

        int index = 0;
        for (int i = 0; i < board.rows; i++)
            for (int j = 0; j < board.columns; j++)
            {
                if (index < objectsList.Count)
                {
                    board.cellObjects[i, j] = objectsList[index];
                    Cell cell = board.cellObjects[i, j].GetComponent<Cell>();
                    if (cell != null) { cell.row = i; cell.col = j; }
                    index++;
                }
                else board.cellObjects[i, j] = null;
            }
    }

    private void ShuffleTypes()
    {
        List<GemType> allTypes = new List<GemType>();
        for (int i = 0; i < board.rows; i++)
            for (int j = 0; j < board.columns; j++)
                if (board.cells[i, j] != null)
                    allTypes.Add(board.cells[i, j].Type);

        for (int i = 0; i < allTypes.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, allTypes.Count);
            GemType temp = allTypes[i];
            allTypes[i] = allTypes[randomIndex];
            allTypes[randomIndex] = temp;
        }

        int idx = 0;
        for (int i = 0; i < board.rows; i++)
            for (int j = 0; j < board.columns; j++)
                if (board.cells[i, j] != null)
                {
                    board.cells[i, j].Type = allTypes[idx];
                    Sprite newSprite = board.gemSprites[(int)allTypes[idx]];
                    board.cells[i, j].SetSpriteAndType(newSprite, allTypes[idx]);
                    idx++;
                }
    }

    private Vector3 FindObjectPosition(GameObject obj)
    {
        for (int i = 0; i < board.rows; i++)
            for (int j = 0; j < board.columns; j++)
                if (board.cellObjects[i, j] == obj)
                    return new Vector3(j * board.cellSpacing, i * board.cellSpacing, 0);
        return Vector3.zero;
    }
}