using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using Sprache;
using SQLitePCL;

public static partial class CLICommands {
    public static RootCommand CreateRoot(CLIContext ctx){

        RootCommand? com = null;
        var msg = "A tool for cataloguing images and other documents.";

        if(ctx.archive is null){
            com = new RootCommand(msg){
                ComInit(ctx)
            };
        } else {
            com = new RootCommand(msg){
                ComTest(ctx),

                ComIngest(ctx),

                ComBind(ctx),
                ComUnbind(ctx),

                ComNew(ctx),
                ComDelete(ctx),

                ComDocs(ctx),
                ComDoc(ctx),

                ComViews(ctx),
                ComView(ctx),

                ComTaxonyms(ctx),
                ComTaxonym(ctx),

                ComTags(ctx),
                ComTag(ctx),

                ComSearch(ctx)
            };
        }

        return com;

    }

    public static Command ComInit(CLIContext ctx){
        var archiveNameOption = new Option<string?>(
            name: "--name",
            description: "The name to give the new archive.",
            getDefaultValue: () => null
        );

        var com = new Command("init", "Initialise a new archive."){
            archiveNameOption
        };

        com.SetHandler((archiveName) => {
            ctx.archive = Archive.Init(ctx.archiveDir, archiveName);
        }, archiveNameOption);

        return com;
    }

    public static Command ComTest(CLIContext ctx){
        var com = new Command("test", "For debugging purposes.");
        com.SetHandler(() => {
            
            var query = QueryParser.Query.Parse("(rose_lalonde AND dirk_strider) OR homestuck2");
            Console.WriteLine(query.root);

            query.GetResults(ctx.archive);

        });

        return com;
    }


}