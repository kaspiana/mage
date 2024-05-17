using System.Data;
using Microsoft.Data.Sqlite;
using System.Resources;
using System.Runtime.InteropServices;
using Mage.IO;

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

    public static readonly string[] BINDING_KEYS = [
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

    public void Unload(){
        DiscnnectDB();
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

    public (int, string) ParseViewFileName(string fileName){
        var tildeIndex = fileName.IndexOf('~');
        var index = int.Parse(fileName[0..tildeIndex]);
        var hash = fileName[(tildeIndex+1)..];
        return (index, hash);
    }

    public View? GetView(string viewName){

        ViewType? viewType = null;
        if(viewName == "main") viewType = ViewType.Main;
        if(viewName == "in") viewType = ViewType.In;
        if(viewName.StartsWith("user")) viewType = ViewType.User;
        if(viewName.StartsWith("query")) viewType = ViewType.Query;
        if(viewName.StartsWith("stash")) viewType = ViewType.Stash;

        if(viewType is null)
            return null;

        var viewsDir = $"{mageDir}{VIEWS_DIR_PATH}";
        var viewDirsFull = Directory.GetDirectories(viewsDir);
        var documentIDs = new List<(int, DocumentID?)>();

        foreach(var viewDirFull in viewDirsFull){
            var viewDir = Path.GetFileName(viewDirFull);
            if(viewDir == viewName){

                var filePaths = Directory.GetFiles(viewDirFull);

                foreach(var filePath in filePaths){
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var (index, hash) = ParseViewFileName(fileName);
                    var docID = GetDocumentID(hash);
                    documentIDs.Add((index, docID));
                }

                break;
            }
        }

        documentIDs.Sort((x, y) => x.Item1.CompareTo(y.Item1));

        return new View(){
            name = viewName,
            viewType = (ViewType)viewType,
            documents = documentIDs.Select((t) => t.Item2).ToArray()
        };
    }

    public int ViewAdd(string viewName, DocumentID documentID){
        var viewDir = $"{mageDir}{VIEWS_DIR_PATH}{viewName}/";

        var document = (Document)GetDocument(documentID)!;

        var filePaths = Directory.GetFiles(viewDir);
        var newIndex = filePaths.Count();

        FileExt.CreateHardLink(
            $"{viewDir}{newIndex}~{document.hash}.{document.extension}",
            $"{fileDir}{document.hash}",
            IntPtr.Zero
        );

        return newIndex;
    }

    public void ViewClear(string viewName){
        var viewDir = $"{mageDir}{VIEWS_DIR_PATH}{viewName}/";
        
        foreach(var filePath in Directory.GetFiles(viewDir)){
            File.Delete(filePath);
        }
    }

    public string? GetDocumentHash(DocumentID documentID){
        ConnectDB();

        var com = db.CreateCommand();
		com.CommandText = $"select Hash from Document where ID = @ID";
        com.Parameters.AddWithValue("ID", documentID);
		
        var reader = com.ExecuteReader();
        if(reader.Read()){
            return reader.GetString(0);
        }

        return null;
    }

    public DocumentID? GetDocumentID(string documentHash){
        ConnectDB();

        var com = db.CreateCommand();
		com.CommandText = $"select ID from Document where Hash = @Hash";
        com.Parameters.AddWithValue("Hash", documentHash);
		
        var reader = com.ExecuteReader();
        if(reader.Read()){
            return (DocumentID)reader.GetInt32(0);
        }

        return null;
    }

    public Document? GetDocument(DocumentID documentID){
        ConnectDB();

        var com = db.CreateCommand();
		com.CommandText = $"select * from Document where ID = @ID";
        com.Parameters.AddWithValue("ID", documentID);
		
        var reader = com.ExecuteReader();
        if(reader.Read()){
            var ingestTimestamp = new DateTime();
            ingestTimestamp.AddSeconds(reader.GetInt32(3)).ToLocalTime();

            return new Document(){
                hash = reader.GetString(1),
                id = documentID,
                extension = reader.GetString(2),
                ingestTimestamp = ingestTimestamp,
                comment = reader.IsDBNull(4) ? null : reader.GetString(4)
            };
        }

        return null;
    }

}