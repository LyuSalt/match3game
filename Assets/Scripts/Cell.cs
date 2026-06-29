using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using DG.Tweening;
using Random = UnityEngine.Random;
public class Cell : MonoBehaviour
{
    public int row;
    public int col;
    protected SpriteRenderer spriteRenderer;
    public GemType Type;  // клетка хранит свой тип. тут живёт "номер"

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
            gameObject.AddComponent<BoxCollider2D>();
    }

    public virtual void Initialize(int row, int col)  
    {
        this.row = row;
        this.col = col;
    }

    //удаление совпадений

    public virtual void OnMatch()
    {
        // Создаём 6 частиц (брызг)
        for (int i = 0; i < 6; i++)
        {
            GameObject particle = new GameObject("Particle");
            particle.transform.position = transform.position;
            SpriteRenderer rend = particle.AddComponent<SpriteRenderer>();
            rend.sprite = spriteRenderer.sprite; // копируем спрайт фишки
            rend.color = spriteRenderer.color;
            rend.sortingOrder = 1;

            float angle = Random.Range(0, 360) * Mathf.Deg2Rad;
            float distance = Random.Range(0.5f, 1.5f);
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // Анимация разлёта и исчезновения
            Sequence seq = DOTween.Sequence();
            seq.Append(particle.transform.DOMove((Vector2)transform.position + direction * distance, 0.3f)
                .SetEase(Ease.OutQuad));
            seq.Join(rend.DOFade(0, 0.3f));
            seq.OnComplete(() => Destroy(particle));
        }

        // Основная анимация фишки (как выше)
        transform.DOScale(1.3f, 0.1f)
            .OnComplete(() =>
            {
                transform.DOScale(0f, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => Destroy(gameObject));
            });
    }

    public virtual void OnSwap()
    {
        Debug.Log($"Cell [{row},{col}] участвует в обмене");
        // Здесь эффекты обмена: мигание, звук
    }

    public void SetSpriteAndType(Sprite sprite, GemType type)
    {
        Type = type;                 // запоминаем тип
        spriteRenderer.sprite = sprite;  // ставим картинку
    }

    public Sprite GetSprite()
    {
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    void OnMouseDown()
    {
        Vector2Int myPos = new Vector2Int(row, col);
        GameManager.Instance.OnCellClicked.Invoke(myPos);  // ✅ СОБЫТИЕ!
    }
    protected IEnumerator ScaleDestroy(float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            yield return null;
        }
    }
}
