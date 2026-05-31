using System;
using System.Collections.Generic;

public static class BitUtils {
    // 取得最低位 1 的索引 (0..63)
    public static int TrailingZeroCount(ulong x) {
        if (x == 0) return 64;
        int n = 0;
        while ((x & 1UL) == 0) { n++; x >>= 1; }
        return n;
    }
    // 位元數（popcount）
    public static int PopCount(ulong x) {
        int c = 0;
        while (x != 0) { x &= (x - 1); c++; }
        return c;
    }
}

public struct BitState {
    public ulong X;    // X 的位板
    public ulong O;    // O 的位板
    public sbyte Player; // +1:X 下、-1:O 下
    public int N;      // 邊長
    public int MatchN; // 連幾子

    public BitState(ulong x, ulong o, sbyte player, int n, int matchN) {
        X = x; O = o; Player = player; N = n; MatchN = matchN;
    }

    public ulong Occupied => X | O;
    public int Cells => N * N * N;

    public ulong EmptyMask() {
        ulong full = (Cells >= 64) ? ulong.MaxValue : ((1UL << Cells) - 1UL);
        return ~Occupied & full;
    }

    public List<int> LegalMoves() {
        var moves = new List<int>();
        ulong em = EmptyMask();
        while (em != 0) {
            ulong lsb = em & (~em + 1);                 // 取最低位 1
            int idx = BitUtils.TrailingZeroCount(lsb);  // <-- 這裡改了
            moves.Add(idx);
            em ^= lsb;
        }
        return moves;
    }

    public BitState Play(int moveIdx) {
        ulong bit = 1UL << moveIdx;
        if ((Occupied & bit) != 0) throw new InvalidOperationException("Cell already occupied.");
        if (Player == +1) return new BitState(X | bit, O, -1, N, MatchN);
        else              return new BitState(X, O | bit, +1, N, MatchN);
    }

    public int Winner() {
        var lines = WinLines.Get(N, MatchN);
        foreach (var line in lines) {
            if ((X & line) == line) return +1;
            if ((O & line) == line) return -1;
        }
        return 0;
    }

    public bool IsDraw() {
        return Winner() == 0 && BitCount(Occupied) == Cells;
    }

    public bool IsTerminal() => Winner() != 0 || IsDraw();

    public static int BitCount(ulong x) => BitUtils.PopCount(x);  // <-- 這裡改了

    public static int Idx(int x, int y, int z, int N) => z * N * N + y * N + x;
}
public static class WinLines {
    private static readonly Dictionary<string, ulong[]> cache = new Dictionary<string, ulong[]>();

    public static ulong[] Get(int N, int matchN) {
        string key = N + "x" + matchN;
        if (!cache.TryGetValue(key, out var arr)) {
            arr = Generate(N, matchN);
            cache[key] = arr;
        }
        return arr;
    }

    private static ulong[] Generate(int N, int matchN) {
        var lines = new List<ulong>();
        int Cells = N * N * N;
        if (Cells > 64) throw new NotSupportedException("N too large for 64-bit bitboard.");

        int Idx(int x,int y,int z) => z*N*N + y*N + x;
        ulong LineFrom(IEnumerable<(int x,int y,int z)> pts) {
            ulong m=0;
            foreach (var p in pts) m |= 1UL << Idx(p.x,p.y,p.z);
            return m;
        }
        IEnumerable<(int x,int y,int z)> IterAlong(int L, Func<int,(int x,int y,int z)> xyz, int start=0) {
            for (int i=start; i<start+L; i++) yield return xyz(i);
        }

        // 直線 (三軸)
        for (int z=0; z<N; z++)
            for (int y=0; y<N; y++)
                lines.Add(LineFrom(IterAlong(matchN, i => (i,y,z))));        // x 向

        for (int z=0; z<N; z++)
            for (int x=0; x<N; x++)
                lines.Add(LineFrom(IterAlong(matchN, i => (x,i,z))));        // y 向

        for (int y=0; y<N; y++)
            for (int x=0; x<N; x++)
                lines.Add(LineFrom(IterAlong(matchN, i => (x,y,i))));        // z 向

        // 平面對角線
        for (int z=0; z<N; z++) {
            lines.Add(LineFrom(IterAlong(matchN, i => ( i,  i, z))));
            lines.Add(LineFrom(IterAlong(matchN, i => ( i, N-1-i, z))));
        }
        for (int y=0; y<N; y++) {
            lines.Add(LineFrom(IterAlong(matchN, i => ( i, y,  i))));
            lines.Add(LineFrom(IterAlong(matchN, i => ( i, y, N-1-i))));
        }
        for (int x=0; x<N; x++) {
            lines.Add(LineFrom(IterAlong(matchN, i => ( x,  i,  i))));
            lines.Add(LineFrom(IterAlong(matchN, i => ( x,  i, N-1-i))));
        }

        // 空間對角線
        lines.Add(LineFrom(IterAlong(matchN, i => ( i,  i,  i))));
        lines.Add(LineFrom(IterAlong(matchN, i => ( i,  i, N-1-i))));
        lines.Add(LineFrom(IterAlong(matchN, i => ( i, N-1-i,  i))));
        lines.Add(LineFrom(IterAlong(matchN, i => ( N-1-i,  i,  i))));

        return lines.ToArray();
    }
}
