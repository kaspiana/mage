using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComIngest(CLIContext ctx){

        var commentOption = new Option<string?>(
            name: "--comment",
            description: "A comment to be given on the document.",
            getDefaultValue: () => null
        );

        // mage ingest
        var com = new Command("ingest", "Ingest files in inbox into archive.")
        {
            commentOption,
            ComIngestFrom(ctx, commentOption),
            ComIngestList(ctx)
        };
        com.SetHandler((comment) => {
            ctx.archive.Ingest();
        }, commentOption);

        return com;
    }

    public static Command ComIngestList(CLIContext ctx){
        var com = new Command("list", "Ingest files from ingest list.");
        com.SetHandler(() => {
            ctx.archive.IngestList();
        });
        return com;
    }

    public static Command ComIngestFrom(CLIContext ctx, Option<string?> commentOption){
        var filePathArgument = new Argument<string[]>(
            name: "File path",
            description: "File to be copied and ingested"
        ){ Arity = ArgumentArity.ZeroOrMore };
        
        // mage ingest from
        var com = new Command("from", "Copy files to ingest into archive.")
        {
            filePathArgument,
            commentOption
        };
        com.SetHandler((comment, filePaths) => {
            foreach(var filePath in filePaths){
                ctx.archive.IngestFile(filePath, comment);
            }
        }, commentOption, filePathArgument);

        return com;
    }

}