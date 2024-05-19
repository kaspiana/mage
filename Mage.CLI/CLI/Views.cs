using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComViews(CLIContext ctx){
        // mage views
        var com = new Command("views", "List all views.");
        com.SetHandler(() => {
            var views = ctx.archive.ViewsGetAll();
            foreach(var view in views){
                Console.WriteLine($" * {view}");
            }
        });
        
        return com;
    }

}