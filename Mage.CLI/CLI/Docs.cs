using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComDocs(CLIContext ctx){
        var viewRefOption = new Option<string>(
            name: "--view",
            getDefaultValue: () => "."
        );

        // mage docs
        var com = new Command("docs", "Reflect *all* documents in bound view."){
            viewRefOption
        };
        com.SetHandler((viewRef) => {
            var viewName = ObjectRef.ResolveView(ctx.archive, viewRef);
            
            ctx.archive.ViewClear(viewName);
            var docIDs = ctx.archive.DocumentsQuery("");
            foreach(var docID in docIDs){
                ctx.archive.ViewAdd(viewName, docID);
            }
        }, viewRefOption);
        
        return com;
    }

}