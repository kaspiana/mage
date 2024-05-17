using System.Data;
using Microsoft.Data.Sqlite;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Mage.IO;
using System.Text;

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
            $"doc=",
            $"tag=",
            $"taxonym=@1",
            $"seq=",
            $"view={DEFAULT_VIEW_NAME}"
        ]);

        var archive = Load(mageDir, fileDir);
        
        // setup db
        archive.ConnectDB();
        var setupCommand = archive.db.CreateCommand();
		setupCommand.CommandText = ResourceLoader.Load("Resources.DB.setup.sqlite.sql");
		setupCommand.ExecuteNonQuery();

        
        // setup views
        archive.ViewCreate("in");
        archive.ViewCreate(DEFAULT_VIEW_NAME);

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

    public string HashFile(string filePath){
        string? hash = null;

        using (FileStream fs = new FileStream(filePath, FileMode.Open))
        using (BufferedStream bs = new BufferedStream(fs))
        {
            using (var sha1 = SHA1.Create())
            {
                byte[] _hash = sha1.ComputeHash(bs);
                StringBuilder formatted = new StringBuilder(2 * _hash.Length);
                foreach (byte b in _hash)
                {
                    formatted.AppendFormat("{0:X2}", b);
                }

                hash = formatted.ToString();
            }
        }

        return hash;
    }

    public void Ingest(){
        var inboxFiles = Directory.GetFiles($"{mageDir}{IN_DIR_PATH}");

        foreach(var filePath in inboxFiles){
            IngestFile(filePath);
            File.Delete(filePath);
        }
    }

    public DocumentID IngestFile(string filePath, string? comment = null){
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath)[1..];
        var hash = HashFile(filePath);
        var ingestTimestamp = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();

        File.Copy(filePath, $"{fileDir}{hash}");

        ConnectDB();

        var com = db.CreateCommand();
		com.CommandText = $@"
            insert into Document (
                Hash,
                FileName,
                Extension,
                IngestTimestamp,
                Comment
            )
            values (
                @Hash,
                @FileName,
                @Extension,
                @IngestTimestamp,
                @Comment
            )
        ";
        com.Parameters.AddWithValue("@Hash", hash);
        com.Parameters.AddWithValue("@FileName", fileName);
        com.Parameters.AddWithValue("@Extension", extension);
        com.Parameters.AddWithValue("@IngestTimestamp", ingestTimestamp);
        com.Parameters.AddWithValue("@Comment", comment is null ? System.DBNull.Value : comment);

        com.ExecuteNonQuery();
        com.Dispose();

        com = new SqliteCommand("select last_insert_rowid()", db);
		long lastRowID = (long)com.ExecuteScalar()!;
		com.Dispose();

        var documentID = (DocumentID)lastRowID;

        ViewAdd("in", documentID);

        return documentID;
    }

    public void DocumentDelete(DocumentID documentID){

        var documentHash = GetDocumentHash(documentID);

        File.Delete($"{fileDir}{documentHash}");

        var views = ViewGetAll();
        foreach(var viewName in views){
            var viewDir = new DirectoryInfo($"{mageDir}{VIEWS_DIR_PATH}{viewName}/");
            foreach(var file in viewDir.EnumerateFiles($"*~{documentHash}.*")){
                file.Delete();
            }
        }
        
        ConnectDB();
        var com = db.CreateCommand();
		com.CommandText = $@"
            delete from Document
            where ID = @ID;
        ";
        com.Parameters.AddWithValue("@ID", documentID);
        com.ExecuteNonQuery();
        com.Dispose();

    }

    public void DocumentDeleteAll(){

        foreach(var filePath in Directory.GetFiles($"{fileDir}")){
            File.Delete(filePath);
        }

        ConnectDB();
        var com = db.CreateCommand();
		com.CommandText = $@"
            delete from Document;
        ";
        com.ExecuteNonQuery();
        com.Dispose();

        var views = ViewGetAll();
        foreach(var view in views){
            ViewDelete(view);
        }

    }

    public (int, string) ParseViewFileName(string fileName){
        var tildeIndex = fileName.IndexOf('~');
        var index = int.Parse(fileName[0..tildeIndex]);
        var hash = fileName[(tildeIndex+1)..];
        return (index, hash);
    }

    public void ViewCreate(string viewName){
        Directory.CreateDirectory($"{mageDir}{VIEWS_DIR_PATH}{viewName}/");
    }

    public string? ViewGenerateNumberedName(string prefix){
        var viewsDir = $"{mageDir}{VIEWS_DIR_PATH}";
        var viewDirsFull = Directory.GetDirectories(viewsDir);
        var viewDirs = viewDirsFull.Select((p) => Path.GetFileName(p));
        viewDirs = viewDirs.Where((n) => n.StartsWith(prefix));
        viewDirs = viewDirs.Where((n) => n[prefix.Count()] != '_');

        var indices = viewDirs.Select((n) => int.Parse(n.Skip(prefix.Count()).ToArray()));
        
        var newIndex = -1;
        if(indices.Count() == 0){
            newIndex = 0;
        } else {
            newIndex = indices.Max() + 1;
        }

        return $"{prefix}{newIndex}";
    }

    public string? ViewUserCreate(string? name = null){
        string? viewName = null;

        if(name is not null){
            viewName = $"user_{name}";
        } else {
            viewName = ViewGenerateNumberedName("user");
        }

        ViewCreate(viewName);
        return viewName;
    }

    public void ViewDelete(string viewName){
        ViewClear(viewName);
        Directory.Delete($"{mageDir}{VIEWS_DIR_PATH}{viewName}/");
    }

    public string[] ViewGetAll(){
        var viewsDir = $"{mageDir}{VIEWS_DIR_PATH}";
        var viewDirsFull = Directory.GetDirectories(viewsDir);
        var viewDirs = viewDirsFull.Select((p) => Path.GetFileName(p));
        return viewDirs.ToArray();
    }

    public View? ViewGet(string viewName){

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

    public void ViewReflect(string targetViewName, string sourceViewName){
        var sourceView = (View)ViewGet(sourceViewName)!;

        var viewDir = $"{mageDir}{VIEWS_DIR_PATH}{targetViewName}/";
        var filePaths = Directory.GetFiles(viewDir);
        var newIndex = filePaths.Count();

        foreach(var documentID in sourceView.documents){
            if(documentID is not null){
                var document = (Document)GetDocument((DocumentID)documentID)!;

                FileExt.CreateHardLink(
                    $"{viewDir}{newIndex}~{document.hash}.{document.extension}",
                    $"{fileDir}{document.hash}",
                    IntPtr.Zero
                );

                newIndex++;
            }
        }
    }

    public string? ViewStash(string viewName){
        var stashViewName = ViewGenerateNumberedName("stash");
        ViewCreate(stashViewName);
        ViewReflect(stashViewName, viewName);
        ViewClear(viewName);

        return stashViewName;
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
            ingestTimestamp.AddSeconds(reader.GetInt32(4)).ToLocalTime();

            return new Document(){
                hash = reader.GetString(1),
                id = documentID,
                fileName = reader.GetString(2),
                extension = reader.GetString(3),
                ingestTimestamp = ingestTimestamp,
                comment = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }

        return null;
    }

}