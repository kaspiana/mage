using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using Microsoft.VisualBasic;
using SQLitePCL;

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

        var singleOption = new Option<string?>(
            name: "--single",
            description: "Only improve ratings of single ranking.",
            getDefaultValue: () => null
        );

        var com = new Command("tournament", "Run a rankings tournament."){
            singleOption
        };

        com.SetHandler((singleRanking) => {
            ctx.archive.db.EnsureConnected();

            using var rankingSampleCom = ctx.archive.db.GenCommand(
                DBCommands.Sample.Ranking,
                ("limit", 1)
            );

            using var docSampleCom = ctx.archive.db.GenCommand(
                DBCommands.Sample.Document(),
                ("limit", 2)
            );

            ctx.archive.ViewCreate("tournament");

            while(true){
                
                var docSample = DBEngine.RunQuery(docSampleCom, r => (DocumentID)r.GetInt32(0)).ToArray();
                if(docSample.Count() != 2){
                    return;
                }

                var docA = docSample[0];
                var docB = docSample[1];

                var ranking = singleRanking switch {
                    null => DBEngine.RunQuerySingle(rankingSampleCom, r => r.GetString(0)),
                    var s => s
                };
                
                var docARating = ctx.archive.DocumentGetRating((DocumentID)docA, ranking);
                var docBRating = ctx.archive.DocumentGetRating((DocumentID)docB, ranking);
                var docAScore = 0;
                var docBScore = 0;

                ctx.archive.ViewClear("tournament");
                ctx.archive.ViewAdd("tournament", (DocumentID)docA);
                ctx.archive.ViewAdd("tournament", (DocumentID)docB);

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
                    break;
                }

                var docAUpdatedRating = Elo.Update(docARating, docBRating, docAScore);
                var docBUpdatedRating = Elo.Update(docBRating, docARating, docBScore);

                ctx.archive.DocumentSetRating((DocumentID)docA, ranking, docAUpdatedRating);
                ctx.archive.DocumentSetRating((DocumentID)docB, ranking, docBUpdatedRating);
            }

            ctx.archive.ViewDelete("tournament");
        }, singleOption);

        return com;
    }

}