using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.CLI;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComTag(CLIContext ctx){

        var tagRefArgument = new Argument<string?>(
            name: "tag"
        );

        var com = new Command("tag", "Manipulate tag."){
            tagRefArgument,
            ComTagImplications(ctx, tagRefArgument),
            ComTagAntecedents(ctx, tagRefArgument),
            ComTagImply(ctx, tagRefArgument),
            ComTagUnimply(ctx, tagRefArgument)
        };

        com.SetHandler((tagRef) => {
            var tagID = (TagID)ObjectRef.ResolveTag(ctx.archive!, tagRef!)!;
            var tag = ctx.archive?.TagGet(tagID);
            var taxonym = ctx.archive?.TaxonymGet((TaxonymID)tag?.taxonymID!);
            var parentTaxonym = ctx.archive?.TaxonymGet((TaxonymID)taxonym?.canonParentID!);

            ConsoleExt.WriteLineColored($"tag /{tagID}", ConsoleColor.Yellow);

            if(parentTaxonym is not null)
                Console.WriteLine($"  Taxonym: {parentTaxonym?.canonAlias}:{taxonym?.canonAlias} (/{taxonym?.id})");
            else
                Console.WriteLine($"  Taxonym: {taxonym?.canonAlias} (/{taxonym?.id})");

            Console.WriteLine($"  Document count: {ctx.archive.db.CountTagDocuments(tagID)}");

        }, tagRefArgument);

        return com;

    }

    public static Command ComTagImplications(CLIContext ctx, Argument<string?> tagRefArgument){

        var com = new Command("implications", "List direct implications of this tag.");

        com.SetHandler((tagRef) => {

            var tagID = (TagID)ObjectRef.ResolveTag(ctx.archive!, tagRef!)!;

            var consequentIDs = ctx.archive.TagGetImplications(tagID);

            foreach(var consequentID in consequentIDs){
                var tag = ctx.archive?.TagGet(consequentID);
                var taxonym = ctx.archive?.TaxonymGet((TaxonymID)tag?.taxonymID!);
                var parentTaxonym = ctx.archive?.TaxonymGet((TaxonymID)taxonym?.canonParentID!);

                if(parentTaxonym is not null)
                    Console.WriteLine($" * {parentTaxonym?.canonAlias}:{taxonym?.canonAlias} (/{taxonym?.id})");
                else
                    Console.WriteLine($" * {taxonym?.canonAlias} (/{taxonym?.id})");
            }

        }, tagRefArgument);

        return com;
    }

    public static Command ComTagAntecedents(CLIContext ctx, Argument<string?> tagRefArgument){

        var com = new Command("antecedents", "List direct antecedents of this tag.");

        com.SetHandler((tagRef) => {

            var tagID = (TagID)ObjectRef.ResolveTag(ctx.archive!, tagRef!)!;

            var antecedentIDs = ctx.archive.TagGetAntecedents(tagID);

            foreach(var antecedentID in antecedentIDs){
                var tag = ctx.archive?.TagGet(antecedentID);
                var taxonym = ctx.archive?.TaxonymGet((TaxonymID)tag?.taxonymID!);
                var parentTaxonym = ctx.archive?.TaxonymGet((TaxonymID)taxonym?.canonParentID!);

                if(parentTaxonym is not null)
                    Console.WriteLine($" * {parentTaxonym?.canonAlias}:{taxonym?.canonAlias} (/{taxonym?.id})");
                else
                    Console.WriteLine($" * {taxonym?.canonAlias} (/{taxonym?.id})");
            }

        }, tagRefArgument);

        return com;
    }

    public static Command ComTagImply(CLIContext ctx, Argument<string?> tagRefArgument){

        var conseqRefArgument = new Argument<string?>(
            name: "implication"
        );

        var com = new Command("imply", "Denote tag as implication of this tag."){
            conseqRefArgument
        };

        com.SetHandler((tagRef, conseqRef) => {

            var tagID = (TagID)ObjectRef.ResolveTag(ctx.archive!, tagRef!)!;
            var conseqID = (TagID)ObjectRef.ResolveTag(ctx.archive!, conseqRef!)!;

            ctx.archive.TagAddImplication(tagID, conseqID);

        }, tagRefArgument, conseqRefArgument);

        return com;

    }

    public static Command ComTagUnimply(CLIContext ctx, Argument<string?> tagRefArgument){

        var conseqRefArgument = new Argument<string?>(
            name: "implication"
        );

        var com = new Command("unimply", "Remove tag as implication of this tag."){
            conseqRefArgument
        };

        com.SetHandler((tagRef, conseqRef) => {

            var tagID = (TagID)ObjectRef.ResolveTag(ctx.archive!, tagRef!)!;
            var conseqID = (TagID)ObjectRef.ResolveTag(ctx.archive!, conseqRef!)!;

            ctx.archive.TagRemoveImplication(tagID, conseqID);

        }, tagRefArgument, conseqRefArgument);

        return com;

    }

}