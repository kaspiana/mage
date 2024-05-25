using System.CommandLine;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Resources;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Mage.Engine;

public partial class DBEngine {

    public string dbPath;
    public SqliteConnection? db;

    public void Connect(){
        db = new SqliteConnection($"DataSource={dbPath}");
        db.Open();
    }

    public void EnsureConnected(){
        if(db is null){
            Connect();
        }
    }

    public void Disconnect(){
        if(db is not null){
            db.Close();
            db.Dispose();
        }
    }

    public void RunScript(string comText){
        using var com = db.CreateCommand();
        com.CommandText = comText;
        com.ExecuteNonQuery();
    }

    public void RunResourceScript(string resourcePath){
        var setupCommand = db.CreateCommand();
        setupCommand.CommandText = ResourceLoader.Load($"Resources.DB.{resourcePath}");
        setupCommand.ExecuteNonQuery();
        setupCommand.Dispose();
    }

}

// Reading
public partial class DBEngine {

    public SqliteCommand GenCommand(string comString, params (string, object?)[] param){
        var com = db.CreateCommand();
        com.CommandText = comString;
        foreach(var p in param){
            com.Parameters.AddWithValue(p.Item1, p.Item2 is null ? System.DBNull.Value : p.Item2);
        }
        return com;
    }

    public static IEnumerable<T> RunQuery<T>(SqliteCommand com, Func<SqliteDataReader, T> read){
        var reader = com.ExecuteReader();
        while(reader.Read()){
            yield return read(reader);
        }
        reader.Close();
    }

    public static T? RunQuerySingle<T>(SqliteCommand com, Func<SqliteDataReader, T> read) {
        var reader = com.ExecuteReader();
        if(reader.Read()){
            return read(reader);
        }
        reader.Close();
        return default(T?);
    }

    public void RunNonQuery(SqliteCommand com){
        com.ExecuteNonQuery();
    }

    public long ReadLastInsertRowID(SqliteTransaction? transaction = null){
        var com = new SqliteCommand("select last_insert_rowid()", db, transaction);
        long lastRowID = (long)com.ExecuteScalar()!;
        com.Dispose();

        return lastRowID;
    }

    public DocumentID[] QueryDocuments(string clause, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Select.DocumentIDClause(clause)
        );
        com.Transaction = transaction;

