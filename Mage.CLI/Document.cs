namespace Mage.Engine;

public struct Document {
    public string hash;
    public DocumentID? id;
    public string extension;
    public DateTime ingestTimestamp;
    public string? comment;
}

public struct DocumentTag {
    public DocumentID? id;
}