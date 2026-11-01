// Static C# port of src/interaction_menu.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 3756ffd804c6a3fbfd1f6651595bfa7827de141107e7be62a75d04ce174c28a4
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_interaction_menu { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"page_size", (V)6.0d), new Entry((V)"x", (V)96.0d), new Entry((V)"y", (V)64.0d), new Entry((V)"width", (V)832.0d), new Entry((V)"height", (V)496.0d), new Entry((V)"row_y", (V)166.0d), new Entry((V)"row_step", (V)34.0d))));
V v_2 = values_1[0];
R.Set(v_2, (V)"new", new V((RuleFunction)(args_3 => {
V v_4 = args_3[0];
V v_5 = args_3[1];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"options", v_4), new Entry((V)"context", R.Or(v_5, () => R.Table())), new Entry((V)"selected", (V)1.0d), new Entry((V)"confirm", (V)false))));
return Results.Empty;
})));
R.Set(v_2, (V)"page", new V((RuleFunction)(args_6 => {
V v_7 = args_6[0];
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(R.Get(v_7, (V)"selected") - (V)1.0d) / R.Get(v_2, (V)"page_size"))))));
return Results.Empty;
})));
R.Set(v_2, (V)"rows", new V((RuleFunction)(args_8 => {
V v_9 = args_8[0];
var values_10 = R.Join(new V[] {}, new Results(R.Table()));
V v_11 = values_10[0];

var values_12 = R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(v_2, (V)"page"), R.Join(new V[] {}, new Results(v_9))).First * R.Get(v_2, (V)"page_size")) + (V)1.0d)));
V v_13 = values_12[0];
{ double end_16 = R.Num(R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)((V)(v_13 + R.Get(v_2, (V)"page_size")) - (V)1.0d)}, new Results((V)R.Length(R.Get(R.Get(v_9, (V)"options"), (V)"entries"))))).First), step_17 = R.Num((V)1.0d);
for (double i_15 = R.Num(v_13); step_17 > 0 ? i_15 <= end_16 : i_15 >= end_16; i_15 += step_17) {
V v_14 = i_15;
var values_18 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"entry", R.Get(R.Get(R.Get(v_9, (V)"options"), (V)"entries"), v_14)), new Entry((V)"index", v_14), new Entry((V)"y", (V)(R.Get(v_2, (V)"row_y") + (V)((V)(v_14 - v_13) * R.Get(v_2, (V)"row_step")))))));
var obj_19 = v_11;
V key_20 = (V)((V)R.Length(v_11) + (V)1.0d);
R.Set(obj_19, key_20, values_18[0]);
}
}
return R.Join(new V[] {}, new Results(v_11));
return Results.Empty;
})));
R.Set(v_2, (V)"key", new V((RuleFunction)(args_21 => {
V v_22 = args_21[0];
V v_23 = args_21[1];
var values_24 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_22, (V)"options"), (V)"entries")));
V v_25 = values_24[0];

