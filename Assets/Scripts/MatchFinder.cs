using UnityEngine;
using System.Collections.Generic;

public class MatchFinder : MonoBehaviour
{
    private BoardManager board;

    void Awake()
    {
        board = GameManager.Instance.boardManager;
    }

    public List<Vector2Int> FindAllMatches()
    {
        List<Vector2Int> matches = new List<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        for (int row = 0; row < board.rows; row++)
        {
            for (int col = 0; col < board.columns; col++)
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
        GemType centerType = board.cells[centerRow, centerCol].Type;
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
            if (board.cells[row, c].Type == type) leftCount++;
            else break;
        }
        for (int c = startCol + 1; c < board.columns; c++)
        {
            if (board.cells[row, c].Type == type) rightCount++;
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
            if (board.cells[r, col].Type == type) upCount++;
            else break;
        }
        for (int r = startRow + 1; r < board.rows; r++)
        {
            if (board.cells[r, col].Type == type) downCount++;
            else break;
        }
        if (upCount + downCount + 1 >= 3)
        {
            for (int r = startRow - upCount; r <= startRow + downCount; r++)
                group.Add(new Vector2Int(r, col));
        }
    }

    public bool HasValidMoves()
    {
        for (int row = 0; row < board.rows; row++)
            for (int col = 0; col < board.columns; col++)
            {
                if (row < board.rows - 1 && IsSwapValid(row, col, row + 1, col)) return true;
                if (col < board.columns - 1 && IsSwapValid(row, col, row, col + 1)) return true;
            }
        return false;
    }

    public bool IsSwapValid(int r1, int c1, int r2, int c2)
    {
        GemType t1 = board.cells[r1, c1].Type, t2 = board.cells[r2, c2].Type;
        board.cells[r1, c1].Type = t2;
        board.cells[r2, c2].Type = t1;
        bool match = FindAllMatches().Count > 0;
        board.cells[r1, c1].Type = t1;
        board.cells[r2, c2].Type = t2;
        return match;
    }

    public bool CheckAfterSwap(Vector2Int aPos, Vector2Int bPos)
    {
        return FindAllMatches().Count > 0;
    }
}
