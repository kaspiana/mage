namespace Mage.Engine;

public class Elo {
    public const double K = 32;

    public static double Expected(int rankA, int rankB){
        return 1.0 / ( 1.0 + (Math.Pow(10.0, (rankB - rankA)/400)) );
    }

    public static int Update(int rankA, int rankB, double scoreActual){
        var scoreExpected = Expected(rankA, rankB);
        return (int)Math.Floor(rankA + K * (scoreActual - scoreExpected));
    }
}