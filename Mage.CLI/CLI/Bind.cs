using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.CLI;
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
            var lines = File.ReadAllLines($"{ctx.archive.archiveDir}{Archive.BIND_FILE_PATH}");
            foreach(var line in lines){
                var parts = line.Split('=');
                switch(parts[0]){
                    case "doc":     ConsoleExt.WriteColored("bound document: ", ConsoleColor.Yellow); break;
                    case "tag":     ConsoleExt.WriteColored("bound tag:      ", ConsoleColor.Yellow); break;
                    case "taxonym": ConsoleExt.WriteColored("bound taxonym:  ", ConsoleColor.Yellow); break;
                    case "series":  ConsoleExt.WriteColored("bound series:   ", ConsoleColor.Yellow); break;
                    case "view":    ConsoleExt.WriteColored("bound view:     ", ConsoleColor.Yellow); break;
                }
                if(parts[1].Trim() == "")
                    Console.WriteLine("<none>");
                else Console.WriteLine(parts[1]);
            }
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