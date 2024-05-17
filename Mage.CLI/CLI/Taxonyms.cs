using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComTaxonyms(CLIContext ctx){
        var com = new Command("taxonyms", "List all taxonyms.");

        com.SetHandler(() => {
            var taxonymIDs = ctx.archive.TaxonymsQuery("");
            var taxonyms = taxonymIDs.Select((id) => ctx.archive.TaxonymGet(id));

            foreach(var taxonym in taxonyms){
                if(taxonym?.id == Archive.ROOT_TAXONYM_ID){
                    Console.WriteLine($"{taxonym?.id}: <root>");
                } else {
                    Console.WriteLine($"{taxonym?.id}: {taxonym?.canonicalParentID}:{taxonym?.canonicalAlias}");
                }
            }
        });

        return com;
    }

}