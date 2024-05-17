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

    public long ReadLastInsertRowID(){
        var com = new SqliteCommand("select last_insert_rowid()", db);
		long lastRowID = (long)com.ExecuteScalar()!;
		com.Dispose();

        return lastRowID;
    }

    public DocumentID[] QueryDocuments(string clause){

        var documents = new List<DocumentID>();

        var com = db.CreateCommand();
		com.CommandText = $"select ID from Document {clause}";
		
        var reader = com.ExecuteReader();
        while(reader.Read()){
            documents.Add((DocumentID)reader.GetInt32(0));
        }

        reader.Close();
        com.Dispose();

        return documents.ToArray();
    }

    public Document? ReadDocument(DocumentID documentID){

        Document? document = null;

        var com = db.CreateCommand();
		com.CommandText = $"select * from Document where ID = @ID;";
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

    public string? ReadDocumentHash(DocumentID documentID){

        string? documentHash = null;

        var com = db.CreateCommand();
		com.CommandText = $"select Hash from Document where ID = @ID;";
        com.Parameters.AddWithValue("ID", documentID);

        var reader = com.ExecuteReader();
        if(reader.Read()){
            documentHash = reader.GetString(0);
        }

        reader.Close();
        com.Dispose();
        
        return documentHash;
    }

    public DocumentID? ReadDocumentID(string documentHash){

        DocumentID? documentID = null;

        var com = db.CreateCommand();
		com.CommandText = $"select ID from Document where Hash = @Hash;";
        com.Parameters.AddWithValue("Hash", documentHash);

        var reader = com.ExecuteReader();
        if(reader.Read()){
            documentID = (DocumentID)reader.GetInt32(0);
        }

        reader.Close();
        com.Dispose();
        
        return documentID;

    }

}

// Insertion
public partial class DBModel {

    public DocumentID InsertDocument(Document document){

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

}


// Deletion
public partial class DBModel {

    public void DeleteDocument(DocumentID documentID){
        var com = db.CreateCommand();
		com.CommandText = $@"
            delete from Document
            where ID = @ID;
        ";
        com.Parameters.AddWithValue("@ID", documentID);
        com.ExecuteNonQuery();
        com.Dispose();
    }

    public void DeleteAllDocuments(){
        var com = db.CreateCommand();
		com.CommandText = $@"delete from Document;";
        com.ExecuteNonQuery();
        com.Dispose();
    }

}