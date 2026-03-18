using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;

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
        //GameManager.Instance.OnCellsToDelete.Invoke();
        // Пока пусто — ждём OnCellsToDelete
        Debug.Log($"Cell [{row},{col}] удаляется! Тип: {Type}");
        StartCoroutine(ScaleDestroy(0.3f));
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
    private IEnumerator ScaleDestroy(float duration)
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
