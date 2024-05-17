using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComSearch(CLIContext ctx){
        var sqlClauseOption = new Option<string>(
            name: "--raw",
            description: "SQL clause",
            getDefaultValue: () => ""
        );

        // mage search --sql
        var com = new Command("search", "Search all documents."){
            sqlClauseOption
        };

        com.SetHandler((sqlClause) => {

            var ids = ctx.archive.DocumentsQuery(sqlClause);
            if(ids.Count() > 0){
                var queryViewName = ctx.archive.ViewQueryCreate();
                foreach(var id in ids){
                    ctx.archive.ViewAdd(queryViewName, id);
                }
                Console.WriteLine($"{ids.Count()} results found, reflected in {queryViewName}");
            } else {
                Console.WriteLine($"no results found");
            }

        }, sqlClauseOption);
        
        return com;
    }

}