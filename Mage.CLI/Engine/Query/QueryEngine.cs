using Sprache;

namespace Mage.Engine;

public static class QueryParser {

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
        }
        return node;
    }

    static readonly Parser<AST.QueryNodeJunction> NodeJunction =
        from inner in Sprache.Parse.ChainOperator(
            Sprache.Parse.String("OR").Text().Token()
            .Or(Sprache.Parse.String("AND").Text().Token()
                .Or(Sprache.Parse.WhiteSpace.AtLeastOnce().Text())),
            Sprache.Parse.Ref(()=>InnerNode),
            (op, lhs, rhs) => {
                if(op == "OR"){
                    return (AST.QueryNode)(new AST.QueryNodeDisjunction(){
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