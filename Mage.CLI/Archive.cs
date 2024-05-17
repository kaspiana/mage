using System.Data;
using Microsoft.Data.Sqlite;
using System.Resources;

namespace Mage.Engine;

public struct Archive {

    public const string MAGE_DIR_PATH = ".mage/";
    public const string IN_DIR_PATH = "in/";
    public const string OUT_DIR_PATH = "out/";
    public const string VIEWS_DIR_PATH = "views/";
    public const string INFO_FILE_PATH = "info";
    public const string BIND_FILE_PATH = "bind";
    public const string DB_FILE_PATH = "db.sqlite";

    public const int CURRENT_VERSION = 1;
    public const string DEFAULT_VIEW_NAME = "main";

    public static readonly string[] BIND_KEYS = [
        "."
    ];

    public string mageDir;
    public string fileDir;

    public string? name;
    public int version;

    public SqliteConnection? db;

    public static Archive Init(string archiveDir, string? name = null){
        var fileDir = archiveDir;
        var mageDir = $"{archiveDir}{MAGE_DIR_PATH}";

        // setup directory structure
        Directory.CreateDirectory($"{mageDir}");
        Directory.CreateDirectory($"{mageDir}{IN_DIR_PATH}");
        Directory.CreateDirectory($"{mageDir}{OUT_DIR_PATH}");
        Directory.CreateDirectory($"{mageDir}{VIEWS_DIR_PATH}");
        
        // write info file
        var infoMap = new Dictionary<string, string>();

        if(name is not null)
            infoMap["name"] = name;
        infoMap["version"] = CURRENT_VERSION.ToString();

        var infoLines = new List<string>();
        foreach(var kv in infoMap){
            infoLines.Add($"{kv.Key}={kv.Value}");
        }
        File.WriteAllLines($"{mageDir}{INFO_FILE_PATH}", infoLines);

        // create bind file
        File.WriteAllLines($"{mageDir}{BIND_FILE_PATH}", [
            "doc=",
            "tag=",
            "taxonym=@0",
            "seq=",
            "view=main"
        ]);

        var archive = Load(mageDir, fileDir);
        
        archive.ConnectDB();
        var setupCommand = archive.db.CreateCommand();
		setupCommand.CommandText = ResourceLoader.Load("Resources.DB.setup.sqlite.sql");
		setupCommand.ExecuteNonQuery();

        return archive;
    }

    public static Archive Load(string mageDir, string fileDir){

        var infoMap = new Dictionary<string, string>();
        foreach(var line in File.ReadAllLines($"{mageDir}{INFO_FILE_PATH}")){
            var splitIndex = line.IndexOf('=');
            var infoKey = line[..splitIndex];
            var infoValue = line[(splitIndex+1)..];
            infoMap[infoKey] = infoValue;
        }

        string? name = infoMap.ContainsKey("name") ? infoMap["name"] : null;
        int version = int.Parse(infoMap["version"]);

        var archive = new Archive(){
            mageDir = mageDir,
            fileDir = fileDir,
            name = name,
            version = version,
            db = null
        };

        return archive;

    }

    public void ConnectDB(){
        if(db is null){
            db = new SqliteConnection($"DataSource={mageDir}{DB_FILE_PATH}");
            db.Open();
        }
    }

    public void DiscnnectDB(){
        if(db is not null){
            db.Close();
            db.Dispose();
            db = null;
        }
    }

}