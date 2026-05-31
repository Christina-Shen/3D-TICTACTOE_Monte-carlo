using System;
using System.Collections.Generic;

public sealed class GameState
{
    // 盤面，鍵是 (depth, row, col)，值是 'x'、'o' 或 '.'
    public Dictionary<(int d, int r, int c), char> Position { get; }

    // 下一手要下的人（side to move）
    public char Player1 { get; }

    // 對手
    public char Player2 { get; }

    public GameState(Dictionary<(int, int, int), char> position, char player1, char player2)
    {
        if (position == null) throw new ArgumentNullException(nameof(position));
        Position = position;
        Player1  = player1;
        Player2  = player2;
    }

    // 深拷貝（給 MCTS rollout 用）
    public GameState Copy()
    {
        return new GameState(
            new Dictionary<(int, int, int), char>(Position),
            Player1,
            Player2
        );
    }

    // （可選）提供索引存取
    public char this[int d, int r, int c]
    {
        get => Position[(d, r, c)];
        set => Position[(d, r, c)] = value;
    }
}
