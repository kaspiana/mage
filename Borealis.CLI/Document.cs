namespace Borealis.Engine;

public struct Document {
    public string hash;
    public DocumentID? id;
    public DateTime ingestDate;
    public string? comment;

    public Document(
        string _hash, 
        DocumentID? _id, 
        DateTime _ingestDate, 
        string? _comment
    ){
        hash = _hash;
        id = _id;
        ingestDate = _ingestDate;
        comment = _comment;
    }
}