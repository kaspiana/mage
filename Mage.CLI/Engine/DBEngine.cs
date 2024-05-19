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

    public long ReadLastInsertRowID(SqliteTransaction? transaction = null){
        var com = new SqliteCommand("select last_insert_rowid()", db, transaction);
        long lastRowID = (long)com.ExecuteScalar()!;
        com.Dispose();

        return lastRowID;
    }

    public DocumentID[] QueryDocuments(string clause, SqliteTransaction? transaction = null){

        var documents = new List<DocumentID>();

        var com = db.CreateCommand();
        com.CommandText = $"select id from document {clause}";
        com.Transaction = transaction;
        
        var reader = com.ExecuteReader();
        while(reader.Read()){
            documents.Add((DocumentID)reader.GetInt32(0));
        }

        reader.Close();
        com.Dispose();

        return documents.ToArray();
    }

    public Document? ReadDocument(DocumentID documentID, SqliteTransaction? transaction = null){

        Document? document = null;

        var com = db.CreateCommand();
        com.CommandText = $"select * from document where id = @id;";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("id", documentID);
        
        var reader = com.ExecuteReader();
        if(reader.Read()){
            var ingestTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            ingestTimestamp = ingestTimestamp.AddSeconds(reader.GetInt32(4)).ToLocalTime();

            document = new Document(){
                hash = reader.GetString(1),
                id = documentID,
                fileName = reader.GetString(2),
                extension = reader.GetString(3),
                ingestedAt = ingestTimestamp,
                comment = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }

        reader.Close();
        com.Dispose();

        return document;
    }

    public string? ReadDocumentHash(DocumentID documentID, SqliteTransaction? transaction = null){

        string? documentHash = null;

        var com = db.CreateCommand();
        com.CommandText = $"select hash from document where id = @id;";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("id", documentID);

        var reader = com.ExecuteReader();
        if(reader.Read()){
            documentHash = reader.GetString(0);
        }

        reader.Close();
        com.Dispose();
        
        return documentHash;
    }

    public DocumentID? ReadDocumentID(string documentHash, SqliteTransaction? transaction = null){

        DocumentID? documentID = null;

        var com = db.CreateCommand();
        com.CommandText = $"select id from document where hash = @hash;";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("hash", documentHash);

        var reader = com.ExecuteReader();
        if(reader.Read()){
            documentID = (DocumentID)reader.GetInt32(0);
        }

        reader.Close();
        com.Dispose();
        
        return documentID;

    }

    public TaxonymID[] QueryTaxonyms(string clause, SqliteTransaction? transaction = null){

        var taxonyms = new List<TaxonymID>();

        var com = db.CreateCommand();
        com.CommandText = $"select id from taxonym {clause}";
        com.Transaction = transaction;
        
        var reader = com.ExecuteReader();
        while(reader.Read()){
            taxonyms.Add((TaxonymID)reader.GetInt32(0));
        }

        reader.Close();
        com.Dispose();

        return taxonyms.ToArray();
    }

    public Taxonym? ReadTaxonym(TaxonymID taxonymID, SqliteTransaction? transaction = null){
        Taxonym? taxonym = null;

        var com = db.CreateCommand();
        com.CommandText = $"select * from taxonym where id = @id";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("id", taxonymID);

        var reader = com.ExecuteReader();
        if(reader.Read()){
            taxonym = new Taxonym(){
                id = taxonymID,
                canonParentID = reader.IsDBNull(1) ? null : (TaxonymID)reader.GetInt32(1),
                canonAlias = reader.GetString(2)
            };
        }

        reader.Close();
        com.Dispose();

        return taxonym;
    }

    public TaxonymID[] ReadTaxonymChildren(TaxonymID taxonymID, SqliteTransaction? transaction = null){
        var taxonymIDs = new List<TaxonymID>();

        var com = db.CreateCommand();
        com.CommandText = $@"
            select child_id
            from taxonym_parent
            where taxonym_parent.parent_id = @parent_id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("parent_id", taxonymID);

        var reader = com.ExecuteReader();
        while(reader.Read()){
            taxonymIDs.Add((TaxonymID)reader.GetInt32(0));
        }

        reader.Close();
        com.Dispose();

        return taxonymIDs.ToArray();
    }

    public TaxonymID[] ReadTaxonymParents(TaxonymID taxonymID, SqliteTransaction? transaction = null){
        var taxonymIDs = new List<TaxonymID>();

        var com = db.CreateCommand();
        com.CommandText = $@"
            select parent_id
            from taxonym_parent
            where taxonym_parent.child_id = @child_id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("child_id", taxonymID);

        var reader = com.ExecuteReader();
        while(reader.Read()){
            taxonymIDs.Add((TaxonymID)reader.GetInt32(0));
        }

        reader.Close();
        com.Dispose();

        return taxonymIDs.ToArray();
    }

    public string[] ReadTaxonymAliases(TaxonymID taxonymID, SqliteTransaction? transaction = null){
        var aliases = new List<string>();

        var com = db.CreateCommand();
        com.CommandText = @"
            select alias
            from taxonym_alias
            where taxonym_id = @taxonym_id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@taxonym_id", taxonymID);

        var reader = com.ExecuteReader();
        while(reader.Read()){
            aliases.Add(reader.GetString(0));
        }

        reader.Close();
        com.Dispose();

        return aliases.ToArray();
    }

    public TagID[] QueryTags(string clause, SqliteTransaction? transaction = null){

        var tags = new List<TagID>();

        var com = db.CreateCommand();
        com.CommandText = $"select id from tag {clause}";
        com.Transaction = transaction;
        
        var reader = com.ExecuteReader();
        while(reader.Read()){
            tags.Add((TagID)reader.GetInt32(0));
        }

        reader.Close();
        com.Dispose();

        return tags.ToArray();
    }

    public Tag? ReadTag(TagID tagID, SqliteTransaction? transaction = null){

        var com = db.CreateCommand();
        com.CommandText = @"
            select *
            from tag
            where id = @id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@id", tagID);

        Tag? tag = null;

        var reader = com.ExecuteReader();
        if(reader.Read()){
            tag = new Tag(){
                id = tagID,
                taxonymID = (TaxonymID)reader.GetInt32(1)
            };
        }

        return tag;
    }

    public TagID? ReadTagID(TaxonymID taxonymID, SqliteTransaction? transaction = null){

        var com = db.CreateCommand();
        com.CommandText = @"
            select id
            from tag
            where taxonym_id = @taxonym_id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@taxonym_id", taxonymID);

        var reader = com.ExecuteReader();
        if(reader.Read()){
            return (TagID)reader.GetInt32(0);
        }

        return null;
    }

    public TagID[] ReadTagConsequents(TagID tagID, SqliteTransaction? transaction = null){
        var consequents = new List<TagID>();

        var com = db.CreateCommand();
        com.CommandText = @"
            select consequent_id
            from tag_implication
            where antecedent_id = @id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@id", tagID);
        
        var reader = com.ExecuteReader();
        while(reader.Read()){
            consequents.Add((TagID)reader.GetInt32(0));
        }

        reader.Close();
        com.Dispose();

        return consequents.ToArray();
    }

    public TagID[] ReadTagAntecedents(TagID tagID, SqliteTransaction? transaction = null){
        var antecedents = new List<TagID>();

        var com = db.CreateCommand();
        com.CommandText = @"
            select antecedent_id
            from tag_implication
            where consequent_id = @id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@id", tagID);
        
        var reader = com.ExecuteReader();
        while(reader.Read()){
            antecedents.Add((TagID)reader.GetInt32(0));
        }

        reader.Close();
        com.Dispose();

        return antecedents.ToArray();
    }

    public TagID[] ReadDocumentTags(DocumentID documentID, SqliteTransaction? transaction = null){
        var tagIDs = new List<TagID>();

        var com = db.CreateCommand();
        com.CommandText = @"
            select tag_id
            from document_tag
            where document_id = @document_id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@document_id", documentID);
        
        var reader = com.ExecuteReader();
        while(reader.Read()){
            tagIDs.Add((TagID)reader.GetInt32(0));
        }

        reader.Close();
        com.Dispose();

        return tagIDs.ToArray();
    }

    public int CountTagDocuments(TagID tagID, SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
        com.CommandText = @"
            select count(document_id)
            from document_tag
            where tag_id = @tag_id;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@tag_id", tagID);

        var reader = com.ExecuteReader();
        if(reader.Read()){
            return reader.GetInt32(0);
        }

        return 0;
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