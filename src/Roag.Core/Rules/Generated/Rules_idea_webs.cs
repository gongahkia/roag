// Static C# port of src/idea_webs.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 25c784a953188464fd7b035d1a7b5ca09c81f8811d4e9a28266db032d7b3ad3f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_webs { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"duration", (V)400.0d), new Entry((V)"delay", (V)50.0d), new Entry((V)"cap", (V)8.0d))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results((V)"core:enemy/web_weaver"));
V v_4 = values_3[0];
V v_5 = V.Nil;
v_5 = new V((RuleFunction)(args_6 => {
V v_7 = args_6[0];
V v_8 = args_6[1];
foreach (var row_9 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"entities"))))))) {
V v_10 = row_9[0];
V v_11 = row_9[1];
if (R.Truth((V)(R.Get(v_11, (V)"id") == v_8))) {
return R.Join(new V[] {}, new Results(v_11));
}
}
return Results.Empty;
}));
V v_12 = V.Nil;
v_12 = new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
V v_15 = args_13[1];
V v_16 = args_13[2];
return R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.And((V)(R.Get(v_14, (V)"ax") == R.Get(v_14, (V)"bx")), () => (V)(v_15 == R.Get(v_14, (V)"ax"))), () => (V)(v_16 > R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_14, (V)"ay")}, new Results(R.Get(v_14, (V)"by")))).First)), () => (V)(v_16 < R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Get(v_14, (V)"ay")}, new Results(R.Get(v_14, (V)"by")))).First)), () => R.And(R.And(R.And((V)(R.Get(v_14, (V)"ay") == R.Get(v_14, (V)"by")), () => (V)(v_16 == R.Get(v_14, (V)"ay"))), () => (V)(v_15 > R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_14, (V)"ax")}, new Results(R.Get(v_14, (V)"bx")))).First)), () => (V)(v_15 < R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Get(v_14, (V)"ax")}, new Results(R.Get(v_14, (V)"bx")))).First)))));
return Results.Empty;
}));
R.Set(v_2, (V)"cells", new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_18, (V)"ax")))).First == (V)"number"), () => (V)((V)(R.Get(v_18, (V)"ax") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_18, (V)"ay")))).First == (V)"number")), () => (V)((V)(R.Get(v_18, (V)"ay") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_18, (V)"bx")))).First == (V)"number")), () => (V)((V)(R.Get(v_18, (V)"bx") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_18, (V)"by")))).First == (V)"number")), () => (V)((V)(R.Get(v_18, (V)"by") % (V)1.0d) == (V)0.0d)), () => R.Or((V)(R.Get(v_18, (V)"ax") == R.Get(v_18, (V)"bx")), () => (V)(R.Get(v_18, (V)"ay") == R.Get(v_18, (V)"by")))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_18, (V)"ax") - R.Get(v_18, (V)"bx"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_18, (V)"ay") - R.Get(v_18, (V)"by"))))).First) == (V)4.0d))}, new Results((V)"invalid web anchors")));
var values_20 = R.Join(new V[] {(V)((V)(R.Get(v_18, (V)"bx") - R.Get(v_18, (V)"ax")) / (V)4.0d)}, new Results((V)((V)(R.Get(v_18, (V)"by") - R.Get(v_18, (V)"ay")) / (V)4.0d)));
V v_21 = values_20[0];
V v_22 = values_20[1];
var values_23 = R.Join(new V[] {}, new Results(R.Table()));
V v_24 = values_23[0];
{ double end_27 = R.Num((V)4.0d), step_28 = R.Num((V)1.0d);
for (double i_26 = R.Num((V)0.0d); step_28 > 0 ? i_26 <= end_27 : i_26 >= end_27; i_26 += step_28) {
V v_25 = i_26;
var values_29 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", v_19), new Entry((V)"x", (V)(R.Get(v_18, (V)"ax") + (V)(v_21 * v_25))), new Entry((V)"y", (V)(R.Get(v_18, (V)"ay") + (V)(v_22 * v_25))))));
var obj_30 = v_24;
V key_31 = (V)((V)R.Length(v_24) + (V)1.0d);
R.Set(obj_30, key_31, values_29[0]);
}
}
return R.Join(new V[] {}, new Results(v_24));
return Results.Empty;
})));
V v_32 = V.Nil;
v_32 = new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
V v_35 = args_33[1];
V v_36 = args_33[2];
V v_37 = args_33[3];
foreach (var row_38 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_35, (V)"placements"), () => R.Get(R.Get(v_35, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_39 = row_38[0];
V v_40 = row_38[1];
if (R.Truth(R.And((V)(R.Get(v_40, (V)"x") == v_36), () => (V)(R.Get(v_40, (V)"y") == v_37)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
foreach (var row_41 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_34, (V)"entities"))))))) {
V v_42 = row_41[0];
V v_43 = row_41[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Get(R.Get(v_43, (V)"position"), (V)"floor_id") == R.Get(v_35, (V)"floor_id")), () => (V)(R.Get(R.Get(v_43, (V)"position"), (V)"x") == v_36)), () => (V)(R.Get(R.Get(v_43, (V)"position"), (V)"y") == v_37)), () => (V)(R.Get(v_43, (V)"controller") != (V)"ai")), () => (V)(R.Get(v_43, (V)"controller") != (V)"player")), () => R.Or(R.Get(v_43, (V)"blocks_movement"), () => (V)(R.Get(v_43, (V)"controller") == (V)"exit"))))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
R.Set(v_2, (V)"fits", new V((RuleFunction)(args_44 => {
V v_45 = args_44[0];
V v_46 = args_44[1];
V v_47 = args_44[2];
V v_48 = args_44[3];
var values_49 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"cells"), R.Join(new V[] {v_47}, new Results(R.Get(R.Get(v_46, (V)"position"), (V)"floor_id")))));
V v_50 = values_49[0];
foreach (var row_51 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_50)))))) {
V v_52 = row_51[0];
V v_53 = row_51[1];
if (R.Truth(R.Or(R.Or(R.Or((V)(!R.Truth(R.Get(R.Get(R.Get(v_48, (V)"map"), (V)"rows"), R.Get(v_53, (V)"y")))), () => (V)(R.Invoke(R.Get(R.Get(R.Get(v_48, (V)"map"), (V)"rows"), R.Get(v_53, (V)"y")), "sub", R.Join(new V[] {R.Get(v_53, (V)"x")}, new Results(R.Get(v_53, (V)"x")))).First != (V)".")), () => R.Call(v_32, R.Join(new V[] {v_45, R.Get(v_48, (V)"map"), R.Get(v_53, (V)"x")}, new Results(R.Get(v_53, (V)"y")))).First), () => (V)(!R.Truth(R.Call(R.Get(v_48, (V)"visible"), R.Join(new V[] {R.Get(v_53, (V)"x")}, new Results(R.Get(v_53, (V)"y")))).First))))) {
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth(R.And(R.Or((V)(v_52 == (V)1.0d), () => (V)(v_52 == (V)5.0d)), () => R.Or((V)(!R.Truth(R.Call(R.Get(v_48, (V)"free"), R.Join(new V[] {v_45, R.Get(v_46, (V)"id"), R.Get(v_53, (V)"x")}, new Results(R.Get(v_53, (V)"y")))).First)), () => R.And((V)(R.Get(v_53, (V)"x") == R.Get(R.Get(v_46, (V)"position"), (V)"x")), () => (V)(R.Get(v_53, (V)"y") == R.Get(R.Get(v_46, (V)"position"), (V)"y"))))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {(V)true}, new Results(v_50));
return Results.Empty;
})));
R.Set(v_2, (V)"choose", new V((RuleFunction)(args_54 => {
V v_55 = args_54[0];
V v_56 = args_54[1];
V v_57 = args_54[2];
V v_58 = args_54[3];
var values_59 = R.Join(new V[] {R.Get(v_56, (V)"position")}, new Results(R.Get(v_57, (V)"position")));
V v_60 = values_59[0];
V v_61 = values_59[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Or(R.Get(R.Get(v_56, (V)"ai_memory"), (V)"web_casts"), () => (V)0.0d) < (V)2.0d), () => (V)((V)R.Length(R.Or(R.Get(v_55, (V)"idea_webs"), () => R.Table())) < R.Get(v_2, (V)"cap"))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_60, (V)"x") - R.Get(v_61, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_60, (V)"y") - R.Get(v_61, (V)"y"))))).First) <= (V)5.0d)), () => R.Call(R.Get(v_58, (V)"clear_line"), R.Join(new V[] {v_55, v_60}, new Results(v_61))).First))) {
foreach (var row_62 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"ax", (V)(R.Get(v_61, (V)"x") - (V)2.0d)), new Entry((V)"ay", R.Get(v_61, (V)"y")), new Entry((V)"bx", (V)(R.Get(v_61, (V)"x") + (V)2.0d)), new Entry((V)"by", R.Get(v_61, (V)"y")))), new Entry((V)2, R.Table(new Entry((V)"ax", R.Get(v_61, (V)"x")), new Entry((V)"ay", (V)(R.Get(v_61, (V)"y") - (V)2.0d)), new Entry((V)"bx", R.Get(v_61, (V)"x")), new Entry((V)"by", (V)(R.Get(v_61, (V)"y") + (V)2.0d))))))))))) {
V v_63 = row_62[0];
V v_64 = row_62[1];
var values_65 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"fits"), R.Join(new V[] {v_55, v_56, v_64}, new Results(v_58))));
V v_66 = values_65[0];
V v_67 = values_65[1];
if (R.Truth(v_66)) {
foreach (var row_68 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_55, (V)"idea_webs"), () => R.Table()))))))) {
V v_69 = row_68[0];
V v_70 = row_68[1];
if (R.Truth(R.And((V)(R.Get(v_70, (V)"floor_id") == R.Get(v_60, (V)"floor_id")), () => (V)(R.Get(v_70, (V)"expires") > R.Get(v_55, (V)"clock"))))) {
foreach (var row_71 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_67)))))) {
V v_72 = row_71[0];
V v_73 = row_71[1];
if (R.Truth(R.Or(R.Or(R.Call(v_12, R.Join(new V[] {v_70, R.Get(v_73, (V)"x")}, new Results(R.Get(v_73, (V)"y")))).First, () => R.And((V)(R.Get(v_73, (V)"x") == R.Get(v_70, (V)"ax")), () => (V)(R.Get(v_73, (V)"y") == R.Get(v_70, (V)"ay")))), () => R.And((V)(R.Get(v_73, (V)"x") == R.Get(v_70, (V)"bx")), () => (V)(R.Get(v_73, (V)"y") == R.Get(v_70, (V)"by")))))) {
var values_74 = R.Join(new V[] {}, new Results((V)false));
v_66 = values_74[0];
}
}
}
}
}
if (R.Truth(v_66)) {
var values_75 = R.Join(new V[] {}, new Results((V)"web"));
var obj_76 = v_64;
V key_77 = (V)"kind";
R.Set(obj_76, key_77, values_75[0]);
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/weaver_web"), new Entry((V)"cells", v_67), new Entry((V)"support", v_64))));
}
}
}
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_60, (V)"x") - R.Get(v_61, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_60, (V)"y") - R.Get(v_61, (V)"y"))))).First) == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/weaver_bite"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_61, (V)"x")), new Entry((V)"y", R.Get(v_61, (V)"y")))))))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_58, (V)"next_step"), R.Join(new V[] {v_55, v_56}, R.Call(R.Get(v_58, (V)"adjacent_goals"), R.Join(new V[] {v_55, v_56, v_57}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
return Results.Empty;
})));
R.Set(v_2, (V)"create", new V((RuleFunction)(args_78 => {
V v_79 = args_78[0];
V v_80 = args_78[1];
V v_81 = args_78[2];
V v_82 = args_78[3];
var values_83 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"fits"), R.Join(new V[] {v_79, v_80, R.Get(v_81, (V)"support")}, new Results(v_82))));
V v_84 = values_83[0];
if (R.Truth(R.Or((V)(!R.Truth(v_84)), () => (V)((V)R.Length(R.Or(R.Get(v_79, (V)"idea_webs"), () => R.Table())) >= R.Get(v_2, (V)"cap"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_85 = R.Join(new V[] {}, new Results(R.Get(v_81, (V)"support")));
V v_86 = values_85[0];
foreach (var row_87 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_79, (V)"idea_webs"), () => R.Table()))))))) {
V v_88 = row_87[0];
V v_89 = row_87[1];
if (R.Truth(R.And((V)(R.Get(v_89, (V)"floor_id") == R.Get(R.Get(v_80, (V)"position"), (V)"floor_id")), () => (V)(R.Get(v_89, (V)"expires") > R.Get(v_79, (V)"clock"))))) {
foreach (var row_90 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"cells"), R.Join(new V[] {v_86}, new Results(R.Get(v_89, (V)"floor_id"))))))))) {
V v_91 = row_90[0];
V v_92 = row_90[1];
if (R.Truth(R.Or(R.Or(R.Call(v_12, R.Join(new V[] {v_89, R.Get(v_92, (V)"x")}, new Results(R.Get(v_92, (V)"y")))).First, () => R.And((V)(R.Get(v_92, (V)"x") == R.Get(v_89, (V)"ax")), () => (V)(R.Get(v_92, (V)"y") == R.Get(v_89, (V)"ay")))), () => R.And((V)(R.Get(v_92, (V)"x") == R.Get(v_89, (V)"bx")), () => (V)(R.Get(v_92, (V)"y") == R.Get(v_89, (V)"by")))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
}
}
var values_93 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_79, (V)"idea_webs"), () => R.Table())));
var obj_94 = v_79;
V key_95 = (V)"idea_webs";
R.Set(obj_94, key_95, values_93[0]);
var values_96 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_81, (V)"action_id")), new Entry((V)"source_id", R.Get(v_80, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_80, (V)"position"), (V)"floor_id")), new Entry((V)"ax", R.Get(v_86, (V)"ax")), new Entry((V)"ay", R.Get(v_86, (V)"ay")), new Entry((V)"bx", R.Get(v_86, (V)"bx")), new Entry((V)"by", R.Get(v_86, (V)"by")), new Entry((V)"expires", (V)(R.Get(v_79, (V)"clock") + R.Get(v_2, (V)"duration"))))));
V v_97 = values_96[0];
var values_98 = R.Join(new V[] {}, new Results(v_97));
var obj_99 = R.Get(v_79, (V)"idea_webs");
V key_100 = (V)((V)R.Length(R.Get(v_79, (V)"idea_webs")) + (V)1.0d);
R.Set(obj_99, key_100, values_98[0]);
return R.Join(new V[] {(V)true}, new Results(v_97));
return Results.Empty;
})));
R.Set(v_2, (V)"prune", new V((RuleFunction)(args_101 => {
V v_102 = args_101[0];
V v_103 = args_101[1];
if (R.Truth((V)(!R.Truth(R.Get(v_102, (V)"idea_webs"))))) {
return Results.Empty;
}
{ double end_106 = R.Num((V)1.0d), step_107 = R.Num((-(V)1.0d));
for (double i_105 = R.Num((V)R.Length(R.Get(v_102, (V)"idea_webs"))); step_107 > 0 ? i_105 <= end_106 : i_105 >= end_106; i_105 += step_107) {
V v_104 = i_105;
var values_108 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_102, (V)"idea_webs"), v_104)));
V v_109 = values_108[0];

var values_110 = R.Join(new V[] {}, R.Call(v_5, R.Join(new V[] {v_102}, new Results(R.Get(v_109, (V)"source_id")))));
V v_111 = values_110[0];
var values_112 = R.Join(new V[] {}, new Results(R.Or((V)(!R.Truth(v_111)), () => (V)(R.Get(v_111, (V)"hp") <= (V)0.0d))));
V v_113 = values_112[0];
if (R.Truth((V)(R.Get(v_109, (V)"floor_id") == R.Get(v_102, (V)"active_floor_id")))) {
var values_114 = R.Join(new V[] {}, new Results(R.Or(v_113, () => (V)(R.Get(v_109, (V)"expires") <= R.Get(v_102, (V)"clock")))));
v_113 = values_114[0];
foreach (var row_115 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"cells"), R.Join(new V[] {v_109}, new Results(R.Get(v_109, (V)"floor_id"))))))))) {
V v_116 = row_115[0];
V v_117 = row_115[1];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(R.Get(R.Get(v_103, (V)"rows"), R.Get(v_117, (V)"y")))), () => (V)(R.Invoke(R.Get(R.Get(v_103, (V)"rows"), R.Get(v_117, (V)"y")), "sub", R.Join(new V[] {R.Get(v_117, (V)"x")}, new Results(R.Get(v_117, (V)"x")))).First != (V)".")), () => R.Call(v_32, R.Join(new V[] {v_102, v_103, R.Get(v_117, (V)"x")}, new Results(R.Get(v_117, (V)"y")))).First))) {
var values_118 = R.Join(new V[] {}, new Results((V)true));
v_113 = values_118[0];
}
}
}
if (R.Truth(v_113)) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_102, (V)"idea_webs")}, new Results(v_104)));
}
}
}
return Results.Empty;
})));
R.Set(v_2, (V)"on_event", new V((RuleFunction)(args_119 => {
V v_120 = args_119[0];
V v_121 = args_119[1];
V v_122 = args_119[2];
V v_123 = args_119[3];
if (R.Truth((V)(!R.Truth(R.Get(v_120, (V)"idea_webs"))))) {
return Results.Empty;
}
{ double end_126 = R.Num((V)1.0d), step_127 = R.Num((-(V)1.0d));
for (double i_125 = R.Num((V)R.Length(R.Get(v_120, (V)"idea_webs"))); step_127 > 0 ? i_125 <= end_126 : i_125 >= end_126; i_125 += step_127) {
V v_124 = i_125;
var values_128 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_120, (V)"idea_webs"), v_124)));
V v_129 = values_128[0];
if (R.Truth(R.And((V)(R.Get(v_129, (V)"floor_id") == R.Get(v_121, (V)"floor_id")), () => (V)(R.Get(v_129, (V)"expires") > R.Get(v_121, (V)"time"))))) {
if (R.Truth(R.And(R.And((V)(R.Get(v_121, (V)"kind") == (V)"TileEntered"), () => (V)(R.Get(v_121, (V)"source_id") != R.Get(v_129, (V)"source_id"))), () => R.Call(v_12, R.Join(new V[] {v_129, R.Get(v_121, (V)"x")}, new Results(R.Get(v_121, (V)"y")))).First))) {
var values_130 = R.Join(new V[] {}, R.Call(v_5, R.Join(new V[] {v_120}, new Results(R.Get(v_121, (V)"source_id")))));
V v_131 = values_130[0];
if (R.Truth(R.And(R.And(v_131, () => R.Get(v_131, (V)"hp")), () => (V)(R.Get(v_131, (V)"hp") > (V)0.0d)))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_120, (V)"idea_webs")}, new Results(v_124)));

R.Call(v_123, R.Join(new V[] {v_131}, new Results(R.Get(v_2, (V)"delay"))));
R.Call(v_122, R.Join(new V[] {(V)"WebCrossed", R.Table(new Entry((V)"web_id", R.Get(v_129, (V)"id")), new Entry((V)"target_id", R.Get(v_131, (V)"id")), new Entry((V)"delay", R.Get(v_2, (V)"delay")), new Entry((V)"floor_id", R.Get(v_129, (V)"floor_id")), new Entry((V)"x", R.Get(v_121, (V)"x")), new Entry((V)"y", R.Get(v_121, (V)"y")))}, new Results(R.Get(v_129, (V)"source_id"))));
}
} else if (R.Truth(R.And((V)(R.Get(v_121, (V)"kind") == (V)"AttackPerformed"), () => (V)(R.Get(v_121, (V)"raw_damage") > (V)0.0d)))) {
foreach (var row_132 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_121, (V)"web_cut_cells"), () => R.Table()))))))) {
V v_133 = row_132[0];
V v_134 = row_132[1];
if (R.Truth(R.Or(R.And((V)(R.Get(v_134, (V)"x") == R.Get(v_129, (V)"ax")), () => (V)(R.Get(v_134, (V)"y") == R.Get(v_129, (V)"ay"))), () => R.And((V)(R.Get(v_134, (V)"x") == R.Get(v_129, (V)"bx")), () => (V)(R.Get(v_134, (V)"y") == R.Get(v_129, (V)"by")))))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_120, (V)"idea_webs")}, new Results(v_124)));
R.Call(v_122, R.Join(new V[] {(V)"WebAnchorCut", R.Table(new Entry((V)"web_id", R.Get(v_129, (V)"id")), new Entry((V)"floor_id", R.Get(v_129, (V)"floor_id")), new Entry((V)"x", R.Get(v_134, (V)"x")), new Entry((V)"y", R.Get(v_134, (V)"y")))}, new Results(R.Get(v_121, (V)"source_id"))));
break;
}
}
}
}
}
}
return Results.Empty;
})));
R.Set(v_2, (V)"validate_pending", new V((RuleFunction)(args_135 => {
V v_136 = args_135[0];
V v_137 = args_135[1];
if (R.Truth(R.Or(R.And(R.Get(v_137, (V)"support"), () => (V)(R.Get(R.Get(v_137, (V)"support"), (V)"kind") == (V)"web")), () => (V)(R.Get(v_137, (V)"ability_id") == (V)"core:ability/weaver_web")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Get(v_136, (V)"definition_id") == v_4), () => R.Get(v_137, (V)"support")), () => (V)(R.Get(R.Get(v_137, (V)"support"), (V)"kind") == (V)"web")), () => (V)(R.Get(v_137, (V)"ability_id") == (V)"core:ability/weaver_web")), () => (V)(R.Get(R.Get(v_136, (V)"ai_memory"), (V)"web_casts") >= (V)1.0d))}, new Results((V)"invalid committed web")));
var values_138 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"cells"), R.Join(new V[] {R.Get(v_137, (V)"support")}, new Results(R.Get(v_137, (V)"floor_id")))));
V v_139 = values_138[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Get(v_137, (V)"target_cells")) == (V)R.Length(v_139))}, new Results((V)"invalid committed web cells")));
foreach (var row_140 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_139)))))) {
V v_141 = row_140[0];
V v_142 = row_140[1];
var values_143 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_137, (V)"target_cells"), v_141)));
V v_144 = values_143[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_144, (V)"floor_id") == R.Get(v_142, (V)"floor_id")), () => (V)(R.Get(v_144, (V)"x") == R.Get(v_142, (V)"x"))), () => (V)(R.Get(v_144, (V)"y") == R.Get(v_142, (V)"y")))}, new Results((V)"invalid committed web footprint")));
}
}
return Results.Empty;
})));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_145 => {
V v_146 = args_145[0];
V v_147 = args_145[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_146, (V)"idea_webs") == V.Nil), () => R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_146, (V)"idea_webs")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_146, (V)"idea_webs")) <= R.Get(v_2, (V)"cap"))))}, new Results((V)"invalid web store")));
var values_148 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_149 = values_148[0];
V v_150 = values_148[1];
foreach (var row_151 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_146, (V)"entities"))))))) {
V v_152 = row_151[0];
V v_153 = row_151[1];
if (R.Truth((V)(R.Get(v_153, (V)"definition_id") == v_4))) {
var values_154 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_153, (V)"ai_memory"), (V)"web_casts"), () => (V)0.0d)));
V v_155 = values_154[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_155))).First == (V)"number"), () => (V)((V)(v_155 % (V)1.0d) == (V)0.0d)), () => (V)(v_155 >= (V)0.0d)), () => (V)(v_155 <= (V)2.0d))}, new Results((V)"invalid web budget")));
}
}
foreach (var row_156 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_146, (V)"idea_webs"), () => R.Table()))))))) {
V v_157 = row_156[0];
V v_158 = row_156[1];
var values_159 = R.Join(new V[] {}, R.Call(v_5, R.Join(new V[] {v_146}, new Results(R.Get(v_158, (V)"source_id")))));
V v_160 = values_159[0];

var values_161 = R.Join(new V[] {}, new Results(R.Get(v_147, R.Get(v_158, (V)"floor_id"))));
V v_162 = values_161[0];
var values_163 = R.Join(new V[] {}, new Results(R.And(R.Get(v_146, (V)"floor_pause"), () => R.Get(R.Get(v_146, (V)"floor_pause"), R.Get(v_158, (V)"floor_id")))));
V v_164 = values_163[0];
var values_165 = R.Join(new V[] {}, new Results(R.Or(R.And(v_164, () => R.Get(v_164, (V)"suspended_at")), () => R.Get(v_146, (V)"clock"))));
V v_166 = values_165[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_158, (V)"id")))).First == (V)"number"), () => (V)((V)(R.Get(v_158, (V)"id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_158, (V)"id") >= (V)1.0d)), () => (V)(R.Get(v_158, (V)"id") < R.Get(v_146, (V)"next_action_id"))), () => (V)(!R.Truth(R.Get(v_149, R.Get(v_158, (V)"id"))))), () => v_160), () => (V)(R.Get(v_160, (V)"definition_id") == v_4)), () => (V)(R.Get(R.Get(v_160, (V)"position"), (V)"floor_id") == R.Get(v_158, (V)"floor_id"))), () => v_162), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_158, (V)"expires")))).First == (V)"number")), () => (V)((V)(R.Get(v_158, (V)"expires") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_158, (V)"expires") > v_166)), () => (V)(R.Get(v_158, (V)"expires") <= (V)(v_166 + R.Get(v_2, (V)"duration"))))}, new Results((V)"invalid active web")));
var values_167 = R.Join(new V[] {}, new Results((V)true));
var obj_168 = v_149;
V key_169 = R.Get(v_158, (V)"id");
R.Set(obj_168, key_169, values_167[0]);

var values_170 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_150, R.Get(v_158, (V)"source_id")), () => (V)0.0d) + (V)1.0d)));
var obj_171 = v_150;
V key_172 = R.Get(v_158, (V)"source_id");
R.Set(obj_171, key_172, values_170[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_150, R.Get(v_158, (V)"source_id")) <= R.Get(R.Get(v_160, (V)"ai_memory"), (V)"web_casts")), () => (V)(R.Get(v_150, R.Get(v_158, (V)"source_id")) <= (V)2.0d))}, new Results((V)"web budget exceeded")));
foreach (var row_173 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"cells"), R.Join(new V[] {v_158}, new Results(R.Get(v_158, (V)"floor_id"))))))))) {
V v_174 = row_173[0];
V v_175 = row_173[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Get(v_175, (V)"x") >= (V)1.0d), () => (V)(R.Get(v_175, (V)"x") <= R.Get(v_162, (V)"width"))), () => (V)(R.Get(v_175, (V)"y") >= (V)1.0d)), () => (V)(R.Get(v_175, (V)"y") <= R.Get(v_162, (V)"height"))), () => (V)(R.Invoke(R.Get(R.Get(v_162, (V)"rows"), R.Get(v_175, (V)"y")), "sub", R.Join(new V[] {R.Get(v_175, (V)"x")}, new Results(R.Get(v_175, (V)"x")))).First == (V)"."))}, new Results((V)"invalid saved web cell")));
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
