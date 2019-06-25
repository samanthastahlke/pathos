using UnityEngine;

/*
DisplayNameAttribute.cs
DisplayNameAttribute (c) Nine Penguins (Samantha Stahlke) 2019
*/


public class DisplayNameAttribute : PropertyAttribute
{
    public string displayName { get; private set; }

    public DisplayNameAttribute(string displayName)
    {
        this.displayName = displayName;
    }
}
