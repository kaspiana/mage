using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComView(CLIContext ctx){
        var viewRefArgument = new Argument<string>(
            name: "view"
        );

        // mage view [view-ref]
        var com = new Command("view", "Manipulate view."){
            viewRefArgument,
            ComViewClear(ctx, viewRefArgument),
            ComViewDelete(ctx, viewRefArgument),
            ComViewReflect(ctx, viewRefArgument),
            ComViewStash(ctx, viewRefArgument),
            ComViewUnstash(ctx, viewRefArgument),
            ComViewAdd(ctx, viewRefArgument)
        };

        com.SetHandler((viewRef) => {

            var viewName = ObjectRef.ResolveView(ctx.archive, viewRef);
            var view = (View)ctx.archive.ViewGet(viewName!)!;

            Console.WriteLine($"view {viewName}");
            
            var pageSize = 3; // move to config file
            for(int i = 0; i < view.documents.Count(); i++){
                if(i % pageSize == 0){
                    if(i > 0){
                        Console.Write($":");
                        var l = Console.ReadKey();
                        if(l.KeyChar == 'q')
                            break;
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                    }
                    var ogFGColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"PAGE {i / pageSize}");
                    Console.ForegroundColor = ogFGColor;
                }
                var documentID = view.documents[i];
                if(documentID is null){
                    Console.WriteLine($" * /{i}: <missing>");
                } else {
                    Console.WriteLine($" * /{i}: {ctx.archive.GetDocumentHash((DocumentID)documentID)}");
                    var doc = ctx.archive.DocumentGet((DocumentID)documentID);
                    Console.WriteLine($"      File extension: {doc?.fileExt}");
                    if(doc?.comment is not null) {
                        Console.WriteLine($"      Comment:");
                        foreach(var line in doc?.comment.Split('\n')){
                            Console.WriteLine($"       {line}");
                        }
                    }

                    var tagNames = ctx.archive.DocumentGetTags((DocumentID)documentID).Select(tagID => ctx.archive.TagAsString(tagID));
                    if(tagNames.Count() == 0)
                        Console.WriteLine("$      Tags: <none>");
                    else {
                        Console.WriteLine($"      Tags:");
                        Console.WriteLine($"       {string.Join(" ", tagNames)}");
                    }
                }
                Console.WriteLine();
            }

        }, viewRefArgument);

        return com;
    }

    public static Command ComViewClear(CLIContext ctx, Argument<string> viewRefArgument){
        // mage view [view-ref] clear
        var com = new Command("clear", "Clear the view.");

        com.SetHandler((viewRef) => {
            var viewName = ObjectRef.ResolveView(ctx.archive, viewRef);
            ctx.archive.ViewClear(viewName!);
        }, viewRefArgument);
        
        return com;
    }

    public static Command ComViewDelete(CLIContext ctx, Argument<string> viewRefArgument){
        // mage view [view-ref] delete
        var com = new Command("delete", "Delete the view.");

        com.SetHandler((viewRef) => {
            var viewName = ObjectRef.ResolveView(ctx.archive, viewRef);
            ctx.archive.ViewDelete(viewName!);
        }, viewRefArgument);
        
        return com;
    }

    public static Command ComViewReflect(CLIContext ctx, Argument<string> viewRefArgument){
        var sourceViewRefArguemnt = new Argument<string>(
            name: "src-view"
        );

        // mage view [view-ref] reflect [view-ref]
        var com = new Command("reflect", "Reflect another view's documents into the view."){
            sourceViewRefArguemnt
        };

        com.SetHandler((viewRef, sourceViewRef) => {
            var viewName = ObjectRef.ResolveView(ctx.archive, viewRef)!;
            var sourceViewName = ObjectRef.ResolveView(ctx.archive, sourceViewRef)!;

            ctx.archive.ViewReflect(viewName, sourceViewName);
        }, viewRefArgument, sourceViewRefArguemnt);
        
        return com;
    }

    public static Command ComViewAdd(CLIContext ctx, Argument<string> viewRefArgument){
        var docRefArgument = new Argument<string>(
            name: "document"
        );
        
        // mage view [view-ref] add [doc-ref]
        var com = new Command("add", "Add a document to the view."){
            docRefArgument
        };

        com.SetHandler((viewRef, docRef) => {
            var viewName = ObjectRef.ResolveView(ctx.archive, viewRef)!;
            var docID = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;

            ctx.archive.ViewAdd(viewName, docID);
        }, viewRefArgument, docRefArgument);
        
        return com;
    }

    public static Command ComViewStash(CLIContext ctx, Argument<string> viewRefArgument){
        // mage view [view-ref] stash
        var com = new Command("stash", "Stash and clear the view's contents.");

        com.SetHandler((viewRef) => {
            var viewName = ObjectRef.ResolveView(ctx.archive, viewRef);
            var stashName = ctx.archive.ViewStash(viewName);

            Console.WriteLine($"stashed documents in {stashName}");
        }, viewRefArgument);
        
        return com;
    }

    public static Command ComViewUnstash(CLIContext ctx, Argument<string> viewRefArgument){
        var stashRefArguemnt = new Argument<string>(
            name: "stash"
        );

        // mage view [view-ref] unstash [stash-ref]
        var com = new Command("unstash", "Reflect a stash into this view and delete the stash."){
            stashRefArguemnt
        };

        com.SetHandler((viewRef, stashRef) => {
            var viewName = ObjectRef.ResolveView(ctx.archive, viewRef)!;
            var stashName = ObjectRef.ResolveView(ctx.archive, stashRef)!;

            ctx.archive.ViewClear(viewName);
            ctx.archive.ViewReflect(viewName, stashName);
            ctx.archive.ViewDelete(stashName);
        }, viewRefArgument, stashRefArguemnt);
        
        return com;
    }

}