using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
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

                ComDocs(ctx),
                ComDoc(ctx),

                ComViews(ctx),
                ComView(ctx),

                ComTaxonyms(ctx),
                ComTaxonym(ctx),

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
            ctx.archive.db.EnsureConnected();
            var taxIDs = ctx.archive.db.ReadTaxonymChildren(Archive.ROOT_TAXONYM_ID);

            foreach(var taxID in taxIDs){
                var taxonym = ctx.archive.db.ReadTaxonym(taxID);
                Console.WriteLine(taxonym?.canonicalAlias);
            }
        });

        return com;
    }


}