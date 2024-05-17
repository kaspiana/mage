namespace Mage.Engine;

public enum ViewType {
    Main,
    User,
    In,
    Query,
    Stash
}

public struct View {
    public string? name;
    public ViewType viewType;
    public DocumentID[] documents;
}