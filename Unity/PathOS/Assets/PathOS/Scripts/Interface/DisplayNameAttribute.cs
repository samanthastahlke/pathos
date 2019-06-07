using UnityEngine;

public class DisplayNameAttribute : PropertyAttribute
{
    public string displayName { get; private set; }

    public DisplayNameAttribute(string displayName)
    {
        this.displayName = displayName;
    }
}
