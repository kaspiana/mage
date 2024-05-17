namespace Mage.Engine;

public enum ViewType {
    Main,
    User,
    In,
    Open,
    Query,
    Stash
}

public struct View {
    public string? name;
    public ViewType viewType;
    public DocumentID?[] documents;
}