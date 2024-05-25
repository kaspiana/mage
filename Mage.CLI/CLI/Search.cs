using System.CommandLine;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using Sprache;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComSearch(CLIContext ctx){

        var tagSearchArgument = new Argument<string[]>(
            name: "query",
            description: "Tag search"
        ){
            Arity = ArgumentArity.ZeroOrMore
        };

        var orderOption = new Option<string>(
            name: "--order",
            getDefaultValue: () => "id"
        );

        var ascOption = new Option<bool>(
            name: "--asc",
            getDefaultValue: () => false
        );

        var descOption = new Option<bool>(
            name: "--desc",
            getDefaultValue: () => false
        );

        // mage search --sql
        var com = new Command("search", "Search all documents."){
            orderOption,
            ascOption,
            descOption,
            tagSearchArgument,
        };

        com.SetHandler((tagSearchStrs, order, asc, desc) => {
            var tagSearchStr = string.Join(" ", tagSearchStrs);
            if(!desc) asc = true;

            Console.WriteLine();
            Console.WriteLine(tagSearchStr);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            var query = QueryParser.Parse(tagSearchStr);

            Console.WriteLine(query.root);

            var ids = query.GetResults(ctx.archive, order, asc);

            sw.Stop();

            if(ids.Count() > 0){
                var queryViewName = ctx.archive.ViewQueryCreate();
                foreach(var id in ids){
                    ctx.archive.ViewAdd(queryViewName, id);
                }
                Console.WriteLine($"{ids.Count()} results found in {sw.ElapsedMilliseconds}ms, reflected in {queryViewName}");
            } else {
                Console.WriteLine($"no results found");
            }

            Console.WriteLine();

        }, 
            tagSearchArgument,
            orderOption,
            ascOption,
            descOption
        );
        
        return com;
    }

}