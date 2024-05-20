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
        IEnumerable<TagID> antecedents;

        if(tag.Contains('*')){

            IEnumerable<TagID> tagIDs = archive.TagFindFuzzy(tag);
            antecedents = tagIDs.Select((tagID) => archive.TagGetAntecedents(tagID).Prepend(tagID)).SelectMany(x => x);

        } else {

            var tagID = (TagID)archive.TagFind(tag);
            antecedents = archive.TagGetAntecedents(tagID).Prepend(tagID);

        }

        return $"select document_id id from document_tag where tag_id in ({string.Join(", ", antecedents)})";
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
        return string.Join(" intersect ", args.Select((l) => l.ToSQL(archive)));

        /*
        if(args.Count() == 0){
            return new QueryNodeNone().ToSQL(archive);
        }
        if(args.Count() == 1){
            return args.First().ToSQL(archive);
        } else {
            var head = args.First();
            var tail = args.Skip(1);
            var i = Query.tempTableIndex++;
            var j = Query.tempTableIndex++;

            return $"select t{i}.id from ({head.ToSQL(archive)}) t{i} {string.Join(" ", tail.Select((x) => {
                var s = $"inner join ({x.ToSQL(archive)}) t{j} on t{i}.id = t{j}.id";
                j = Query.tempTableIndex++;
                return s;
            }))}";
        }*/
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