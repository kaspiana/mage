namespace Mage.Engine;

public enum ObjectRefType
{
    ID,
    Name,
    Binding,
    ViewIndex
}

public abstract class ObjectRef
{
    public ObjectRefType refType;

    public abstract object Resolve(Archive archive, ObjectType objType);

    public static ObjectRef Parse(string objectRefStr)
    {
        if ("0123456789".Contains(objectRefStr[0]))
            return new ObjectRef_ID(int.Parse(objectRefStr));

        if (objectRefStr == ".")
            return new ObjectRef_Binding(objectRefStr);

        var slashIndex = objectRefStr.IndexOf('/');
        if (slashIndex != -1)
        {
            var viewStr = objectRefStr[0..slashIndex];
            var indexStr = objectRefStr[(slashIndex + 1)..];
            return new ObjectRef_ViewIndex(
                Parse(viewStr),
                int.Parse(indexStr)
            );
        }

        return new ObjectRef_Name(objectRefStr);
    }

    public static object ParseResolve(Archive archive, string objectRefStr, ObjectType objType){
        return Parse(objectRefStr).Resolve(archive, objType);
    }

    public static DocumentID? ResolveDocument(Archive archive, string objectRefStr){
        return (DocumentID)ParseResolve(archive, objectRefStr, ObjectType.Document);
    }

    public static string? ResolveView(Archive archive, string objectRefStr){
        return (string?)ParseResolve(archive, objectRefStr, ObjectType.View);
    }

    public static TaxonymID? ResolveTaxonym(Archive archive, string objectRefStr){
        return (TaxonymID)ParseResolve(archive, objectRefStr, ObjectType.Taxonym);
    }
}

public class ObjectRef_ID : ObjectRef
{
    public int id;

    public ObjectRef_ID(int _id)
    {
        id = _id;
    }

    override public string ToString()
    {
        return $"id({id})";
    }

    override public object? Resolve(Archive archive, ObjectType objType)
    {
        return id;
    }
}

public class ObjectRef_Name : ObjectRef
{
    public string name;

    public ObjectRef_Name(string _name)
    {
        name = _name;
    }

    override public string ToString()
    {
        return $"namedObject({name})";
    }

    override public object? Resolve(Archive archive, ObjectType objType)
    {
        switch(objType){
            case ObjectType.View:
                return name;

            case ObjectType.Document:
                return archive.GetDocumentID(name);

            case ObjectType.Tag:
                return null; // TODO

            case ObjectType.Taxonym:
                return null; // TODO

            case ObjectType.Series:
                return null;
        }

        return null;
    }
}

public class ObjectRef_Binding : ObjectRef
{
    public string bindName;

    public ObjectRef_Binding(string _bindName)
    {
        bindName = _bindName;
    }

    override public string ToString(){
        return $"boundObject({bindName})";
    }

    override public object Resolve(Archive archive, ObjectType objType)
    {
        return ParseResolve(
            archive,
            archive.BindingGet(objType),
            objType
        );
    }
}

public class ObjectRef_ViewIndex : ObjectRef
{
    public ObjectRef view;
    public int index;

    public ObjectRef_ViewIndex(ObjectRef _view, int _index)
    {
        view = _view;
        index = _index;
    }

    override public string ToString(){
        return $"viewIndexedObject({view}, {index})";
    }

    override public object Resolve(Archive archive, ObjectType objType)
    {
        var viewName = (string)view.Resolve(archive, ObjectType.View);
        var viewData = (View)archive.ViewGet(viewName);
        return (DocumentID?)viewData.documents[index];
    }
}

public enum ObjectListRefType
{
    Array,
    View
}

public abstract class ObjectListRef
{
    public ObjectListRefType refType;

    public abstract object[] Resolve(Archive archive, ObjectType objType);
}

public class ObjectListRef_Array : ObjectListRef
{
    public ObjectRef[] refs;

    override public object[] Resolve(Archive archive, ObjectType objType)
    {
        return refs.Select((r) => r.Resolve(archive, objType)).ToArray();
    }
}

public class ObjectListRef_View : ObjectListRef
{
    public ObjectRef view;

    override public object[] Resolve(Archive archive, ObjectType objType)
    {
        return []; // TODO: Real implementation
    }
}