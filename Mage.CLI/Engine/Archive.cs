using System.Data;
using Microsoft.Data.Sqlite;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Mage.IO;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Mage.CLI;

namespace Mage.Engine;


public class Archive {

    public static readonly SemanticVersion VERSION = new SemanticVersion(){
        releaseType = -1,
        major = 15,
        minor = 6,
        patch = 0
    };

    public const string IN_DIR_PATH = "in/";
    public const string OUT_DIR_PATH = "out/";
    public const string FILES_DIR_PATH = "files/";
    public const string VIEWS_DIR_PATH = "views/";
    public const string DATA_DIR_PATH = "data/";
    public const string INFO_FILE_PATH = DATA_DIR_PATH + "info.ini";
    public const string BIND_FILE_PATH = DATA_DIR_PATH + "bind.ini";
    public const string DB_FILE_PATH = DATA_DIR_PATH + "db.sqlite";
    public const string INGEST_LIST_FILE_PATH = DATA_DIR_PATH + "ingestlist.txt";
    public const string COMMENT_FILE_PATH = DATA_DIR_PATH + "comment.txt";

    public const string INGEST_LIST_FILE_HEADER = "# file path | comment | tag list | series | source list\n";

    public const string IN_VIEW_NAME = "in";
    public const string OPEN_VIEW_NAME = "open";
    public const string DEFAULT_VIEW_NAME = "main";
    public const TaxonymID ROOT_TAXONYM_ID = (TaxonymID)1;

    public string archiveDir;

    public string? name;
    public SemanticVersion version;

    public DBEngine db;

    public static bool Exists(string archiveDir){
        return File.Exists($"{archiveDir}{INFO_FILE_PATH}");
    }

    public static void WriteInfoFile(string archiveDir, Dictionary<string, string> infoMap){
        var infoLines = new List<string>();
        foreach(var kv in infoMap){
            infoLines.Add($"{kv.Key}={kv.Value}");
        }
        File.WriteAllLines($"{archiveDir}{INFO_FILE_PATH}", infoLines);
    }

    public static Dictionary<string, string> ReadInfoFile(string archiveDir){
        var infoMap = new Dictionary<string, string>();
        foreach(var line in File.ReadAllLines($"{archiveDir}{INFO_FILE_PATH}")){
            var splitIndex = line.IndexOf('=');
            var infoKey = line[..splitIndex];
            var infoValue = line[(splitIndex+1)..];
            infoMap[infoKey] = infoValue;
        }
        return infoMap;
    }

    public void SetCommentFile(string? comment){
        File.WriteAllText($"{archiveDir}{COMMENT_FILE_PATH}", comment ?? "");
    }

    public string? ReadCommentFile(){
        if(!File.Exists($"{archiveDir}{COMMENT_FILE_PATH}")){
            File.Create($"{archiveDir}{COMMENT_FILE_PATH}");
        }
        var comment = File.ReadAllText($"{archiveDir}{COMMENT_FILE_PATH}");
        if(comment == "") return null;
        else return comment;
    }

