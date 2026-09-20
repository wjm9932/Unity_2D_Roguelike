using UnityEngine;

namespace CustomAttribute.Runtime
{
    public class ShowIfAttribute : PropertyAttribute
    {
        public string FlagField { get; }
        public int RequiredFlag { get; }

        public ShowIfAttribute(string flagField, int requiredFlag)
        {
            FlagField = flagField;
            RequiredFlag = requiredFlag;
        }
    }
}
