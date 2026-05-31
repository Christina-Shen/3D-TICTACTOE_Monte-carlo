using System.Collections.Generic;

public static class GameRules
{
    // 明確指定 tuple 陣列型別，避免推斷問題
    private static readonly (int dx, int dy, int dz)[] DIRS_3D = new (int dx, int dy, int dz)[]
    {
        (1,0,0),(0,1,0),(0,0,1),
        (1,1,0),(1,-1,0),
        (1,0,1),(1,0,-1),
        (0,1,1),(0,1,-1),
        (1,1,1),(1,1,-1),(1,-1,1),(1,-1,-1)
    };

    // 任一方勝利
    public static bool IsWinAny(GameState state)
    {
        return IsWinBy(state, 'x') || IsWinBy(state, 'o');
    }

    // 檢查指定棋子是否勝利
    public static bool IsWinBy(GameState state, char who)
    {
        int n = 3;
        var pos = state.Position;

        for (int z = 0; z < n; z++)
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            if (pos[(z, y, x)] != who) continue;

            // 不用解構，直接拿成員名稱
            foreach (var dir in DIRS_3D)
            {
                int dx = dir.dx, dy = dir.dy, dz = dir.dz;

                int xEnd = x + (n - 1) * dx;
                int yEnd = y + (n - 1) * dy;
                int zEnd = z + (n - 1) * dz;
                if (xEnd < 0 || xEnd >= n || yEnd < 0 || yEnd >= n || zEnd < 0 || zEnd >= n)
                    continue;

                bool ok = true;
                for (int k = 0; k < n; k++)
                {
                    if (pos[(z + k * dz, y + k * dy, x + k * dx)] != who)
                    { ok = false; break; }
                }
                if (ok) return true;
            }
        }
        return false;
    }

    public static bool IsDraw(GameState state)
    {
        foreach (var piece in state.Position.Values)
            if (piece == '.') return false;
        return true;
    }

    public static List<GameState> GenerateStates(GameState state)
    {
        if (IsWinAny(state) || IsDraw(state))
            return new List<GameState>();   // 不用 target-typed new()

        var actions = new List<GameState>();

        for (int d = 0; d < 3; d++)
        for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
        {
            if (state.Position[(d, r, c)] == '.')
            {
                // 明確指定字典型別，舊版編譯器較穩
                var newPos = new Dictionary<(int, int, int), char>(state.Position);
                newPos[(d, r, c)] = state.Player1;

                // 換手
                var next = new GameState(newPos, state.Player2, state.Player1);
                actions.Add(next);
            }
        }
        return actions;
    }
}
