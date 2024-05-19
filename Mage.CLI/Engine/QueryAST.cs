namespace Mage.Engine.AST;



public abstract class QueryNode {
    public abstract string ToSQL(Archive archive);
}
public class QueryNodeAll : QueryNode {

    public override string ToString()
    {
        return $"(all)";
    }

    public override string ToSQL(Archive archive){
        return $"{Query.documentTagNormalised}";
    }
}
public class QueryNodeNone : QueryNode {
    public override string ToString()
    {
        return $"(none)";
    }

    public override string ToSQL(Archive archive)
    {
        return $"select id from document where 1=0";
    }
}
public class QueryNodeTagExplicit : QueryNode {
    public TagID tagID;

    public override string ToString()
    {
        return $"'/{tagID}'";
    }

    public override string ToSQL(Archive archive)
    {
        return $"select document_id id from document_tag where tag_id = {tagID}";
    }
}
public class QueryNodeTag : QueryNode {
    public string tag;

    public override string ToString()
    {
        return $"'{tag}'";
    }

    public override string ToSQL(Archive archive)
    {
        if(tag.Contains('*')){

            IEnumerable<TagID> tagIDs = archive.TagFindFuzzy(tag);
            var antecedents = tagIDs.Select((tagID) => archive.TagGetAntecedents(tagID).Prepend(tagID)).SelectMany(x => x);
            return (new QueryNodeDisjunction(){
                args = antecedents.Select((id) 
                    => new QueryNodeTagExplicit(){tagID = id})
            }).ToSQL(archive);

        } else {

            var tagID = (TagID)archive.TagFind(tag);
            var antecedents = archive.TagGetAntecedents(tagID);
            return (new QueryNodeDisjunction(){
                args = antecedents.Prepend(tagID).Select((id) 
                    => new QueryNodeTagExplicit(){tagID = id})
            }).ToSQL(archive);

        }
    }
}
public class QueryNodeNegation : QueryNode {
    public QueryNode arg;

    public override string ToString()
    {
        return $"(not {arg})";
    }

    public override string ToSQL(Archive archive){
        var a = Query.tempTableIndex++;
        var b = Query.tempTableIndex++;
        return $"{Query.documentTagNormalised} where id not in (select id from ({arg.ToSQL(archive)}))";
    }
}
public abstract class QueryNodeJunction : QueryNode {
    public IEnumerable<QueryNode> args;
}
public class QueryNodeConjunction : QueryNodeJunction {

    public override string ToString()
    {
        return $"(and {(string.Join(' ', args))})";
    }

    public override string ToSQL(Archive archive)
    {
        string? lhs;
        string? rhs;

        if(args.Count() == 1){
            return args.First().ToSQL(archive);
        } else if(args.Count() == 2){
            lhs = args.First().ToSQL(archive);
            rhs = args.Skip(1).First().ToSQL(archive);
        } else {
            lhs = args.First().ToSQL(archive);
            rhs = (new QueryNodeConjunction(){
                args = args.Skip(1)
            }).ToSQL(archive);
        }
        var a = Query.tempTableIndex++;
        var b = Query.tempTableIndex++;
        var sql = $"select t{a}.ID from ({lhs}) t{a} inner join ({rhs}) t{b} on t{a}.ID = t{b}.ID";
        return sql;
    }
}
public class QueryNodeDisjunction : QueryNodeJunction {

    public override string ToString()
    {
        return $"(or {(string.Join(' ', args))})";
    }

    public override string ToSQL(Archive archive){
        return string.Join(" union ", args.Select((l) => l.ToSQL(archive)));
    }
}

public class Query {
    public QueryNode root;
    public static int tempTableIndex = 0;
    public static string documentTagNormalised = "select id from document";

    public DocumentID[] GetResults(Archive archive){

        archive.db.EnsureConnected();
        var db = archive.db.db;

        var documents = new List<DocumentID>();

        var com = db.CreateCommand();
		com.CommandText = @$"select distinct id from ({root.ToSQL(archive)});";
		
        Console.WriteLine("SQL: " + com.CommandText);

        var reader = com.ExecuteReader();
        while(reader.Read()){
            documents.Add((DocumentID)reader.GetInt32(0));
        }

        reader.Close();
        com.Dispose();

        return documents.ToArray();

    }
}