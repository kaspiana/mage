namespace Mage.Engine;

public class Elo {
    public const double K = 32.0;
    public const double BASE = 10.0;
    public const double DIVISOR = 400.0;

    public static double Expected(int rankA, int rankB){
        return 1.0 / ( 1.0 + Math.Pow(BASE, (rankB - rankA)/DIVISOR) );
    }

    public static int Update(int rankA, int rankB, double scoreActual){
        var scoreExpected = Expected(rankA, rankB);
        return (int)Math.Floor(rankA + K * (scoreActual - scoreExpected));
    }
}