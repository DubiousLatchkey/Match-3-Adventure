using System.Collections.Generic;
using NUnit.Framework;

public sealed class MatchBoardTests {
    private const int BoardLength = 8;
    private const int BoardSize = BoardLength * BoardLength;

    public static IEnumerable<TestCaseData> WildcardMatchCases {
        get {
            yield return WildcardCase("horizontal left wildcard", 16, 1, 0);
            yield return WildcardCase("horizontal middle wildcard", 16, 1, 1);
            yield return WildcardCase("horizontal right wildcard", 16, 1, 2);
            yield return WildcardCase("horizontal left edge wildcard", 0, 1, 0);
            yield return WildcardCase("horizontal right edge wildcard", 5, 1, 2);
            yield return WildcardCase("vertical bottom wildcard", 8, BoardLength, 0);
            yield return WildcardCase("vertical middle wildcard", 8, BoardLength, 1);
            yield return WildcardCase("vertical top wildcard", 8, BoardLength, 2);
            yield return WildcardCase("vertical bottom edge wildcard", 0, BoardLength, 0);
            yield return WildcardCase("vertical top edge wildcard", 40, BoardLength, 2);
        }
    }

    [TestCaseSource(nameof(WildcardMatchCases))]
    public void FindMatches_IncludesWildcardInEveryThreePiecePosition(int[] types, int[] expected) {
        CollectionAssert.AreEquivalent(expected, FindMatches(types));
    }

    [Test]
    public void FindMatches_DoesNotLetWildcardStartMatchByItself() {
        int[] types = EmptyBoard();
        types[16] = ThingTypes.Wildcard;
        types[17] = ThingTypes.Wildcard;
        types[18] = ThingTypes.Wildcard;

        CollectionAssert.IsEmpty(FindMatches(types));
    }

    [Test]
    public void FindMatches_DoesNotWrapHorizontalMatchesAcrossRows() {
        int[] types = EmptyBoard();
        types[6] = ThingTypes.Red;
        types[7] = ThingTypes.Red;
        types[8] = ThingTypes.Wildcard;

        CollectionAssert.IsEmpty(FindMatches(types));
    }

    [Test]
    public void FindMatches_DoesNotMatchOnePieceAtRowEndWithTwoPiecesAtNextRowStart() {
        int[] types = BrickBoard();
        types[7] = ThingTypes.Red;
        types[8] = ThingTypes.Red;
        types[9] = ThingTypes.Red;

        CollectionAssert.IsEmpty(FindMatches(types));
    }

    [Test]
    public void FindMatches_DoesNotMatchTwoPiecesAtRowEndWithOnePieceAtNextRowStart() {
        int[] types = BrickBoard();
        types[14] = ThingTypes.Red;
        types[15] = ThingTypes.Red;
        types[16] = ThingTypes.Red;

        CollectionAssert.IsEmpty(FindMatches(types));
    }

    [Test]
    public void FindMoves_DoesNotFindWraparoundMoveAcrossRows() {
        int[] types = BrickBoard();
        types[6] = ThingTypes.Red;
        types[7] = ThingTypes.Blue;
        types[8] = ThingTypes.Red;
        types[9] = ThingTypes.Red;

        MatchBoard board = new MatchBoard(types, BoardLength);

        CollectionAssert.IsEmpty(board.FindMoves());
        CollectionAssert.IsEmpty(board.FindMovesAtPoint(7));
        CollectionAssert.IsEmpty(board.FindMovesAtPoint(8));
    }

    [Test]
    public void FindMatches_BrickBreaksManaAndWildcardLine() {
        int[] types = EmptyBoard();
        types[16] = ThingTypes.Red;
        types[17] = ThingTypes.Brick;
        types[18] = ThingTypes.Wildcard;

        CollectionAssert.IsEmpty(FindMatches(types));
    }

