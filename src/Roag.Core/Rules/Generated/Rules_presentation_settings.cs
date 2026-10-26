// Static C# port of src/presentation_settings.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 8e7a477f641f57471b8cf86f60e310449d58c05e3381acf62131516af66f9eb0
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_presentation_settings { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"format", (V)"roag-presentation-settings/1"), new Entry((V)"file", (V)"presentation-settings.roag"))));
V v_4 = values_3[0];
R.Set(v_4, (V)"defaults", new V((RuleFunction)(args_5 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"format", R.Get(v_4, (V)"format")), new Entry((V)"master", (V)100.0d), new Entry((V)"music", (V)80.0d), new Entry((V)"effects", (V)90.0d), new Entry((V)"shake", (V)true), new Entry((V)"flash", (V)true), new Entry((V)"combat_effects", (V)true), new Entry((V)"damage_numbers", (V)true), new Entry((V)"font", (V)"pixel"), new Entry((V)"text_size", (V)"normal"), new Entry((V)"high_contrast", (V)false))));
return Results.Empty;
})));
R.Set(v_4, (V)"validate", new V((RuleFunction)(args_6 => {
V v_7 = args_6[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_7))).First == (V)"table"), () => (V)(R.Get(v_7, (V)"format") == R.Get(v_4, (V)"format")))}, new Results((V)"invalid presentation format")));
foreach (var row_8 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"master"), new Entry((V)2, (V)"music"), new Entry((V)3, (V)"effects")))))))) {
V v_9 = row_8[0];
V v_10 = row_8[1];
var values_11 = R.Join(new V[] {}, new Results(R.Get(v_7, v_10)));
V v_12 = values_11[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_12))).First == (V)"number"), () => (V)((V)(v_12 % (V)1.0d) == (V)0.0d)), () => (V)(v_12 >= (V)0.0d)), () => (V)(v_12 <= (V)100.0d))}, new Results(R.Concat((V)"invalid ", R.Concat(v_10, (V)" volume")))));
}
foreach (var row_13 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"shake"), new Entry((V)2, (V)"flash"), new Entry((V)3, (V)"combat_effects"), new Entry((V)4, (V)"damage_numbers"), new Entry((V)5, (V)"high_contrast")))))))) {
V v_14 = row_13[0];
V v_15 = row_13[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_7, v_15)))).First == (V)"boolean")}, new Results(R.Concat((V)"invalid ", R.Concat(v_15, (V)" option")))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_7, (V)"font") == (V)"pixel"), () => (V)(R.Get(v_7, (V)"font") == (V)"sans"))}, new Results((V)"invalid font option")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or((V)(R.Get(v_7, (V)"text_size") == (V)"small"), () => (V)(R.Get(v_7, (V)"text_size") == (V)"normal")), () => (V)(R.Get(v_7, (V)"text_size") == (V)"large"))}, new Results((V)"invalid text size")));
foreach (var row_16 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_7)))))) {
V v_17 = row_16[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(R.Call(R.Get(v_4, (V)"defaults"), Results.Empty).First, v_17) != V.Nil)}, new Results((V)"unexpected presentation option")));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_4, (V)"load", new V((RuleFunction)(args_18 => {
V v_19 = args_18[0];
var values_20 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_19, (V)"read"), v_19}, new Results(R.Get(v_4, (V)"file")))));
V v_21 = values_20[0];
V v_22 = values_20[1];
if (R.Truth((V)(!R.Truth(v_21)))) {
return R.Join(new V[] {R.Call(R.Get(v_4, (V)"defaults"), Results.Empty).First}, new Results((V)"read error"));
}
if (R.Truth((V)(v_22 == V.Nil))) {
return R.Join(new V[] {R.Call(R.Get(v_4, (V)"defaults"), Results.Empty).First}, new Results((V)"default"));
}
var values_23 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"decode")}, new Results(v_22))));
V v_24 = values_23[0];
V v_25 = values_23[1];
if (R.Truth(R.Or((V)(!R.Truth(v_24)), () => (V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_4, (V)"validate")}, new Results(v_25))).First))))) {
return R.Join(new V[] {R.Call(R.Get(v_4, (V)"defaults"), Results.Empty).First}, new Results((V)"invalid"));
}
return R.Join(new V[] {v_25}, new Results((V)"loaded"));
return Results.Empty;
})));
R.Set(v_4, (V)"save", new V((RuleFunction)(args_26 => {
V v_27 = args_26[0];
V v_28 = args_26[1];
var values_29 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_4, (V)"validate")}, new Results(v_28))));
V v_30 = values_29[0];
V v_31 = values_29[1];
if (R.Truth((V)(!R.Truth(v_30)))) {
return R.Join(new V[] {(V)false}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_31))));
}
var values_32 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"encode"), R.Join(new V[] {}, new Results(v_28))));
V v_33 = values_32[0];
var values_34 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_27, (V)"write"), v_27, R.Get(v_4, (V)"file")}, new Results(v_33))));
V v_35 = values_34[0];
V v_36 = values_34[1];
if (R.Truth(R.Or((V)(!R.Truth(v_35)), () => (V)(!R.Truth(v_36))))) {
return R.Join(new V[] {(V)false}, new Results((V)"settings write failed"));
}
var values_37 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_27, (V)"read"), v_27}, new Results(R.Get(v_4, (V)"file")))));
V v_38 = values_37[0];
V v_39 = values_37[1];
if (R.Truth(R.Or((V)(!R.Truth(v_38)), () => (V)(v_39 != v_33)))) {
return R.Join(new V[] {(V)false}, new Results((V)"settings verification failed"));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
