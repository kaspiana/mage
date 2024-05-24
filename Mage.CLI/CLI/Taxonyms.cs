using System.CommandLine;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Mage.CLI;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    private const char BOX_DRAWING_LR = '─';
    private const char BOX_DRAWING_TB = '│';
    private const char BOX_DRAWING_TBR = '├';
    private const char BOX_DRAWING_BR = '┌';
    private const char BOX_DRAWING_TR = '└';

    private static void PrintTaxonymTree(CLIContext ctx, TaxonymID taxonymID, Stack<bool> stack){
        var taxonym = ctx.archive.TaxonymGet(taxonymID);

        var i = 0;
        foreach(var level in stack.Reverse()){
            if(i == stack.Count() - 1){
                if(!stack.First()){
                    Console.Write(BOX_DRAWING_TR);
                } else {
                    Console.Write(BOX_DRAWING_TBR);
                }
            } else {
                if(level){
                    Console.Write(BOX_DRAWING_TB);
                } else {
                    Console.Write(' ');
                }
            }
            if(i < stack.Count() - 1){
                Console.Write(' ');
                Console.Write(' ');
            }
            i++;
        }

        if(taxonymID != Archive.ROOT_TAXONYM_ID){
            Console.Write($"{BOX_DRAWING_LR} ");

            ConsoleExt.WriteColored($"{taxonym?.canonAlias}", ConsoleColor.Yellow);

            Console.Write($" (/{taxonymID}) ");
            
            ctx.archive.db.EnsureConnected();
            var tagID = ctx.archive.db.ReadTagID(taxonymID);
            if(tagID is not 0) // FIX
                Console.Write($"(tag /{tagID})");

            Console.WriteLine();
        }

        var children = ctx.archive.TaxonymGetChildren(taxonymID);
        var j = 0;
        foreach(var child in children){
            stack.Push(j != children.Count() - 1);
            PrintTaxonymTree(ctx, child, stack);
            stack.Pop();
            j++;
        }
    }

    public static Command ComTaxonyms(CLIContext ctx){
        var com = new Command("taxonyms", "List all taxonyms.");

        com.SetHandler(() => {
            var taxonymID = Archive.ROOT_TAXONYM_ID;
            PrintTaxonymTree(ctx, taxonymID, new Stack<bool>());
        });

        return com;
    }

}