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

    // mage in
    var inCommand = new Command("in", "Ingest files in inbox into archive.");
    inCommand.Add(commentOption);
    inCommand.SetHandler((comment) => {
        archive.Ingest();
    }, commentOption);
    rootCommand.Add(inCommand);

    // mage in from
    var inFromCommand = new Command("from", "Copy files to ingest into archive.");
    var filePathArgument = new Argument<string[]>(
        name: "File path",
        description: "File to be copied and ingested"
    ){ Arity = ArgumentArity.ZeroOrMore };
    inFromCommand.Add(filePathArgument);
    inFromCommand.Add(commentOption);
    inFromCommand.SetHandler((comment, filePaths) => {
        foreach(var filePath in filePaths){
            archive.IngestFile(filePath, comment);
        }
    }, commentOption, filePathArgument);
    inCommand.Add(inFromCommand);

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
