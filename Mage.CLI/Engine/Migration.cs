using Mage.CLI;

namespace Mage.Engine;

public class Migration {

    /// <summary>Please order by version.</summary>
    public static List<(SemanticVersion version, Func<string, bool> migrate)> MigrationScripts = [
        
        (SemanticVersion.FromString("alpha_12.0.0"), archiveDir => {
            var db = new DBEngine(){ dbPath = $"{archiveDir}{Archive.DB_FILE_PATH}" };
            db.EnsureConnected();

            db.RunScript(@"
                create table ranking_category (
                    name            text not null primary key
                );

                create table document_ranking (
                    document_id     integer not null,
                    category        text not null,
                    score           integer not null default 0,

                    foreign key (document_id) references document(id),
                    foreign key (category) references ranking_category(name),
                    primary key (document_id, category)
                );
            ");

            return true;
        })

    ];

    public static bool Migrate(string archiveDir){

        var infoMap = Archive.ReadInfoFile(archiveDir);

        string versionStr = infoMap["version"];

        var archiveVersion = SemanticVersion.FromString(versionStr).Normalise();
        var buildVersion = Archive.VERSION.Normalise();

        if(archiveVersion > buildVersion){
            ConsoleExt.WriteColored("ERROR: ", ConsoleColor.Red);
                Console.WriteLine($"Archive was made in a newer version: {archiveVersion}");
            return false;
        }

        var failure = false;

        for(int i = 0; i < MigrationScripts.Count(); i++){
            var nextVersion = MigrationScripts[i].version;

            // versions predating the archive
            if(nextVersion <= archiveVersion)
                continue;

            // versions predating the build (edge case)
            if(nextVersion > buildVersion)
                break;
            
            var success = MigrationScripts[i].migrate(archiveDir);

            if(success){
                ConsoleExt.WriteColored("SUCCESS: ", ConsoleColor.Green);
                Console.WriteLine($"Migrated from {archiveVersion} to {nextVersion}.");
                archiveVersion = nextVersion;

                infoMap["version"] = archiveVersion.ToString();
                Archive.WriteInfoFile(archiveDir, infoMap);
            } else {
                ConsoleExt.WriteColored("ERROR: ", ConsoleColor.Red);
                Console.WriteLine($"Unable to migrate from {archiveVersion} to {nextVersion}. (Script failed.)");
                failure = true;
            }
        }

        if(!failure && archiveVersion.Normalise() < buildVersion.Normalise()){
            ConsoleExt.WriteColored("ERROR: ", ConsoleColor.Red);
            Console.WriteLine($"Unable to migrate from {archiveVersion} to {buildVersion}. (No script.)");
        }

        if(archiveVersion == buildVersion){
            infoMap["version"] = Archive.VERSION.ToString();
            Archive.WriteInfoFile(archiveDir, infoMap);
        }        

        return archiveVersion == buildVersion;
    }

}