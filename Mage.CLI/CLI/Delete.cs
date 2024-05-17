using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComDelete(CLIContext ctx){
        var com = new Command("delete", "Delete an object."){
            ComDeleteDoc(ctx),
            ComDeleteTaxonym(ctx)
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

}