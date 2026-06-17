using System.Collections.Generic;

public sealed class MatchBoard {
    private readonly int[] types;

    public MatchBoard(IReadOnlyList<int> types, int boardLength) {
        BoardLength = boardLength;
        this.types = new int[types.Count];
        for (int i = 0; i < types.Count; i++) {
            this.types[i] = types[i];
        }
    }

    public int BoardLength { get; }
    public int Count => types.Length;

    public int GetTypeAt(int index) {
        return types[index];
    }

    public void Swap(int index1, int index2) {
        int temp = types[index1];
        types[index1] = types[index2];
        types[index2] = temp;
    }

    public List<int> FindMatches() {
        SortedSet<int> matches = new SortedSet<int>();
        for (int i = 0; i < types.Length; i++) {
            AddMatchesFrom(i, 1, matches);
            AddMatchesFrom(i, BoardLength, matches);
        }
        return new List<int>(matches);
    }

    public List<Move> FindMoves() {
        List<Move> moves = new List<Move>();
        for (int i = 0; i < types.Length; i++) {
            moves.AddRange(FindMovesAtPoint(i));
        }
        return moves;
    }

    public List<Move> FindMovesAtPoint(int i) {
        List<Move> moves = new List<Move>();
        int type = types[i];

        List<int> pieces = new List<int>();
        if (i % BoardLength != 0) {
            pieces.AddRange(MovesAtPointHelperVertical(i, BoardLength, BoardLength - 1, type));
            pieces.AddRange(MovesAtPointHelperVertical(i, -BoardLength, -BoardLength - 1, type));
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i - 1, pieces.Count + 1, type));
            }
            pieces.Clear();
        }

        if (i % BoardLength != BoardLength - 1) {
            pieces.AddRange(MovesAtPointHelperVertical(i, BoardLength, BoardLength + 1, type));
            pieces.AddRange(MovesAtPointHelperVertical(i, -BoardLength, -BoardLength + 1, type));
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i + 1, pieces.Count + 1, type));
            }
            pieces.Clear();
        }

        if (i < BoardLength * BoardLength - BoardLength) {
            int displacement = BoardLength - 1;
            while (i + displacement >= (i / BoardLength + 1) * BoardLength && types[i + displacement] == type) {
                pieces.Add(i + displacement);
                displacement -= 1;
            }
            displacement = BoardLength + 1;
            while (i + displacement < (i / BoardLength + 2) * BoardLength && types[i + displacement] == type) {
                pieces.Add(i + displacement);
                displacement += 1;
            }
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i + BoardLength, pieces.Count + 1, type));
            }
            pieces.Clear();
        }

        if (i >= BoardLength) {
            int displacement = -BoardLength - 1;
            while (i + displacement >= (i / BoardLength - 1) * BoardLength && types[i + displacement] == type) {
                pieces.Add(i + displacement);
                displacement -= 1;
            }
            displacement = -BoardLength + 1;
            while (i + displacement < (i / BoardLength) * BoardLength && types[i + displacement] == type) {
                pieces.Add(i + displacement);
                displacement += 1;
            }
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i - BoardLength, pieces.Count + 1, type));
            }
            pieces.Clear();
        }

        if (GetLowerHorizontalBound(i) < BoardLength * BoardLength - 3 * BoardLength) {
            int displacement = BoardLength * 2;
            while (i + displacement < BoardLength * BoardLength && types[i + displacement] == type) {
                pieces.Add(i + displacement);
                displacement += BoardLength;
            }
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i + BoardLength, pieces.Count + 1, type));
            }
            pieces.Clear();
        }

        if (GetLowerHorizontalBound(i) >= 3 * BoardLength) {
            int displacement = -BoardLength * 2;
            while (i + displacement >= 0 && types[i + displacement] == type) {
                pieces.Add(i + displacement);
                displacement -= BoardLength;
            }
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i - BoardLength, pieces.Count + 1, type));
            }
            pieces.Clear();
        }

        if (GetUpperHorizontalBound(i) >= i + 3) {
            int displacement = 2;
            while (i + displacement < GetUpperHorizontalBound(i) && types[i + displacement] == type) {
                pieces.Add(i + displacement);
                displacement += 1;
            }
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i + 1, pieces.Count + 1, type));
            }
            pieces.Clear();
        }

        if (GetLowerHorizontalBound(i) <= i - 3) {
            int displacement = -2;
            while (i + displacement >= GetLowerHorizontalBound(i) && types[i + displacement] == type) {
                pieces.Add(i + displacement);
                displacement -= 1;
            }
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i - 1, pieces.Count + 1, type));
            }
        }

        return moves;
    }

    public Dictionary<int, int> CountTypes(IEnumerable<int> indexes) {
        Dictionary<int, int> typeCounts = new Dictionary<int, int> {
            { 0, 0 },
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
            { 4, 0 },
            { 5, 0 }
        };

        foreach (int index in indexes) {
            int type = types[index];
            if (!typeCounts.ContainsKey(type)) {
                typeCounts[type] = 0;
            }
            typeCounts[type]++;
        }

        return typeCounts;
    }

    private void AddMatchesFrom(int index, int increment, SortedSet<int> matches) {
        int type = types[index];
        if (type == -1) {
            return;
        }

        HashSet<int> potentialDeletion = new HashSet<int>();
        int extension = 0;
        while ((index + extension) % BoardLength < BoardLength - 1 && index + extension < BoardLength * BoardLength) {
            if (types[index + extension] == type) {
                potentialDeletion.Add(index + extension);
                extension += increment;
            }
            else {
                break;
            }
        }

        extension = 0;
        while ((index + extension) % BoardLength > 0 && index + extension > 0) {
            if (types[index + extension] == type) {
                potentialDeletion.Add(index + extension);
                extension -= increment;
            }
            else {
                break;
            }
        }

        if (potentialDeletion.Count >= 3) {
            matches.UnionWith(potentialDeletion);
        }
    }

    private int GetLowerHorizontalBound(int position) {
        return (position / BoardLength) * BoardLength;
    }

    private int GetUpperHorizontalBound(int position) {
        return ((position / BoardLength) + 1) * BoardLength;
    }

    private List<int> MovesAtPointHelperVertical(int i, int increment, int displacement, int type) {
        List<int> pieces = new List<int>();
        while (i + displacement < BoardLength * BoardLength && i + displacement >= 0 && types[i + displacement] == type) {
            pieces.Add(i + displacement);
            displacement += increment;
        }
        return pieces;
    }
}
