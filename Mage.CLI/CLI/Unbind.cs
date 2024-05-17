using System.CommandLine;
using System.Security.Cryptography.X509Certificates;
using Mage.Engine;
using SQLitePCL;

public static partial class CLICommands {

    public static Command ComUnbind(CLIContext ctx){
        // mage unbind
        var com = new Command("unbind", "Unbind a bound value."){
            ComUnbindDoc(ctx),
            ComUnbindView(ctx)
        };
        return com;
    }

    public static Command ComUnbindDoc(CLIContext ctx){
        // mage unbind doc
        var com = new Command("doc", "Unbind the bound document.");
        
        com.SetHandler(() => {
            ctx.archive.BindDocument(null);
        });
       
        return com;
    }

    public static Command ComUnbindView(CLIContext ctx){
        // mage unbind view
        var com = new Command("view", "Unbind the bound view.");

        com.SetHandler(() => {
            ctx.archive.BindView(null);
        });
        
        return com;
    }

}