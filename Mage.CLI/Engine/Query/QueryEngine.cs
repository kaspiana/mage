using Mage.Engine.AST;
using Sprache;

namespace Mage.Engine;

public static class QueryParser {

    static readonly Parser<string> DoubleQuotedString =
        from lquote in Sprache.Parse.Char('"')
        from content in
            Sprache.Parse.String("\\\"").Text()
            .Or(Sprache.Parse.String("\\").Text())
            .Or(Sprache.Parse.String("\\\\").Text())
            .Or(Sprache.Parse.AnyChar
                .Except(Sprache.Parse.String("\\\"").Text())
                .Except(Sprache.Parse.Char('"'))
                .Many()
                .Text()
            )
        from rquote in Sprache.Parse.Char('"')
        select $"\"{content}\"";

    static readonly Parser<string> SingleQuotedString =
        from lquote in Sprache.Parse.Char('\'')
        from content in
            Sprache.Parse.String("\\\"").Text()
            .Or(Sprache.Parse.String("\\").Text())
            .Or(Sprache.Parse.String("\\\\").Text())
            .Or(Sprache.Parse.AnyChar
                .Except(Sprache.Parse.String("\\'").Text())
                .Except(Sprache.Parse.Char('\''))
                .Many()
                .Text()
            )
        from rquote in Sprache.Parse.Char('\'')
        select $"'{content}'";

    static QueryParameterOperator ParseParameterOperator(string op){
        switch(op){
            case "=": return QueryParameterOperator.Equals; break;
            case "!=": return QueryParameterOperator.NotEquals; break;
            case ">": return QueryParameterOperator.Greater; break;
            case ">=": return QueryParameterOperator.GreaterOrEquals; break;
            case "<": return QueryParameterOperator.Lesser; break;
            case "<=": return QueryParameterOperator.LesserOrEquals; break;
            case "like": return QueryParameterOperator.Like; break;
        }
        return QueryParameterOperator.Equals;
    }

    static readonly Parser<AST.QueryNodeMetaTag> NodeMetaTag =
        from tag in 
            Sprache.Parse.String("id").Text()
            .Or(Sprache.Parse.String("hash").Text())
            .Or(Sprache.Parse.String("file_name").Text())
            .Or(Sprache.Parse.String("extension").Text())
            .Or(Sprache.Parse.String("ingested_at").Text())
            .Or(Sprache.Parse.String("comment").Text())
        from lparen in Sprache.Parse.Char('(')
        from op in Sprache.Parse.Optional(
            Sprache.Parse.String("=").Text()
            .Or(Sprache.Parse.String("!=").Text())
            .Or(Sprache.Parse.String(">").Text())
            .Or(Sprache.Parse.String(">=").Text())
            .Or(Sprache.Parse.String("<").Text())
            .Or(Sprache.Parse.String("<=").Text())
            .Or(Sprache.Parse.String("like").Text())
        ).Token()
        from param in 
            Sprache.Parse.Numeric.AtLeastOnce().Text().Token()
            .Or(SingleQuotedString.Token())
        from rparen in Sprache.Parse.Char(')')
        select new QueryNodeMetaTag(){
            tag = tag,
            op = ParseParameterOperator(op.GetOrElse("")),
            param = param
        };

    static readonly Parser<string> Tag =
        Sprache.Parse.LetterOrDigit.XOr(Sprache.Parse.Chars('_', ':', '*', '!')).Many().Text();

    static readonly Parser<AST.QueryNodeTag> NodeTag =
        from tag in Tag
        select new AST.QueryNodeTag(){ tag = tag };

    static readonly Parser<AST.QueryNodeNegation> NodeNegation =
        from minus in Sprache.Parse.Char('-')
        from arg in Sprache.Parse.Ref(()=>InnerNode)
        select new AST.QueryNodeNegation(){ arg = arg };

    static AST.QueryNodeJunction NodeToJunction(AST.QueryNode node) {
        if(node is AST.QueryNodeJunction)
            return (AST.QueryNodeJunction)node;
        else
            return new AST.QueryNodeDisjunction(){
                args = [node]
            };
    }

    static AST.QueryNodeJunction JunctionFlatten(AST.QueryNodeJunction node){
        if(node is AST.QueryNodeDisjunction){
            node.args = node.args.SelectMany(x => {
                if(x is AST.QueryNodeDisjunction){
                    return JunctionFlatten((AST.QueryNodeDisjunction)x).args;
                } else {
                    return [x];
                }
            });
        } else if(node is AST.QueryNodeConjunction){
            node.args = node.args.SelectMany(x => {
                if(x is AST.QueryNodeConjunction){
                    return JunctionFlatten((AST.QueryNodeConjunction)x).args;
                } else {
                    return [x];
                }
            });
        } else if(node is AST.QueryNodeExclusiveDisjunction){
            node.args = node.args.SelectMany(x => {
                if(x is AST.QueryNodeExclusiveDisjunction){
                    return JunctionFlatten((AST.QueryNodeExclusiveDisjunction)x).args;
                } else {
                    return [x];
                }
            });
        }
        return node;
    }

    static readonly Parser<AST.QueryNodeJunction> NodeJunction =
        from inner in Sprache.Parse.ChainOperator(
            Sprache.Parse.String("OR").Text().Token()
            .Or(Sprache.Parse.String("AND").Text().Token()
            .Or(Sprache.Parse.String("XOR").Text().Token())
            .Or(Sprache.Parse.WhiteSpace.AtLeastOnce().Text())),
            Sprache.Parse.Ref(()=>InnerNode),
            (op, lhs, rhs) => {
                if(op == "OR"){
                    return (AST.QueryNode)(new AST.QueryNodeDisjunction(){
                        args = [lhs, rhs]
                    });
                } else if (op == "XOR"){
                    return (AST.QueryNode)(new AST.QueryNodeExclusiveDisjunction(){ 
                        args = [lhs, rhs]
                    });
                } else {
                    return (AST.QueryNode)(new AST.QueryNodeConjunction(){ 
                        args = [lhs, rhs]
                    });
                }
            }
        )
        select JunctionFlatten(NodeToJunction(inner));

    static readonly Parser<AST.QueryNode> Group =
        from lparen in Sprache.Parse.Char('(').Token()
        from inner in Sprache.Parse.Ref(()=>Node)
        from rparen in Sprache.Parse.Char(')').Token()
        select inner;

    static readonly Parser<AST.QueryNode> InnerNode =
        Group
        .Or(NodeNegation.Select(n => (AST.QueryNode)n))
        .Or(NodeMetaTag.Select(n => (AST.QueryNode)n))
        .Or(NodeTag.Select(n => (AST.QueryNode)n));

    static readonly Parser<AST.QueryNode> Node =
        NodeJunction.Select(n => {
            if(n.args.Count() == 1){
                return n.args.First();
            }
            return (AST.QueryNode)n;
        })
        .Or(Group);

    public readonly static Parser<AST.Query> Query =
        from root in Node
        select new AST.Query(){ root = root };

    public static AST.Query Parse(string text){
        if(text.Trim() == "")
            return new AST.Query(){ root = new AST.QueryNodeAll() };

        return Query.Parse(text);
    }
}

public class QueryEngine {

    public Archive archive;

}