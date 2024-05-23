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
            ComDocUntag(ctx, docRefArgument),
            ComDocSources(ctx, docRefArgument),
            ComDocSource(ctx, docRefArgument),
            ComDocUnsource(ctx, docRefArgument)
        };

        com.SetHandler((docRef, reflect) => {
            var docID = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;
            var doc = (Document)ctx.archive.DocumentGet(docID)!;

            Console.WriteLine($"document {doc.hash}");
            Console.WriteLine($"  Archive ID: /{doc.id}");
            Console.WriteLine($"  File name: {doc.fileName}");
            Console.WriteLine($"  File extension: {doc.fileExt}");
            var fileSizeStr = "";
            switch(doc.fileSize){
                case < (1 << 10): fileSizeStr = $"{doc.fileSize / (1 << 0)} B"; break;
                case < (1 << 20): fileSizeStr = $"{doc.fileSize / (1 << 10)} KB"; break;
                case < (1 << 30): fileSizeStr = $"{doc.fileSize / (1 << 20)} MB"; break;
                default: fileSizeStr = $"{doc.fileSize / (1 << 30)} GB"; break;
            }
            Console.WriteLine($"  File size: {fileSizeStr}");
            Console.WriteLine($"  Added at: {doc.addedAt}");
            Console.WriteLine($"  Comment: {(doc.comment is null ? "<none>" : doc.comment)}");
            Console.WriteLine($"  Deleted: {(doc.isDeleted ? "yes" : "no")}");

            var tagNames = ctx.archive.DocumentGetTags(docID).Select(tagID => ctx.archive.TagAsString(tagID));
            Console.WriteLine($"  Tags: {string.Join(" ", tagNames)}");

            if(reflect){
                var boundView = ctx.archive.BindingGet(ObjectType.View);
                ctx.archive.ViewAdd(boundView, docID);
            }

            var sources = ctx.archive.DocumentGetSources(docID);
            Console.WriteLine($"  Sources: {(sources.Count() == 0 ? "<none>" : "")}");
            foreach(var source in sources){
                Console.WriteLine($"   * {source}");
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
            var viewFilePath = $"{ctx.archive.archiveDir}{Archive.VIEWS_DIR_PATH}{Archive.OPEN_VIEW_NAME}/{viewIndex}~{doc.hash}.{doc.fileExt}";

            using Process fileOpener = new Process();

            fileOpener.StartInfo.FileName = "\"" + viewFilePath + "\"";
            fileOpener.StartInfo.UseShellExecute = true;
            fileOpener.Start();

        }, docRefArgument);
        
        return com;
    }

    public static Command ComDocSources(CLIContext ctx, Argument<string> docRefArgument){
        var com = new Command("sources", "List document's sources.");

        com.SetHandler((docRef) => {
            var docID = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;

            var sources = ctx.archive.DocumentGetSources(docID);

            if(sources.Count() == 0){
                Console.WriteLine("no sources");
                return;
            }

            foreach(var source in sources){
                Console.WriteLine($" * {source}");
            }

        }, docRefArgument);

        return com;
    }

    public static Command ComDocSource(CLIContext ctx, Argument<string> docRefArgument){
        var urlArgument = new Argument<string>(
            name: "url"
        );

        var com = new Command("source", "Add source to document."){
            urlArgument
        };

        com.SetHandler((docRef, url) => {
            var docID = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;
            ctx.archive.DocumentAddSource(docID, url);
        }, docRefArgument, urlArgument);

        return com;
    }

    public static Command ComDocUnsource(CLIContext ctx, Argument<string> docRefArgument){
        var urlArgument = new Argument<string>(
            name: "url"
        );

        var com = new Command("unsource", "From source from document."){
            urlArgument
        };

        com.SetHandler((docRef, url) => {
            var docID = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;
            ctx.archive.DocumentRemoveSource(docID, url);
        }, docRefArgument, urlArgument);

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