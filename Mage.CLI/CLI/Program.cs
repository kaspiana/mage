using System.CommandLine;
using System.Diagnostics;
using System.Text;
using Mage.Engine;

Console.OutputEncoding = Encoding.UTF8;

var _archiveDir = Directory.GetCurrentDirectory().Replace('\\', '/') + "/";

var ctx = new CLIContext(){
    archiveDir = _archiveDir,
    archive = null
};

try {
    if(Archive.Exists(ctx.archiveDir))
        ctx.archive = Archive.Load(ctx.archiveDir);
    else {
        var ogFGColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("NOTE: ");
        Console.ForegroundColor = ogFGColor;
        Console.WriteLine("No archive exists in this directory. (Ignore if using mage init.)");
    }
} catch(Archive.IncompatibleArchiveException ex){
    var ogFGColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write("ERROR: ");
    Console.ForegroundColor = ogFGColor;
    Console.WriteLine(ex);
    return;
}

var rootCommand = CLICommands.CreateRoot(ctx);
rootCommand.Invoke(args);

if(ctx.archive is not null)
    ctx.archive.Unload();

return;