        return RunQuery<DocumentID>(com, (r) => (DocumentID)r.GetInt32(0)).ToArray();
    }

    public bool ExistsDocument(string documentHash, SqliteTransaction? transaction = null){
        using var com = GenCommand(
            DBCommands.Count.DocumentWhereHash,
            ("hash", documentHash)
        );
        com.Transaction = transaction;
        return 0 < RunQuerySingle<int>(com, r => r.GetInt32(0));
    }

    public Document? ReadDocument(DocumentID documentID, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Select.DocumentWherePK,
            ("id", documentID)
        );
        com.Transaction = transaction;

        return RunQuerySingle<Document>(com, (r) => {
            var unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return new Document(){
                hash = r.GetString(1),
                id = documentID,
                fileName = r.GetString(2),
                fileExt = r.GetString(3),
                fileSize = r.GetInt32(4),
                mediaType = r.GetChar(5) switch {
                    'b' => MediaType.Binary,
                    't' => MediaType.Text,
                    'i' => MediaType.Image,
                    'm' => MediaType.Animation,
                    'a' => MediaType.Audio,
                    'v' => MediaType.Video,
                    _ => MediaType.Binary
                },
                addedAt = unixStart.AddSeconds(r.GetInt32(6)).ToLocalTime(),
                updatedAt = unixStart.AddSeconds(r.GetInt32(7)).ToLocalTime(),
                comment = r.IsDBNull(8) ? null : r.GetString(8),
                isDeleted = r.GetBoolean(9)
            };
        });
    }

    public string? ReadDocumentHash(DocumentID documentID, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Select.DocumentHashWhereID,
            ("id", documentID)
        );
        com.Transaction = transaction;

        return RunQuerySingle<string>(com, (r) => r.GetString(0));
    }

    public DocumentID? ReadDocumentID(string documentHash, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Select.DocumentWhereHash,
            ("hash", documentHash)
        );
        com.Transaction = transaction;

        return RunQuerySingle<DocumentID>(com, (r) => (DocumentID)r.GetInt32(0));
    }

    public MediaMetadata ReadDocumentMetadata(DocumentID documentID, MediaType mediaType, SqliteTransaction? transaction = null){
        switch(mediaType){
            default: return new MediaMetadataBinary(); break;
            case MediaType.Text: return new MediaMetadataText(); break;

            case MediaType.Image: {
                using var com = GenCommand(
                    DBCommands.Select.ImageMetadataWhereID,
                    ("document_id", documentID)
                );
                com.Transaction = transaction;
                return RunQuerySingle(com, r => new MediaMetadataImage(){
                    width = r.GetInt32(1),
                    height = r.GetInt32(2)
                })!;
            } break;

            case MediaType.Animation: {
                using var com = GenCommand(
                    DBCommands.Select.VideoMetadataWhereID,
                    ("document_id", documentID)
                );
                com.Transaction = transaction;
                return RunQuerySingle(com, r => new MediaMetadataAnimation(){
                    width = r.GetInt32(1),
                    height = r.GetInt32(2),
                    duration = r.GetInt32(3)
                })!;
            } break;

            case MediaType.Video: {
                using var com = GenCommand(
                    DBCommands.Select.VideoMetadataWhereID,
                    ("document_id", documentID)
                );
                com.Transaction = transaction;
                return RunQuerySingle(com, r => new MediaMetadataVideo(){
                    width = r.GetInt32(1),
                    height = r.GetInt32(2),
                    duration = r.GetInt32(3)
                })!;
            } break;

            case MediaType.Audio: {
                using var com = GenCommand(
                    DBCommands.Select.AudioMetadataWhereID,
                    ("document_id", documentID)
                );
                com.Transaction = transaction;
                return RunQuerySingle(com, r => new MediaMetadataAudio(){
                    duration = r.GetInt32(1)
                })!;
            } break;
        }
    }

    public string[] ReadRankings(SqliteTransaction? transaction = null){
        using var com = GenCommand(
            DBCommands.Select.Ranking
        );
        com.Transaction = transaction;
        return RunQuery(com, r => r.GetString(0)).ToArray();
    }

    public int ReadDocumentRanking(DocumentID documentID, string rankingName, SqliteTransaction? transaction = null){
        using var com = GenCommand(
            DBCommands.Select.DocumentRankingWherePK,
            ("document_id", documentID),
            ("ranking_name", rankingName)
        );
        com.Transaction = transaction;
        return RunQuerySingle(com, r => r.GetInt32(0));
    }

    public (string name, int score)[] ReadDocumentRankings(DocumentID documentID, SqliteTransaction? transaction = null){
        using var com = GenCommand(
            DBCommands.Select.DocumentRankingWhereID,
            ("document_id", documentID)
        );
        com.Transaction = transaction;
        return RunQuery(com, r => (r.GetString(0), r.GetInt32(1))).ToArray();
    }

    public string[] ReadDocumentSources(DocumentID documentID, SqliteTransaction? transaction = null){
        using var com = GenCommand(
            DBCommands.Select.DocumentSourceWhereID,
            ("document_id", documentID)
        );
        com.Transaction = transaction;

        return RunQuery<string>(com, (r) => r.GetString(0)).ToArray();
    }

    public TaxonymID[] QueryTaxonyms(string clause, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Select.TaxonymIDClause(clause)
        );
        com.Transaction = transaction;

        return RunQuery<TaxonymID>(com, (r) => (TaxonymID)r.GetInt32(0)).ToArray();
    }

    public Taxonym? ReadTaxonym(TaxonymID taxonymID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Select.TaxonymWherePK,
            ("id", taxonymID)
        );
        com.Transaction = transaction;

        return RunQuerySingle<Taxonym>(com, (r) => new Taxonym(){
            id = taxonymID,
            canonParentID = r.IsDBNull(1) ? null : (TaxonymID)r.GetInt32(1),
            canonAlias = r.GetString(2)
        });
    }

    public TaxonymID[] ReadTaxonymChildren(TaxonymID taxonymID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Select.TaxonymChildIDWhereID,
            ("parent_id", taxonymID)
        );
        com.Transaction = transaction;

        return RunQuery<TaxonymID>(com, (r) => (TaxonymID)r.GetInt32(0)).ToArray();
    }

    public TaxonymID[] ReadTaxonymParents(TaxonymID taxonymID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Select.TaxonymParentIDWhereID,
            ("child_id", taxonymID)
        );
        com.Transaction = transaction;

        return RunQuery<TaxonymID>(com, (r) => (TaxonymID)r.GetInt32(0)).ToArray();
    }

    public string[] ReadTaxonymAliases(TaxonymID taxonymID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Select.TaxonymAliasWhereID,
            ("taxonym_id", taxonymID)
        );
        com.Transaction = transaction;

        return RunQuery<string>(com, (r) => r.GetString(0)).ToArray();
    }

    public TagID[] QueryTags(string clause, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Select.TagIDClause(clause)
        );
        com.Transaction = transaction;

        return RunQuery<TagID>(com, (r) => (TagID)r.GetInt32(0)).ToArray();
    }

    public Tag? ReadTag(TagID tagID, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Select.TagWherePK,
            ("id", tagID)
        );
        com.Transaction = transaction;

        return RunQuerySingle(com, (r) => new Tag(){
            id = tagID,
            taxonymID = (TaxonymID)r.GetInt32(1)
        });
    }

    public TagID? ReadTagID(TaxonymID taxonymID, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Select.TagIDWhereTaxonymID,
            ("taxonym_id", taxonymID)
        );
        com.Transaction = transaction;

        return RunQuerySingle<TagID>(com, (r) => (TagID)r.GetInt32(0));
    }

    public TagID[] ReadTagConsequents(TagID tagID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Select.TagConsequentIDWhereID,
            ("antecedent_id", tagID)
        );
        com.Transaction = transaction;

        return RunQuery<TagID>(com, (r) => (TagID)r.GetInt32(0)).ToArray();
    }

    public TagID[] ReadTagAntecedents(TagID tagID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Select.TagAntecedentIDWhereID,
            ("consequent_id", tagID)
        );
        com.Transaction = transaction;

        return RunQuery<TagID>(com, (r) => (TagID)r.GetInt32(0)).ToArray();
    }

    public TagID[] ReadDocumentTags(DocumentID documentID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Select.DocumentTagIDWhereDocumentID,
            ("document_id", documentID)
        );
        com.Transaction = transaction;

        return RunQuery<TagID>(com, (r) => (TagID)r.GetInt32(0)).ToArray();
    }

    public int CountTagDocuments(TagID tagID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Count.DocumentTagWhereTagID(),
            ("tag_id", tagID)
        );
        com.Transaction = transaction;

        return RunQuerySingle<int>(com, (r) => r.GetInt32(0));
    }

}

