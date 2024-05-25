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
            description: "Match opponents against single selected document.",
            getDefaultValue: () => null
        );

        var com = new Command("tournament", "Run a rankings tournament."){
            singleOption
        };

        com.SetHandler((docRef) => {
            ctx.archive.db.EnsureConnected();

            using var rankingSampleCom = ctx.archive.db.GenCommand(
                DBCommands.Sample.Ranking,
                ("limit", 1)
            );

            ctx.archive.ViewCreate("tournament");

            using var docSampleCom = ctx.archive.db.GenCommand(
                DBCommands.Sample.Document(),
                ("limit", null)
            );

            DocumentID? docA = null;
            
            if(docRef is null){
                docSampleCom.Parameters["limit"].Value = 2;
            } else {
                docSampleCom.Parameters["limit"].Value = 1;
                docA = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;
            }

            while(true){

                DocumentID? docB = null;
                
                if(docRef is null){
                    var docSample = DBEngine.RunQuery(docSampleCom, r => (DocumentID)r.GetInt32(0)).ToArray();
                    if(docSample.Count() != 2){
                        return;
                    }

                    docA = docSample[0];
                    docB = docSample[1];
                } else {
                    while(docB is null || docB == docA){
                        docB = DBEngine.RunQuerySingle(docSampleCom, r => (DocumentID)r.GetInt32(0));
                    }
                }

                var rankingSample = DBEngine.RunQuery(rankingSampleCom, r => r.GetString(0)).ToArray();
                if(rankingSample.Count() != 1){
                    return;
                }

                var ranking = rankingSample[0];
                
                var docARanking = ctx.archive.DocumentGetRanking((DocumentID)docA, ranking);
                var docBRanking = ctx.archive.DocumentGetRanking((DocumentID)docB, ranking);
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

                var docAUpdatedRanking = Elo.Update(docARanking, docBRanking, docAScore);
                var docBUpdatedRanking = Elo.Update(docBRanking, docARanking, docBScore);

                ctx.archive.DocumentSetRanking((DocumentID)docA, ranking, docAUpdatedRanking);
                ctx.archive.DocumentSetRanking((DocumentID)docB, ranking, docBUpdatedRanking);
            }

            ctx.archive.ViewDelete("tournament");
        }, singleOption);

        return com;
    }

}