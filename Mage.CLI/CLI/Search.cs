using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using Sprache;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComSearch(CLIContext ctx){
        var sqlClauseOption = new Option<string>(
            name: "--raw",
            description: "SQL clause",
            getDefaultValue: () => ""
        );

        var tagSearchArgument = new Argument<string[]>(
            name: "query",
            description: "Tag search"
        ){
            Arity = ArgumentArity.ZeroOrMore
        };

        // mage search --sql
        var com = new Command("search", "Search all documents."){
            sqlClauseOption,
            tagSearchArgument
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

        com.SetHandler((tagSearchStrs) => {
            var tagSearchStr = string.Join(" ", tagSearchStrs);

            Console.WriteLine();
            Console.WriteLine(tagSearchStr);


            
            var query = QueryParser.Parse(tagSearchStr);

            Console.WriteLine(query.root);

            var ids = query.GetResults(ctx.archive);

            if(ids.Count() > 0){
                var queryViewName = ctx.archive.ViewQueryCreate();
                foreach(var id in ids){
                    ctx.archive.ViewAdd(queryViewName, id);
                }
                Console.WriteLine($"{ids.Count()} results found, reflected in {queryViewName}");
            } else {
                Console.WriteLine($"no results found");
            }

            Console.WriteLine();

        }, tagSearchArgument);
        
        return com;
    }

}