namespace Mage.Engine;

public struct Document {
    public string hash;
    public DocumentID? id;
    public string fileName;
    public string extension;
    public DateTime ingestTimestamp;
    public string? comment;
}