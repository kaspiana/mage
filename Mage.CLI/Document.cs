namespace Mage.Engine;

public struct Document {
    public string hash;
    public DocumentID? id;
    public string format;
    public DateTime ingestDate;
    public string? comment;
    public byte[]? thumbnail;
}

public struct DocumentTag {
    public DocumentID? id;
}