using System.Data;
using Microsoft.Data.Sqlite;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Mage.IO;
using System.Text;
using System.Diagnostics;

namespace Mage.Engine;

public class Archive {

    public const string MAGE_DIR_PATH = ".mage/";
    public const string IN_DIR_PATH = "in/";
    public const string OUT_DIR_PATH = "out/";
    public const string VIEWS_DIR_PATH = "views/";
    public const string INFO_FILE_PATH = "info";
    public const string BIND_FILE_PATH = "bind";
    public const string DB_FILE_PATH = "db.sqlite";

    public const int CURRENT_VERSION = 6;
    public const string IN_VIEW_NAME = "in";
    public const string OPEN_VIEW_NAME = "open";
    public const string DEFAULT_VIEW_NAME = "main";
    public const TaxonymID ROOT_TAXONYM_ID = (TaxonymID)1;

    public string mageDir;
    public string fileDir;

    public string? name;
    public int version;

    public DBModel db;

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
            $"taxonym=/1",
            $"series=",
            $"view={DEFAULT_VIEW_NAME}"
        ]);

        var archive = Load(mageDir, fileDir);
        
        // setup db
        archive.db.EnsureConnected();
        archive.db.RunResourceScript("setup.sqlite.sql");
        
        // setup views
        archive.ViewCreate(IN_VIEW_NAME);
        archive.ViewCreate(OPEN_VIEW_NAME);
        archive.ViewCreate(DEFAULT_VIEW_NAME);

        return archive;
    }

    public void Unload(){
        db.Disconnect();
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
            db = new DBModel(){ dbPath = $"{mageDir}{DB_FILE_PATH}" }
        };

        return archive;

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

        File.Copy(filePath, $"{fileDir}{hash}");

        db.EnsureConnected();
        var documentID = db.InsertDocument(new Document(){
            hash = hash,
            fileName = fileName,
            extension = extension,
            ingestTimestamp = DateTime.Now,
            comment = comment
        });

        ViewAdd("in", documentID);

        return documentID;
    }

    public void DocumentDelete(DocumentID documentID){

        var doc = (Document)DocumentGet(documentID)!;

        File.Move($"{fileDir}{doc.hash}", $"{mageDir}{OUT_DIR_PATH}{doc.fileName}.{doc.extension}");

        var views = ViewsGetAll();
        foreach(var viewName in views){
            var viewDir = new DirectoryInfo($"{mageDir}{VIEWS_DIR_PATH}{viewName}/");
            foreach(var file in viewDir.EnumerateFiles($"*~{doc.hash}.*")){
                file.Delete();
            }
        }
        
        db.EnsureConnected();
        db.DeleteDocument(documentID);

    }

    public void DocumentsDeleteAll(){

        foreach(var filePath in Directory.GetFiles($"{fileDir}")){
            File.Delete(filePath);
        }

        db.EnsureConnected();
        db.DeleteAllDocuments();

    }

    public (int, string) ParseViewFileName(string fileName){
        var tildeIndex = fileName.IndexOf('~');
        var index = int.Parse(fileName[0..tildeIndex]);
        var hash = fileName[(tildeIndex+1)..];
        return (index, hash);
    }

    public Taxonym? TaxonymGet(TaxonymID taxonymID){
        db.EnsureConnected();
        return db.ReadTaxonym(taxonymID);
    }

    public TaxonymID? TaxonymFind(IEnumerable<string> qualifiedNameParts, TaxonymID? context = null){
        
        if(qualifiedNameParts.Count() == 0)
			return context is null ? (TaxonymID)1 : (TaxonymID)context;
		
		var target = qualifiedNameParts.First();

		List<TaxonymID> layerIDs = [];
		List<(TaxonymID taxonymID, string alias)> layerAliases = [];
		
		var seedID = context is null ? (TaxonymID)1 : (TaxonymID)context;
		var seedAlias = TaxonymGet(seedID)?.canonicalAlias;
		layerIDs.Add(seedID);
		layerAliases.Add((seedID, seedAlias));

		while(layerIDs.Count() > 0){
			foreach(var (taxonymID, alias) in layerAliases){
				if(alias == target){
					if(qualifiedNameParts.Count() == 1){
						return taxonymID;
					} else {
						return TaxonymFind(qualifiedNameParts.Skip(1), taxonymID);
					}
				}
			}

			var idCount = layerIDs.Count();
			var idList = String.Join(',', layerIDs.Take(idCount));
            for(int i = 0; i < idCount; i++){
                var id = layerIDs[i];
                layerIDs.AddRange(TaxonymGetChildren(id));
            }
			layerIDs.RemoveRange(0, idCount);
            
            layerAliases.Clear();
            foreach(var id in layerIDs){
                layerAliases.AddRange(TaxonymGetAliases(id).Select((alias) => (id, alias)));
            }
		}

        return null;
    }
    
    public TaxonymID? TaxonymFind(string qualifiedName, TaxonymID? context = null){
		return TaxonymFind(qualifiedName.Split(':'), context);
	}

    public TaxonymID[] TaxonymGetChildren(TaxonymID taxonymID){
        db.EnsureConnected();
        return db.ReadTaxonymChildren(taxonymID);
    }

    public TaxonymID[] TaxonymGetParents(TaxonymID taxonymID){
        db.EnsureConnected();
        return db.ReadTaxonymParents(taxonymID);
    }

    public TaxonymID[] TaxonymsQuery(string sqlClause){
        db.EnsureConnected();
        return db.QueryTaxonyms(sqlClause);
    }

    public TaxonymID? TaxonymCreate(TaxonymID parentID, string name){
        db.EnsureConnected();
        var taxonymID = db.InsertTaxonym(new Taxonym(){
            canonicalParentID = parentID,
            canonicalAlias = name
        });
        return taxonymID;
    }

    public void TaxonymDelete(TaxonymID taxonymID){
        db.EnsureConnected();
        db.DeleteTaxonym(taxonymID);
    }

    public string[] TaxonymGetAliases(TaxonymID taxonymID){
        db.EnsureConnected();
        return db.ReadTaxonymAliases(taxonymID);
    }

    public void TaxonymAddAlias(TaxonymID taxonymID, string alias){
        db.EnsureConnected();
        db.InsertTaxonymAlias(taxonymID, alias);
    }

    public void TaxonymRemoveAlias(TaxonymID taxonymID, string alias){
        db.EnsureConnected();
        db.DeleteTaxonymAlias(taxonymID, alias);
    }

    public void TaxonymAddChild(TaxonymID taxonymID, TaxonymID childID){
        db.EnsureConnected();
        db.InsertTaxonymParent(childID, taxonymID);
    }

    public void TaxonymRemoveChild(TaxonymID taxonymID, TaxonymID childID){
        db.EnsureConnected();
        db.DeleteTaxonymParent(childID, taxonymID);
    }

    public void TaxonymAddParent(TaxonymID taxonymID, TaxonymID parentID){
        db.EnsureConnected();
        db.InsertTaxonymParent(taxonymID, parentID);
    }

    public void TaxonymRemoveParent(TaxonymID taxonymID, TaxonymID parentID){
        db.EnsureConnected();
        db.DeleteTaxonymParent(taxonymID, parentID);
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

    public string? ViewQueryCreate(){
        var viewName = ViewGenerateNumberedName("query");
        ViewCreate(viewName);
        return viewName;
    }

    public void ViewDelete(string viewName){
        ViewClear(viewName);
        Directory.Delete($"{mageDir}{VIEWS_DIR_PATH}{viewName}/");
    }

    public string[] ViewsGetAll(){
        var viewsDir = $"{mageDir}{VIEWS_DIR_PATH}";
        var viewDirsFull = Directory.GetDirectories(viewsDir);
        var viewDirs = viewDirsFull.Select((p) => Path.GetFileName(p));
        return viewDirs.ToArray();
    }

    public View? ViewGet(string viewName){

        ViewType? viewType = null;
        if(viewName == DEFAULT_VIEW_NAME) viewType = ViewType.Main;
        if(viewName == IN_VIEW_NAME) viewType = ViewType.In;
        if(viewName == OPEN_VIEW_NAME) viewType = ViewType.Open;
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

        var document = (Document)DocumentGet(documentID)!;

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
                var document = (Document)DocumentGet((DocumentID)documentID)!;

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

    public string BindingGet(ObjectType objType){
        var lines = File.ReadAllLines($"{mageDir}{BIND_FILE_PATH}");

        var kw = "";
        switch(objType){
            case ObjectType.Document: kw = "doc"; break;
            case ObjectType.Tag: kw = "tag"; break;
            case ObjectType.Taxonym: kw = "taxonym"; break;
            case ObjectType.Series: kw = "series"; break;
            case ObjectType.View: kw = "view"; break;
        }

        foreach(var line in lines){
            if(line.StartsWith($"{kw}=")){
                return line[(kw.Count()+1)..];
            }
        }

        throw new UnreachableException();
    }

    public void BindingSet(ObjectType objType, string val){
        var lines = File.ReadAllLines($"{mageDir}{BIND_FILE_PATH}");

        for(int i = 0; i < lines.Count(); i++){
            if((ObjectType)i == objType){
                var line = lines[i];
                var equalIndex = line.IndexOf('=');
                var key = line[0..equalIndex];
                lines[i] = $"{key}={val}";
            }
        }

        File.WriteAllLines($"{mageDir}{BIND_FILE_PATH}", lines);
    }

    public void BindDocument(DocumentID? documentID){
        if(documentID is null) BindingSet(ObjectType.Document, "");
        else BindingSet(ObjectType.Document, $"/{documentID}");
    }

    public void BindView(string? viewName){
        if(viewName is null) BindingSet(ObjectType.View, DEFAULT_VIEW_NAME);
        else BindingSet(ObjectType.View, viewName);
    }

    public string? GetDocumentHash(DocumentID documentID){
        db.EnsureConnected();
        return db.ReadDocumentHash(documentID);
    }

    public DocumentID? GetDocumentID(string documentHash){
        db.EnsureConnected();
        return db.ReadDocumentID(documentHash);
    }

    public DocumentID[] DocumentsQuery(string queryString){
        db.EnsureConnected();
        return db.QueryDocuments(queryString);
    }

    public Document? DocumentGet(DocumentID documentID){
        db.EnsureConnected();
        return db.ReadDocument(documentID);
    }

}