    public static Archive Init(string archiveDir, string? name = null){
        var fileDir = archiveDir;

        // setup directory structure
        Directory.CreateDirectory($"{archiveDir}{FILES_DIR_PATH}");
        Directory.CreateDirectory($"{archiveDir}{IN_DIR_PATH}");
        Directory.CreateDirectory($"{archiveDir}{OUT_DIR_PATH}");
        Directory.CreateDirectory($"{archiveDir}{VIEWS_DIR_PATH}");
        Directory.CreateDirectory($"{archiveDir}{DATA_DIR_PATH}");
        
        // write info file
        var infoMap = new Dictionary<string, string>();

        if(name is not null)
            infoMap["name"] = name;
        infoMap["version"] = VERSION.ToString();
        WriteInfoFile(archiveDir, infoMap);

        // create bind file
        File.WriteAllLines($"{archiveDir}{BIND_FILE_PATH}", [
            $"doc=",
            $"tag=",
            $"taxonym=/1",
            $"series=",
            $"view={DEFAULT_VIEW_NAME}"
        ]);

        // create other files
        File.WriteAllText($"{archiveDir}{INGEST_LIST_FILE_PATH}", INGEST_LIST_FILE_HEADER);
        File.Create($"{archiveDir}{COMMENT_FILE_PATH}");

        var archive = Load(archiveDir);
        
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

    public static Archive Load(string archiveDir){

        var infoMap = ReadInfoFile(archiveDir);

        string versionStr = infoMap["version"];

        var version = SemanticVersion.FromString(versionStr);

        if(version.Normalise() != VERSION.Normalise()){
            throw new IncompatibleArchiveException(VERSION, version);
        }

        string? name = infoMap.ContainsKey("name") ? infoMap["name"] : null;

        var archive = new Archive(){
            archiveDir = archiveDir,
            name = name,
            version = version,
            db = new DBEngine(){ dbPath = $"{archiveDir}{DB_FILE_PATH}" }
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
        var inboxFiles = Directory.GetFiles($"{archiveDir}{IN_DIR_PATH}");

        foreach(var filePath in inboxFiles){
            var docID = IngestFile(filePath);
            if(docID is not null)
                File.Delete(filePath);
        }
    }

    public void IngestList(){
        var ingestListPath = $"{archiveDir}{INGEST_LIST_FILE_PATH}";

        if(!File.Exists(ingestListPath)){
            var ogFGColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("NOTE: ");
            Console.ForegroundColor = ogFGColor;
            Console.WriteLine("Ingest list file did not exist.");

            File.WriteAllText($"{archiveDir}{INGEST_LIST_FILE_PATH}", INGEST_LIST_FILE_HEADER);

            return;
        }

        var ingestListLines = (string[])File.ReadAllLines(ingestListPath);

        ingestListLines = ingestListLines.Where(
            (l) => {
                var t = l.Trim();
                return (t.Count() > 0) && (t[0] != '#');
            }
        ).ToArray();

        var ingestListItems = ingestListLines.Select(
            (l) => Regex.Split(l, @"\|(?=(?:[^""]*""[^""]*"")*[^""]*$)")
                        .Select(s => s.Trim())
                        .ToArray()
        );

        // filePath | comment | tag list | series / source list

        var i = 0;
        foreach(var ingestListItem in ingestListItems){
            if(ingestListItem.Count() < 3)
                continue;

            var filePath = ingestListItem[0];
            var comment = ingestListItem[1];
            if(comment.Count() > 0 && comment[0] == '"'){
                comment = comment.Trim('"');
            }
            var tags = ingestListItem[2]
                        .Split(' ')
                        .Select((s) => s.Trim())
                        .Where((s) => s.Count() > 0)
                        .Select((s) => ObjectRef.ResolveTag(this, s));
            //var series = ingestListItem[3];     // TODO
            //var sources = ingestListItem[4];    // TODO

            Console.WriteLine($"ingested document #{i}:");
            Console.WriteLine($"  file path: {filePath}");
            Console.WriteLine($"  comment: {comment}");
            Console.WriteLine($"  tags: {string.Join(' ', tags)}");

            i++;

            var documentID = IngestFile(filePath, comment);
            if(documentID is null)
                return;

            foreach(var tagID in tags){
                if(tagID is null) continue;
                DocumentAddTag((DocumentID)documentID, (TagID)tagID);
            }
        }
    }

    public DocumentID? IngestFile(string filePath, string? comment = null){
        var fileInfo = new FileInfo(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var fileExt = Path.GetExtension(filePath)[1..];
        var hash = HashFile(filePath);

        db.EnsureConnected();
        if(db.ExistsDocument(hash)){
            ConsoleExt.WriteColored("ERROR: ", ConsoleColor.Red);
            Console.WriteLine("File already present in archive.");
            return null;
        }

        File.Copy(filePath, $"{archiveDir}{FILES_DIR_PATH}{hash}");

        MediaMetadata mediaMetadata = Media.GetMediaType(filePath);

        var documentID = db.InsertDocument(new Document(){
            hash = hash,
            fileName = fileName,
            fileExt = fileExt,
            fileSize = (int)fileInfo.Length,
            mediaType = mediaMetadata.mediaType,
            comment = comment
        }, mediaMetadata);

        ViewAdd("in", documentID);

        return documentID;
    }

    public void DocumentDelete(DocumentID documentID){

        /*
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
        */

        db.EnsureConnected();
        db.UpdateDocumentIsDeleted(documentID, true);

    }

    public void DocumentsDeleteAll(){

        foreach(var filePath in Directory.GetFiles($"{archiveDir}{FILES_DIR_PATH}")){
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

    public Tag? TagGet(TagID tagID){
        db.EnsureConnected();
        return db.ReadTag(tagID);
    }

    public string TaxonymAsString(TaxonymID taxonymID){
        var taxonym = TaxonymGet(taxonymID!);
        if(taxonym?.id == ROOT_TAXONYM_ID)
            return "<root>";
        var parentTaxonym = TaxonymGet((TaxonymID)taxonym?.canonParentID!);
        if(parentTaxonym is not null)
            return $"{parentTaxonym?.canonAlias}:{taxonym?.canonAlias}";
        else
            return $"{taxonym?.canonAlias}";
    }

    public string TagAsString(TagID tagID){
        var tag = TagGet(tagID);
        return TaxonymAsString((TaxonymID)tag?.taxonymID);
    }

    public TagID? TagFind(string qualifiedName){
        db.EnsureConnected();
        return db.ReadTagID((TaxonymID)TaxonymFind(qualifiedName)!);
    }

    public TagID[] TagFindFuzzy(string qualifiedNameFuzzy){
        db.EnsureConnected();
        return TaxonymFindFuzzy(qualifiedNameFuzzy).Select(id => (TagID)db.ReadTagID(id)).ToArray();
    }

    public TagID? TagCreate(TaxonymID taxonymID){
        db.EnsureConnected();
        return db.InsertTag(new Tag(){ taxonymID = taxonymID });
    }

    public TagID? TagCreate(TaxonymID parentID, string name){
        return TagCreate((TaxonymID)TaxonymCreate(parentID, name)!);
    }

    public void TagDelete(TagID tagID, bool preserveTaxonym = false){
        var tag = TagGet(tagID);

        db.EnsureConnected();
        db.DeleteTag(tagID);

        if(!preserveTaxonym){
            db.DeleteTaxonym((TaxonymID)tag?.taxonymID);
        }
    }

    public void TagAddImplication(TagID tagID, TagID consequentID){
        db.EnsureConnected();
        db.InsertTagImplication(tagID, consequentID);
    }

    public void TagRemoveImplication(TagID tagID, TagID consequentID){
        db.EnsureConnected();
        db.DeleteTagImplication(tagID, consequentID);
    }

    public TagID[] TagGetImplications(TagID tagID){
        db.EnsureConnected();
        return db.ReadTagConsequents(tagID);
    }

    public TagID[] TagGetAntecedents(TagID tagID){
        db.EnsureConnected();
        return db.ReadTagAntecedents(tagID);
    }

    public TagID[] DocumentGetTags(DocumentID documentID){
        db.EnsureConnected();
        return db.ReadDocumentTags(documentID);
    }

    public void DocumentAddTag(DocumentID documentID, TagID tagID){
        db.EnsureConnected();
        db.InsertDocumentTag(documentID, tagID);
    }

    public void DocumentRemoveTag(DocumentID documentID, TagID tagID){
        db.EnsureConnected();
        db.DeleteDocumentTag(documentID, tagID);
    }

    public string[] DocumentGetSources(DocumentID documentID){
        db.EnsureConnected();
        return db.ReadDocumentSources(documentID);
    }

    public void DocumentAddSource(DocumentID documentID, string url){
        db.EnsureConnected();
        db.InsertDocumentSource(documentID, url);
    }

    public void DocumentRemoveSource(DocumentID documentID, string url){
        db.EnsureConnected();
        db.DeleteDocumentSource(documentID, url);
    }

    public void RankingCreate(string name){
        db.EnsureConnected();
        db.InsertRanking(name);
    }

    public void RankingDelete(string name){
        db.EnsureConnected();
        db.DeleteRanking(name);
    }

    public string[] RankingsGet(){
        db.EnsureConnected();
        return db.ReadRankings();
    }

    public Dictionary<string, int> DocumentGetRatings(DocumentID documentID){
        db.EnsureConnected();
        var ratingMap = new Dictionary<string, int>();
        var ratings = db.ReadDocumentRatings(documentID);
        foreach(var rating in ratings){
            ratingMap[rating.name] = rating.rating;
        }
        return ratingMap;
    }

    public Dictionary<string, double> DocumentGetRatingsNormalised(DocumentID documentID){
        db.EnsureConnected();
        var ratingMap = new Dictionary<string, double>();
        var ratings = db.ReadDocumentRatingsNormalised(documentID);
        foreach(var rating in ratings){
            ratingMap[rating.name] = rating.rating;
        }
        return ratingMap;
    }

    public int DocumentGetRating(DocumentID documentID, string rankingName){
        db.EnsureConnected();
        return db.ReadDocumentRating(documentID, rankingName);
    }

    public double DocumentGetRatingNormalised(DocumentID documentID, string rankingName){
        db.EnsureConnected();
        return db.ReadDocumentRatingNormalised(documentID, rankingName);
    }

    public void DocumentSetRating(DocumentID documentID, string rankingName, int score){
        db.EnsureConnected();
        db.UpdateDocumentRating(documentID, rankingName, score);
    }

    public void DocumentSetComment(DocumentID documentID, string? comment){
        db.EnsureConnected();
        db.UpdateDocumentComment(documentID, comment);
    }

    public Taxonym? TaxonymGet(TaxonymID taxonymID){
        db.EnsureConnected();
        return db.ReadTaxonym(taxonymID);
    }

    public TaxonymID? TaxonymFind(IEnumerable<string> qualifiedNameParts, TaxonymID? context = null){
        return TaxonymFindFuzzy(qualifiedNameParts, context).First();
    }

    public TaxonymID[] TaxonymFindFuzzy(IEnumerable<string> qualifiedNameParts, TaxonymID? context = null){
        if(qualifiedNameParts.Count() == 0){
            if(context is null){
                return [(TaxonymID)1];
            } else {
                return [(TaxonymID)context];
            }
        }
        
        var target = qualifiedNameParts.First();

        // replace wild cards with regex
        target = target.Replace("*", ".*");

        List<TaxonymID> layerIDs = [];
        List<(TaxonymID taxonymID, string alias)> layerAliases = [];
        
        var seedID = context is null ? (TaxonymID)1 : (TaxonymID)context;
        var seedAlias = TaxonymGet(seedID)?.canonAlias;
        layerIDs.Add(seedID);
        layerAliases.Add((seedID, seedAlias));

        var taxonymIDs = new HashSet<TaxonymID>();

        while(layerIDs.Count() > 0){
            foreach(var (taxonymID, alias) in layerAliases){
                if(Regex.IsMatch(alias, $"^{target}$")){
                    if(qualifiedNameParts.Count() == 1){
                        taxonymIDs.Add(taxonymID);
                    } else {
                        taxonymIDs.UnionWith(TaxonymFindFuzzy(qualifiedNameParts.Skip(1), taxonymID));
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

        return taxonymIDs.ToArray();
    }

    public TaxonymID[] TaxonymFindFuzzy(string qualifiedNameFuzzy, TaxonymID? context = null){
        return TaxonymFindFuzzy(qualifiedNameFuzzy.Split(':'), context);
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
            canonParentID = parentID,
            canonAlias = name
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
        Directory.CreateDirectory($"{archiveDir}{VIEWS_DIR_PATH}{viewName}/");
    }

    public string? ViewGenerateNumberedName(string prefix){
        var viewsDir = $"{archiveDir}{VIEWS_DIR_PATH}";
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
        Directory.Delete($"{archiveDir}{VIEWS_DIR_PATH}{viewName}/");
    }

    public string[] ViewsGetAll(){
        var viewsDir = $"{archiveDir}{VIEWS_DIR_PATH}";
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

        var viewsDir = $"{archiveDir}{VIEWS_DIR_PATH}";
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
        var viewDir = $"{archiveDir}{VIEWS_DIR_PATH}{viewName}/";

        var document = (Document)DocumentGet(documentID)!;

        var filePaths = Directory.GetFiles(viewDir);
        var newIndex = filePaths.Count();

        FileExt.CreateHardLink(
            $"{viewDir}{newIndex}~{document.hash}.{document.fileExt}",
            $"{archiveDir}{FILES_DIR_PATH}{document.hash}",
            IntPtr.Zero
        );

        return newIndex;
    }

    public void ViewClear(string viewName){
        var viewDir = $"{archiveDir}{VIEWS_DIR_PATH}{viewName}/";
        
        foreach(var filePath in Directory.GetFiles(viewDir)){
            File.Delete(filePath);
        }
    }

    public void ViewReflect(string targetViewName, string sourceViewName){
        var sourceView = (View)ViewGet(sourceViewName)!;

        var viewDir = $"{archiveDir}{VIEWS_DIR_PATH}{targetViewName}/";
        var filePaths = Directory.GetFiles(viewDir);
        var newIndex = filePaths.Count();

        foreach(var documentID in sourceView.documents){
            if(documentID is not null){
                var document = (Document)DocumentGet((DocumentID)documentID)!;

                FileExt.CreateHardLink(
                    $"{viewDir}{newIndex}~{document.hash}.{document.fileExt}",
                    $"{archiveDir}{FILES_DIR_PATH}{document.hash}",
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
        var lines = File.ReadAllLines($"{archiveDir}{BIND_FILE_PATH}");

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
        var lines = File.ReadAllLines($"{archiveDir}{BIND_FILE_PATH}");

        for(int i = 0; i < lines.Count(); i++){
            if((ObjectType)i == objType){
                var line = lines[i];
                var equalIndex = line.IndexOf('=');
                var key = line[0..equalIndex];
                lines[i] = $"{key}={val}";
            }
        }

        File.WriteAllLines($"{archiveDir}{BIND_FILE_PATH}", lines);
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

    public MediaMetadata DocumentGetMetadata(DocumentID documentID){
        db.EnsureConnected();
        var mediaType = db.ReadDocument(documentID)?.mediaType ?? MediaType.Binary;
        return db.ReadDocumentMetadata(documentID, mediaType);
    }

    public class IncompatibleArchiveException : Exception {
        SemanticVersion expected;
        SemanticVersion actual;

        public IncompatibleArchiveException(
            SemanticVersion expected, SemanticVersion actual){
            
            this.expected = expected;
            this.actual = actual;
        }

        override public string ToString(){
            return $"Archive made in version {actual} is incompatible with Mage {expected}";
        }
    }

}