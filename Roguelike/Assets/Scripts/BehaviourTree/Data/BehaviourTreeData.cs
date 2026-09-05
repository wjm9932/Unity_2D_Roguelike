using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Behaviour Tree Data")]

public class BehaviourTreeData : ScriptableObject
{
    [field: SerializeField, HideInInspector] public string RootId { get; set; }


#if UNITY_EDITOR
    private void OnValidate()
    {
        var path = UnityEditor.AssetDatabase.GetAssetPath(this);
        if (!string.IsNullOrEmpty(path))
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name != fileName)
            {
                name = fileName;
            }
        }
    }
#endif
}
