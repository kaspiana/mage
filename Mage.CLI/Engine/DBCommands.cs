namespace Mage.Engine;

public static class DBCommands {

    public static class Select {

        public const string Document = "select * from document";
        public const string DocumentWhereID = "select * from document where id = @id";
        public const string DocumentWhereHash = "select * from document where hash = @hash";
        public const string DocumentHashWhereID = "select hash from document where id = @id";

        public const string Taxonym = "select * from taxonym";
        public const string TaxonymWhereID = "select * from taxonym where id = @id";
        public const string TaxonymRelationship = "select * from taxonym_parent";
        public const string TaxonymParentIDWhereID = "select parent_id from taxonym_parent where child_id = @child_id";
        public const string TaxonymChildIDWhereID = "select child_id from taxonym_parent where parent_id = @parent_id";
        public const string TaxonymAlias = "select * from taxonym_alias";
        public const string TaxonymAliasWhereID = "select alias from taxonym_alias where taxonym_id = @taxonym_id";


        public const string Tag = "select * from tag";
        public const string TagWhereID = "select * from tag where id = @id";
        public const string TagIDWhereTaxonymID = "select id from tag where taxonym_id = @taxonym_id";
        public const string TagImplication = "select * from tag_implication";
        public const string TagAntecedentIDWhereID = "select antecedent_id from tag_implication where consequent_id = @consequent_id";
        public const string TagConsequentIDWhereID = "select consequent_id from tag_implication where antecedent_id = @antecedent_id";

        public const string DocumentTag = "select * from document_tag";
        public const string DocumentTagWhereDocumentID = "select * from document_tag where document_id = @document_id";
        public const string DocumentTagWhereTagID = "select * from document_tag where tag_id = @tag_id";

    }

}