var values_26 = R.Join(new V[] {}, new Results((V)R.Length(v_25)));
V v_27 = values_26[0];
if (R.Truth((V)(v_23 == (V)"escape"))) {
if (R.Truth(R.Get(v_22, (V)"confirm"))) {
var values_28 = R.Join(new V[] {}, new Results((V)false));
var obj_29 = v_22;
V key_30 = (V)"confirm";
R.Set(obj_29, key_30, values_28[0]);

return R.Join(new V[] {}, new Results((V)"changed"));
}
return R.Join(new V[] {}, new Results((V)"close"));
}
if (R.Truth((V)(v_27 == (V)0.0d))) {
return R.Join(new V[] {}, new Results((V)"changed"));
}
var values_31 = R.Join(new V[] {}, new Results(R.Get(v_22, (V)"selected")));
V v_32 = values_31[0];
if (R.Truth(R.Or((V)(v_23 == (V)"up"), () => (V)(v_23 == (V)"w")))) {
var values_33 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_22, (V)"selected") - (V)1.0d)))));
var obj_34 = v_22;
V key_35 = (V)"selected";
R.Set(obj_34, key_35, values_33[0]);
} else if (R.Truth(R.Or((V)(v_23 == (V)"down"), () => (V)(v_23 == (V)"s")))) {
var values_36 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_27}, new Results((V)(R.Get(v_22, (V)"selected") + (V)1.0d)))));
var obj_37 = v_22;
V key_38 = (V)"selected";
R.Set(obj_37, key_38, values_36[0]);
} else if (R.Truth((V)(v_23 == (V)"left"))) {
var values_39 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_22, (V)"selected") - R.Get(v_2, (V)"page_size"))))));
var obj_40 = v_22;
V key_41 = (V)"selected";
R.Set(obj_40, key_41, values_39[0]);
} else if (R.Truth((V)(v_23 == (V)"right"))) {
var values_42 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_27}, new Results((V)(R.Get(v_22, (V)"selected") + R.Get(v_2, (V)"page_size"))))));
var obj_43 = v_22;
V key_44 = (V)"selected";
R.Set(obj_43, key_44, values_42[0]);
} else if (R.Truth(R.And(R.And(R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_23))).First, () => (V)(R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_23))).First >= (V)1.0d)), () => (V)(R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_23))).First <= R.Get(v_2, (V)"page_size"))))) {
var values_45 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_27}, new Results((V)((V)(R.Call(R.Get(v_2, (V)"page"), R.Join(new V[] {}, new Results(v_22))).First * R.Get(v_2, (V)"page_size")) + R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_23))).First)))));
var obj_46 = v_22;
V key_47 = (V)"selected";
R.Set(obj_46, key_47, values_45[0]);
} else if (R.Truth(R.Or((V)(v_23 == (V)"return"), () => (V)(v_23 == (V)"kpenter")))) {
var values_48 = R.Join(new V[] {}, new Results(R.Get(v_25, R.Get(v_22, (V)"selected"))));
V v_49 = values_48[0];
if (R.Truth((V)(R.Get(v_49, (V)"enabled") == (V)false))) {
return R.Join(new V[] {(V)"status"}, new Results(R.Or(R.Get(v_49, (V)"reason"), () => (V)"Unavailable")));
}
if (R.Truth(R.Or(R.Get(v_49, (V)"select"), () => R.Get(R.Get(v_22, (V)"context"), (V)"nearby")))) {
return R.Join(new V[] {(V)"choose"}, new Results(v_49));
}
if (R.Truth((V)(!R.Truth(R.Get(v_22, (V)"confirm"))))) {
var values_50 = R.Join(new V[] {}, new Results((V)true));
var obj_51 = v_22;
V key_52 = (V)"confirm";
R.Set(obj_51, key_52, values_50[0]);

return R.Join(new V[] {}, new Results((V)"changed"));
}
return R.Join(new V[] {(V)"choose"}, new Results(v_49));
}
if (R.Truth((V)(R.Get(v_22, (V)"selected") != v_32))) {
var values_53 = R.Join(new V[] {}, new Results((V)false));
var obj_54 = v_22;
V key_55 = (V)"confirm";
R.Set(obj_54, key_55, values_53[0]);
}
return R.Join(new V[] {}, new Results((V)"changed"));
return Results.Empty;
})));
R.Set(v_2, (V)"mouse", new V((RuleFunction)(args_56 => {
V v_57 = args_56[0];
V v_58 = args_56[1];
V v_59 = args_56[2];
V v_60 = args_56[3];
if (R.Truth(R.And((V)(v_58 >= (V)(R.Get(v_2, (V)"x") + (V)24.0d)), () => (V)(v_58 <= (V)((V)(R.Get(v_2, (V)"x") + R.Get(v_2, (V)"width")) - (V)24.0d))))) {
foreach (var row_61 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"rows"), R.Join(new V[] {}, new Results(v_57)))))))) {
V v_62 = row_61[0];
V v_63 = row_61[1];
if (R.Truth(R.And((V)(v_59 >= R.Get(v_63, (V)"y")), () => (V)(v_59 < (V)(R.Get(v_63, (V)"y") + (V)30.0d))))) {
if (R.Truth((V)(R.Get(v_57, (V)"selected") != R.Get(v_63, (V)"index")))) {
var values_64 = R.Join(new V[] {}, new Results((V)false));
var obj_65 = v_57;
V key_66 = (V)"confirm";
R.Set(obj_65, key_66, values_64[0]);
}
var values_67 = R.Join(new V[] {}, new Results(R.Get(v_63, (V)"index")));
var obj_68 = v_57;
V key_69 = (V)"selected";
R.Set(obj_68, key_69, values_67[0]);
if (R.Truth((V)(R.Or(v_60, () => (V)1.0d) >= (V)2.0d))) {
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"key"), R.Join(new V[] {v_57}, new Results((V)"return"))));
}
return R.Join(new V[] {}, new Results((V)"changed"));
}
}
}
if (R.Truth(R.And((V)(v_59 >= (V)508.0d), () => (V)(v_59 <= (V)546.0d)))) {
if (R.Truth(R.And((V)(v_58 >= (V)600.0d), () => (V)(v_58 <= (V)896.0d)))) {
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"key"), R.Join(new V[] {v_57}, new Results((V)"return"))));
}
if (R.Truth(R.And((V)(v_58 >= (V)120.0d), () => (V)(v_58 <= (V)360.0d)))) {
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"key"), R.Join(new V[] {v_57}, new Results((V)"escape"))));
}
}
return R.Join(new V[] {}, new Results((V)"changed"));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
