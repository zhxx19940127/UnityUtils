using System.Collections.Generic;
using UnityEngine;


public static class MessageHelper
{
    public static List<string> GetFilterMessageName()
    {
        var t = UnityEngine.Resources.Load<TextAsset>("MessageFilter");
        if (t == null)
        {
            return new List<string>();
        }

        string s = t.text;
        var allFilter = new List<string>();
        var ss = s.Split(',');
        for (int i = 0; i < ss.Length; i++)
        {
            allFilter.Add(ss[i]);
        }

        return allFilter;
    }
}