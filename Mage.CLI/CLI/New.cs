using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComNew(CLIContext ctx){
        var com = new Command("new", "Create an object."){
            ComNewTaxonym(ctx)
        };
        return com;
    }

    public static Command ComNewTaxonym(CLIContext ctx){

        var parentRefArgument = new Argument<string>(
            name: "parent"
        );

        var nameArgument = new Argument<string>(
            name: "name"
        );

        var com = new Command("taxonym", "Create a taxonym."){
            parentRefArgument,
            nameArgument
        };

        com.SetHandler((parentRef, name) => {
            var parentID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, parentRef)!;

            var taxonymID = ctx.archive.TaxonymCreate(parentID, name);

            Console.WriteLine($"{taxonymID}: {parentID}:{name}");
        }, parentRefArgument, nameArgument);

        return com;

    }

}