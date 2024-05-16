using System.Data;
using Microsoft.Data.Sqlite;

namespace Mage.Engine;

public struct Archive {

    public const string MAGE_DIR_PATH = ".mage/";
    public const string IN_DIR_PATH = "in/";
    public const string OUT_DIR_PATH = "out/";
    public const string VIEWS_DIR_PATH = "views/";
    public const string INFO_FILE_PATH = "info";
    public const string BIND_FILE_PATH = "bind";
    public const string DB_FILE_PATH = "db.sqlite";

    public const string DEFAULT_VIEW_NAME = "main";

    public string mageDir;
    public string fileDir;

    public string? name;

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
        if(name is not null) infoMap["name"] = name;
        var infoLines = new List<string>();
        foreach(var kv in infoMap){
            infoLines.Add($"{kv.Key}={kv.Value}");
        }
        File.WriteAllLines($"{mageDir}{INFO_FILE_PATH}", infoLines);

        // create bind file
        File.Create($"{mageDir}{BIND_FILE_PATH}");

        var archive = Load(mageDir, fileDir);
        archive.ConnectDB();
        // TODO: Run setup script.

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

        var archive = new Archive(){
            mageDir = mageDir,
            fileDir = fileDir,
            name = name,
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