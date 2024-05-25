using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using Microsoft.VisualBasic;
using SQLitePCL;

public class ELO {
    public const double K = 32;

    public static double Expected(int rankA, int rankB){
        return 1.0 / ( 1.0 + (Math.Pow(10.0, (rankB - rankA)/400)) );
    }

    public static int Update(int rankA, int rankB, double scoreActual){
        var scoreExpected = Expected(rankA, rankB);
        return Math.Max(0, (int)Math.Floor(rankA + K * (scoreActual - scoreExpected)));
    }
}

public static partial class CLICommands {

    public static Command ComRankings(CLIContext ctx){
        var com = new Command("rankings", "List all rankings."){
            ComRankingsTournament(ctx)
        };

        com.SetHandler(() => {
            var rankings = ctx.archive.RankingsGet();

            foreach(var ranking in rankings){
                Console.WriteLine($" * {ranking}");
            }
        });

        return com;
    }

    public static Command ComRankingsTournament(CLIContext ctx){
        var com = new Command("tournament", "Run a rankings tournament.");

        com.SetHandler(() => {
            ctx.archive.db.EnsureConnected();

            using var docSampleCom = ctx.archive.db.GenCommand(
                DBCommands.Sample.Document(),
                ("limit", 2)
            );

            using var rankingSampleCom = ctx.archive.db.GenCommand(
                DBCommands.Sample.Ranking,
                ("limit", 1)
            );

            ctx.archive.ViewCreate("tournament");

            while(true){
                var docSample = DBEngine.RunQuery(docSampleCom, r => (DocumentID)r.GetInt32(0)).ToArray();
                if(docSample.Count() != 2){
                    return;
                }

                var rankingSample = DBEngine.RunQuery(rankingSampleCom, r => r.GetString(0)).ToArray();
                if(rankingSample.Count() != 1){
                    return;
                }

                var docA = docSample[0];
                var docB = docSample[1];
                var ranking = rankingSample[0];
                
                var docARanking = ctx.archive.DocumentGetRanking(docA, ranking);
                var docBRanking = ctx.archive.DocumentGetRanking(docB, ranking);
                var docAScore = 0;
                var docBScore = 0;

                ctx.archive.ViewClear("tournament");
                ctx.archive.ViewAdd("tournament", docA);
                ctx.archive.ViewAdd("tournament", docB);

                Console.WriteLine($"Which is more '{ranking}': tournament/0 (a), tournament/1 (b), or neither (n)? (q to quit)");
                Console.Write("> ");
                var lineIn = Console.ReadLine().Trim().ToLower();

                if(lineIn == "a"){
                    docAScore = 1;
                    docBScore = 0;
                } else if(lineIn == "b") {
                    docAScore = 0;
                    docBScore = 1;
                } else if(lineIn == "n"){
                    docAScore = 0;
                    docBScore = 0;
                } else if(lineIn == "q"){
                    return;
                }

                var docAUpdatedRanking = ELO.Update(docARanking, docBRanking, docAScore);
                var docBUpdatedRanking = ELO.Update(docBRanking, docARanking, docBScore);

                ctx.archive.DocumentSetRanking(docA, ranking, docAUpdatedRanking);
                ctx.archive.DocumentSetRanking(docB, ranking, docBUpdatedRanking);
            }

            ctx.archive.ViewDelete("tournament");
        });

        return com;
    }

}