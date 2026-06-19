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
        if (!ThingTypes.IsMovable(type)) {
            return moves;
        }

        if (i % BoardLength != 0) {
            AddMoveIfSwapMatches(moves, i, i - 1);
        }
        if (i % BoardLength != BoardLength - 1) {
            AddMoveIfSwapMatches(moves, i, i + 1);
        }
        if (i >= BoardLength) {
            AddMoveIfSwapMatches(moves, i, i - BoardLength);
        }
        if (i < types.Length - BoardLength) {
            AddMoveIfSwapMatches(moves, i, i + BoardLength);
        }

        return moves;
    }

    private void AddMoveIfSwapMatches(List<Move> moves, int origin, int swap) {
        if (!ThingTypes.IsMovable(types[swap])) {
            return;
        }

        MatchBoard candidate = new MatchBoard(types, BoardLength);
        candidate.Swap(origin, swap);
        List<int> matches = candidate.FindMatches();
        if (matches.Count == 0) {
            return;
        }

        int matchType = candidate.GetBestMatchedType(matches, origin, swap);
        moves.Add(new Move(origin, swap, matches.Count, matchType));
    }

    private int GetBestMatchedType(List<int> matches, int origin, int swap) {
        if (matches.Contains(origin) && ThingTypes.CanStartMatch(types[origin])) {
            return types[origin];
        }
        if (matches.Contains(swap) && ThingTypes.CanStartMatch(types[swap])) {
            return types[swap];
        }
        foreach (int index in matches) {
            if (ThingTypes.CanStartMatch(types[index])) {
                return types[index];
            }
        }
        return ThingTypes.Wildcard;
    }

    public Dictionary<int, int> CountTypes(IEnumerable<int> indexes) {
        Dictionary<int, int> typeCounts = new Dictionary<int, int> {
            { 0, 0 },
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
            { 4, 0 },
            { 5, 0 },
            { 6, 0 },
            { 7, 0 },
            { 8, 0 },
            { 9, 0 }
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
        if (!ThingTypes.CanStartMatch(type)) {
            return;
        }

        HashSet<int> potentialDeletion = new HashSet<int>();
        AddMatchingLine(index, index, type, increment, potentialDeletion);
        AddMatchingLine(index, index - increment, type, -increment, potentialDeletion);

        if (potentialDeletion.Count >= 3) {
            matches.UnionWith(potentialDeletion);
        }
    }

    private void AddMatchingLine(int origin, int start, int matchType, int increment, HashSet<int> matches) {
        int position = start;
        int originRow = origin / BoardLength;
        while (IsInScanBounds(position, increment, originRow)) {
            if (!ThingTypes.CanMatchAs(types[position], matchType)) {
                break;
            }

            matches.Add(position);
            position += increment;
        }
    }

    private bool IsInScanBounds(int position, int increment, int startRow) {
        if (position < 0 || position >= Count) {
            return false;
        }

        if (increment == 1 || increment == -1) {
            return position / BoardLength == startRow;
        }

        return true;
    }

    private int GetLowerHorizontalBound(int position) {
        return (position / BoardLength) * BoardLength;
    }

    private int GetUpperHorizontalBound(int position) {
        return ((position / BoardLength) + 1) * BoardLength;
    }

}
