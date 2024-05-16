namespace Mage.Engine;

public struct Taxonym {
    public TaxonymID? id;
    public TaxonymID? canonicalParentID;
	public string canonicalAlias;
}

public struct TaxonymAlias {
    public TaxonymID id;
    public string alias;
}