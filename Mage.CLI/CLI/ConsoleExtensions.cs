namespace Mage.CLI;

public class ConsoleExt {

    public static void WriteLineColored(string text, ConsoleColor color){
        var ogFGColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = ogFGColor;
    }

    public static void WriteColored(string text, ConsoleColor color){
        var ogFGColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = ogFGColor;
    }

}