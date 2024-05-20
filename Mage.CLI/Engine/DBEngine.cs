using System.CommandLine;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Resources;
using Microsoft.Data.Sqlite;

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

    public void RunResourceScript(string resourcePath){
        var setupCommand = db.CreateCommand();
        setupCommand.CommandText = ResourceLoader.Load($"Resources.DB.{resourcePath}");
        setupCommand.ExecuteNonQuery();
        setupCommand.Dispose();
    }

}

// Reading
public partial class DBEngine {

    public SqliteCommand GenCommand(string comString, params (string, object)[] param){
        var com = db.CreateCommand();
        com.CommandText = comString;
        foreach(var p in param){
            com.Parameters.AddWithValue(p.Item1, p.Item2);
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
            DBCommands.Select.DocumentIDWhere(clause)
        );
        com.Transaction = transaction;

        return RunQuery<DocumentID>(com, (r) => (DocumentID)r.GetInt32(0)).ToArray();
    }

    public Document? ReadDocument(DocumentID documentID, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Select.DocumentWhereID,
            ("id", documentID)
        );
        com.Transaction = transaction;

        return RunQuerySingle<Document>(com, (r) => {
            var ingestTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            ingestTimestamp = ingestTimestamp.AddSeconds(r.GetInt32(4)).ToLocalTime();

            return new Document(){
                hash = r.GetString(1),
                id = documentID,
                fileName = r.GetString(2),
                extension = r.GetString(3),
                ingestedAt = ingestTimestamp,
                comment = r.IsDBNull(5) ? null : r.GetString(5)
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

    public TaxonymID[] QueryTaxonyms(string clause, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Select.TaxonymIDWhere(clause)
        );
        com.Transaction = transaction;

        return RunQuery<TaxonymID>(com, (r) => (TaxonymID)r.GetInt32(0)).ToArray();
    }

    public Taxonym? ReadTaxonym(TaxonymID taxonymID, SqliteTransaction? transaction = null){
        
        using var com = GenCommand(
            DBCommands.Select.TaxonymWhereID,
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
            DBCommands.Select.TagIDWhere(clause)
        );
        com.Transaction = transaction;

        return RunQuery<TagID>(com, (r) => (TagID)r.GetInt32(0)).ToArray();
    }

    public Tag? ReadTag(TagID tagID, SqliteTransaction? transaction = null){

        using var com = GenCommand(
            DBCommands.Select.TagWhereID,
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
            DBCommands.Count.DocumentTagWhereTagID,
            ("tag_id", tagID)
        );
        com.Transaction = transaction;

        return RunQuerySingle<int>(com, (r) => r.GetInt32(0));
    }

}

// Insertion
public partial class DBEngine {

    public void InsertDocumentTag(DocumentID documentID, TagID tagID, SqliteTransaction? transaction = null){

        var com = db.CreateCommand();
        com.CommandText = @"
            insert into document_tag (
                document_id,
                tag_id
            )
            values (
                @document_id,
                @tag_id
            );
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@document_id", documentID);
        com.Parameters.AddWithValue("@tag_id", tagID);
        com.ExecuteNonQuery();
        com.Dispose();

    }

    public TagID InsertTag(Tag tag, SqliteTransaction? transaction = null){

        var com = db.CreateCommand();
        com.CommandText = @"
            insert into tag (
                taxonym_id
            )
            values (
                @taxonym_id
            );
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@taxonym_id", tag.taxonymID);
        com.ExecuteNonQuery();
        com.Dispose();

        var tagID = (TagID)ReadLastInsertRowID();
        return tagID;
    }

    public void InsertTagImplication(TagID antecedentID, TagID consequentID, SqliteTransaction? transaction = null){

        var com = db.CreateCommand();
        com.CommandText = @"
            insert into tag_implication (
                antecedent_id,
                consequent_id
            )
            values (
                @antecedent_id,
                @consequent_id
            );
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@antecedent_id", antecedentID);
        com.Parameters.AddWithValue("@consequent_id", consequentID);
        com.ExecuteNonQuery();
        com.Dispose();

    }

    public DocumentID InsertDocument(Document document, SqliteTransaction? transaction = null){

        var com = db.CreateCommand();
        com.CommandText = $@"
            insert into document (
                hash,
                file_name,
                extension,
                ingested_at,
                comment
            )
            values (
                @hash,
                @file_name,
                @extension,
                @ingested_at,
                @comment
            );
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@hash", document.hash);
        com.Parameters.AddWithValue("@file_name", document.fileName);
        com.Parameters.AddWithValue("@extension", document.extension);
        com.Parameters.AddWithValue("@ingested_at", ((DateTimeOffset)(document.ingestedAt)).ToUnixTimeSeconds());
        com.Parameters.AddWithValue("@comment", document.comment is null ? System.DBNull.Value : document.comment);
        com.ExecuteNonQuery();
        com.Dispose();

        var documentID = (DocumentID)ReadLastInsertRowID();
        return documentID;

    }

    public void InsertTaxonymAlias(TaxonymID taxonymID, string alias, SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
        com.CommandText = $@"
            insert into taxonym_alias
            (taxonym_id, alias)
            values (@taxonym_id, @alias);
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@taxonym_id", taxonymID);
        com.Parameters.AddWithValue("@alias", alias);
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public void InsertTaxonymParent(TaxonymID childID, TaxonymID parentID, SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
        com.CommandText = $@"
            insert into taxonym_parent
            (child_id, parent_id)
            values (@child_id, @parent_id);
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@child_id", childID);
        com.Parameters.AddWithValue("@parent_id", parentID);
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public TaxonymID InsertTaxonym(Taxonym taxonym){

        var transaction = db.BeginTransaction();

        var com = db.CreateCommand();
        com.CommandText = $@"
            insert into taxonym
            (canon_parent_id, canon_alias)
            values (@canon_parent_id, @canon_alias)
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@canon_parent_id", ((object?)taxonym.canonParentID) ?? DBNull.Value);
        com.Parameters.AddWithValue("@canon_alias", taxonym.canonAlias);
        com.ExecuteNonQuery();
        com.Dispose();

        var taxonymID = (TaxonymID)ReadLastInsertRowID(transaction);

        InsertTaxonymAlias(taxonymID, taxonym.canonAlias, transaction);
        if(taxonym.canonParentID is not null)
            InsertTaxonymParent(taxonymID, (TaxonymID)taxonym.canonParentID, transaction);

        transaction.Commit();
        transaction.Dispose();

        return taxonymID;
    }

}


// Deletion
public partial class DBEngine {

    public void DeleteDocumentTag(DocumentID documentID, TagID tagID, SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
        com.CommandText = $@"
            delete from document_tag
            where 
                document_id = @document_id 
                and tag_id = @tag_id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@document_id", documentID);
        com.Parameters.AddWithValue("@tag_id", tagID);
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public void DeleteTag(TagID tagID, SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
        com.CommandText = $@"
            delete from tag_implication
            where antecedent_id = @id or consequent_id = @id;

            delete from tag
            where id = @id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@id", tagID);
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public void DeleteTagImplication(TagID antecedentID, TagID consequentID, SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
        com.CommandText = @"
            delete from tag_implication
            where
                antecedent_id = @antecedent_id
                and consequent_id = @consequent_id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@antecedent_id", antecedentID);
        com.Parameters.AddWithValue("@consequent_id", consequentID);
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public void DeleteDocument(DocumentID documentID, SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
        com.CommandText = $@"
            delete from document
            where id = @id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@id", documentID);
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public void DeleteAllDocuments(SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
        com.CommandText = $@"delete from document;";
        com.Transaction = transaction;
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public void DeleteTaxonymAlias(TaxonymID taxonymID, string alias, SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
        com.CommandText = $@"
            delete from taxonym_alias
            where
                taxonym_id = @taxonym_id
                and alias = @alias;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@taxonym_id", taxonymID);
        com.Parameters.AddWithValue("@alias", alias);
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public void DeleteTaxonymParent(TaxonymID childID, TaxonymID parentID, SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
        com.CommandText = $@"
            delete from taxonym_parent
            where
                child_id = @child_id
                and parent_id = @parent_id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@child_id", childID);
        com.Parameters.AddWithValue("@parent_id", parentID);
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public void DeleteTaxonym(TaxonymID taxonymID, SqliteTransaction? transaction = null){
        transaction = db.BeginTransaction();

        var com = db.CreateCommand();
        com.CommandText = $@"
            delete from taxonym
            where id = @id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@id", taxonymID);
        com.ExecuteNonQuery();
        com.Dispose();

        com = db.CreateCommand();
        com.CommandText = $@"
            delete from taxonym_alias
            where taxonym_id = @taxonym_id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@taxonym_id", taxonymID);
        com.ExecuteNonQuery();
        com.Dispose();

        com = db.CreateCommand();
        com.CommandText = $@"
            delete from taxonym_parent
            where 
                child_id = @taxonym_id
                or parent_id = @taxonym_id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@taxonym_id", taxonymID);
        com.ExecuteNonQuery();
        com.Dispose();

        transaction.Commit();
        transaction.Dispose();
    }

}