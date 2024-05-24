using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.CLI;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComTaxonym(CLIContext ctx){

        var taxonymRefArgument = new Argument<string>(
            name: "taxonym"
        );

        var com = new Command("taxonym", "Manipulate taxonym"){
            taxonymRefArgument,
            ComTaxonymAliases(ctx, taxonymRefArgument),
            ComTaxonymAlias(ctx, taxonymRefArgument),
            ComTaxonymUnalias(ctx, taxonymRefArgument),
            ComTaxonymChildren(ctx, taxonymRefArgument),
            ComTaxonymParents(ctx, taxonymRefArgument),
            ComTaxonymChild(ctx, taxonymRefArgument),
            ComTaxonymUnchild(ctx, taxonymRefArgument),
            ComTaxonymParent(ctx, taxonymRefArgument),
            ComTaxonymUnparent(ctx, taxonymRefArgument)
        };

        com.SetHandler((taxonymRef) => {
            var taxonymID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, taxonymRef)!;
            var taxonym = ctx.archive.TaxonymGet(taxonymID);

            if(taxonymID == Archive.ROOT_TAXONYM_ID){
                ConsoleExt.WriteColored($"taxonym /{taxonymID}: ", ConsoleColor.Yellow);
                Console.WriteLine("<root>");
            } else {
                ConsoleExt.WriteLineColored($"taxonym /{taxonymID}", ConsoleColor.Yellow);

                Console.WriteLine($"\tParents:");
                var parentIDs = ctx.archive.TaxonymGetParents(taxonymID);
                foreach(var parentID in parentIDs){
                    var parent = ctx.archive.TaxonymGet(parentID);
                    if(parentID == taxonym?.canonParentID){
                        Console.WriteLine($"   * {parent?.canonAlias} (/{parentID})");
                    } else {
                        Console.WriteLine($"     {parent?.canonAlias} (/{parentID})");
                    }
                }

                Console.WriteLine($"  Aliases:");
                var aliases = ctx.archive.TaxonymGetAliases(taxonymID);
                foreach(var alias in aliases){
                    if(alias == taxonym?.canonAlias){
                        Console.WriteLine($"   * {alias}");
                    } else {
                        Console.WriteLine($"     {alias}");
                    }
                }
            }
        }, taxonymRefArgument);

        return com;
    }

    public static Command ComTaxonymAliases(CLIContext ctx, Argument<string> taxonymRefArgument){

        var com = new Command("aliases", "List the taxonym's aliases.");

        com.SetHandler((taxonymRef) => {
            var taxonymID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, taxonymRef)!;
            var taxonym = ctx.archive.TaxonymGet(taxonymID);
            var aliases = ctx.archive.TaxonymGetAliases(taxonymID);

            foreach(var alias in aliases){
                if(alias == taxonym?.canonAlias){
                    Console.WriteLine($" * {alias}");
                } else {
                    Console.WriteLine($"   {alias}");
                }
            }
        }, taxonymRefArgument);

        return com;
    }

    public static Command ComTaxonymAlias(CLIContext ctx, Argument<string> taxonymRefArgument){

        var aliasArgument = new Argument<string[]>(
            name: "alias"
        ){ Arity = ArgumentArity.OneOrMore };

        var com = new Command("alias", "Add an alias to the taxonym."){
            aliasArgument
        };

        com.SetHandler((taxonymRef, aliases) => {
            var taxonymID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, taxonymRef)!;
            foreach(var alias in aliases){
                ctx.archive.TaxonymAddAlias(taxonymID, alias);
            }
        }, taxonymRefArgument, aliasArgument);

        return com;
    }

    public static Command ComTaxonymUnalias(CLIContext ctx, Argument<string> taxonymRefArgument){

        var aliasArgument = new Argument<string[]>(
            name: "alias"
        ){ Arity = ArgumentArity.OneOrMore };

        var com = new Command("unalias", "Remove an alias from the taxonym."){
            aliasArgument
        };

        com.SetHandler((taxonymRef, aliases) => {
            var taxonymID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, taxonymRef)!;
            foreach(var alias in aliases){
                ctx.archive.TaxonymRemoveAlias(taxonymID, alias);
            }
        }, taxonymRefArgument, aliasArgument);

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
                    Console.WriteLine($" * /{taxonym?.id}: <root>");
                } else {
                    Console.WriteLine($" * /{taxonym?.id}: /{taxonym?.canonParentID}:{taxonym?.canonAlias}");
                }
            }


        }, taxonymRefArgument);

        return com;
    }

    public static Command ComTaxonymParents(CLIContext ctx, Argument<string> taxonymRefArgument){

        var com = new Command("parents", "List the taxonym's parents.");

        com.SetHandler((taxonymRef) => {
            var taxonymID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, taxonymRef)!;
            var parentIDs = ctx.archive.TaxonymGetParents(taxonymID);
            var parents = parentIDs.Select((id) => ctx.archive.TaxonymGet(id));

            foreach(var taxonym in parents){
                if(taxonym?.id == Archive.ROOT_TAXONYM_ID){
                    Console.WriteLine($" * /{taxonym?.id}: <root>");
                } else {
                    Console.WriteLine($" * /{taxonym?.id}: /{taxonym?.canonParentID}:{taxonym?.canonAlias}");
                }
            }


        }, taxonymRefArgument);

        return com;
    }

    public static Command ComTaxonymChild(CLIContext ctx, Argument<string> taxonymRefArgument){

        var childRefArgument = new Argument<string[]>(
            name: "child"
        ){ Arity = ArgumentArity.OneOrMore };

        var com = new Command("child", "Add a child to the taxonym."){
            childRefArgument
        };

        com.SetHandler((taxonymRef, childRefs) => {
            var taxonymID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, taxonymRef)!;
            foreach(var childRef in childRefs){
                var childID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, childRef)!;
                ctx.archive.TaxonymAddChild(taxonymID, childID);   
            }
        }, taxonymRefArgument, childRefArgument);

        return com;

    }

    public static Command ComTaxonymUnchild(CLIContext ctx, Argument<string> taxonymRefArgument){

        var childRefArgument = new Argument<string[]>(
            name: "child"
        ){ Arity = ArgumentArity.OneOrMore };

        var com = new Command("unchild", "Remove a child from the taxonym."){
            childRefArgument
        };

        com.SetHandler((taxonymRef, childRefs) => {
            var taxonymID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, taxonymRef)!;
            foreach(var childRef in childRefs){
                var childID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, childRef)!;
                ctx.archive.TaxonymRemoveChild(taxonymID, childID);   
            }
        }, taxonymRefArgument, childRefArgument);

        return com;

    }

    public static Command ComTaxonymParent(CLIContext ctx, Argument<string> taxonymRefArgument){

        var parentRefArgument = new Argument<string[]>(
            name: "parent"
        ){ Arity = ArgumentArity.OneOrMore };

        var com = new Command("parent", "Add a parent to the taxonym."){
            parentRefArgument
        };

        com.SetHandler((taxonymRef, parentRefs) => {
            var taxonymID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, taxonymRef)!;
            foreach(var parentRef in parentRefs){
                var parentID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, parentRef)!;
                ctx.archive.TaxonymAddParent(taxonymID, parentID);   
            }
        }, taxonymRefArgument, parentRefArgument);

        return com;

    }

    public static Command ComTaxonymUnparent(CLIContext ctx, Argument<string> taxonymRefArgument){

        var parentRefArgument = new Argument<string[]>(
            name: "parent"
        ){ Arity = ArgumentArity.OneOrMore };

        var com = new Command("unparent", "Remove a parent from the taxonym."){
            parentRefArgument
        };

        com.SetHandler((taxonymRef, parentRefs) => {
            var taxonymID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, taxonymRef)!;
            foreach(var parentRef in parentRefs){
                var parentID = (TaxonymID)ObjectRef.ResolveTaxonym(ctx.archive, parentRef)!;
                ctx.archive.TaxonymRemoveParent(taxonymID, parentID);   
            }
        }, taxonymRefArgument, parentRefArgument);

        return com;

    }

}