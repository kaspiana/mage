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
            ComDocOpen(ctx, docRefArgument),
            ComDocTags(ctx, docRefArgument),
            ComDocTag(ctx, docRefArgument),
            ComDocUntag(ctx, docRefArgument)
        };

        com.SetHandler((docRef, reflect) => {
            var docID = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;
            var doc = (Document)ctx.archive.DocumentGet(docID)!;

            Console.WriteLine($"document {doc.hash}");
            Console.WriteLine($"  Archive ID: /{doc.id}");
            Console.WriteLine($"  File name: {doc.fileName}");
            Console.WriteLine($"  Extension: {doc.extension}");
            Console.WriteLine($"  Ingest timestamp: {doc.ingestedAt}");
            Console.WriteLine($"  Comment: {(doc.comment is null ? "<none>" : doc.comment)}");
            Console.WriteLine($"  Deleted: {(doc.isDeleted ? "yes" : "no")}");

            var tagNames = ctx.archive.DocumentGetTags(docID).Select(tagID => ctx.archive.TagAsString(tagID));
            Console.WriteLine($"  Tags: {string.Join(" ", tagNames)}");

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
            var viewFilePath = $"{ctx.archive.archiveDir}{Archive.VIEWS_DIR_PATH}{Archive.OPEN_VIEW_NAME}/{viewIndex}~{doc.hash}.{doc.extension}";

            using Process fileOpener = new Process();

            fileOpener.StartInfo.FileName = "\"" + viewFilePath + "\"";
            fileOpener.StartInfo.UseShellExecute = true;
            fileOpener.Start();

        }, docRefArgument);
        
        return com;
    }

    public static Command ComDocTags(CLIContext ctx, Argument<string> docRefArgument){

        var com = new Command("tags", "List documents tags.");

        com.SetHandler((docRef) => {
            var docID = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;

            var tagIDs = ctx.archive.DocumentGetTags(docID);
            foreach(var tagID in tagIDs){
                var tag = ctx.archive?.TagGet(tagID);
                var taxonym = ctx.archive?.TaxonymGet((TaxonymID)tag?.taxonymID!);
                var parentTaxonym = ctx.archive?.TaxonymGet((TaxonymID)taxonym?.canonParentID!);

                if(parentTaxonym is not null)
                    Console.WriteLine($" * {parentTaxonym?.canonAlias}:{taxonym?.canonAlias} (/{taxonym?.id})");
                else
                    Console.WriteLine($" * {taxonym?.canonAlias} (/{taxonym?.id})");
            }

        }, docRefArgument);

        return com;
    }

    public static Command ComDocTag(CLIContext ctx, Argument<string> docRefArgument){

        var tagRefArgument = new Argument<string[]>(
            name: "tag"
        ){ Arity = ArgumentArity.ZeroOrMore };

        var com = new Command("tag", "Add tag to the document."){
            tagRefArgument
        };

        com.SetHandler((docRef, tagRefs) => {
            var docID = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;
            foreach(var tagRef in tagRefs){
                var tagID = (TagID)ObjectRef.ResolveTag(ctx.archive, tagRef)!;
                ctx.archive.DocumentAddTag(docID, tagID);
            }
        }, docRefArgument, tagRefArgument);

        return com;
    }

    public static Command ComDocUntag(CLIContext ctx, Argument<string> docRefArgument){

        var tagRefArgument = new Argument<string[]>(
            name: "tag"
        ){ Arity = ArgumentArity.ZeroOrMore };

        var com = new Command("untag", "Remove tag from the document."){
            tagRefArgument
        };

        com.SetHandler((docRef, tagRefs) => {
            var docID = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;
            foreach(var tagRef in tagRefs){
                var tagID = (TagID)ObjectRef.ResolveTag(ctx.archive, tagRef)!;
                ctx.archive.DocumentRemoveTag(docID, tagID);
            }
        }, docRefArgument, tagRefArgument);

        return com;
    }

}