// Insertion
public partial class DBEngine {

    public void InsertDocumentTag(DocumentID documentID, TagID tagID, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Insert.DocumentTag,
            ("document_id", documentID),
            ("tag_id", tagID)
        );
        com.Transaction = transaction;
        RunNonQuery(com);

        UpdateDocumentUpdatedAt(documentID);
    }

    public void InsertDocumentSource(DocumentID documentID, string url, SqliteTransaction? transaction = null){
        using var com = GenCommand(
            DBCommands.Insert.DocumentSource,
            ("document_id", documentID),
            ("url", url)
        );
        com.Transaction = transaction;
        RunNonQuery(com);

        UpdateDocumentUpdatedAt(documentID);
    }

    public TagID InsertTag(Tag tag, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Insert.Tag,
            ("taxonym_id", tag.taxonymID)
        );
        com.Transaction = transaction;
        RunNonQuery(com);

        var tagID = (TagID)ReadLastInsertRowID();
        return tagID;
    }

    public void InsertTagImplication(TagID antecedentID, TagID consequentID, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Insert.TagImplication,
            ("antecedent_id", antecedentID),
            ("consequent_id", consequentID)
        );
        com.Transaction = transaction;
        RunNonQuery(com);
    }

    public DocumentID InsertDocument(Document document, MediaMetadata mediaMetadata, SqliteTransaction? transaction = null){

        using var com1 = GenCommand(
            DBCommands.Insert.Document,
            ("hash", document.hash),
            ("file_name", document.fileName),
            ("file_ext", document.fileExt),
            ("file_size", document.fileSize),
            ("media_type", document.mediaType switch {
                MediaType.Binary => 'b',
                MediaType.Text => 't',
                MediaType.Image => 'i',
                MediaType.Animation => 'm',
                MediaType.Audio => 'a',
                MediaType.Video => 'v',
                _ => MediaType.Binary
            }),
            ("comment", document.comment)
        );
        com1.Transaction = transaction;
        RunNonQuery(com1);

        var documentID = (DocumentID)ReadLastInsertRowID();

        switch(mediaMetadata){
            default: break;

            case MediaMetadataImage mm: {
                using var com2 = GenCommand(
                    DBCommands.Insert.ImageMetadata,
                    ("document_id", documentID),
                    ("width", mm.width),
                    ("height", mm.height)
                );
                com2.Transaction = transaction;
                RunNonQuery(com2);
            } break;

            case MediaMetadataAudio mm: {
                using var com2 = GenCommand(
                    DBCommands.Insert.AudioMetadata,
                    ("document_id", documentID),
                    ("duration", mm.duration)
                );
                com2.Transaction = transaction;
                RunNonQuery(com2);
            } break;

            case MediaMetadataAnimation mm: {
                using var com2 = GenCommand(
                    DBCommands.Insert.VideoMetadata,
                    ("document_id", documentID),
                    ("width", mm.width),
                    ("height", mm.height),
                    ("duration", mm.duration)
                );
                com2.Transaction = transaction;
                RunNonQuery(com2);
            } break;

            case MediaMetadataVideo mm: {
                using var com2 = GenCommand(
                    DBCommands.Insert.VideoMetadata,
                    ("document_id", documentID),
                    ("width", mm.width),
                    ("height", mm.height),
                    ("duration", mm.duration)
                );
                com2.Transaction = transaction;
                RunNonQuery(com2);
            } break;
        }

        return documentID;

    }

    public void InsertTaxonymAlias(TaxonymID taxonymID, string alias, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Insert.TaxonymAlias,
            ("taxonym_id", taxonymID),
            ("alias", alias)
        );
        com.Transaction = transaction;
        RunNonQuery(com);
    }

    public void InsertTaxonymParent(TaxonymID childID, TaxonymID parentID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Insert.TaxonymRelationship,
            ("child_id", childID),
            ("parent_id", parentID)
        );
        com.Transaction = transaction;
        RunNonQuery(com);
    }

    public TaxonymID InsertTaxonym(Taxonym taxonym){

        using var transaction = db.BeginTransaction();

        using var com = GenCommand(
            DBCommands.Insert.Taxonym,
            ("canon_parent_id", taxonym.canonParentID),
            ("canon_alias", taxonym.canonAlias)
        );
        com.Transaction = transaction;
        RunNonQuery(com);

        var taxonymID = (TaxonymID)ReadLastInsertRowID(transaction);

        InsertTaxonymAlias(taxonymID, taxonym.canonAlias, transaction);
        if(taxonym.canonParentID is not null)
            InsertTaxonymParent(taxonymID, (TaxonymID)taxonym.canonParentID, transaction);

        transaction.Commit();

        return taxonymID;
    }

    public void InsertRanking(string rankingName, SqliteTransaction? transaction = null){
        using var com = GenCommand(
            DBCommands.Insert.Ranking,
            ("name", rankingName)
        );
        com.Transaction = transaction;
        RunNonQuery(com);

    }

}


