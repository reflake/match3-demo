using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public int Row { get; set; }
    public int Column { get; set; }

    public Cell Top { get; set; }
    public Cell Bottom { get; set; }
    public Cell Left { get; set; }
    public Cell Right { get; set; }

    public Gem Gem { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
