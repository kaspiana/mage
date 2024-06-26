namespace Mage.Engine.AST;

public enum QueryParameterOperator {
    Equals,
    NotEquals,
    Greater,
    GreaterOrEquals,
    Lesser,
    LesserOrEquals,
    Like
}

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
public class QueryNodeMetaTag : QueryNode {

    public string tag;
    public QueryParameterOperator op;
    public string param;

    private string OpToString(){
        return op switch{
            QueryParameterOperator.Equals => "=",
            QueryParameterOperator.NotEquals => "!=",
            QueryParameterOperator.Greater => ">",
            QueryParameterOperator.GreaterOrEquals => ">=",
            QueryParameterOperator.Lesser => "<",
            QueryParameterOperator.LesserOrEquals => "<=",
            QueryParameterOperator.Like => "like",
            _ => "="
        };
    }

    public override string ToString()
    {
        return $"(meta '{tag}' {OpToString()} {param})";
    }

    public override string ToSQL(Archive archive)
    {

        switch(tag){
            default:
                return $"select id from document where 1=0";
            break;

            // basic metatags
            case    "id" 
                    or "hash" 
                    or "file_name" 
                    or "file_ext"
                    or "added_at"
                    or "updated_at"
                    or "comment"
                    or "media_type":

                return $"select id from document where {tag} {OpToString()} {param}";

            break;

            // media metatags
            case    "width"
                    or "height"
                    or "duration":

                return $@"
select document_id id from (
select document_id, width, height, null duration from image_metadata
union select document_id, null width, null height, duration from audio_metadata
union select document_id, width, height, duration from video_metadata)
where {tag} {OpToString()} {param}
                ";

            break;
        }
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
        IEnumerable<TagID> tagIDs;


        if(tag.Contains('*')){
            tagIDs = archive.TagFindFuzzy(tag);
        } else {
            tagIDs = [(TagID)archive.TagFind(tag)];
        }

        return @$"
select document_id id from (document_tag
inner join (with recursive antecedent_tag (id) as (
values ({string.Join("),(", tagIDs)})
union select tag_implication.antecedent_id from 
antecedent_tag inner join tag_implication
on tag_implication.consequent_id = antecedent_tag.id)
select tag.id from antecedent_tag 
inner join tag
on antecedent_tag.id = tag.id) antecedent_tag
on document_tag.tag_id = antecedent_tag.id)";
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
        return string.Join(" intersect ", args.Select((l) => $"select * from ({l.ToSQL(archive)})"));

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
        return string.Join(" union ", args.Select((l) => $"select * from ({l.ToSQL(archive)})"));
    }
}

public class QueryNodeExclusiveDisjunction : QueryNodeJunction {
    public override string ToString()
    {
        return $"(xor {(string.Join(' ', args))})";
    }
    
    public override string ToSQL(Archive archive){
        var inDisj = string.Join(" union all ", args.Select((l) => $"select * from ({l.ToSQL(archive)})"));
        return $"select result.id id from (select *, row_number() over (order by id) rn from ({inDisj})) result group by result.id having count(*) = 1";
    }
}

public class Query {
    public QueryNode root;
    public static int tempTableIndex = 0;
    public static string documentTagNormalised = "select id from document";

    public DocumentID[] GetResults(Archive archive, string orderBy, bool asc, bool public_ = true){

        archive.db.EnsureConnected();
        var db = archive.db.db;

        var documents = new List<DocumentID>();

        var mainQuery = @$"
select distinct result.id from 
({root.ToSQL(archive)}) result
inner join {(public_ ? "public_": "")}document
on result.id = {(public_ ? "public_": "")}document.id
";

        var sortDirStr = asc ? "asc" : "desc";
        var sortedQuery = orderBy switch {
            var s when s.StartsWith("rank:") => @$"
                select id
                from
                    ({mainQuery}) 
					left join 
					(select 
						document_id, 
						rating 
					 from document_rating
					 where ranking_name = '{s[5..]}')
                    on id = document_id
                order by rating {sortDirStr}
            ",
            _ => $"{mainQuery} order by result.id {sortDirStr}"
        };

        var com = db.CreateCommand();
        com.CommandText = sortedQuery;
        
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