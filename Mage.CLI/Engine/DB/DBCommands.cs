using System.Runtime.CompilerServices;

namespace Mage.Engine;

public static class DBCommands {

    public static class Select {

        public const string Document = "select * from document";
        public static string DocumentIDClause(string clause) => $"select id from document {clause}";
        public const string DocumentWherePK = "select * from document where id = @id";
        public const string DocumentWhereHash = "select * from document where hash = @hash";
        public const string DocumentHashWhereID = "select hash from document where id = @id";

        public const string Taxonym = "select * from taxonym";
        public static string TaxonymIDClause(string clause) => $"select id from taxonym {clause}";
        public const string TaxonymWherePK = "select * from taxonym where id = @id";
        public const string TaxonymRelationship = "select * from taxonym_parent";
        public const string TaxonymParentIDWhereID = "select parent_id from taxonym_parent where child_id = @child_id";
        public const string TaxonymChildIDWhereID = "select child_id from taxonym_parent where parent_id = @parent_id";
        public const string TaxonymAlias = "select * from taxonym_alias";
        public const string TaxonymAliasWhereID = "select alias from taxonym_alias where taxonym_id = @taxonym_id";


        public const string Tag = "select * from tag";
        public static string TagIDClause(string clause) => $"select id from tag {clause}";
        public const string TagWherePK = "select * from tag where id = @id";
        public const string TagIDWhereTaxonymID = "select id from tag where taxonym_id = @taxonym_id";
        public const string TagImplication = "select * from tag_implication";
        public const string TagAntecedentIDWhereID = "select antecedent_id from tag_implication where consequent_id = @consequent_id";
        public const string TagConsequentIDWhereID = "select consequent_id from tag_implication where antecedent_id = @antecedent_id";

        public const string DocumentTag = "select * from document_tag";
        public const string DocumentTagIDWhereDocumentID = "select tag_id from document_tag where document_id = @document_id";
        public const string TagDocumentIDWhereTagID = "select document_id from document_tag where tag_id = @tag_id";
    
    }

    public static class Count {

        public const string DocumentTagWhereTagID = "select count(*) from document_tag where tag_id = @tag_id";
        public const string DocumentTagWhereDocumentID = "select count(*) from document_tag where document_id = @document_id";
   
    }

    public static class Insert {

        public const string Document = "insert into document (hash, file_name, extension, ingested_at, comment) values (@hash, @file_name, @extension, @ingested_at, @comment)";
        public const string DocumentTag = "insert into document_tag (document_id, tag_id) values (@document_id, @tag_id)";
        public const string Tag = "insert into tag (taxonym_id) values (@taxonym_id)";
        public const string TagImplication = "insert into tag_implication (antecedent_id, consequent_id) values (@antecedent_id, @consequent_id)";
        public const string Taxonym = "insert into taxonym (canon_parent_id, canon_alias) values (@canon_parent_id, @canon_alias)";
        public const string TaxonymRelationship = "insert into taxonym_parent (child_id, parent_id) values (@child_id, @parent_id)";
        public const string TaxonymAlias = "insert into taxonym_alias (taxonym_id, alias) values (@taxonym_id, @alias)";

    }

    public static class Delete {

        public const string Document = "delete from document";
        public const string DocumentWherePK = "delete from document where id = @id";

        public const string Taxonym = "delete from taxonym";
        public const string TaxonymWherePK = "delete from taxonym where id = @id";

        public const string TaxonymRelationship = "delete from taxonym_parent";
        public const string TaxonymRelationshipWherePK = "delete from taxonym_parent child_id = @child_id and parent_id = @parent_id";
        public const string TaxonymRelationshipWhereEitherID = "delete from taxonym_parent where child_id = @id or parent_id = @id";
        public const string TaxonymRelationshipWhereChildID = "delete from taxonym_parent where child_id = @child_id";
        public const string TaxonymRelationshipWhereParentID = "delete from taxonym_parent where parent_id = @parent_id";

        public const string TaxonymAlias = "delete from taxonym_alias";
        public const string TaxonymAliasWherePK = "delete from taxonym_alias where taxonym_id = @taxonym_id and alias = @alias";
        public const string TaxonymAliasWhereTaxonymID = "delete from taxonym_alias where taxonym_id = @taxonym_id";

        public const string Tag = "delete from tag";
        public const string TagWherePK = "delete from tag where id = @id";

        public const string TagImplication = "delete from tag_implication";
        public const string TagImplicationWherePK = "delete from tag_implication where antecedent_id = @antecedent_id and consequent_id = @consequent_id";
        public const string TagImplicationWhereEitherID = "delete from tag_implication where antecedent_id = @id or consequent_id = @id";
        public const string TagImplicationWhereAntecedentID = "delete from tag_implication where antecedent_id = @antecedent_id";
        public const string TagImplicationWhereConsequentID = "delete from tag_implication where consequent_id = @consequent_id";

        public const string DocumentTag = "delete from document_tag";
        public const string DocumentTagWherePK = "delete from document_tag where document_id = @document_id and tag_id = @tag_id";
        public const string DocumentTagWhereDocumentID = "delete from document_tag where document_id = @document_id";
        public const string DocumentTagWhereTagID = "delete from document_tag where tag_id = @tag_id";


    }

}