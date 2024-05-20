using System.CommandLine;
using System.Diagnostics;
using Mage.Engine;

var archiveDir = "C:/home/catalogue-tool/v2/test-archives/a/";

var ctx = new CLIContext(){
    archiveDir = archiveDir,
    mageDir = $"{archiveDir}.mage/",
    archive = null
};

try {
    if(Directory.Exists(ctx.mageDir))
        ctx.archive = Archive.Load(ctx.mageDir, archiveDir);
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