using System.Text;
using Roag.Core.Rules;

var rt=new NativeRuntime();
Parity.Register(rt);
var io=new RuleTable();
V Stream(TextWriter writer)=>R.Table(new("setvbuf",R.Function(a=>Results.Empty)),new("write",R.Function(a=>{writer.Write(string.Concat(a.Array.Skip(1).Select(v=>v.Text)));return Results.Empty;})));
io.Set("stdout",Stream(Console.Out));io.Set("stderr",Stream(Console.Error));
io.Set("open",R.Function(a=>{
    string path=a[0].Text;bool write=a[1].Text.Contains('w');
    if(!write&&!File.Exists(path))return new(V.Nil);
    V handle=R.Table(new("read",R.Function(args=>new(Encoding.Latin1.GetString(File.ReadAllBytes(path))))),
        new("write",R.Function(args=>{File.AppendAllText(path,string.Concat(args.Array.Skip(1).Select(v=>v.Text)),Encoding.Latin1);return new(true);})),
        new("close",R.Function(args=>new(true))));return new(handle);
}));
rt.SetGlobal("io",io);
R.Set(rt.Global("os"),"exit",R.Function(a=>{if(a[0].Number!=0)throw new InvalidOperationException("Parity tests failed.");return Results.Empty;}));
try {rt.Require("tests.run");}catch(Exception e){Console.Error.WriteLine(e);return 1;}return 0;
