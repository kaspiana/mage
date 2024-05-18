using System.CommandLine;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComDoc(CLIContext ctx){
        var docRefArgument = new Argument<string>(
            name: "document"
        );

        var reflectOption = new Option<bool>(
            name: "--reflect",
            description: "Add the document to the bound view.",
            getDefaultValue: () => false
        );

        // mage doc
        var com = new Command("doc", "Manipulate document.")
        {
            docRefArgument,
            reflectOption,
            ComDocOpen(ctx, docRefArgument)
        };

        com.SetHandler((docRef, reflect) => {
            var docID = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;
            var doc = (Document)ctx.archive.DocumentGet(docID)!;

            Console.WriteLine($"document {doc.hash}");
            Console.WriteLine($"\tArchive ID: /{doc.id}");
            Console.WriteLine($"\tFile name: {doc.fileName}");
            Console.WriteLine($"\tExtension: {doc.extension}");
            Console.WriteLine($"\tIngest timestamp: {doc.ingestTimestamp}");
            Console.WriteLine($"\tComment: {(doc.comment is null ? "<none>" : doc.comment)}");

            if(reflect){
                var boundView = ctx.archive.BindingGet(ObjectType.View);
                ctx.archive.ViewAdd(boundView, docID);
            }

        }, docRefArgument, reflectOption);
        
        return com;
    }

    public static Command ComDocOpen(CLIContext ctx, Argument<string> docRefArgument){
        // mage doc [doc-ref] open
        var com = new Command("open", "Open document with appropriate handler.");
        com.SetHandler((docRef) => {
            var docID = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;
            var doc = (Document)ctx.archive.DocumentGet(docID)!;

            var viewIndex = ctx.archive.ViewAdd(Archive.OPEN_VIEW_NAME, docID);
            var viewFilePath = $"{ctx.archive.mageDir}{Archive.VIEWS_DIR_PATH}{Archive.OPEN_VIEW_NAME}/{viewIndex}~{doc.hash}.{doc.extension}";

            using Process fileOpener = new Process();

            fileOpener.StartInfo.FileName = "\"" + viewFilePath + "\"";
            fileOpener.StartInfo.UseShellExecute = true;
            fileOpener.Start();

        }, docRefArgument);
        
        return com;
    }

}