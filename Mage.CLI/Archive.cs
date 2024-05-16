using System.Data;
using Microsoft.Data.Sqlite;

namespace Mage.Engine;

public struct Archive {

    public string mageDir;
    public string fileDir;

    public string? name;

    public SqliteConnection? db;

    public static Archive Load(string mageDir, string fileDir){

        var infoMap = new Dictionary<string, string>();
        foreach(var line in File.ReadAllLines(mageDir + "info")){
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

}