    [Test]
    public void FindMatches_BricksDoNotMatchEachOther() {
        int[] types = EmptyBoard();
        types[16] = ThingTypes.Brick;
        types[17] = ThingTypes.Brick;
        types[18] = ThingTypes.Brick;

        CollectionAssert.IsEmpty(FindMatches(types));
    }

    [Test]
    public void FindMoves_DoesNotAllowBrickSwaps() {
        int[] types = EmptyBoard();
        types[16] = ThingTypes.Red;
        types[17] = ThingTypes.Brick;
        types[18] = ThingTypes.Blue;

        CollectionAssert.IsEmpty(new MatchBoard(types, BoardLength).FindMovesAtPoint(17));
    }

    [Test]
    public void FindMatches_RainbowManaMatchesWithRainbowMana() {
        int[] types = EmptyBoard();
        types[16] = ThingTypes.RainbowMana;
        types[17] = ThingTypes.RainbowMana;
        types[18] = ThingTypes.RainbowMana;

        CollectionAssert.AreEquivalent(new[] { 16, 17, 18 }, FindMatches(types));
    }

    [TestCase(0, TestName = "rainbow wildcard first")]
    [TestCase(1, TestName = "rainbow wildcard middle")]
    [TestCase(2, TestName = "rainbow wildcard last")]
    public void FindMatches_RainbowManaMatchesWithWildcard(int wildcardOffset) {
        int[] types = EmptyBoard();
        int[] positions = new[] { 16, 17, 18 };
        foreach (int position in positions) {
            types[position] = ThingTypes.RainbowMana;
        }
        types[positions[wildcardOffset]] = ThingTypes.Wildcard;

        CollectionAssert.AreEquivalent(positions, FindMatches(types));
    }

    [Test]
    public void FindMatches_RainbowManaDoesNotMatchColoredMana() {
        int[] types = EmptyBoard();
        types[16] = ThingTypes.RainbowMana;
        types[17] = ThingTypes.Red;
        types[18] = ThingTypes.RainbowMana;

        CollectionAssert.IsEmpty(FindMatches(types));
    }

    [Test]
    public void FindMatches_ColoredManaDoesNotUseRainbowManaAsWildcard() {
        int[] types = EmptyBoard();
        types[16] = ThingTypes.Red;
        types[17] = ThingTypes.RainbowMana;
        types[18] = ThingTypes.Red;

        CollectionAssert.IsEmpty(FindMatches(types));
    }

    [Test]
    public void CountTypes_CountsRainbowAndWildcardSeparately() {
        int[] types = EmptyBoard();
        types[16] = ThingTypes.RainbowMana;
        types[17] = ThingTypes.Wildcard;
        types[18] = ThingTypes.RainbowMana;

        Dictionary<int, int> counts = new MatchBoard(types, BoardLength).CountTypes(new[] { 16, 17, 18 });

        Assert.AreEqual(2, counts[ThingTypes.RainbowMana]);
        Assert.AreEqual(1, counts[ThingTypes.Wildcard]);
    }

    private static TestCaseData WildcardCase(string name, int start, int increment, int wildcardOffset) {
        int[] types = EmptyBoard();
        int[] positions = new[] { start, start + increment, start + increment + increment };
        foreach (int position in positions) {
            types[position] = ThingTypes.Red;
        }
        types[positions[wildcardOffset]] = ThingTypes.Wildcard;

        return new TestCaseData(types, positions).SetName(name);
    }

    private static List<int> FindMatches(int[] types) {
        return new MatchBoard(types, BoardLength).FindMatches();
    }

    private static int[] EmptyBoard() {
        int[] types = new int[BoardSize];
        for (int i = 0; i < types.Length; i++) {
            types[i] = ThingTypes.Empty;
        }
        return types;
    }

    private static int[] BrickBoard() {
        int[] types = new int[BoardSize];
        for (int i = 0; i < types.Length; i++) {
            types[i] = ThingTypes.Brick;
        }
        return types;
    }
}
