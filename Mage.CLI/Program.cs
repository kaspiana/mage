using System.CommandLine;
using System.Diagnostics;
using Mage.Engine;

var archiveDir = "C:/home/catalogue-tool/v2/test-archives/a/";
var mageDir = $"{archiveDir}.mage/";

Archive? archive = null;

if(Directory.Exists(mageDir))
    archive = Archive.Load(mageDir, archiveDir);

var rootCommand = new RootCommand("A tool for cataloguing images and other documents.");

    // mage init
    var initCommand = new Command("init", "Initialise a new archive.");
    var archiveNameOption = new Option<string?>(
        name: "--name",
        description: "The name to give the new archive.",
        getDefaultValue: () => null
    );
    initCommand.SetHandler((archiveName) => {
        archive = Archive.Init(archiveDir, archiveName);
    }, archiveNameOption);
    initCommand.Add(archiveNameOption);
    rootCommand.Add(initCommand);

if(archive is not null){

    var testCommand = new Command("test", "For debugging purposes.");
    testCommand.SetHandler(() => {



    });
    rootCommand.Add(testCommand);

    var commentOption = new Option<string?>(
        name: "--comment",
        description: "A comment to be given on the document.",
        getDefaultValue: () => null
    );

    // mage ingest
    var ingestCommand = new Command("ingest", "Ingest files in inbox into archive.");
    ingestCommand.Add(commentOption);
    ingestCommand.SetHandler((comment) => {
        archive.Ingest();
    }, commentOption);
    rootCommand.Add(ingestCommand);

    // mage ingest from
    var ingestFromCommand = new Command("from", "Copy files to ingest into archive.");
    var filePathArgument = new Argument<string[]>(
        name: "File path",
        description: "File to be copied and ingested"
    ){ Arity = ArgumentArity.ZeroOrMore };
    ingestFromCommand.Add(filePathArgument);
    ingestFromCommand.Add(commentOption);
    ingestFromCommand.SetHandler((comment, filePaths) => {
        foreach(var filePath in filePaths){
            archive.IngestFile(filePath, comment);
        }
    }, commentOption, filePathArgument);
    ingestCommand.Add(ingestFromCommand);

    // mage doc
    var docCommand = new Command("doc", "Manipulate document.");
    var docRefArgument = new Argument<string>(
        name: "document"
    );
    var reflectOption = new Option<bool>(
        name: "--reflect",
        description: "Add the document to the bound view.",
        getDefaultValue: () => false
    );
    docCommand.Add(docRefArgument);
    docCommand.Add(reflectOption);
    docCommand.SetHandler((docRef, reflect) => {
        var docID = (DocumentID)ObjectRef.ResolveDocument(archive, docRef)!;
        var doc = (Document)archive.GetDocument(docID)!;

        Console.WriteLine($"document {doc.hash}");
        Console.WriteLine($"\tArchive ID: @{doc.id}");
        Console.WriteLine($"\tFile name: {doc.fileName}");
        Console.WriteLine($"\tExtension: {doc.extension}");
        Console.WriteLine($"\tIngest timestamp: {doc.ingestTimestamp}");
        Console.WriteLine($"\tComment: {(doc.comment is null ? "<none>" : doc.comment)}");

        if(reflect){
            var boundView = archive.GetBinding(ObjectType.View);
            archive.ViewAdd(boundView, docID);
        }

    }, docRefArgument, reflectOption);
    rootCommand.Add(docCommand);

    // mage doc [doc-ref] open
    var docOpenCommand = new Command("open", "Open document with appropriate handler.");
    docOpenCommand.SetHandler((docRef) => {
        var docID = (DocumentID)ObjectRef.ResolveDocument(archive, docRef)!;
        var doc = (Document)archive.GetDocument(docID)!;

        var viewIndex = archive.ViewAdd("open", docID);
        var viewFilePath = $"{archive.mageDir}{Archive.VIEWS_DIR_PATH}open/{viewIndex}~{doc.hash}.{doc.extension}";

        using Process fileOpener = new Process();

        fileOpener.StartInfo.FileName = "\"" + viewFilePath + "\"";
        fileOpener.StartInfo.UseShellExecute = true;
        fileOpener.Start();

    }, docRefArgument);
    docCommand.Add(docOpenCommand);

    // mage doc [doc-ref] delete
    var docDeleteCommand = new Command("delete", "Delete document.");
    docDeleteCommand.SetHandler((docRef) => {
        var docID = (DocumentID)ObjectRef.ResolveDocument(archive, docRef)!;
        archive.DocumentDelete(docID);
    }, docRefArgument);
    docCommand.Add(docDeleteCommand);


    // mage view [view-ref]
    var viewCommand = new Command("view", "Manipulate view.");
    var viewRefArgument = new Argument<string>(
        name: "view"
    );
    viewCommand.Add(viewRefArgument);
    viewCommand.SetHandler((viewRef) => {

        var viewName = ObjectRef.ResolveView(archive, viewRef);
        var view = (View)archive.ViewGet(viewName!)!;

        Console.WriteLine($"view {viewName}");
        
        for(int i = 0; i < view.documents.Count(); i++){
            var documentID = view.documents[i];
            if(documentID is null){
                Console.WriteLine($"\t/{i}: <missing>");
            } else {
                Console.WriteLine($"\t/{i}: {archive.GetDocumentHash((DocumentID)documentID)}");
            }
        }

    }, viewRefArgument);
    rootCommand.Add(viewCommand);

    // mage view [view-ref] clear
    var viewClearCommand = new Command("clear", "Clear the view.");
    viewClearCommand.SetHandler((viewRef) => {
        var viewName = ObjectRef.ResolveView(archive, viewRef);
        archive.ViewClear(viewName!);
    }, viewRefArgument);
    viewCommand.Add(viewClearCommand);

    // mage view [view-ref] delete
    var viewDeleteCommand = new Command("delete", "Delete the view.");
    viewDeleteCommand.SetHandler((viewRef) => {
        var viewName = ObjectRef.ResolveView(archive, viewRef);
        archive.ViewDelete(viewName!);
    }, viewRefArgument);
    viewCommand.Add(viewDeleteCommand);

    // mage view [view-ref] reflect [view-ref]
    var viewReflectCommand = new Command("reflect", "Reflect another view's documents into the view.");
    var sourceViewRefArguemnt = new Argument<string>(
        name: "src-view"
    );
    viewReflectCommand.Add(sourceViewRefArguemnt);
    viewReflectCommand.SetHandler((viewRef, sourceViewRef) => {
        var viewName = ObjectRef.ResolveView(archive, viewRef)!;
        var sourceViewName = ObjectRef.ResolveView(archive, sourceViewRef)!;

        archive.ViewReflect(viewName, sourceViewName);
    }, viewRefArgument, sourceViewRefArguemnt);
    viewCommand.Add(viewReflectCommand);

    // mage view [view-ref] stash
    var viewStashCommand = new Command("stash", "Stash and clear the view's contents.");
    viewStashCommand.SetHandler((viewRef) => {
        var viewName = ObjectRef.ResolveView(archive, viewRef);
        var stashName = archive.ViewStash(viewName);

        Console.WriteLine($"stashed documents in {stashName}");
    }, viewRefArgument);
    viewCommand.Add(viewStashCommand);

    // mage view [view-ref] unstash [stash-ref]
    var viewUnstashCommand = new Command("unstash", "Reflect a stash into this view and delete the stash.");
    var stashRefArguemnt = new Argument<string>(
        name: "stash"
    );
    viewUnstashCommand.Add(stashRefArguemnt);
    viewUnstashCommand.SetHandler((viewRef, stashRef) => {
        var viewName = ObjectRef.ResolveView(archive, viewRef)!;
        var stashName = ObjectRef.ResolveView(archive, stashRef)!;

        archive.ViewClear(viewName);
        archive.ViewReflect(viewName, stashName);
        archive.ViewDelete(stashName);
    }, viewRefArgument, stashRefArguemnt);
    viewCommand.Add(viewUnstashCommand);

    // mage view [view-ref] add [doc-ref]
    var viewAddCommand = new Command("add", "Add a document to the view.");
    viewAddCommand.Add(docRefArgument);
    viewAddCommand.SetHandler((viewRef, docRef) => {
        var viewName = ObjectRef.ResolveView(archive, viewRef)!;
        var docID = (DocumentID)ObjectRef.ResolveDocument(archive, docRef)!;

        archive.ViewAdd(viewName, docID);
    }, viewRefArgument, docRefArgument);
    viewCommand.Add(viewAddCommand);

}

rootCommand.Invoke(args);

if(archive is not null)
    archive.Unload();

return;

//archive.Ingest();


//var docID = archive.GetDocumentID("AFB1D7C9ABAA162CCEAF4A60DD51231F0B9D0424");
//archive.DocumentDelete((DocumentID)docID!);
//archive.DocumentDeleteAll();



/*
string[] objectRefStrs = [
    "@0",
    "query0",
    "952663dd...",
    ".",
    "./2",
    "query0/0"
];

Console.WriteLine();
foreach(var objectRefStr in objectRefStrs){
    var objectRef = ObjectRef.Parse(objectRefStr);
    Console.WriteLine($"'{objectRefStr}' -> {objectRef}");
}
Console.WriteLine();
*/

//Console.Write(archive.name);
