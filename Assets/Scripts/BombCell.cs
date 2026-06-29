using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class BombCell : Cell
{
    [Header("Настройки бомбы")]
    public float explosionDelay = 0.5f;
    public float explosionRadius = 1f; // Радиус взрыва в клетках

    private bool isExploding = false;

    public override void Initialize(int row, int col)
    {
        base.Initialize(row, col);
        Type = GemType.Bomb;
        // Можно добавить визуальные отличия
        transform.localScale = Vector3.one * 1.2f;
    }

    public override void OnMatch()
    {
        if (isExploding) return;

        Debug.Log($"💥 Бомба [{row},{col}] взрывается!");
        isExploding = true;

        // Запускаем взрыв
        StartCoroutine(ExplodeCoroutine());
    }

    private IEnumerator ExplodeCoroutine()
    {
        // Мигание красным с помощью DOTween
        spriteRenderer.DOColor(Color.red, 0.15f).SetLoops(2, LoopType.Yoyo);

        yield return new WaitForSeconds(0.3f); // ждём, пока мигание завершится

        // Собираем список клеток для взрыва (как и раньше)
        List<Vector2Int> cellsToDestroy = new List<Vector2Int>();
        for (int r = row - (int)explosionRadius; r <= row + (int)explosionRadius; r++)
        {
            for (int c = col - (int)explosionRadius; c <= col + (int)explosionRadius; c++)
            {
                if (r >= 0 && r < GameManager.Instance.rows &&
                    c >= 0 && c < GameManager.Instance.columns)
                {
                    if (r == row && c == col) continue;
                    cellsToDestroy.Add(new Vector2Int(r, c));
                }
            }
        }

        // Отправляем событие о взрыве
        GameManager.Instance.OnBombExploded?.Invoke(new Vector2Int(row, col), cellsToDestroy);

        // Анимация взрыва бомбы (как пузырька, но с красным)
        transform.DOScale(Vector3.one * 2f, 0.15f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => Destroy(gameObject));
            });
    }
    // Переопределяем OnSwap для эффекта при клике на бомбу
    public override void OnSwap()
    {
        Debug.Log($"💣 Бомба [{row},{col}] участвует в обмене");
        // Можно добавить специальный звук или визуальный эффект
    }
}
