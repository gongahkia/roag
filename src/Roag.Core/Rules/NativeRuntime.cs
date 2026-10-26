using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Roag.Core.Rules;

public sealed partial class NativeRuntime
{
    public readonly Dictionary<string,Func<V>> Factories=[];
    private readonly RuleTable _globals=new();
    private readonly Dictionary<string,V> _modules=[];
    public static readonly RuleTable StringFunctions=BuildStrings();
    public Action<string> Output { get; set; } = Console.WriteLine;
    public NativeRuntime()
    {
        RegisterRules();
        RegisterPartyHooks();
        SetGlobal("_G",_globals);SetGlobal("_VERSION","C# native Roag rules");
        Fn("require",a=>new(Require(a[0].Text)));
        Fn("assert",a=>R.Truth(a[0])?a:throw new InvalidOperationException(a[1].IsNil?"assertion failed":a[1].Text));
        Fn("error",a=>throw new InvalidOperationException(a[0].Text));
        Fn("type",a=>new(a[0].Data switch {null=>"nil",double=>"number",bool=>"boolean",string=>"string",RuleTable=>"table",RuleFunction=>"function",_=>"userdata"}));
        Fn("tostring",a=>new(a[0].Text));
        Fn("tonumber",a=> { if(a[0].Data is double)return new(a[0]); try{return new(a[1].IsNil?double.Parse(a[0].Text,CultureInfo.InvariantCulture):Convert.ToInt64(a[0].Text,(int)a[1].Number));}catch{return new(V.Nil);} });
        Fn("pairs",a=>new(new V(Pairs(a[0],false))));Fn("ipairs",a=>new(new V(Pairs(a[0],true))));
        Fn("next",a=> {var t=(RuleTable)a[0].Data!;bool found=a[1].IsNil;foreach(var p in t){if(found)return new(p.Key,p.Value);if(p.Key==a[1])found=true;}return Results.Empty;});
        Fn("unpack",a=> {int start=a[1].IsNil?1:(int)a[1].Number,end=a[2].IsNil?R.Length(a[0]):(int)a[2].Number;return new(Enumerable.Range(start,Math.Max(0,end-start+1)).Select(i=>R.Get(a[0],i)).ToArray());});
        Fn("select",a=>a[0].Text=="#"?new(a.Count-1):R.Tail(a,a[0].Number<0?a.Count+(int)a[0].Number:(int)a[0].Number));
        Fn("pcall",a=> {try{return R.Join([true],R.Call(a[0],R.Tail(a,1)));}catch(Exception e){return new(false,e.Message);}});
        Fn("setmetatable",a=>{((RuleTable)a[0].Data!).Meta=a[1].Data as RuleTable;return new(a[0]);});
        Fn("getmetatable",a=>new(new V((a[0].Data as RuleTable)?.Meta)));
        Fn("rawget",a=>new(((RuleTable)a[0].Data!).Get(a[1])));Fn("rawset",a=>{((RuleTable)a[0].Data!).Set(a[1],a[2]);return new(a[0]);});
        Fn("print",a=>{Output(string.Join("\t",a.Array.Select(v=>v.Text)));return Results.Empty;});
        SetGlobal("string",StringFunctions);SetGlobal("math",BuildMath());SetGlobal("table",BuildTable());
        SetGlobal("os",R.Table(new("clock",R.Function(a=>new(Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency))),new("getenv",R.Function(a=>new((V?)Environment.GetEnvironmentVariable(a[0].Text)??V.Nil))),new("time",R.Function(a=>new(DateTimeOffset.UtcNow.ToUnixTimeSeconds())))));
        SetGlobal("package",R.Table(new("path",""),new("loaded",new RuleTable())));
        _modules["bit"]=BuildBit();
    }
    public V Global(string name)=>_globals.Get(name);
    public void SetGlobal(string name,V value)=>_globals.Set(name,value);
    private void Fn(string name,RuleFunction f)=>SetGlobal(name,new(f));
    public V Require(string name)
    {
        if(_modules.TryGetValue(name,out var value))return value;
        if(!Factories.TryGetValue(name,out var factory))throw new InvalidOperationException("Unknown compiled rules module "+name);
        value=factory();_modules[name]=value;R.Set(Global("package")["loaded"],name,value);return value;
    }
    public Results Call(string module,string function,params V[] args)=>R.Call(Require(module)[function],new(args));
    private static IEnumerable<Results> Pairs(V value,bool array)
    {
        var t=(RuleTable)value.Data!;
        if(array) {for(int i=1;;i++){var v=t.Get(i);if(v.IsNil)yield break;yield return new(i,v);}}
        else {foreach(var p in t.ToArray())if(!t.Get(p.Key).IsNil)yield return new(p.Key,t.Get(p.Key));}
    }
    private static RuleTable Library(params (string,RuleFunction)[] functions)
    {var t=new RuleTable();foreach(var (name,fn) in functions)t.Set(name,new V(fn));return t;}
    private static RuleTable BuildMath()
    {
        var t=Library(("floor",a=>new(Math.Floor(a[0].Number))),("ceil",a=>new(Math.Ceiling(a[0].Number))),
            ("abs",a=>new(Math.Abs(a[0].Number))),("min",a=>new(a.Array.Min(v=>v.Number))),("max",a=>new(a.Array.Max(v=>v.Number))),
            ("sqrt",a=>new(Math.Sqrt(a[0].Number))),("sin",a=>new(Math.Sin(a[0].Number))),("cos",a=>new(Math.Cos(a[0].Number))),
            ("atan2",a=>new(Math.Atan2(a[0].Number,a[1].Number))),("pow",a=>new(Math.Pow(a[0].Number,a[1].Number))),
            ("exp",a=>new(Math.Exp(a[0].Number))),("log",a=>new(Math.Log(a[0].Number))),("fmod",a=>new(a[0].Number%a[1].Number)));
        t.Set("huge",double.PositiveInfinity);t.Set("pi",Math.PI);return t;
    }
    private static RuleTable BuildTable()=>Library(
        ("insert",a=>{var t=(RuleTable)a[0].Data!;int pos=a.Count==2?t.Length+1:(int)a[1].Number;var value=a.Count==2?a[1]:a[2];for(int i=t.Length;i>=pos;i--)t.Set(i+1,t.Get(i));t.Set(pos,value);return Results.Empty;}),
        ("remove",a=>{var t=(RuleTable)a[0].Data!;int len=t.Length,pos=a[1].IsNil?len:(int)a[1].Number;if(pos<1||pos>len)return Results.Empty;var value=t.Get(pos);for(int i=pos;i<len;i++)t.Set(i,t.Get(i+1));t.Set(len,V.Nil);return new(value);}),
        ("concat",a=>{int first=a[2].IsNil?1:(int)a[2].Number,last=a[3].IsNil?R.Length(a[0]):(int)a[3].Number;return new(string.Join(a[1].IsNil?"":a[1].Text,Enumerable.Range(first,Math.Max(0,last-first+1)).Select(i=>R.Get(a[0],i).Text)));}),
        ("sort",a=>{var t=(RuleTable)a[0].Data!;var items=Enumerable.Range(1,t.Length).Select(t.GetInt).ToArray();Array.Sort(items,(x,y)=>a[1].IsNil?(x==y?0:x<y?-1:1):R.Truth(R.Call(a[1],new(x,y)).First)?-1:R.Truth(R.Call(a[1],new(y,x)).First)?1:0);for(int i=0;i<items.Length;i++)t.Set(i+1,items[i]);return Results.Empty;}));
    private static RuleTable BuildBit()=>Library(("band",a=>new((int)a.Array.Select(v=>unchecked((uint)(long)v.Number)).Aggregate((x,y)=>x&y))),
        ("bor",a=>new((int)a.Array.Select(v=>unchecked((uint)(long)v.Number)).Aggregate((x,y)=>x|y))),
        ("bxor",a=>new((int)a.Array.Select(v=>unchecked((uint)(long)v.Number)).Aggregate((x,y)=>x^y))),
        ("bnot",a=>new(~(int)a[0].Number)),("lshift",a=>new(unchecked((int)(long)a[0].Number)<<(int)a[1].Number)),
        ("rshift",a=>new((int)(unchecked((uint)(long)a[0].Number)>>(int)a[1].Number))),
        ("tobit",a=>new(unchecked((int)(long)a[0].Number))));
    private static int Index(V v,int length,int fallback) {if(v.IsNil)return fallback;int i=(int)v.Number;return i<0?length+i+1:i;}
    private static RuleTable BuildStrings()=>Library(
        ("len",a=>new(a[0].Text.Length)),("lower",a=>new(a[0].Text.ToLowerInvariant())),("upper",a=>new(a[0].Text.ToUpperInvariant())),
        ("sub",a=>{var s=a[0].Text;int start=Math.Max(1,Index(a[1],s.Length,1)),end=Math.Min(s.Length,Index(a[2],s.Length,s.Length));return new(end<start?"":s.Substring(start-1,end-start+1));}),
        ("byte",a=>{var s=a[0].Text;int start=Math.Max(1,Index(a[1],s.Length,1)),end=Math.Min(s.Length,Index(a[2],s.Length,start));return new(Enumerable.Range(start,Math.Max(0,end-start+1)).Select(i=>(V)(int)s[i-1]).ToArray());}),
        ("char",a=>new(new string(a.Array.Select(v=>(char)(byte)v.Number).ToArray()))),
        ("rep",a=>new(string.Join(a[2].IsNil?"":a[2].Text,Enumerable.Repeat(a[0].Text,Math.Max(0,(int)a[1].Number))))),
        ("find",a=>Find(a,false)),("match",a=>Find(a,true)),("gmatch",a=>new(new V(Matches(a)))),
        ("gsub",Substitute),("format",Format));
    internal static string Pattern(string pattern)
    {
        var b=new StringBuilder();bool set=false;
        for(int i=0;i<pattern.Length;i++)
        {
            char c=pattern[i];
            if(c=='%'&&i+1<pattern.Length)
            {
                char p=pattern[++i];b.Append(p switch {'d'=>@"\d",'D'=>@"\D",'-'=>@"\-",'W'=>"[^a-zA-Z0-9]",'L'=>"[^a-z]",'U'=>"[^A-Z]",'X'=>"[^a-fA-F0-9]",'c'=>@"\p{Cc}",'C'=>@"\P{Cc}",'a'=>set?"a-zA-Z":"[a-zA-Z]",'A'=>"[^a-zA-Z]",'w'=>set?"a-zA-Z0-9":"[a-zA-Z0-9]",'s'=>@"\s",'S'=>@"\S",'l'=>set?"a-z":"[a-z]",'u'=>set?"A-Z":"[A-Z]",'x'=>set?"a-fA-F0-9":"[a-fA-F0-9]",'p'=>set?@"\p{P}":@"[\p{P}]",'z'=>"\\x00",>= '1' and <= '9'=>"\\"+p,_=>Regex.Escape(p.ToString())});continue;
            }
            if(c=='[')set=true;if(c==']')set=false;
            if(c=='-'&&!set)b.Append("*?");
            else if(c=='\\'||c=='|'||c=='{'||c=='}')b.Append('\\').Append(c);
            else b.Append(c);
        }
        return b.ToString();
    }
    private static Regex RegexFor(string p)=>new(Pattern(p),RegexOptions.Singleline|RegexOptions.CultureInvariant,TimeSpan.FromSeconds(2));
    private static Results Captures(Match m,bool whole=true)=>m.Groups.Count>1?new(m.Groups.Cast<Group>().Skip(1).Select(g=>(V)g.Value).ToArray()):whole?new(m.Value):Results.Empty;
    private static Results Find(Results a,bool match)
    {
        var s=a[0].Text;int start=Math.Max(0,Index(a[2],s.Length,1)-1);if(start>s.Length)return Results.Empty;
        if(!match&&R.Truth(a[3])) {int p=s.IndexOf(a[1].Text,start,StringComparison.Ordinal);return p<0?Results.Empty:new(p+1,p+a[1].Text.Length);}
        var m=RegexFor(a[1].Text).Match(s,start);if(!m.Success)return Results.Empty;
        return match?Captures(m):R.Join([m.Index+1,m.Index+m.Length],Captures(m,false));
    }
    private static IEnumerable<Results> Matches(Results a)
    {foreach(Match m in RegexFor(a[1].Text).Matches(a[0].Text))yield return Captures(m);}
    private static Results Substitute(Results a)
    {
        int count=0,limit=a[3].IsNil?int.MaxValue:(int)a[3].Number;
        string result=RegexFor(a[1].Text).Replace(a[0].Text,m=>{
            if(count>=limit)return m.Value;count++;var replacement=a[2];
            if(replacement.Data is RuleFunction)replacement=R.Call(replacement,Captures(m)).First;
            else if(replacement.Data is RuleTable)replacement=R.Get(replacement,Captures(m).First);
            else return Regex.Replace(replacement.Text,"%([%0-9])",r=>r.Groups[1].Value=="%"?"%":r.Groups[1].Value=="0"?m.Value:m.Groups[int.Parse(r.Groups[1].Value)].Value);
            return replacement.IsNil||replacement.Data is false?m.Value:replacement.Text;
        });return new(result,count);
    }
    private static Results Format(Results a)
    {
        int index=1;
        string result=Regex.Replace(a[0].Text,@"%([-+ #0]*)(\d*)(?:\.(\d+))?([%sdifgGxXocqeu])",m=>{
            char type=m.Groups[4].Value[0];if(type=='%')return "%";var v=a[index++];string flags=m.Groups[1].Value;
            int width=m.Groups[2].Value.Length>0?int.Parse(m.Groups[2].Value):0,precision=m.Groups[3].Success?int.Parse(m.Groups[3].Value):6;
            string text=type switch {'s'=>v.Text,'q'=>System.Text.Json.JsonSerializer.Serialize(v.Text),'c'=>((char)(int)v.Number).ToString(),
                'x'=>unchecked((ulong)(long)v.Number).ToString("x",CultureInfo.InvariantCulture),'X'=>unchecked((ulong)(long)v.Number).ToString("X",CultureInfo.InvariantCulture),
                'd' or 'i' or 'u'=>((long)v.Number).ToString(CultureInfo.InvariantCulture),'f'=>v.Number.ToString("F"+precision,CultureInfo.InvariantCulture),
                'e'=>v.Number.ToString("e"+precision,CultureInfo.InvariantCulture),_=>v.Number.ToString("G"+precision,CultureInfo.InvariantCulture)};
            if(flags.Contains('+')&&v.Data is double d&&d>=0)text="+"+text;
            return flags.Contains('-')?text.PadRight(width):text.PadLeft(width,flags.Contains('0')?'0':' ');
        });return new(result);
    }
}
internal static class RuleTableExtensions { public static V GetInt(this RuleTable table,int key)=>table.Get(key); }