// Deletion
public partial class DBEngine {

    public void DeleteRanking(string rankingName, SqliteTransaction? transaction = null){

        using var com1 = GenCommand(
            DBCommands.Delete.DocumentRankingWhereName,
            ("ranking_name", rankingName)
        );
        com1.Transaction = transaction;

        using var com2 = GenCommand(
            DBCommands.Delete.RankingWherePK,
            ("name", rankingName)
        );
        com2.Transaction = transaction;

        RunNonQuery(com1);
        RunNonQuery(com2);
    }

    public void DeleteDocumentTag(DocumentID documentID, TagID tagID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Delete.DocumentTagWherePK,
            ("document_id", documentID),
            ("tag_id", tagID)
        );
        com.Transaction = transaction;
        RunNonQuery(com);

        UpdateDocumentUpdatedAt(documentID);
    }

    public void DeleteDocumentSource(DocumentID documentID, string url, SqliteTransaction? transaction = null){
        using var com = GenCommand(
            DBCommands.Delete.DocumentSourceWherePK,
            ("document_id", documentID),
            ("url", url)
        );
        com.Transaction = transaction;
        RunNonQuery(com);

        UpdateDocumentUpdatedAt(documentID);
    }

    public void DeleteTag(TagID tagID, SqliteTransaction? transaction = null){

        using var com1 = GenCommand(
            DBCommands.Delete.TagImplicationWhereEitherID,
            ("id", tagID)
        );
        com1.Transaction = transaction;

        using var com2 = GenCommand(
            DBCommands.Delete.DocumentTagWhereTagID,
            ("tag_id", tagID)
        );
        com2.Transaction = transaction;

        using var com3 = GenCommand(
            DBCommands.Delete.TagWherePK,
            ("id", tagID)
        );
        com3.Transaction = transaction;

        RunNonQuery(com1);
        RunNonQuery(com2);
        RunNonQuery(com3);
    }

    public void DeleteTagImplication(TagID antecedentID, TagID consequentID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Delete.TagImplicationWherePK,
            ("antecedent_id", antecedentID),
            ("consequent_id", consequentID)
        );
        com.Transaction = transaction;
        RunNonQuery(com);
    }

    public void DeleteDocument(DocumentID documentID, SqliteTransaction? transaction = null){
        
        using var com1 = GenCommand(
            DBCommands.Delete.DocumentTagWhereDocumentID,
            ("document_id", documentID)
        );
        com1.Transaction = transaction;

        using var com2 = GenCommand(
            DBCommands.Delete.DocumentWherePK,
            ("id", documentID)
        );
        com2.Transaction = transaction;

        RunNonQuery(com1);
        RunNonQuery(com2);

        UpdateDocumentUpdatedAt(documentID);
    }

    public void DeleteAllDocuments(SqliteTransaction? transaction = null){

        using var com1 = GenCommand(DBCommands.Delete.DocumentTag);
        com1.Transaction = transaction;

        using var com2 = GenCommand(DBCommands.Delete.Document);
        com2.Transaction = transaction;

        using var com3 = GenCommand(DBCommands.Update.DocumentUpdatedAt);
        com3.Transaction = transaction;
        
        RunNonQuery(com1);
        RunNonQuery(com2);
        RunNonQuery(com3);
    }

    public void DeleteTaxonymAlias(TaxonymID taxonymID, string alias, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Delete.TaxonymAliasWherePK,
            ("taxonym_id", taxonymID),
            ("alias", alias)
        );
        com.Transaction = transaction;
        RunNonQuery(com);
    }

    public void DeleteTaxonymParent(TaxonymID childID, TaxonymID parentID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Delete.TaxonymRelationshipWherePK,
            ("child_id", childID),
            ("parent_id", parentID)
        );
        com.Transaction = transaction;
        RunNonQuery(com);
    }

    public void DeleteTaxonym(TaxonymID taxonymID){
        using var transaction = db.BeginTransaction();

        using var com1 = GenCommand(
            DBCommands.Delete.TaxonymWherePK,
            ("id", taxonymID)
        );
        com1.Transaction = transaction;

        using var com2 = GenCommand(
            DBCommands.Delete.TaxonymAliasWhereTaxonymID,
            ("taxonym_id", taxonymID)
        );
        com2.Transaction = transaction;

        using var com3 = GenCommand(
            DBCommands.Delete.TaxonymRelationshipWhereEitherID,
            ("id", taxonymID)
        );
        com3.Transaction = transaction;
        
        RunNonQuery(com1);
        RunNonQuery(com2);
        RunNonQuery(com3);

        transaction.Commit();
    }

}

// Update
public partial class DBEngine {

    public void UpdateDocumentIsDeleted(DocumentID documentID, bool isDeleted, SqliteTransaction? transaction = null){
        using var com = GenCommand(
            DBCommands.Update.DocumentIsDeletedWhereID,
            ("id", documentID),
            ("is_deleted", isDeleted)
        );
        com.Transaction = transaction;
        RunNonQuery(com);

        UpdateDocumentUpdatedAt(documentID);
    }

    public void UpdateDocumentUpdatedAt(DocumentID documentID, SqliteTransaction? transaction = null){
        using var com = GenCommand(
            DBCommands.Update.DocumentUpdatedAtWhereID,
            ("id", documentID)
        );
        com.Transaction = transaction;
        RunNonQuery(com);
    }

    public void UpdateDocumentRanking(DocumentID documentID, string rankingName, int score, SqliteTransaction? transaction = null){
        using var com = GenCommand(
            DBCommands.Update.DocumentRanking,
            ("document_id", documentID),
            ("ranking_name", rankingName),
            ("score", score)
        );
        com.Transaction = transaction;
        RunNonQuery(com);
    }

}