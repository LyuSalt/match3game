using UnityEngine;

[System.Serializable]
public struct Vector2Int
{
    public int x, y;

    public Vector2Int(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static Vector2Int zero = new Vector2Int(0, 0);
}