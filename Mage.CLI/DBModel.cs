using System.Diagnostics;
using System.Resources;
using Microsoft.Data.Sqlite;

namespace Mage.Engine;

public partial class DBModel {

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
        db.Close();
        db.Dispose();
    }

    public void RunResourceScript(string resourcePath){
        var setupCommand = db.CreateCommand();
		setupCommand.CommandText = ResourceLoader.Load($"Resources.DB.{resourcePath}");
		setupCommand.ExecuteNonQuery();
        setupCommand.Dispose();
    }

}

// Reading
public partial class DBModel {

    public long ReadLastInsertRowID(SqliteTransaction? transaction = null){
        var com = new SqliteCommand("select last_insert_rowid()", db, transaction);
		long lastRowID = (long)com.ExecuteScalar()!;
		com.Dispose();

        return lastRowID;
    }

    public DocumentID[] QueryDocuments(string clause, SqliteTransaction? transaction = null){

        var documents = new List<DocumentID>();

        var com = db.CreateCommand();
		com.CommandText = $"select ID from Document {clause}";
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
		com.CommandText = $"select * from Document where ID = @ID;";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("ID", documentID);
		
        var reader = com.ExecuteReader();
        if(reader.Read()){
            var ingestTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            ingestTimestamp = ingestTimestamp.AddSeconds(reader.GetInt32(4)).ToLocalTime();

            document = new Document(){
                hash = reader.GetString(1),
                id = documentID,
                fileName = reader.GetString(2),
                extension = reader.GetString(3),
                ingestTimestamp = ingestTimestamp,
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
		com.CommandText = $"select Hash from Document where ID = @ID;";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("ID", documentID);

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
		com.CommandText = $"select ID from Document where Hash = @Hash;";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("Hash", documentHash);

        var reader = com.ExecuteReader();
        if(reader.Read()){
            documentID = (DocumentID)reader.GetInt32(0);
        }

        reader.Close();
        com.Dispose();
        
        return documentID;

    }

    public Taxonym? ReadTaxonym(TaxonymID taxonymID, SqliteTransaction? transaction = null){
        Taxonym? taxonym = null;

        var com = db.CreateCommand();
        com.CommandText = $"select * from Taxonym where ID = @ID";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("ID", taxonymID);

        var reader = com.ExecuteReader();
        if(reader.Read()){
            taxonym = new Taxonym(){
                canonicalParentID = reader.IsDBNull(1) ? null : (TaxonymID)reader.GetInt32(1),
                canonicalAlias = reader.GetString(2)
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
            select ChildID
            from TaxonymParent
            where TaxonymParent.ParentID = @ParentID;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("ParentID", taxonymID);

        var reader = com.ExecuteReader();
        while(reader.Read()){
            taxonymIDs.Add((TaxonymID)reader.GetInt32(0));
        }

        reader.Close();
        com.Dispose();

        return taxonymIDs.ToArray();
    }

}

// Insertion
public partial class DBModel {

    public DocumentID InsertDocument(Document document, SqliteTransaction? transaction = null){

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
            );
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@Hash", document.hash);
        com.Parameters.AddWithValue("@FileName", document.fileName);
        com.Parameters.AddWithValue("@Extension", document.extension);
        com.Parameters.AddWithValue("@IngestTimestamp", ((DateTimeOffset)(document.ingestTimestamp)).ToUnixTimeSeconds());
        com.Parameters.AddWithValue("@Comment", document.comment is null ? System.DBNull.Value : document.comment);
        com.ExecuteNonQuery();
        com.Dispose();

        var documentID = (DocumentID)ReadLastInsertRowID();
        return documentID;

    }

    public void InsertTaxonymAlias(TaxonymID taxonymID, string alias, SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
		com.CommandText = $@"
            insert into TaxonymAlias
            (TaxonymID, Alias)
            values (@TaxonymID, @Alias);
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@TaxonymID", taxonymID);
        com.Parameters.AddWithValue("@Alias", alias);
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public void InsertTaxonymParent(TaxonymID childID, TaxonymID parentID, SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
		com.CommandText = $@"
            insert into TaxonymParent
            (ChildID, ParentID)
            values (@ChildID, @ParentID);
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@ChildID", childID);
        com.Parameters.AddWithValue("@ParentID", parentID);
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public TaxonymID InsertTaxonym(Taxonym taxonym){

        var transaction = db.BeginTransaction();

        var com = db.CreateCommand();
        com.CommandText = $@"
            insert into Taxonym
            (CanonicalParentID, CanonicalAlias)
            values (@CanonicalParentID, @CanonicalAlias)
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@CanonicalParentID", ((object?)taxonym.canonicalParentID) ?? DBNull.Value);
		com.Parameters.AddWithValue("@CanonicalAlias", taxonym.canonicalAlias);
		com.ExecuteNonQuery();
		com.Dispose();

        var taxonymID = (TaxonymID)ReadLastInsertRowID(transaction);

        InsertTaxonymAlias(taxonymID, taxonym.canonicalAlias, transaction);
        if(taxonym.canonicalParentID is not null)
            InsertTaxonymParent(taxonymID, (TaxonymID)taxonym.canonicalParentID, transaction);

        transaction.Commit();
        transaction.Dispose();

        return taxonymID;
    }

}


// Deletion
public partial class DBModel {

    public void DeleteDocument(DocumentID documentID, SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
		com.CommandText = $@"
            delete from Document
            where ID = @ID;
        ";
        com.Transaction = transaction;
        com.Parameters.AddWithValue("@ID", documentID);
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public void DeleteAllDocuments(SqliteTransaction? transaction = null){
        var com = db.CreateCommand();
		com.CommandText = $@"delete from Document;";
        com.Transaction = transaction;
        com.ExecuteNonQuery();
        com.Dispose();
    }

}