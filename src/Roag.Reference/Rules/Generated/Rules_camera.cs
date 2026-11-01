// Static C# port of src/camera.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: cc6f5fb9c76bf5098d859c3ef83a3b51632c8aa77f66e67b68a5d3745839ac3b
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_camera { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
V v_3 = V.Nil;
v_3 = new V((RuleFunction)(args_4 => {
V v_5 = args_4[0];
V v_6 = args_4[1];
V v_7 = args_4[2];
var values_8 = R.Join(new V[] {}, new Results((V)(R.Get(v_5, (V)"map_width") * R.Get(v_5, (V)"tile"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, new Results((V)(R.Get(v_5, (V)"map_height") * R.Get(v_5, (V)"tile"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {(V)(R.Get(R.Get(v_5, (V)"viewport"), (V)"width") / (V)2.0d)}, new Results((V)(R.Get(R.Get(v_5, (V)"viewport"), (V)"height") / (V)2.0d)));
V v_13 = values_12[0];
V v_14 = values_12[1];
var values_15 = R.Join(new V[] {(V)((V)(v_6 - (V)0.5d) * R.Get(v_5, (V)"tile"))}, new Results((V)((V)(v_7 - (V)0.5d) * R.Get(v_5, (V)"tile"))));
V v_16 = values_15[0];
V v_17 = values_15[1];
if (R.Truth((V)(v_9 <= R.Get(R.Get(v_5, (V)"viewport"), (V)"width")))) {
var values_18 = R.Join(new V[] {}, new Results((V)(v_9 / (V)2.0d)));
v_16 = values_18[0];
} else {
var values_19 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {v_13}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)(v_9 - v_13)}, new Results(v_16))))));
v_16 = values_19[0];
}
if (R.Truth((V)(v_11 <= R.Get(R.Get(v_5, (V)"viewport"), (V)"height")))) {
var values_20 = R.Join(new V[] {}, new Results((V)(v_11 / (V)2.0d)));
v_17 = values_20[0];
} else {
var values_21 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {v_14}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)(v_11 - v_14)}, new Results(v_17))))));
v_17 = values_21[0];
}
return R.Join(new V[] {v_16}, new Results(v_17));
return Results.Empty;
}));
R.Set(v_2, (V)"new", new V((RuleFunction)(args_22 => {
V v_23 = args_22[0];
V v_24 = args_22[1];
V v_25 = args_22[2];
V v_26 = args_22[3];
V v_27 = args_22[4];
var values_28 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"map_width", R.Get(v_23, (V)"width")), new Entry((V)"map_height", R.Get(v_23, (V)"height")), new Entry((V)"viewport", v_24), new Entry((V)"tile", v_25), new Entry((V)"time_constant", (V)0.12d), new Entry((V)"offset_x", (V)0.0d), new Entry((V)"offset_y", (V)0.0d))));
V v_29 = values_28[0];
var values_30 = R.Join(new V[] {}, R.Call(v_3, R.Join(new V[] {v_29, v_26}, new Results(v_27))));
var obj_31 = v_29;
V key_32 = (V)"x";
var obj_33 = v_29;
V key_34 = (V)"y";
R.Set(obj_31, key_32, values_30[0]);
R.Set(obj_33, key_34, values_30[1]);
return R.Join(new V[] {}, new Results(v_29));
return Results.Empty;
})));
R.Set(v_2, (V)"set_presentation_offset", new V((RuleFunction)(args_35 => {
V v_36 = args_35[0];
V v_37 = args_35[1];
V v_38 = args_35[2];
var values_39 = R.Join(new V[] {v_37}, new Results(v_38));
var obj_40 = v_36;
V key_41 = (V)"offset_x";
var obj_42 = v_36;
V key_43 = (V)"offset_y";
R.Set(obj_40, key_41, values_39[0]);
R.Set(obj_42, key_43, values_39[1]);
return Results.Empty;
})));
R.Set(v_2, (V)"set_tile", new V((RuleFunction)(args_44 => {
V v_45 = args_44[0];
V v_46 = args_44[1];
V v_47 = args_44[2];
V v_48 = args_44[3];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_46))).First == (V)"number"), () => (V)(v_46 > (V)0.0d)), () => (V)((V)(v_46 % (V)1.0d) == (V)0.0d))}, new Results((V)"invalid presentation tile size")));
var values_49 = R.Join(new V[] {}, new Results(v_46));
var obj_50 = v_45;
V key_51 = (V)"tile";
R.Set(obj_50, key_51, values_49[0]);
var values_52 = R.Join(new V[] {}, R.Call(v_3, R.Join(new V[] {v_45, v_47}, new Results(v_48))));
var obj_53 = v_45;
V key_54 = (V)"x";
var obj_55 = v_45;
V key_56 = (V)"y";
R.Set(obj_53, key_54, values_52[0]);
R.Set(obj_55, key_56, values_52[1]);
return Results.Empty;
})));
R.Set(v_2, (V)"update", new V((RuleFunction)(args_57 => {
V v_58 = args_57[0];
V v_59 = args_57[1];
V v_60 = args_57[2];
V v_61 = args_57[3];
var values_62 = R.Join(new V[] {}, R.Call(v_3, R.Join(new V[] {v_58, v_60}, new Results(v_61))));
V v_63 = values_62[0];
V v_64 = values_62[1];
var values_65 = R.Join(new V[] {}, new Results((V)((V)1.0d - R.Call(R.Get(rt.Global("math"), (V)"exp"), R.Join(new V[] {}, new Results((V)((-R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {v_59}, new Results((V)0.0d))).First) / R.Get(v_58, (V)"time_constant"))))).First)));
V v_66 = values_65[0];
var values_67 = R.Join(new V[] {}, new Results((V)(R.Get(v_58, (V)"x") + (V)((V)(v_63 - R.Get(v_58, (V)"x")) * v_66))));
var obj_68 = v_58;
V key_69 = (V)"x";
R.Set(obj_68, key_69, values_67[0]);
var values_70 = R.Join(new V[] {}, new Results((V)(R.Get(v_58, (V)"y") + (V)((V)(v_64 - R.Get(v_58, (V)"y")) * v_66))));
var obj_71 = v_58;
V key_72 = (V)"y";
R.Set(obj_71, key_72, values_70[0]);
return Results.Empty;
})));
R.Set(v_2, (V)"tile_top_left", new V((RuleFunction)(args_73 => {
V v_74 = args_73[0];
V v_75 = args_73[1];
V v_76 = args_73[2];
var values_77 = R.Join(new V[] {}, new Results(R.Get(v_74, (V)"viewport")));
V v_78 = values_77[0];
return R.Join(new V[] {(V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)((V)(R.Get(v_78, (V)"x") + (V)(R.Get(v_78, (V)"width") / (V)2.0d)) + (V)((V)(v_75 - (V)1.0d) * R.Get(v_74, (V)"tile"))) - R.Get(v_74, (V)"x")) + (V)0.5d)))).First + R.Get(v_74, (V)"offset_x"))}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)((V)(R.Get(v_78, (V)"y") + (V)(R.Get(v_78, (V)"height") / (V)2.0d)) + (V)((V)(v_76 - (V)1.0d) * R.Get(v_74, (V)"tile"))) - R.Get(v_74, (V)"y")) + (V)0.5d)))).First + R.Get(v_74, (V)"offset_y"))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
