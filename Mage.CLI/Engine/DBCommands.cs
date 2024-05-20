using System.Runtime.CompilerServices;

namespace Mage.Engine;

public static class DBCommands {

    public static class Select {

        public const string Document = "select * from document";
        public static string DocumentIDWhere(string clause) => $"select id from document where {clause}";
        public const string DocumentWhereID = "select * from document where id = @id";
        public const string DocumentWhereHash = "select * from document where hash = @hash";
        public const string DocumentHashWhereID = "select hash from document where id = @id";

        public const string Taxonym = "select * from taxonym";
        public static string TaxonymIDWhere(string clause) => $"select id from taxonym where {clause}";
        public const string TaxonymWhereID = "select * from taxonym where id = @id";
        public const string TaxonymRelationship = "select * from taxonym_parent";
        public const string TaxonymParentIDWhereID = "select parent_id from taxonym_parent where child_id = @child_id";
        public const string TaxonymChildIDWhereID = "select child_id from taxonym_parent where parent_id = @parent_id";
        public const string TaxonymAlias = "select * from taxonym_alias";
        public const string TaxonymAliasWhereID = "select alias from taxonym_alias where taxonym_id = @taxonym_id";


        public const string Tag = "select * from tag";
        public static string TagIDWhere(string clause) => $"select id from tag where {clause}";
        public const string TagWhereID = "select * from tag where id = @id";
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

}