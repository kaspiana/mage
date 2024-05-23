using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComBind(CLIContext ctx){
        // mage bind
        var com = new Command("bind", "List bound values."){
            ComBindDoc(ctx),
            ComBindView(ctx)
        };

        com.SetHandler(() => {
            Console.Write( File.ReadAllText($"{ctx.archive.archiveDir}{Archive.BIND_FILE_PATH}") );
        });

        return com;
    }

    public static Command ComBindDoc(CLIContext ctx){
        var docRefArgument = new Argument<string>(
            name: "document"
        );

        // mage bind doc
        var com = new Command("doc", "Bind a document to the context."){
            docRefArgument
        };

        com.SetHandler((docRef) => {
            var docID = ObjectRef.ResolveDocument(ctx.archive, docRef);
            ctx.archive.BindDocument(docID);
        }, docRefArgument);

        return com;
    }

    public static Command ComBindView(CLIContext ctx){
        var viewRefArgument = new Argument<string>(
            name: "view"
        );

        // mage bind view
        var com = new Command("view", "Bind a view to the context."){
            viewRefArgument
        };
        
        com.SetHandler((viewRef) => {
            var viewName = ObjectRef.ResolveView(ctx.archive, viewRef);
            ctx.archive.BindView(viewName);
        }, viewRefArgument);

        return com;
    }

}