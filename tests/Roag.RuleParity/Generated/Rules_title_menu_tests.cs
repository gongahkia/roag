// Static C# port of tests/title_menu_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 65638624af77ac9d0b6c21b56de06647570b8ffe59bbbea944055bc518caf437
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_title_menu_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.title_menu"))));
V v_4 = values_3[0];
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_5 => {
V v_6 = args_5[0];
V v_7 = args_5[1];
R.Call(v_6, R.Join(new V[] {(V)"title defaults to Continue only for a resumable active run"}, new Results(new V((RuleFunction)(args_8 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_4, (V)"default_selection"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", (V)"absent"))))).First == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_4, (V)"default_selection"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", (V)"terminal"))))).First == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_4, (V)"default_selection"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", (V)"recovered"))))).First == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"resumable"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", (V)"active")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_4, (V)"resumable"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", (V)"incompatible"))))).First)))));
return Results.Empty;
})))));
R.Call(v_6, R.Join(new V[] {(V)"title save summary describes real saved state without modifying it"}, new Results(new V((RuleFunction)(args_9 => {

var values_10 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)13.0d}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/ranger"))))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_11))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"save_summary"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", (V)"active"), new Entry((V)"state", v_11))))));
V v_15 = values_14[0];
V v_16 = values_14[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Invoke(v_15, "find", R.Join(new V[] {(V)"Ranger", (V)1.0d}, new Results((V)true))).First, () => R.Invoke(v_15, "find", R.Join(new V[] {(V)"Lv 1", (V)1.0d}, new Results((V)true))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Invoke(v_16, "find", R.Join(new V[] {(V)"Overgrown Ruins", (V)1.0d}, new Results((V)true))).First, () => R.Invoke(v_16, "find", R.Join(new V[] {(V)"HP 20/20", (V)1.0d}, new Results((V)true))).First))));
R.Call(v_7, R.Join(new V[] {v_13}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_11)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_4, (V)"save_summary"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", (V)"terminal"))))).First == (V)"RUN COMPLETE"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_4, (V)"save_summary"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", (V)"pending_terminal"))))).First == (V)"UNLOCK RECOVERY NEEDED"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_4, (V)"save_summary"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", (V)"incompatible"))))).First == (V)"INCOMPATIBLE SAVE"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_4, (V)"save_summary"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", (V)"corrupt"))))).First == (V)"DAMAGED SAVE"))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
