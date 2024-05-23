namespace Mage.Engine;

public struct Document {
    public string hash;
    public DocumentID? id;
    public string fileName;
    public string fileExt;
    public int fileSize;
    public DateTime ingestedAt;
    public string? comment;
    public bool isDeleted;
}