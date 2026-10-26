// Static C# port of src/idea_embers.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: f41240a4df77bbcf48cd6823a516944a58277f641034d14b428c237e7af44a13
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_embers { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_silt"))));
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
var values_12 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:enemy/ember_moth"), new Entry((V)"interval", (V)100.0d), new Entry((V)"ticks", (V)2.0d), new Entry((V)"cap", (V)8.0d))));
V v_13 = values_12[0];
V v_14 = V.Nil;
v_14 = new V((RuleFunction)(args_15 => {
V v_16 = args_15[0];
V v_17 = args_15[1];
var values_18 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"enqueue"), R.Join(new V[] {v_16}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", R.Get(v_17, (V)"next_due")), new Entry((V)"actor_id", (V)0.0d), new Entry((V)"attack_id", R.Get(v_17, (V)"action_id")), new Entry((V)"floor_id", R.Get(v_17, (V)"floor_id")))))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, new Results(R.Get(v_19, (V)"id")));
var obj_21 = v_19;
V key_22 = (V)"effect_id";
R.Set(obj_21, key_22, values_20[0]);

var values_23 = R.Join(new V[] {}, new Results((V)true));
var obj_24 = v_19;
V key_25 = (V)"idea_ember";
R.Set(obj_24, key_25, values_23[0]);

var values_26 = R.Join(new V[] {}, new Results(R.Get(v_19, (V)"id")));
var obj_27 = v_17;
V key_28 = (V)"schedule_id";
R.Set(obj_27, key_28, values_26[0]);
return Results.Empty;
}));
R.Set(v_13, (V)"choose", new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
V v_31 = args_29[1];
V v_32 = args_29[2];
V v_33 = args_29[3];
var values_34 = R.Join(new V[] {R.Get(v_31, (V)"position")}, new Results(R.Get(v_32, (V)"position")));
V v_35 = values_34[0];
V v_36 = values_34[1];
var values_37 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_35, (V)"x") - R.Get(v_36, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_35, (V)"y") - R.Get(v_36, (V)"y"))))).First)));
V v_38 = values_37[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Or(R.Get(R.Get(v_31, (V)"ai_memory"), (V)"ember_dives"), () => (V)0.0d) < (V)2.0d), () => (V)(v_38 >= (V)2.0d)), () => (V)(v_38 <= (V)3.0d)), () => R.Call(R.Get(v_33, (V)"visible"), R.Join(new V[] {R.Get(v_35, (V)"x")}, new Results(R.Get(v_35, (V)"y")))).First), () => R.Call(R.Get(v_33, (V)"visible"), R.Join(new V[] {R.Get(v_36, (V)"x")}, new Results(R.Get(v_36, (V)"y")))).First), () => R.Call(R.Get(v_33, (V)"clear_line"), R.Join(new V[] {v_30, v_35}, new Results(v_36))).First), () => (V)(R.Invoke(R.Get(R.Get(R.Get(v_33, (V)"map"), (V)"rows"), R.Get(v_36, (V)"y")), "sub", R.Join(new V[] {R.Get(v_36, (V)"x")}, new Results(R.Get(v_36, (V)"x")))).First == (V)".")), () => (V)(!R.Truth(R.Call(R.Get(v_4, (V)"protected"), R.Join(new V[] {v_30, R.Get(v_33, (V)"map"), R.Get(v_36, (V)"x")}, new Results(R.Get(v_36, (V)"y")))).First))))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/moth_dive"), new Entry((V)"ranged", (V)"area"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_36, (V)"x")), new Entry((V)"y", R.Get(v_36, (V)"y")))))), new Entry((V)"ember_landing", R.Table(new Entry((V)"floor_id", R.Get(v_35, (V)"floor_id")), new Entry((V)"x", R.Get(v_36, (V)"x")), new Entry((V)"y", R.Get(v_36, (V)"y")))))));
}
if (R.Truth((V)(v_38 == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/moth_touch"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_36, (V)"x")), new Entry((V)"y", R.Get(v_36, (V)"y")))))))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_33, (V)"next_step"), R.Join(new V[] {v_30, v_31}, R.Call(R.Get(v_33, (V)"adjacent_goals"), R.Join(new V[] {v_30, v_31, v_32}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
return Results.Empty;
})));
R.Set(v_13, (V)"fits", new V((RuleFunction)(args_39 => {
V v_40 = args_39[0];
V v_41 = args_39[1];
V v_42 = args_39[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_42, (V)"floor_id") == R.Get(v_40, (V)"active_floor_id")), () => R.Get(R.Get(v_41, (V)"rows"), R.Get(v_42, (V)"y"))), () => (V)(R.Invoke(R.Get(R.Get(v_41, (V)"rows"), R.Get(v_42, (V)"y")), "sub", R.Join(new V[] {R.Get(v_42, (V)"x")}, new Results(R.Get(v_42, (V)"x")))).First == (V)".")), () => (V)(!R.Truth(R.Call(R.Get(v_4, (V)"protected"), R.Join(new V[] {v_40, v_41, R.Get(v_42, (V)"x")}, new Results(R.Get(v_42, (V)"y")))).First)))));
return Results.Empty;
})));
R.Set(v_13, (V)"create", new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
V v_45 = args_43[1];
V v_46 = args_43[2];
V v_47 = args_43[3];
V v_48 = args_43[4];
var values_49 = R.Join(new V[] {}, new Results(R.Get(v_46, (V)"ember_landing")));
V v_50 = values_49[0];
if (R.Truth(R.Or((V)(!R.Truth(R.Call(R.Get(v_13, (V)"fits"), R.Join(new V[] {v_44, v_47}, new Results(v_50))).First)), () => (V)((V)R.Length(R.Or(R.Get(v_44, (V)"idea_embers"), () => R.Table())) >= R.Get(v_13, (V)"cap"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_51 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_44, (V)"idea_embers"), () => R.Table()))))))) {
V v_52 = row_51[0];
V v_53 = row_51[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_53, (V)"floor_id") == R.Get(v_50, (V)"floor_id")), () => (V)(R.Get(v_53, (V)"x") == R.Get(v_50, (V)"x"))), () => (V)(R.Get(v_53, (V)"y") == R.Get(v_50, (V)"y"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
var values_54 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_44, (V)"idea_embers"), () => R.Table())));
var obj_55 = v_44;
V key_56 = (V)"idea_embers";
R.Set(obj_55, key_56, values_54[0]);
var values_57 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"action_id", R.Get(v_46, (V)"action_id")), new Entry((V)"source_id", R.Get(v_45, (V)"id")), new Entry((V)"floor_id", R.Get(v_50, (V)"floor_id")), new Entry((V)"x", R.Get(v_50, (V)"x")), new Entry((V)"y", R.Get(v_50, (V)"y")), new Entry((V)"remaining", R.Get(v_13, (V)"ticks")), new Entry((V)"next_due", (V)(R.Get(v_44, (V)"clock") + R.Get(v_13, (V)"interval"))), new Entry((V)"expires", (V)(R.Get(v_44, (V)"clock") + (V)(R.Get(v_13, (V)"interval") * R.Get(v_13, (V)"ticks")))))));
V v_58 = values_57[0];
R.Call(v_14, R.Join(new V[] {v_44}, new Results(v_58)));

