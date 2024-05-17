using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComTaxonym(CLIContext ctx){

        var taxonymRefArgument = new Argument<string>(
            name: "taxonym"
        );

        var com = new Command("taxonym", "Manipulate taxonym"){
            taxonymRefArgument,
            ComTaxonymChildren(ctx, taxonymRefArgument)
        };

        com.SetHandler((taxonymRef) => {
            var taxonymID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, taxonymRef)!;
            var taxonym = ctx.archive.TaxonymGet(taxonymID);

            if(taxonym?.id == Archive.ROOT_TAXONYM_ID){
                Console.WriteLine($"{taxonym?.id}: <root>");
            } else {
                Console.WriteLine($"{taxonym?.id}: {taxonym?.canonicalParentID}:{taxonym?.canonicalAlias}");
            }
        }, taxonymRefArgument);

        return com;
    }

    public static Command ComTaxonymChildren(CLIContext ctx, Argument<string> taxonymRefArgument){

        var com = new Command("children", "List the taxonym's children.");

        com.SetHandler((taxonymRef) => {
            var taxonymID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, taxonymRef)!;
            var childIDs = ctx.archive.TaxonymGetChildren(taxonymID);
            var children = childIDs.Select((id) => ctx.archive.TaxonymGet(id));

            foreach(var taxonym in children){
                if(taxonym?.id == Archive.ROOT_TAXONYM_ID){
                    Console.WriteLine($"{taxonym?.id}: <root>");
                } else {
                    Console.WriteLine($"{taxonym?.id}: {taxonym?.canonicalParentID}:{taxonym?.canonicalAlias}");
                }
            }


        }, taxonymRefArgument);

        return com;
    }

}