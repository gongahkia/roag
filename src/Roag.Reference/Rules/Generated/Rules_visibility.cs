// Static C# port of src/visibility.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: c04d846d8d140db79a42d288a524fc86e47d09af183f41a3cd0fce8b2d2e42a4
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_visibility { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table()));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results((V)8.0d));
var obj_6 = v_4;
V key_7 = (V)"radius";
R.Set(obj_6, key_7, values_5[0]);
V v_8 = V.Nil;
v_8 = new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
V v_11 = args_9[1];
V v_12 = args_9[2];
return R.Join(new V[] {}, new Results((V)((V)((V)(v_12 - (V)1.0d) * R.Get(v_10, (V)"width")) + v_11)));
return Results.Empty;
}));
V v_13 = V.Nil;
v_13 = new V((RuleFunction)(args_14 => {
V v_15 = args_14[0];
V v_16 = args_14[1];
V v_17 = args_14[2];
V v_18 = args_14[3];
V v_19 = args_14[4];
V v_20 = args_14[5];
var values_21 = R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_19 - v_17)))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_20 - v_18)))));
V v_22 = values_21[0];
V v_23 = values_21[1];
var values_24 = R.Join(new V[] {R.Or(R.And((V)(v_17 < v_19), () => (V)1.0d), () => (-(V)1.0d))}, new Results(R.Or(R.And((V)(v_18 < v_20), () => (V)1.0d), () => (-(V)1.0d))));
V v_25 = values_24[0];
V v_26 = values_24[1];
var values_27 = R.Join(new V[] {}, new Results((V)(v_22 - v_23)));
V v_28 = values_27[0];
var values_29 = R.Join(new V[] {v_17}, new Results(v_18));
V v_30 = values_29[0];
V v_31 = values_29[1];
while (R.Truth(R.Or((V)(v_30 != v_19), () => (V)(v_31 != v_20)))) {
var values_32 = R.Join(new V[] {}, new Results((V)((V)2.0d * v_28)));
V v_33 = values_32[0];
var values_34 = R.Join(new V[] {v_30}, new Results(v_31));
V v_35 = values_34[0];
V v_36 = values_34[1];
if (R.Truth((V)(v_33 > (-v_23)))) {
var values_37 = R.Join(new V[] {}, new Results((V)(v_28 - v_23)));
v_28 = values_37[0];

var values_38 = R.Join(new V[] {}, new Results((V)(v_30 + v_25)));
v_30 = values_38[0];
}
if (R.Truth((V)(v_33 < v_22))) {
var values_39 = R.Join(new V[] {}, new Results((V)(v_28 + v_22)));
v_28 = values_39[0];

var values_40 = R.Join(new V[] {}, new Results((V)(v_31 + v_26)));
v_31 = values_40[0];
}
if (R.Truth(R.And(R.And(R.And((V)(v_30 != v_35), () => (V)(v_31 != v_36)), () => R.Call(R.Get(v_2, (V)"blocks_vision"), R.Join(new V[] {v_16, v_30, v_36}, new Results(v_15))).First), () => R.Call(R.Get(v_2, (V)"blocks_vision"), R.Join(new V[] {v_16, v_35, v_31}, new Results(v_15))).First))) {
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth(R.And(R.Or((V)(v_30 != v_19), () => (V)(v_31 != v_20)), () => R.Call(R.Get(v_2, (V)"blocks_vision"), R.Join(new V[] {v_16, v_30, v_31}, new Results(v_15))).First))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
R.Set(v_4, (V)"can_see", new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
V v_43 = args_41[1];
V v_44 = args_41[2];
V v_45 = args_41[3];
var values_46 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_42))));
V v_47 = values_46[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or((V)(!R.Truth(v_47)), () => (V)(v_43 != R.Get(v_42, (V)"active_floor_id"))), () => (V)(v_44 < (V)1.0d)), () => (V)(v_44 > R.Get(v_47, (V)"width"))), () => (V)(v_45 < (V)1.0d)), () => (V)(v_45 > R.Get(v_47, (V)"height"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_48 = Results.Empty;
V v_49 = values_48[0];
foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_42, (V)"entities"))))))) {
V v_51 = row_50[0];
V v_52 = row_50[1];
if (R.Truth((V)(R.Get(v_52, (V)"id") == R.Get(v_42, (V)"player_id")))) {
var values_53 = R.Join(new V[] {}, new Results(v_52));
v_49 = values_53[0];

break;
}
}
if (R.Truth((V)(!R.Truth(v_49)))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_54 = R.Join(new V[] {}, new Results(R.Get(v_49, (V)"position")));
V v_55 = values_54[0];
var values_56 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_42, (V)"visible"), () => R.Get(R.Get(v_42, (V)"visible"), (V)"radius")), () => R.Get(v_4, (V)"radius"))));
V v_57 = values_56[0];
return R.Join(new V[] {}, new Results(R.And((V)((V)((V)Math.Pow(R.Num((V)(v_44 - R.Get(v_55, (V)"x"))), R.Num((V)2.0d)) + (V)Math.Pow(R.Num((V)(v_45 - R.Get(v_55, (V)"y"))), R.Num((V)2.0d))) <= (V)Math.Pow(R.Num(v_57), R.Num((V)2.0d))), () => R.Call(v_13, R.Join(new V[] {v_42, v_47, R.Get(v_55, (V)"x"), R.Get(v_55, (V)"y"), v_44}, new Results(v_45))).First)));
return Results.Empty;
})));
R.Set(v_4, (V)"recompute", new V((RuleFunction)(args_58 => {
V v_59 = args_58[0];
V v_60 = args_58[1];
var values_61 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_59))).First}, new Results((V)"missing visibility map"))));
V v_62 = values_61[0];
var values_63 = Results.Empty;
V v_64 = values_63[0];
foreach (var row_65 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_59, (V)"entities"))))))) {
V v_66 = row_65[0];
V v_67 = row_65[1];
if (R.Truth((V)(R.Get(v_67, (V)"id") == R.Get(v_59, (V)"player_id")))) {
var values_68 = R.Join(new V[] {}, new Results(v_67));
v_64 = values_68[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_64}, new Results((V)"missing visibility observer")));
var values_69 = R.Join(new V[] {}, new Results(R.Or(v_60, () => R.Get(v_4, (V)"radius"))));
v_60 = values_69[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_60))).First == (V)"number"), () => (V)((V)(v_60 % (V)1.0d) == (V)0.0d)), () => (V)(v_60 >= (V)1.0d)), () => (V)(v_60 <= (V)32.0d))}, new Results((V)"invalid sight radius")));
var values_70 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_64, (V)"position"), (V)"floor_id")));
V v_71 = values_70[0];
var values_72 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_59, (V)"explored"), () => R.Table())));
var obj_73 = v_59;
V key_74 = (V)"explored";
R.Set(obj_73, key_74, values_72[0]);
var values_75 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_59, (V)"explored"), v_71)));
V v_76 = values_75[0];
if (R.Truth((V)(!R.Truth(v_76)))) {
var values_77 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"cells", R.Table()), new Entry((V)"order", R.Table()))));
v_76 = values_77[0];
var values_78 = R.Join(new V[] {}, new Results(v_76));
var obj_79 = R.Get(v_59, (V)"explored");
V key_80 = v_71;
R.Set(obj_79, key_80, values_78[0]);
}
var values_81 = R.Join(new V[] {}, new Results(R.Table()));
V v_82 = values_81[0];
var values_83 = R.Join(new V[] {R.Get(R.Get(v_64, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_64, (V)"position"), (V)"y")));
V v_84 = values_83[0];
V v_85 = values_83[1];
{ double end_88 = R.Num(R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_62, (V)"height")}, new Results((V)(v_85 + v_60)))).First), step_89 = R.Num((V)1.0d);
for (double i_87 = R.Num(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, new Results((V)(v_85 - v_60)))).First); step_89 > 0 ? i_87 <= end_88 : i_87 >= end_88; i_87 += step_89) {
V v_86 = i_87;
{ double end_92 = R.Num(R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_62, (V)"width")}, new Results((V)(v_84 + v_60)))).First), step_93 = R.Num((V)1.0d);
for (double i_91 = R.Num(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, new Results((V)(v_84 - v_60)))).First); step_93 > 0 ? i_91 <= end_92 : i_91 >= end_92; i_91 += step_93) {
V v_90 = i_91;
var values_94 = R.Join(new V[] {(V)(v_90 - v_84)}, new Results((V)(v_86 - v_85)));
V v_95 = values_94[0];
V v_96 = values_94[1];
if (R.Truth(R.And((V)((V)((V)(v_95 * v_95) + (V)(v_96 * v_96)) <= (V)(v_60 * v_60)), () => R.Call(v_13, R.Join(new V[] {v_59, v_62, v_84, v_85, v_90}, new Results(v_86))).First))) {
var values_97 = R.Join(new V[] {}, R.Call(v_8, R.Join(new V[] {v_62, v_90}, new Results(v_86))));
V v_98 = values_97[0];
var values_99 = R.Join(new V[] {}, new Results((V)true));
var obj_100 = v_82;
V key_101 = v_98;
R.Set(obj_100, key_101, values_99[0]);
if (R.Truth((V)(!R.Truth(R.Get(R.Get(v_76, (V)"cells"), v_98))))) {
var values_102 = R.Join(new V[] {}, new Results((V)true));
var obj_103 = R.Get(v_76, (V)"cells");
V key_104 = v_98;
R.Set(obj_103, key_104, values_102[0]);
var values_105 = R.Join(new V[] {}, new Results(v_98));
var obj_106 = R.Get(v_76, (V)"order");
V key_107 = (V)((V)R.Length(R.Get(v_76, (V)"order")) + (V)1.0d);
R.Set(obj_106, key_107, values_105[0]);
}
}
}
}
}
}
var values_108 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", v_71), new Entry((V)"cells", v_82), new Entry((V)"radius", v_60))));
var obj_109 = v_59;
V key_110 = (V)"visible";
R.Set(obj_109, key_110, values_108[0]);
return Results.Empty;
})));
R.Set(v_4, (V)"is_visible", new V((RuleFunction)(args_111 => {
V v_112 = args_111[0];
V v_113 = args_111[1];
V v_114 = args_111[2];
V v_115 = args_111[3];
var values_116 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_112))));
V v_117 = values_116[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.And(R.And(R.And(R.And(v_117, () => (V)(v_113 == R.Get(R.Get(v_112, (V)"visible"), (V)"floor_id"))), () => (V)(v_114 >= (V)1.0d)), () => (V)(v_114 <= R.Get(v_117, (V)"width"))), () => (V)(v_115 >= (V)1.0d)), () => (V)(v_115 <= R.Get(v_117, (V)"height"))), () => (V)(R.Get(R.Get(R.Get(v_112, (V)"visible"), (V)"cells"), R.Call(v_8, R.Join(new V[] {v_117, v_114}, new Results(v_115))).First) == (V)true)), () => (V)false)));
return Results.Empty;
})));
R.Set(v_4, (V)"was_explored", new V((RuleFunction)(args_118 => {
V v_119 = args_118[0];
V v_120 = args_118[1];
V v_121 = args_118[2];
V v_122 = args_118[3];
var values_123 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_119))));
V v_124 = values_123[0];
var values_125 = R.Join(new V[] {}, new Results(R.And(R.Get(v_119, (V)"explored"), () => R.Get(R.Get(v_119, (V)"explored"), v_120))));
V v_126 = values_125[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.And(R.And(R.And(R.And(v_124, () => v_126), () => (V)(v_121 >= (V)1.0d)), () => (V)(v_121 <= R.Get(v_124, (V)"width"))), () => (V)(v_122 >= (V)1.0d)), () => (V)(v_122 <= R.Get(v_124, (V)"height"))), () => (V)(R.Get(R.Get(v_126, (V)"cells"), R.Call(v_8, R.Join(new V[] {v_124, v_121}, new Results(v_122))).First) == (V)true)), () => (V)false)));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