var values_59 = R.Join(new V[] {}, new Results(v_58));
var obj_60 = R.Get(v_44, (V)"idea_embers");
V key_61 = (V)((V)R.Length(R.Get(v_44, (V)"idea_embers")) + (V)1.0d);
R.Set(obj_60, key_61, values_59[0]);
R.Call(v_48, R.Join(new V[] {(V)"EmberCreated"}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_50, (V)"floor_id")), new Entry((V)"x", R.Get(v_50, (V)"x")), new Entry((V)"y", R.Get(v_50, (V)"y")), new Entry((V)"next_due", R.Get(v_58, (V)"next_due")), new Entry((V)"expires", R.Get(v_58, (V)"expires"))))));
return R.Join(new V[] {(V)true}, new Results(v_58));
return Results.Empty;
})));
V v_62 = V.Nil;
v_62 = new V((RuleFunction)(args_63 => {
V v_64 = args_63[0];
V v_65 = args_63[1];
var values_66 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_64, (V)"idea_embers"), v_65)));
V v_67 = values_66[0];
{ double end_70 = R.Num((V)1.0d), step_71 = R.Num((-(V)1.0d));
for (double i_69 = R.Num((V)R.Length(R.Get(v_64, (V)"schedule"))); step_71 > 0 ? i_69 <= end_70 : i_69 >= end_70; i_69 += step_71) {
V v_68 = i_69;
if (R.Truth((V)(R.Get(R.Get(R.Get(v_64, (V)"schedule"), v_68), (V)"id") == R.Get(v_67, (V)"schedule_id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_64, (V)"schedule")}, new Results(v_68)));
}
}
}
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_64, (V)"idea_embers")}, new Results(v_65)));
return Results.Empty;
}));
R.Set(v_13, (V)"prune", new V((RuleFunction)(args_72 => {
V v_73 = args_72[0];
V v_74 = args_72[1];
{ double end_77 = R.Num((V)1.0d), step_78 = R.Num((-(V)1.0d));
for (double i_76 = R.Num((V)R.Length(R.Or(R.Get(v_73, (V)"idea_embers"), () => R.Table()))); step_78 > 0 ? i_76 <= end_77 : i_76 >= end_77; i_76 += step_78) {
V v_75 = i_76;
var values_79 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_73, (V)"idea_embers"), v_75)));
V v_80 = values_79[0];
if (R.Truth(R.And((V)(R.Get(v_80, (V)"floor_id") == R.Get(v_73, (V)"active_floor_id")), () => R.Or((V)(R.Get(v_80, (V)"expires") < R.Get(v_73, (V)"clock")), () => (V)(!R.Truth(R.Call(R.Get(v_13, (V)"fits"), R.Join(new V[] {v_73, v_74}, new Results(v_80))).First)))))) {
R.Call(v_62, R.Join(new V[] {v_73}, new Results(v_75)));
}
}
}
return Results.Empty;
})));
R.Set(v_13, (V)"resolve", new V((RuleFunction)(args_81 => {
V v_82 = args_81[0];
V v_83 = args_81[1];
V v_84 = args_81[2];
V v_85 = args_81[3];
V v_86 = args_81[4];
if (R.Truth((V)(!R.Truth(R.Get(v_83, (V)"idea_ember"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_87 = Results.Empty;
V v_88 = values_87[0];
V v_89 = values_87[1];
foreach (var row_90 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_82, (V)"idea_embers"), () => R.Table()))))))) {
V v_91 = row_90[0];
V v_92 = row_90[1];
if (R.Truth((V)(R.Get(v_92, (V)"schedule_id") == R.Get(v_83, (V)"id")))) {
var values_93 = R.Join(new V[] {v_91}, new Results(v_92));
v_88 = values_93[0];
v_89 = values_93[1];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(v_89, () => (V)(R.Get(v_89, (V)"floor_id") == R.Get(v_82, (V)"active_floor_id"))), () => (V)(R.Get(v_89, (V)"next_due") == R.Get(v_82, (V)"clock")))}, new Results((V)"missing scheduled ember")));
var values_94 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {R.Get(v_89, (V)"x")}, new Results(R.Get(v_89, (V)"y")))));
V v_95 = values_94[0];
R.Call(v_85, R.Join(new V[] {(V)"EmberPulsed", R.Table(new Entry((V)"floor_id", R.Get(v_89, (V)"floor_id")), new Entry((V)"x", R.Get(v_89, (V)"x")), new Entry((V)"y", R.Get(v_89, (V)"y")), new Entry((V)"target_id", R.And(v_95, () => R.Get(v_95, (V)"id"))), new Entry((V)"remaining", (V)(R.Get(v_89, (V)"remaining") - (V)1.0d))), R.Get(v_89, (V)"source_id")}, new Results(R.Get(v_89, (V)"action_id"))));
if (R.Truth(R.And(v_95, () => (V)(R.Get(v_95, (V)"id") != R.Get(v_89, (V)"source_id"))))) {
R.Call(v_86, R.Join(new V[] {v_95, R.Get(v_89, (V)"source_id"), R.Get(v_89, (V)"action_id")}, new Results((V)1.0d)));
}
var values_96 = R.Join(new V[] {}, new Results((V)(R.Get(v_89, (V)"remaining") - (V)1.0d)));
var obj_97 = v_89;
V key_98 = (V)"remaining";
R.Set(obj_97, key_98, values_96[0]);
if (R.Truth((V)(R.Get(v_89, (V)"remaining") > (V)0.0d))) {
var values_99 = R.Join(new V[] {}, new Results((V)(R.Get(v_82, (V)"clock") + R.Get(v_13, (V)"interval"))));
var obj_100 = v_89;
V key_101 = (V)"next_due";
R.Set(obj_100, key_101, values_99[0]);

R.Call(v_14, R.Join(new V[] {v_82}, new Results(v_89)));
} else {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_82, (V)"idea_embers")}, new Results(v_88)));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_13, (V)"validate", new V((RuleFunction)(args_102 => {
V v_103 = args_102[0];
V v_104 = args_102[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_103, (V)"idea_embers") == V.Nil), () => R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_103, (V)"idea_embers")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_103, (V)"idea_embers")) <= R.Get(v_13, (V)"cap"))))}, new Results((V)"invalid ember store")));
var values_105 = R.Join(new V[] {R.Table(), R.Table()}, new Results(R.Table()));
V v_106 = values_105[0];
V v_107 = values_105[1];
V v_108 = values_105[2];
var values_109 = R.Join(new V[] {}, new Results(R.Table()));
V v_110 = values_109[0];
foreach (var row_111 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_103, (V)"schedule"))))))) {
V v_112 = row_111[0];
V v_113 = row_111[1];
var values_114 = R.Join(new V[] {}, new Results(v_113));
var obj_115 = v_110;
V key_116 = (V)((V)R.Length(v_110) + (V)1.0d);
R.Set(obj_115, key_116, values_114[0]);
}
foreach (var row_117 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_103, (V)"floor_pause"), () => R.Table()))))))) {
V v_118 = row_117[0];
V v_119 = row_117[1];
foreach (var row_120 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_119, (V)"schedule"))))))) {
V v_121 = row_120[0];
V v_122 = row_120[1];
var values_123 = R.Join(new V[] {}, new Results(v_122));
var obj_124 = v_110;
V key_125 = (V)((V)R.Length(v_110) + (V)1.0d);
R.Set(obj_124, key_125, values_123[0]);
}
}
foreach (var row_126 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_103, (V)"entities"))))))) {
V v_127 = row_126[0];
V v_128 = row_126[1];
if (R.Truth((V)(R.Get(v_128, (V)"definition_id") == R.Get(v_13, (V)"id")))) {
var values_129 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_128, (V)"ai_memory"), (V)"ember_dives"), () => (V)0.0d)));
V v_130 = values_129[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_130))).First == (V)"number"), () => (V)((V)(v_130 % (V)1.0d) == (V)0.0d)), () => (V)(v_130 >= (V)0.0d)), () => (V)(v_130 <= (V)2.0d))}, new Results((V)"invalid moth dive budget")));
var values_131 = R.Join(new V[] {}, new Results(R.Get(v_128, (V)"pending_attack")));
V v_132 = values_131[0];
if (R.Truth(R.And(v_132, () => R.Or(R.Get(v_132, (V)"ember_landing"), () => (V)(R.Get(v_132, (V)"ability_id") == (V)"core:ability/moth_dive"))))) {
var values_133 = R.Join(new V[] {}, new Results(R.Get(v_132, (V)"ember_landing")));
V v_134 = values_133[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(v_130 >= (V)1.0d), () => v_134), () => (V)(R.Get(v_132, (V)"ability_id") == (V)"core:ability/moth_dive")), () => (V)(R.Get(v_132, (V)"ranged") == (V)"area")), () => (V)(R.Get(v_134, (V)"floor_id") == R.Get(v_132, (V)"floor_id"))), () => (V)((V)R.Length(R.Get(v_132, (V)"target_cells")) == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_132, (V)"target_cells"), (V)1.0d), (V)"x") == R.Get(v_134, (V)"x"))), () => (V)(R.Get(R.Get(R.Get(v_132, (V)"target_cells"), (V)1.0d), (V)"y") == R.Get(v_134, (V)"y"))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_132, (V)"source_x") - R.Get(v_134, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_132, (V)"source_y") - R.Get(v_134, (V)"y"))))).First) >= (V)2.0d)), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_132, (V)"source_x") - R.Get(v_134, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_132, (V)"source_y") - R.Get(v_134, (V)"y"))))).First) <= (V)3.0d))}, new Results((V)"invalid locked moth landing")));
}
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(!R.Truth(R.Get(v_128, (V)"pending_attack"))), () => (V)(!R.Truth(R.Get(R.Get(v_128, (V)"pending_attack"), (V)"ember_landing"))))}, new Results((V)"non-Moth landing")));
}
}
foreach (var row_135 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_103, (V)"idea_embers"), () => R.Table()))))))) {
V v_136 = row_135[0];
V v_137 = row_135[1];
var values_138 = R.Join(new V[] {}, new Results(R.Get(v_104, R.Get(v_137, (V)"floor_id"))));
V v_139 = values_138[0];

var values_140 = R.Join(new V[] {}, R.Call(v_5, R.Join(new V[] {v_103}, new Results(R.Get(v_137, (V)"source_id")))));
V v_141 = values_140[0];
var values_142 = R.Join(new V[] {}, new Results(R.And(R.Get(v_103, (V)"floor_pause"), () => R.Get(R.Get(v_103, (V)"floor_pause"), R.Get(v_137, (V)"floor_id")))));
V v_143 = values_142[0];
var values_144 = R.Join(new V[] {}, new Results(R.Or(R.And(v_143, () => R.Get(v_143, (V)"suspended_at")), () => R.Get(v_103, (V)"clock"))));
V v_145 = values_144[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_139, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"x")))).First == (V)"number")), () => (V)((V)(R.Get(v_137, (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_137, (V)"x") >= (V)1.0d)), () => (V)(R.Get(v_137, (V)"x") <= R.Get(v_139, (V)"width"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(v_137, (V)"y") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_137, (V)"y") >= (V)1.0d)), () => (V)(R.Get(v_137, (V)"y") <= R.Get(v_139, (V)"height"))), () => (V)(R.Invoke(R.Get(R.Get(v_139, (V)"rows"), R.Get(v_137, (V)"y")), "sub", R.Join(new V[] {R.Get(v_137, (V)"x")}, new Results(R.Get(v_137, (V)"x")))).First == (V)".")), () => (V)(!R.Truth(R.Call(R.Get(v_4, (V)"protected"), R.Join(new V[] {v_103, v_139, R.Get(v_137, (V)"x")}, new Results(R.Get(v_137, (V)"y")))).First))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"source_id")))).First == (V)"number")), () => (V)((V)(R.Get(v_137, (V)"source_id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_137, (V)"source_id") >= (V)1.0d)), () => (V)(R.Get(v_137, (V)"source_id") < R.Get(v_103, (V)"next_entity_id"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"action_id")))).First == (V)"number")), () => (V)((V)(R.Get(v_137, (V)"action_id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_137, (V)"action_id") >= (V)1.0d)), () => (V)(R.Get(v_137, (V)"action_id") < R.Get(v_103, (V)"next_action_id"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"remaining")))).First == (V)"number")), () => (V)((V)(R.Get(v_137, (V)"remaining") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_137, (V)"remaining") >= (V)1.0d)), () => (V)(R.Get(v_137, (V)"remaining") <= R.Get(v_13, (V)"ticks"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"next_due")))).First == (V)"number")), () => (V)((V)(R.Get(v_137, (V)"next_due") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_137, (V)"next_due") >= v_145)), () => (V)(R.Get(v_137, (V)"next_due") <= (V)(v_145 + R.Get(v_13, (V)"interval")))), () => (V)(R.Get(v_137, (V)"expires") == (V)(R.Get(v_137, (V)"next_due") + (V)((V)(R.Get(v_137, (V)"remaining") - (V)1.0d) * R.Get(v_13, (V)"interval"))))), () => (V)(!R.Truth(R.Get(v_106, R.Get(v_137, (V)"schedule_id")))))}, new Results((V)"invalid active ember")));
var values_146 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_137, (V)"floor_id"), R.Concat((V)":", R.Concat(R.Get(v_137, (V)"x"), R.Concat((V)",", R.Get(v_137, (V)"y")))))));
V v_147 = values_146[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_107, v_147)))}, new Results((V)"overlapping ember")));

