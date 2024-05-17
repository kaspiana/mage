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

        var aliasArgument = new Option<string[]>(
            name: "--alias"
        ){ 
            Arity = ArgumentArity.OneOrMore, 
            AllowMultipleArgumentsPerToken = true 
        };

        var noncanonicalParentRefArgument = new Option<string[]>(
            name: "--parent"
        ){
            Arity = ArgumentArity.OneOrMore, 
            AllowMultipleArgumentsPerToken = true 
        };

        var com = new Command("taxonym", "Create a taxonym."){
            parentRefArgument,
            nameArgument,
            aliasArgument,
            noncanonicalParentRefArgument
        };

        com.SetHandler((parentRef, name, aliases, noncanonicalParentRefs) => {
            var parentID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, parentRef)!;
            var taxonymID = (TaxonymID)ctx.archive.TaxonymCreate(parentID, name);
            foreach(var alias in aliases){
                ctx.archive.TaxonymAddAlias(taxonymID, alias);
            }
            foreach(var ncParentRef in noncanonicalParentRefs){
                var ncParentID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, ncParentRef)!;
                ctx.archive.TaxonymAddParent(taxonymID, ncParentID);
            }

            Console.WriteLine($"{taxonymID}: {parentID}:{name}");
        }, 
            parentRefArgument, 
            nameArgument, 
            aliasArgument, 
            noncanonicalParentRefArgument
        );

        return com;

    }

}