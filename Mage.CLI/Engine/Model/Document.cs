namespace Mage.Engine;

public enum MediaType {
    Binary,
    Text,
    Image,
    Animation,
    Audio,
    Video
}

public struct Document {
    public string hash;
    public DocumentID? id;
    public string fileName;
    public string fileExt;
    public int fileSize;
    public MediaType mediaType;
    public DateTime addedAt;
    public DateTime updatedAt;
    public string? comment;
    public bool isDeleted;
}