var values_148 = R.Join(new V[] {}, new Results((V)true));
var obj_149 = v_107;
V key_150 = v_147;
R.Set(obj_149, key_150, values_148[0]);
var values_151 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_108, R.Get(v_137, (V)"source_id")), () => (V)0.0d) + (V)1.0d)));
var obj_152 = v_108;
V key_153 = R.Get(v_137, (V)"source_id");
R.Set(obj_152, key_153, values_151[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_108, R.Get(v_137, (V)"source_id")) <= (V)2.0d), () => R.Or((V)(!R.Truth(v_141)), () => R.And(R.And((V)(R.Get(v_141, (V)"definition_id") == R.Get(v_13, (V)"id")), () => (V)(R.Get(R.Get(v_141, (V)"position"), (V)"floor_id") == R.Get(v_137, (V)"floor_id"))), () => (V)(R.Get(v_108, R.Get(v_137, (V)"source_id")) <= R.Or(R.Get(R.Get(v_141, (V)"ai_memory"), (V)"ember_dives"), () => (V)0.0d)))))}, new Results((V)"ember dive budget exceeded")));
var values_154 = R.Join(new V[] {}, new Results((V)false));
V v_155 = values_154[0];
foreach (var row_156 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_110)))))) {
V v_157 = row_156[0];
V v_158 = row_156[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_158, (V)"id") == R.Get(v_137, (V)"schedule_id")), () => (V)(R.Get(v_158, (V)"kind") == (V)"effect")), () => (V)(R.Get(v_158, (V)"idea_ember") == (V)true)), () => (V)(R.Get(v_158, (V)"effect_id") == R.Get(v_158, (V)"id"))), () => (V)(R.Get(v_158, (V)"actor_id") == (V)0.0d)), () => (V)(R.Get(v_158, (V)"attack_id") == R.Get(v_137, (V)"action_id"))), () => (V)(R.Get(v_158, (V)"floor_id") == R.Get(v_137, (V)"floor_id"))), () => (V)(R.Get(v_158, (V)"due") == R.Get(v_137, (V)"next_due"))))) {
var values_159 = R.Join(new V[] {}, new Results((V)true));
v_155 = values_159[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_155}, new Results((V)"missing ember pulse")));

var values_160 = R.Join(new V[] {}, new Results((V)true));
var obj_161 = v_106;
V key_162 = R.Get(v_137, (V)"schedule_id");
R.Set(obj_161, key_162, values_160[0]);
}
foreach (var row_163 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_110)))))) {
V v_164 = row_163[0];
V v_165 = row_163[1];
if (R.Truth(R.Get(v_165, (V)"idea_ember"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_165, (V)"idea_ember") == (V)true), () => R.Get(v_106, R.Get(v_165, (V)"id")))}, new Results((V)"orphaned ember pulse")));
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_13));
return Results.Empty;
} }
