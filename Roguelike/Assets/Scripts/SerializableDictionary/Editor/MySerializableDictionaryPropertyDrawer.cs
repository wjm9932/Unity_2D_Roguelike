using UnityEditor;

[CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
[CustomPropertyDrawer(typeof(SerializableDictionary<,,>))]
[CustomPropertyDrawer(typeof(SerializableDictionaryBase<,,>))]
public class AnySerializableDictionaryPropertyDrawers : SerializableDictionaryPropertyDrawer
{
}

[CustomPropertyDrawer(typeof(SerializableDictionary.Storage<>))]
public class AnySerializableDictionaryStoragePropertyDrawer : SerializableDictionaryStoragePropertyDrawer
{

}
