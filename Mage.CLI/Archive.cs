using System.Data;
using Microsoft.Data.Sqlite;

namespace Mage.Engine;

public struct Archive {

    public const string INFO_FILE_PATH = "info";
    public const string DB_FILE_PATH = "db.sqlite";

    public string mageDir;
    public string fileDir;

    public string? name;

    public SqliteConnection? db;

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