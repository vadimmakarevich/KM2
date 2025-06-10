using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CatSortLevelDefinition
{
    [Serializable]
    public class ShelfData
    {
        public string side; // "Left" or "Right"
        public List<int> cats = new List<int>();
    }

    public List<ShelfData> shelves = new List<ShelfData>();
}
