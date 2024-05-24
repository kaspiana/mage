using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComRankings(CLIContext ctx){
        var com = new Command("rankings", "List all rankings.");

        com.SetHandler(() => {
            var rankings = ctx.archive.RankingsGet();

            foreach(var ranking in rankings){
                Console.WriteLine($" * {ranking}");
            }
        });

        return com;
    }

}