using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Pair<T>
{
    public T a, b;
    public Pair(T a, T b)
    {
        this.a = a;
        this.b = b;
    }
}
public struct Pair<T1, T2>
{
    public T1 a;
    public T2 b;
    public Pair(T1 a, T2 b)
    {
        this.a = a;
        this.b = b;
    }
}
