using System.CommandLine;
using System.Diagnostics;
using System.Text;
using Mage.CLI;
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
        ConsoleExt.WriteColored("NOTE: ", ConsoleColor.Yellow);
        Console.WriteLine("No archive exists in this directory. (Ignore if using mage init.)");
    }
} catch(Archive.IncompatibleArchiveException ex){
    ConsoleExt.WriteColored("ERROR: ", ConsoleColor.Red);
    Console.WriteLine("Incompatible archive version. Attempting to migrate...");

    var success = Migration.Migrate(ctx.archiveDir);
    
    if(success){
        ctx.archive = Archive.Load(ctx.archiveDir);
    } else {
        return;
    }
}

var rootCommand = CLICommands.CreateRoot(ctx);
rootCommand.Invoke(args);

if(ctx.archive is not null)
    ctx.archive.Unload();

return;