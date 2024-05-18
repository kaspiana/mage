using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComTag(CLIContext ctx){

        var tagRefArgument = new Argument<string?>(
            name: "tag"
        );

        var com = new Command("tag", "Manipulate tag."){
            tagRefArgument
        };

        com.SetHandler((tagRef) => {
            var tagID = (TagID)ObjectRef.ResolveTag(ctx.archive!, tagRef!)!;
            var tag = ctx.archive?.TagGet(tagID);
            var taxonym = ctx.archive?.TaxonymGet((TaxonymID)tag?.taxonymID!);
            var parentTaxonym = ctx.archive?.TaxonymGet((TaxonymID)taxonym?.canonicalParentID!);

            Console.WriteLine($"tag /{tagID}");

            if(parentTaxonym is not null)
                Console.WriteLine($"\tTaxonym: {parentTaxonym?.canonicalAlias}:{taxonym?.canonicalAlias} (/{taxonym?.id})");
            else
                Console.WriteLine($"\tTaxonym: {taxonym?.canonicalAlias} (/{taxonym?.id})");

            

        }, tagRefArgument);

        return com;

    }

}