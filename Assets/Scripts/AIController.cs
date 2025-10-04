using UnityEngine;
using System.Collections.Generic;
using static Gem;

public class AIController : MonoBehaviour
{
    [SerializeField] Match3Manager board;
    public float thinkDelay = 0.0f; 

    public void PlayTurn()
    {
        if (!board) return;
        StartCoroutine(DoAITurn());
    }

    System.Collections.IEnumerator DoAITurn()
    {
        if (thinkDelay > 0) yield return new WaitForSeconds(thinkDelay);

        var best = FindBestMove();
        if (best.a != null && best.b != null)
            board.DoSwap(best.a, best.b);
        
    }

    struct AIMove { public Gem a, b; public int score; public AIMove(Gem a, Gem b, int s) { this.a = a; this.b = b; this.score = s; } }

    AIMove FindBestMove()
    {
        AIMove best = new AIMove(null, null, -1);
        int w = board.gridWidth, h = board.gridHeight;
        int[] dx = { 1, 0 }, dy = { 0, 1 };

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                var g = board.GetGem(x, y);
                if (!g) continue;

                for (int d = 0; d < 2; d++)
                {
                    int nx = x + dx[d], ny = y + dy[d];
                    var g2 = board.GetGem(nx, ny);
                    if (!g2) continue;

                    int s = SimulateSwapScore(x, y, nx, ny);
                    if (s > best.score) best = new AIMove(g, g2, s);
                }
            }
        return best;
    }

    int SimulateSwapScore(int x1, int y1, int x2, int y2)
    {
        var t = BuildTypeGrid();
        if (t[x1, y1] == t[x2, y2]) return -1;
        Swap(t, x1, y1, x2, y2);

        var count = new Dictionary<int, int>();
        // quét ngang
        for (int y = 0; y < board.gridHeight; y++)
        {
            int rt = -2, len = 0;
            for (int x = 0; x < board.gridWidth; x++)
            {
                int tt = t[x, y];
                if (tt != -1 && tt == rt) len++;
                else { if (len >= 3) { if (!count.ContainsKey(rt)) count[rt] = 0; count[rt] += len; } rt = tt; len = (tt == -1) ? 0 : 1; }
            }
            if (len >= 3) { if (!count.ContainsKey(rt)) count[rt] = 0; count[rt] += len; }
        }
        // quét dọc
        for (int x = 0; x < board.gridWidth; x++)
        {
            int rt = -2, len = 0;
            for (int y = 0; y < board.gridHeight; y++)
            {
                int tt = t[x, y];
                if (tt != -1 && tt == rt) len++;
                else { if (len >= 3) { if (!count.ContainsKey(rt)) count[rt] = 0; count[rt] += len; } rt = tt; len = (tt == -1) ? 0 : 1; }
            }
            if (len >= 3) { if (!count.ContainsKey(rt)) count[rt] = 0; count[rt] += len; }
        }

        Swap(t, x1, y1, x2, y2); // hoàn tác

        int score = 0;
        foreach (var kv in count)
        {
            int type = kv.Key, cells = kv.Value;
            int groups = Mathf.CeilToInt(cells / 3f);
            int baseVal = CombatSystem.GetBaseEffectFor((GemType)type);
            int val = baseVal * groups;

            if (type == (int)GemType.Yellow) score += val * 5;
            else if (type == (int)GemType.Grey) score += val * 4;
            else if (type == (int)GemType.Purple) score += val * 3;
            else if (type == (int)GemType.Blue) score += val * 2;
            else if (type == (int)GemType.Red) score += val * 2;
            else if (type == (int)GemType.Green) score += val * 1;
        }
        return score > 0 ? score : -1;
    }

    int[,] BuildTypeGrid()
    {
        int w = board.gridWidth, h = board.gridHeight;
        int[,] a = new int[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                a[x, y] = board.GetGem(x, y) ? (int)board.GetGem(x, y).gemType : -1;
        return a;
    }
    void Swap(int[,] a, int x1, int y1, int x2, int y2) { int tmp = a[x1, y1]; a[x1, y1] = a[x2, y2]; a[x2, y2] = tmp; }
}
