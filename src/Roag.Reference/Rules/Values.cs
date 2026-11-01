using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Roag.Core.Rules;

// Values at the boundary of the mechanically ported deterministic rules. All rule
// bodies are compiled C# delegates; this is data/collection support, not a VM.
public delegate Results RuleFunction(Results arguments);
public readonly struct V : IEquatable<V>
{
    public readonly object? Data;
    public V(object? data) => Data = data;
    public static V Nil => default;
    public bool IsNil => Data == null;
    public V this[V key] => R.Get(this, key);
    public double Number => R.Num(this);
    public string Text => ToString();
    public static implicit operator V(string? v) => new(v);
    public static implicit operator V(double v) => new(v);
    public static implicit operator V(int v) => new((double)v);
    public static implicit operator V(long v) => new((double)v);
    public static implicit operator V(bool v) => new(v);
    public static implicit operator V(RuleTable v) => new(v);
    public static V operator +(V a,V b) => R.Num(a)+R.Num(b);
    public static V operator -(V a,V b) => R.Num(a)-R.Num(b);
    public static V operator -(V a) => -R.Num(a);
    public static V operator *(V a,V b) => R.Num(a)*R.Num(b);
    public static V operator /(V a,V b) => R.Num(a)/R.Num(b);
    public static V operator %(V a,V b) { double x=R.Num(a), y=R.Num(b); return x-Math.Floor(x/y)*y; }
    public static bool operator ==(V a,V b) => a.Data is double x && b.Data is double y ? x == y : a.Equals(b);
    public static bool operator !=(V a,V b) => !(a == b);
    public static bool operator <(V a,V b) => Compare(a,b)<0;
    public static bool operator >(V a,V b) => Compare(a,b)>0;
    public static bool operator <=(V a,V b) => Compare(a,b)<=0;
    public static bool operator >=(V a,V b) => Compare(a,b)>=0;
    private static int Compare(V a,V b) => a.Data is string x && b.Data is string y ? string.CompareOrdinal(x,y) : R.Num(a).CompareTo(R.Num(b));
    public bool Equals(V other) => Equals(Data,other.Data);
    public override bool Equals(object? other) => other is V v && Equals(v);
    public override int GetHashCode() => Data?.GetHashCode() ?? 0;
    public override string ToString() => Data switch { null=>"nil",bool b=>b?"true":"false",double d=>d.ToString("G14",CultureInfo.InvariantCulture),string s=>s,RuleTable=>"table",RuleFunction=>"function",_=>Data.ToString()??"nil" };
}
public readonly struct Results(params V[] values)
{
    private readonly V[]? _values = values;
    public static Results Empty => default;
    public int Count => _values?.Length ?? 0;
    public V this[int i] => i>=0 && i<Count ? _values![i] : V.Nil;
    public V First => this[0];
    public V[] Array => _values ?? [];
}
public readonly record struct Entry(V Key, Results Values)
{
    public Entry(V key,V value) : this(key,new Results(value)) { }
}
public sealed class RuleTable : IEnumerable<KeyValuePair<V,V>>
{
    private readonly Dictionary<V,V> _entries = [];
    private int _length;
    private readonly ConditionalWeakTable<object, Box> _weak = new();
    private sealed class Box(V value) { public V Value=value; }
    public RuleTable? Meta { get; set; }
    private bool WeakKeys => Meta?.Get("__mode").Text.Contains('k') == true;
    public V Get(V key)
    {
        if (key.IsNil) return V.Nil;
        if (WeakKeys && key.Data is RuleTable or RuleFunction) return _weak.TryGetValue(key.Data,out var box) ? box.Value : V.Nil;
        return _entries.GetValueOrDefault(key);
    }
    public void Set(V key,V value)
    {
        if (key.IsNil || key.Data is double d && double.IsNaN(d)) throw new InvalidOperationException("Invalid table key.");
        if (WeakKeys && key.Data is RuleTable or RuleFunction)
        { _weak.Remove(key.Data); if (!value.IsNil) _weak.Add(key.Data,new(value)); return; }
        if (value.IsNil) _entries.Remove(key); else _entries[key]=value;
        if (key.Data is double index && index >= 1 && index <= int.MaxValue && index % 1 == 0)
        {
            if(value.IsNil && index<=_length) _length=(int)index-1;
            else if(!value.IsNil && index==_length+1) { while(_entries.ContainsKey(_length+1))_length++; }
        }
    }
    public int Length => _length;
    public IEnumerator<KeyValuePair<V,V>> GetEnumerator() => _entries.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
public static class R
{
    public static bool Truth(V v) => v.Data != null && v.Data is not false;
    public static double Num(V v) => v.Data switch { double d=>d, string s when double.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out var n)=>n,_=>throw new InvalidOperationException("Expected number, got "+v.ToString()) };
    public static V Get(V value,V key)
    {
        if (value.Data is RuleTable t)
        {
            var v=t.Get(key); if (!v.IsNil) return v;
            var index=t.Meta?.Get("__index") ?? V.Nil;
            return index.Data is RuleFunction ? Call(index,new(value,key)).First : index.Data is RuleTable ? Get(index,key) : V.Nil;
        }
        if (value.Data is string) return NativeRuntime.StringFunctions.Get(key);
        throw new InvalidOperationException($"Cannot index {value} by {key}.");
    }
    public static void Set(V value,V key,V item)
    {
        if (value.Data is not RuleTable t) throw new InvalidOperationException($"Cannot write {key} on {value}.");
        t.Set(key,item);
    }
    public static Results Call(V fn,Results args) => fn.Data is RuleFunction f ? f(args) : throw new InvalidOperationException("Attempt to call "+fn.ToString());
    public static Results Invoke(V self,string name,Results args) => Call(Get(self,name),Join([self],args));
    public static Results Join(V[] first,Results last) => new([..first,..last.Array]);
    public static Results Tail(Results a,int offset) => new(a.Array.Skip(offset).ToArray());
    public static V And(V a,Func<V> b) => Truth(a)?b():a;
    public static V Or(V a,Func<V> b) => Truth(a)?a:b();
    public static V Concat(V a,V b) => a.ToString()+b.ToString();
    public static int Length(V v) => v.Data switch { string s=>s.Length,RuleTable t=>t.Length,_=>throw new InvalidOperationException("No length for "+v.ToString()) };
    public static V Table(params Entry[] fields)
    {
        var t=new RuleTable();
        foreach (var field in fields) for (int i=0;i<field.Values.Count;i++) t.Set(i==0?field.Key:field.Key+i,field.Values[i]);
        return t;
    }
    public static IEnumerable<Results> Iterate(Results expression)
    {
        if (expression.First.Data is IEnumerable<Results> rows) { foreach (var row in rows) yield return row; yield break; }
        var control=expression[2];
        while (true) { var next=Call(expression[0],new(expression[1],control)); if(next.First.IsNil)yield break; control=next.First;yield return next; }
    }
    public static V Function(RuleFunction f) => new(f);
    public static V Copy(V value)
    {
        if(value.Data is not RuleTable t)return value;
        var result=new RuleTable();foreach(var pair in t)result.Set(pair.Key,Copy(pair.Value));return result;
    }
}
