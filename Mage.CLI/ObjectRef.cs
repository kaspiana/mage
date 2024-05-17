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

    public abstract int Resolve(Archive archive, ObjectType objType);

    public static ObjectRef Parse(string objectRefStr)
    {
        if (objectRefStr[0] == '@')
            return new ObjectRef_ID(int.Parse(objectRefStr[1..]));

        if (Archive.BINDING_KEYS.Contains(objectRefStr))
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

    override public int Resolve(Archive archive, ObjectType objType)
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

    override public int Resolve(Archive archive, ObjectType objType)
    {
        return 0; // TODO
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

    override public int Resolve(Archive archive, ObjectType objType)
    {
        return 0; // TODO
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

    override public int Resolve(Archive archive, ObjectType objType)
    {
        return 0; // TODO
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

    public abstract int[] Resolve(Archive archive, ObjectType objType);
}

public class ObjectListRef_Array : ObjectListRef
{
    public ObjectRef[] refs;

    override public int[] Resolve(Archive archive, ObjectType objType)
    {
        return refs.Select((r) => r.Resolve(archive, objType)).ToArray();
    }
}

public class ObjectListRef_View : ObjectListRef
{
    public ObjectRef view;

    override public int[] Resolve(Archive archive, ObjectType objType)
    {
        return []; // TODO: Real implementation
    }
}