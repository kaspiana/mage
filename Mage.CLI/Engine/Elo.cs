namespace Mage.Engine;

public class Elo {
    public const double K = 32.0;
    public const double BASE = 10.0;
    public const double DIVISOR = 400.0;

    public static double Expected(int ratingA, int ratingB){
        return 1.0 / ( 1.0 + Math.Pow(BASE, (ratingA - ratingB)/DIVISOR) );
    }

    public static int Update(int ratingA, int ratingB, double perfActual){
        var perfExpected = Expected(ratingA, ratingB);
        return (int)Math.Floor(ratingA + K * (perfActual - perfExpected));
    }
}