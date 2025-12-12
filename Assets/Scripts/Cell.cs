using UnityEngine;

public class Cell : MonoBehaviour
{
    public int row;
    public int col;
    protected GameManager gameManager;
    protected SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        // Получаем или добавляем SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        
        // Добавляем коллайдер для кликов
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
            gameObject.AddComponent<BoxCollider2D>();
    }
    
    public virtual void Initialize(GameManager manager, int row, int col)
    {
        this.gameManager = manager;
        this.row = row;
        this.col = col;
    }
    
    public virtual void OnMatch()
    {
        if (gameManager != null)
            gameManager.MarkCellToDelete(row, col);
    }
    
    public virtual void OnSwap()
    {
        // Можно добавить эффекты при обмене
    }
    
    // Метод для установки спрайта
    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = sprite;
    }
    
    // Метод для получения текущего спрайта
    public Sprite GetSprite()
    {
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }
    
    // Этот метод будет вызываться при клике
    void OnMouseDown()
    {
        if (gameManager != null)
            gameManager.OnCellClicked(row, col);
    }
}