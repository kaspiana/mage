using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComTags(CLIContext ctx){

        var com = new Command("tags", "List all tags.");

        com.SetHandler(() => {
            ctx.archive.db.EnsureConnected();
            var tagIDs = ctx.archive.db.QueryTags("");
            var tags = tagIDs.Select((id) => ctx.archive.TagGet(id));

            foreach(var tag in tags){
                var taxonym = ctx.archive.TaxonymGet((TaxonymID)tag?.taxonymID);
                if(taxonym?.canonParentID == Archive.ROOT_TAXONYM_ID){
                    Console.WriteLine($" * /{tag?.id}: {taxonym?.canonAlias}");
                } else {
                    var parentTaxonymName = ctx.archive.TaxonymGet((TaxonymID)taxonym?.canonParentID)?.canonAlias;
                    Console.WriteLine($" * /{tag?.id}: {parentTaxonymName}:{taxonym?.canonAlias}");
                }
            }
        });

        return com;
        
    }

}