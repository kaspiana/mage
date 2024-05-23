using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComDelete(CLIContext ctx){
        var com = new Command("delete", "Delete an object."){
            ComDeleteDoc(ctx),
            ComDeleteTaxonym(ctx),
            ComDeleteTag(ctx)
        };
        return com;
    }

    public static Command ComDeleteDoc(CLIContext ctx){
        var docRefArgument = new Argument<string>(
            name: "doc"
        );

        var com = new Command("doc", "Delete document."){
            docRefArgument
        };

        com.SetHandler((docRef) => {
            var documentID = (DocumentID)ObjectRef.ResolveDocument(ctx.archive, docRef)!;
            var documentHash = ctx.archive.GetDocumentHash(documentID);
            Console.WriteLine($"document {documentHash} marked for deletion");
            ctx.archive.DocumentDelete(documentID);
        }, docRefArgument);

        return com;
    }

    public static Command ComDeleteTaxonym(CLIContext ctx){
        var taxonymRefArgument = new Argument<string>(
            name: "taxonym"
        );

        var com = new Command("taxonym", "Delete taxonym."){
            taxonymRefArgument
        };

        com.SetHandler((taxonymRef) => {
            var taxonymID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, taxonymRef)!;
            ctx.archive.TaxonymDelete(taxonymID);
        }, taxonymRefArgument);

        return com;
    }

    public static Command ComDeleteTag(CLIContext ctx){
        var tagRefArgument = new Argument<string>(
            name: "tag"
        );
        
        var preserveTaxOption = new Option<bool>(
            name: "--preserve-taxonym",
            getDefaultValue: () => false
        );

        var com = new Command("tag", "Delete tag."){
            tagRefArgument,
            preserveTaxOption
        };

        com.SetHandler((tagRef, preserveTax) => {
            var tagID = (TagID)ObjectRef.ResolveTag(ctx.archive, tagRef)!;
            ctx.archive.TagDelete(tagID, preserveTax);
        }, tagRefArgument, preserveTaxOption);

        return com;
    }

}