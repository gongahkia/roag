// Static C# port of src/playback.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: dc09c7b7c3d28ffd7bdee1c2aa1cac3bd4a1456ba2e10cad158a14baeb131668
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_playback { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"WindupStarted", (V)0.55d), new Entry((V)"AttackPerformed", (V)0.26d), new Entry((V)"DamageTaken", (V)0.2d), new Entry((V)"DamageBlocked", (V)0.2d), new Entry((V)"Died", (V)0.3d), new Entry((V)"WindupCancelled", (V)0.2d), new Entry((V)"BoonActivated", (V)0.22d), new Entry((V)"EffectApplied", (V)0.22d), new Entry((V)"DelayedEffectScheduled", (V)0.18d), new Entry((V)"DelayedEffectResolved", (V)0.28d), new Entry((V)"BoonGranted", (V)0.28d), new Entry((V)"StatusApplied", (V)0.12d), new Entry((V)"StatusTick", (V)0.12d), new Entry((V)"SecretDiscovered", (V)0.48d), new Entry((V)"SecretRewardSpawned", (V)0.28d), new Entry((V)"DoorOpened", (V)0.22d), new Entry((V)"TerrainTriggered", (V)0.12d), new Entry((V)"TerrainBroken", (V)0.2d), new Entry((V)"HeatVentWarned", (V)0.16d), new Entry((V)"HeatVentBurst", (V)0.16d), new Entry((V)"FragileArmed", (V)0.1d), new Entry((V)"FragileCollapsed", (V)0.12d), new Entry((V)"Healed", (V)0.08d), new Entry((V)"SupplyCollected", (V)0.08d), new Entry((V)"ElitePhaseChanged", (V)0.5d), new Entry((V)"EliteDefeated", (V)0.8d), new Entry((V)"RegentPhaseChanged", (V)0.45d), new Entry((V)"RegentDefeated", (V)0.8d), new Entry((V)"PursuerArrived", (V)0.8d))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"DamageTaken", (V)true), new Entry((V)"DamageBlocked", (V)true), new Entry((V)"BoonActivated", (V)true), new Entry((V)"EffectApplied", (V)true), new Entry((V)"StatusApplied", (V)true), new Entry((V)"StatusTick", (V)true), new Entry((V)"DelayedEffectScheduled", (V)true), new Entry((V)"DelayedEffectResolved", (V)true), new Entry((V)"TerrainTriggered", (V)true), new Entry((V)"Healed", (V)true), new Entry((V)"SupplyCollected", (V)true), new Entry((V)"Died", (V)true))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results((V)0.36d));
var obj_8 = v_2;
V key_9 = (V)"max_cascade_seconds";
R.Set(obj_8, key_9, values_7[0]);
var values_10 = R.Join(new V[] {}, new Results((V)0.08d));
var obj_11 = v_2;
V key_12 = (V)"tile_seconds";
R.Set(obj_11, key_12, values_10[0]);
var values_13 = R.Join(new V[] {}, new Results((V)0.32d));
var obj_14 = v_2;
V key_15 = (V)"max_beat_seconds";
R.Set(obj_14, key_15, values_13[0]);
R.Set(v_2, (V)"new", new V((RuleFunction)(args_16 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"items", R.Table()), new Entry((V)"remaining", (V)0.0d), new Entry((V)"poses", R.Table()), new Entry((V)"facings", R.Table()))));
return Results.Empty;
})));
V v_17 = V.Nil;
v_17 = new V((RuleFunction)(args_18 => {
V v_19 = args_18[0];
V v_20 = args_18[1];
if (R.Truth((V)(v_20 < (V)0.0d))) {
return R.Join(new V[] {}, new Results((-(V)1.0d)));
} else if (R.Truth((V)(v_20 > (V)0.0d))) {
return R.Join(new V[] {}, new Results((V)1.0d));
}
return R.Join(new V[] {}, new Results(v_19));
return Results.Empty;
}));
V v_21 = V.Nil;
v_21 = new V((RuleFunction)(args_22 => {
V v_23 = args_22[0];
V v_24 = args_22[1];
var values_25 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_24, (V)"from_x"), () => R.Get(v_24, (V)"source_x"))));
V v_26 = values_25[0];
if (R.Truth((V)(!R.Truth(v_26)))) {
return R.Join(new V[] {}, new Results(v_23));
}
var values_27 = R.Join(new V[] {}, new Results((V)0.0d));
V v_28 = values_27[0];
foreach (var row_29 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_24, (V)"target_cells"), () => R.Table()))))))) {
V v_30 = row_29[0];
V v_31 = row_29[1];
var values_32 = R.Join(new V[] {}, new Results((V)((V)(v_28 + R.Get(v_31, (V)"x")) - v_26)));
v_28 = values_32[0];
}
return R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {v_23}, new Results(v_28))));
return Results.Empty;
}));
V v_33 = V.Nil;
v_33 = new V((RuleFunction)(args_34 => {
V v_35 = args_34[0];
V v_36 = args_34[1];
V v_37 = args_34[2];
V v_38 = args_34[3];
var values_39 = R.Join(new V[] {}, new Results(R.And(R.Get(v_35, (V)"motions"), () => R.Get(R.Get(v_35, (V)"motions"), v_36))));
V v_40 = values_39[0];
if (R.Truth(v_40)) {
var values_41 = R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_40, (V)"path")) - (V)1.0d)));
V v_42 = values_41[0];
var values_43 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_42}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results(v_38))).First / R.Get(v_40, (V)"duration")) * v_42)))).First + (V)1.0d)))));
V v_44 = values_43[0];
{ double end_47 = R.Num(v_44), step_48 = R.Num((V)1.0d);
for (double i_46 = R.Num((V)1.0d); step_48 > 0 ? i_46 <= end_47 : i_46 >= end_47; i_46 += step_48) {
V v_45 = i_46;
var values_49 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {v_37}, new Results(R.Or(R.Get(R.Get(v_40, (V)"facing_dx"), v_45), () => (V)0.0d)))));
v_37 = values_49[0];
}
}
}
foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_35, (V)"events"), () => R.Table(new Entry((V)1, R.Get(v_35, (V)"event")))))))))) {
V v_51 = row_50[0];
V v_52 = row_50[1];
if (R.Truth(R.And((V)(R.Get(v_52, (V)"source_id") == v_36), () => R.Or((V)(R.Get(v_52, (V)"kind") == (V)"WindupStarted"), () => (V)(R.Get(v_52, (V)"kind") == (V)"AttackPerformed"))))) {
var values_53 = R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {v_37}, new Results(v_52))));
v_37 = values_53[0];
}
}
return R.Join(new V[] {}, new Results(v_37));
return Results.Empty;
}));
V v_54 = V.Nil;
v_54 = new V((RuleFunction)(args_55 => {
V v_56 = args_55[0];
if (R.Truth(R.And((V)(R.Get(v_56, (V)"kind") != (V)"Moved"), () => (V)(R.Get(v_56, (V)"kind") != (V)"Displaced")))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_57 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_56, (V)"kind") == (V)"Displaced"), () => R.Get(v_56, (V)"target_id")), () => R.Get(v_56, (V)"source_id"))));
V v_58 = values_57[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Or((V)(!R.Truth(v_58)), () => (V)(!R.Truth(R.Get(v_56, (V)"from_x")))), () => (V)(!R.Truth(R.Get(v_56, (V)"from_y")))), () => (V)(!R.Truth(R.Get(v_56, (V)"x")))), () => (V)(!R.Truth(R.Get(v_56, (V)"y")))))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_59 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_56, (V)"from_x")), new Entry((V)"y", R.Get(v_56, (V)"from_y")))))));
V v_60 = values_59[0];
if (R.Truth(R.Get(v_56, (V)"path"))) {
foreach (var row_61 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_56, (V)"path"))))))) {
V v_62 = row_61[0];
V v_63 = row_61[1];
var values_64 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_63, (V)"x")), new Entry((V)"y", R.Get(v_63, (V)"y")))));
var obj_65 = v_60;
V key_66 = (V)((V)R.Length(v_60) + (V)1.0d);
R.Set(obj_65, key_66, values_64[0]);
}
} else {
var values_67 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_56, (V)"x")), new Entry((V)"y", R.Get(v_56, (V)"y")))));
var obj_68 = v_60;
V key_69 = (V)((V)R.Length(v_60) + (V)1.0d);
R.Set(obj_68, key_69, values_67[0]);
}
var values_70 = R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_56, (V)"kind") == (V)"Displaced"), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_56, (V)"x") - R.Get(v_56, (V)"from_x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_56, (V)"y") - R.Get(v_56, (V)"from_y"))))).First) > (V)1.0d))));
V v_71 = values_70[0];
return R.Join(new V[] {v_58}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_56, (V)"floor_id")), new Entry((V)"path", v_60), new Entry((V)"snap", v_71))));
return Results.Empty;
}));
R.Set(v_2, (V)"add", new V((RuleFunction)(args_72 => {
V v_73 = args_72[0];
V v_74 = args_72[1];
V v_75 = args_72[2];
var values_76 = R.Join(new V[] {R.Table(), R.Table()}, new Results(V.Nil));
V v_77 = values_76[0];
V v_78 = values_76[1];
V v_79 = values_76[2];
var values_80 = R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_73, (V)"items")) + (V)1.0d)));
V v_81 = values_80[0];
var values_82 = R.Join(new V[] {}, new Results(R.Table()));
V v_83 = values_82[0];
V v_84 = V.Nil;
v_84 = new V((RuleFunction)(args_85 => {

if (R.Truth((V)(!R.Truth(v_79)))) {
return Results.Empty;
}
var values_86 = R.Join(new V[] {}, new Results((V)1.0d));
V v_87 = values_86[0];
foreach (var row_88 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_78)))))) {
V v_89 = row_88[0];
V v_90 = row_88[1];
var values_91 = R.Join(new V[] {}, new Results(R.Get(v_77, v_90)));
V v_92 = values_91[0];
var values_93 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {v_87}, new Results((V)((V)R.Length(R.Get(v_92, (V)"path")) - (V)1.0d)))));
v_87 = values_93[0];
var values_94 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_2, (V)"max_beat_seconds")}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, new Results((V)((V)R.Length(R.Get(v_92, (V)"path")) - (V)1.0d)))).First * R.Get(v_2, (V)"tile_seconds"))))));
var obj_95 = v_92;
V key_96 = (V)"duration";
R.Set(obj_95, key_96, values_94[0]);
}
var values_97 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"event", v_79), new Entry((V)"motions", v_77), new Entry((V)"order", v_78), new Entry((V)"duration", R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_2, (V)"max_beat_seconds")}, new Results((V)(v_87 * R.Get(v_2, (V)"tile_seconds"))))).First))));
var obj_98 = R.Get(v_73, (V)"items");
V key_99 = (V)((V)R.Length(R.Get(v_73, (V)"items")) + (V)1.0d);
R.Set(obj_98, key_99, values_97[0]);
var values_100 = R.Join(new V[] {R.Table(), R.Table()}, new Results(V.Nil));
v_77 = values_100[0];
v_78 = values_100[1];
v_79 = values_100[2];
return Results.Empty;
}));
foreach (var row_101 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_74)))))) {
V v_102 = row_101[0];
V v_103 = row_101[1];
var values_104 = R.Join(new V[] {}, R.Call(v_54, R.Join(new V[] {}, new Results(v_103))));
V v_105 = values_104[0];
V v_106 = values_104[1];
if (R.Truth(v_106)) {
var values_107 = R.Join(new V[] {}, new Results(R.Get(v_77, v_105)));
V v_108 = values_107[0];
if (R.Truth(R.And(v_108, () => (V)(R.Get(v_108, (V)"floor_id") != R.Get(v_106, (V)"floor_id"))))) {
R.Call(v_84, Results.Empty);
var values_109 = R.Join(new V[] {}, new Results(V.Nil));
v_108 = values_109[0];
}
if (R.Truth((V)(!R.Truth(v_108)))) {
var values_110 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_106, (V)"floor_id")), new Entry((V)"path", R.Table()), new Entry((V)"facing_dx", R.Table()), new Entry((V)"snap", (V)false))));
v_108 = values_110[0];
var values_111 = R.Join(new V[] {}, new Results(v_108));
var obj_112 = v_77;
V key_113 = v_105;
R.Set(obj_112, key_113, values_111[0]);
var values_114 = R.Join(new V[] {}, new Results(v_105));
var obj_115 = v_78;
V key_116 = (V)((V)R.Length(v_78) + (V)1.0d);
R.Set(obj_115, key_116, values_114[0]);
if (R.Truth((V)(!R.Truth(R.Get(R.Get(v_73, (V)"poses"), v_105))))) {
var values_117 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_106, (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(R.Get(v_106, (V)"path"), (V)1.0d), (V)"x")), new Entry((V)"y", R.Get(R.Get(R.Get(v_106, (V)"path"), (V)1.0d), (V)"y")))));
var obj_118 = R.Get(v_73, (V)"poses");
V key_119 = v_105;
R.Set(obj_118, key_119, values_117[0]);
}
}
if (R.Truth((V)((V)R.Length(R.Get(v_108, (V)"path")) == (V)0.0d))) {
var values_120 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_106, (V)"path"), (V)1.0d)));
var obj_121 = R.Get(v_108, (V)"path");
V key_122 = (V)1.0d;
R.Set(obj_121, key_122, values_120[0]);
} else if (R.Truth(R.Or((V)(R.Get(R.Get(R.Get(v_108, (V)"path"), (V)R.Length(R.Get(v_108, (V)"path"))), (V)"x") != R.Get(R.Get(R.Get(v_106, (V)"path"), (V)1.0d), (V)"x")), () => (V)(R.Get(R.Get(R.Get(v_108, (V)"path"), (V)R.Length(R.Get(v_108, (V)"path"))), (V)"y") != R.Get(R.Get(R.Get(v_106, (V)"path"), (V)1.0d), (V)"y"))))) {
var values_123 = R.Join(new V[] {}, new Results((V)true));
var obj_124 = v_108;
V key_125 = (V)"snap";
R.Set(obj_124, key_125, values_123[0]);
}
{ double end_128 = R.Num((V)R.Length(R.Get(v_106, (V)"path"))), step_129 = R.Num((V)1.0d);
for (double i_127 = R.Num((V)2.0d); step_129 > 0 ? i_127 <= end_128 : i_127 >= end_128; i_127 += step_129) {
V v_126 = i_127;
var values_130 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_108, (V)"path"))));
V v_131 = values_130[0];
var values_132 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_103, (V)"kind") == (V)"Moved"), () => (V)(R.Get(R.Get(R.Get(v_106, (V)"path"), v_126), (V)"x") - R.Get(R.Get(R.Get(v_106, (V)"path"), (V)(v_126 - (V)1.0d)), (V)"x"))), () => (V)0.0d)));
var obj_133 = R.Get(v_108, (V)"facing_dx");
V key_134 = v_131;
R.Set(obj_133, key_134, values_132[0]);
var values_135 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_106, (V)"path"), v_126)));
var obj_136 = R.Get(v_108, (V)"path");
V key_137 = (V)(v_131 + (V)1.0d);
R.Set(obj_136, key_137, values_135[0]);
}
}
var values_138 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_108, (V)"snap"), () => R.Get(v_106, (V)"snap"))));
var obj_139 = v_108;
V key_140 = (V)"snap";
R.Set(obj_139, key_140, values_138[0]);
var values_141 = R.Join(new V[] {}, new Results(R.Or(v_79, () => v_103)));
v_79 = values_141[0];
} else {
var values_142 = R.Join(new V[] {}, new Results(R.Get(v_4, R.Get(v_103, (V)"kind"))));
V v_143 = values_142[0];
if (R.Truth(R.And((V)(R.Get(v_103, (V)"kind") == (V)"Healed"), () => (V)(R.Or(R.Get(v_103, (V)"actual_gain"), () => (V)0.0d) <= (V)0.0d)))) {
var values_144 = R.Join(new V[] {}, new Results(V.Nil));
v_143 = values_144[0];
}
if (R.Truth(R.And((V)(R.Get(v_103, (V)"kind") == (V)"AttackPerformed"), () => (V)(R.Get(v_103, (V)"source_id") == v_75)))) {
R.Call(v_84, Results.Empty);
var values_145 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"event", v_103), new Entry((V)"index", (V)((V)R.Length(R.Get(v_73, (V)"items")) + (V)1.0d)))));
var obj_146 = v_83;
V key_147 = (V)((V)R.Length(v_83) + (V)1.0d);
R.Set(obj_146, key_147, values_145[0]);
} else if (R.Truth(v_143)) {
R.Call(v_84, Results.Empty);
var values_148 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_73, (V)"items"), (V)R.Length(R.Get(v_73, (V)"items")))));
V v_149 = values_148[0];
var values_150 = R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_6, R.Get(v_103, (V)"kind")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_103, (V)"chain_id")))).First == (V)"number")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_103, (V)"time")))).First == (V)"number"))));
V v_151 = values_150[0];
var values_152 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.Or(R.And((V)(R.Get(v_103, (V)"kind") == (V)"WindupStarted"), () => (V)"windup"), () => R.And(v_151, () => (V)"causal_cascade")), () => R.And(R.And((V)(R.Get(v_103, (V)"kind") != (V)"Died"), () => R.Get(v_6, R.Get(v_103, (V)"kind"))), () => R.Get(v_103, (V)"kind"))), () => V.Nil)));
V v_153 = values_152[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_103, (V)"kind") == (V)"Died"), () => (V)((V)R.Length(R.Get(v_73, (V)"items")) >= v_81)), () => (V)(!R.Truth(v_151))), () => v_149), () => (V)(R.Get(R.Get(v_149, (V)"event"), (V)"kind") == (V)"DamageTaken")), () => (V)(!R.Truth(R.Get(v_149, (V)"events")))), () => R.Get(v_103, (V)"target_id")), () => (V)(R.Get(R.Get(v_149, (V)"event"), (V)"target_id") == R.Get(v_103, (V)"target_id"))), () => (V)(R.Get(R.Get(v_149, (V)"event"), (V)"time") == R.Get(v_103, (V)"time"))), () => (V)(R.Get(R.Get(v_149, (V)"event"), (V)"floor_id") == R.Get(v_103, (V)"floor_id"))))) {
var values_154 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_149, (V)"event")), new Entry((V)2, v_103))));
var obj_155 = v_149;
V key_156 = (V)"events";
R.Set(obj_155, key_156, values_154[0]);
var values_157 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Get(v_149, (V)"duration")}, new Results(v_143))));
var obj_158 = v_149;
V key_159 = (V)"duration";
R.Set(obj_158, key_159, values_157[0]);
var values_160 = R.Join(new V[] {}, new Results((V)"lethal"));
var obj_161 = v_149;
V key_162 = (V)"group_kind";
R.Set(obj_161, key_162, values_160[0]);
} else if (R.Truth(R.And(R.And(R.And(R.And(v_153, () => (V)((V)R.Length(R.Get(v_73, (V)"items")) >= v_81)), () => v_149), () => (V)(R.Get(v_149, (V)"group_kind") == v_153)), () => R.Or(R.And((V)(!R.Truth(v_151)), () => (V)(R.Get(R.Get(v_149, (V)"event"), (V)"floor_id") == R.Get(v_103, (V)"floor_id"))), () => R.And(R.And(R.And(v_151, () => (V)(R.Get(R.Get(v_149, (V)"event"), (V)"chain_id") == R.Get(v_103, (V)"chain_id"))), () => (V)(R.Get(R.Get(v_149, (V)"event"), (V)"time") == R.Get(v_103, (V)"time"))), () => (V)(R.Or(R.Get(R.Get(v_149, (V)"event"), (V)"floor_id"), () => R.And(R.Get(R.Get(v_149, (V)"event"), (V)"source_position"), () => R.Get(R.Get(R.Get(v_149, (V)"event"), (V)"source_position"), (V)"floor_id"))) == R.Or(R.Get(v_103, (V)"floor_id"), () => R.And(R.Get(v_103, (V)"source_position"), () => R.Get(R.Get(v_103, (V)"source_position"), (V)"floor_id"))))))))) {
var values_163 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_149, (V)"events"), () => R.Table(new Entry((V)1, R.Get(v_149, (V)"event"))))));
var obj_164 = v_149;
V key_165 = (V)"events";
R.Set(obj_164, key_165, values_163[0]);
var values_166 = R.Join(new V[] {}, new Results(v_103));
var obj_167 = R.Get(v_149, (V)"events");
V key_168 = (V)((V)R.Length(R.Get(v_149, (V)"events")) + (V)1.0d);
R.Set(obj_167, key_168, values_166[0]);
if (R.Truth(R.Get(v_6, R.Get(v_103, (V)"kind")))) {
var values_169 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_2, (V)"max_cascade_seconds")}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Get(v_149, (V)"duration")}, new Results(v_143))).First + (V)0.015d)))));
var obj_170 = v_149;
V key_171 = (V)"duration";
R.Set(obj_170, key_171, values_169[0]);
}
} else {
var values_172 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"event", v_103), new Entry((V)"duration", v_143), new Entry((V)"group_kind", v_153))));
var obj_173 = R.Get(v_73, (V)"items");
V key_174 = (V)((V)R.Length(R.Get(v_73, (V)"items")) + (V)1.0d);
R.Set(obj_173, key_174, values_172[0]);
}
}
}
}
R.Call(v_84, Results.Empty);
foreach (var row_175 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_83)))))) {
V v_176 = row_175[0];
V v_177 = row_175[1];
var values_178 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_73, (V)"items"), R.Get(v_177, (V)"index"))));
V v_179 = values_178[0];
if (R.Truth((V)(!R.Truth(v_179)))) {
var values_180 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"event", R.Get(v_177, (V)"event")), new Entry((V)"duration", R.Get(v_2, (V)"tile_seconds")), new Entry((V)"pose_only", (V)true))));
v_179 = values_180[0];
var values_181 = R.Join(new V[] {}, new Results(v_179));
var obj_182 = R.Get(v_73, (V)"items");
V key_183 = (V)((V)R.Length(R.Get(v_73, (V)"items")) + (V)1.0d);
R.Set(obj_182, key_183, values_181[0]);
} else {
var values_184 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_179, (V)"pose_events"), () => R.Table())));
var obj_185 = v_179;
V key_186 = (V)"pose_events";
R.Set(obj_185, key_186, values_184[0]);
var values_187 = R.Join(new V[] {}, new Results(R.Get(v_177, (V)"event")));
var obj_188 = R.Get(v_179, (V)"pose_events");
V key_189 = (V)((V)R.Length(R.Get(v_179, (V)"pose_events")) + (V)1.0d);
R.Set(obj_188, key_189, values_187[0]);
}
}
if (R.Truth(R.And((V)(R.Get(v_73, (V)"remaining") == (V)0.0d), () => (V)((V)R.Length(R.Get(v_73, (V)"items")) > (V)0.0d)))) {
var values_190 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_73, (V)"items"), (V)1.0d), (V)"duration")));
var obj_191 = v_73;
V key_192 = (V)"remaining";
R.Set(obj_191, key_192, values_190[0]);
}
return Results.Empty;
})));
R.Set(v_2, (V)"current", new V((RuleFunction)(args_193 => {
V v_194 = args_193[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(R.Get(v_194, (V)"items"), (V)1.0d), () => R.Get(R.Get(R.Get(v_194, (V)"items"), (V)1.0d), (V)"event")), () => V.Nil)));
return Results.Empty;
})));
R.Set(v_2, (V)"current_group", new V((RuleFunction)(args_195 => {
V v_196 = args_195[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(R.Get(v_196, (V)"items"), (V)1.0d), () => R.Get(R.Get(R.Get(v_196, (V)"items"), (V)1.0d), (V)"events")), () => V.Nil)));
return Results.Empty;
})));
R.Set(v_2, (V)"current_pose_events", new V((RuleFunction)(args_197 => {
V v_198 = args_197[0];
var values_199 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_198, (V)"items"), (V)1.0d)));
V v_200 = values_199[0];
if (R.Truth((V)(!R.Truth(v_200)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
if (R.Truth((V)(!R.Truth(R.Get(v_200, (V)"pose_events"))))) {
return R.Join(new V[] {}, new Results(R.Or(R.Get(v_200, (V)"events"), () => R.Table(new Entry((V)1, R.Get(v_200, (V)"event"))))));
}
var values_201 = R.Join(new V[] {}, new Results(R.Table()));
V v_202 = values_201[0];
foreach (var row_203 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_200, (V)"events"), () => R.Table(new Entry((V)1, R.Get(v_200, (V)"event")))))))))) {
V v_204 = row_203[0];
V v_205 = row_203[1];
var values_206 = R.Join(new V[] {}, new Results(v_205));
var obj_207 = v_202;
V key_208 = (V)((V)R.Length(v_202) + (V)1.0d);
R.Set(obj_207, key_208, values_206[0]);
}
foreach (var row_209 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_200, (V)"pose_events"))))))) {
V v_210 = row_209[0];
V v_211 = row_209[1];
var values_212 = R.Join(new V[] {}, new Results(v_211));
var obj_213 = v_202;
V key_214 = (V)((V)R.Length(v_202) + (V)1.0d);
R.Set(obj_213, key_214, values_212[0]);
}
return R.Join(new V[] {}, new Results(v_202));
return Results.Empty;
})));
R.Set(v_2, (V)"busy", new V((RuleFunction)(args_215 => {
V v_216 = args_215[0];
return R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_216, (V)"items")) > (V)0.0d)));
return Results.Empty;
})));
V v_217 = V.Nil;
v_217 = new V((RuleFunction)(args_218 => {
V v_219 = args_218[0];
V v_220 = args_218[1];
if (R.Truth(R.Get(v_220, (V)"motions"))) {
foreach (var row_221 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_220, (V)"order"))))))) {
V v_222 = row_221[0];
V v_223 = row_221[1];
var values_224 = R.Join(new V[] {}, R.Call(v_33, R.Join(new V[] {v_220, v_223, R.Or(R.Get(R.Get(v_219, (V)"facings"), v_223), () => (V)1.0d)}, new Results(R.Get(v_220, (V)"duration")))));
var obj_225 = R.Get(v_219, (V)"facings");
V key_226 = v_223;
R.Set(obj_225, key_226, values_224[0]);
}
} else {
foreach (var row_227 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_220, (V)"events"), () => R.Table(new Entry((V)1, R.Get(v_220, (V)"event")))))))))) {
V v_228 = row_227[0];
V v_229 = row_227[1];
if (R.Truth(R.And(R.Get(v_229, (V)"source_id"), () => R.Or((V)(R.Get(v_229, (V)"kind") == (V)"WindupStarted"), () => (V)(R.Get(v_229, (V)"kind") == (V)"AttackPerformed"))))) {
var values_230 = R.Join(new V[] {}, new Results(R.Get(v_229, (V)"source_id")));
V v_231 = values_230[0];
var values_232 = R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {R.Or(R.Get(R.Get(v_219, (V)"facings"), v_231), () => (V)1.0d)}, new Results(v_229))));
var obj_233 = R.Get(v_219, (V)"facings");
V key_234 = v_231;
R.Set(obj_233, key_234, values_232[0]);
}
}
}
if (R.Truth((V)(!R.Truth(R.Get(v_220, (V)"motions"))))) {
return Results.Empty;
}
foreach (var row_235 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_220, (V)"order"))))))) {
V v_236 = row_235[0];
V v_237 = row_235[1];
var values_238 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_220, (V)"motions"), v_237)));
V v_239 = values_238[0];
var values_240 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_239, (V)"path"), (V)R.Length(R.Get(v_239, (V)"path")))));
V v_241 = values_240[0];
var values_242 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_239, (V)"floor_id")), new Entry((V)"x", R.Get(v_241, (V)"x")), new Entry((V)"y", R.Get(v_241, (V)"y")))));
var obj_243 = R.Get(v_219, (V)"poses");
V key_244 = v_237;
R.Set(obj_243, key_244, values_242[0]);
}
return Results.Empty;
}));
R.Set(v_2, (V)"facing", new V((RuleFunction)(args_245 => {
V v_246 = args_245[0];
V v_247 = args_245[1];
V v_248 = args_245[2];
V v_249 = args_245[3];
var values_250 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_246, (V)"facings"), v_247), () => (V)1.0d)));
V v_251 = values_250[0];
var values_252 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_246, (V)"items"), (V)1.0d)));
V v_253 = values_252[0];
if (R.Truth(v_253)) {
return R.Join(new V[] {}, R.Call(v_33, R.Join(new V[] {v_253, v_247, v_251}, new Results((V)(R.Get(v_253, (V)"duration") - R.Get(v_246, (V)"remaining"))))));
}
if (R.Truth((V)(!R.Truth(R.Get(R.Get(v_246, (V)"facings"), v_247))))) {
var values_254 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {v_251}, new Results(R.Or(v_249, () => (V)0.0d)))));
v_251 = values_254[0];
}
return R.Join(new V[] {}, new Results(R.Or(R.And(v_248, () => R.Call(v_21, R.Join(new V[] {v_251}, new Results(v_248))).First), () => v_251)));
return Results.Empty;
})));
R.Set(v_2, (V)"update", new V((RuleFunction)(args_255 => {
V v_256 = args_255[0];
V v_257 = args_255[1];
if (R.Truth((V)((V)R.Length(R.Get(v_256, (V)"items")) == (V)0.0d))) {
return Results.Empty;
}
var values_258 = R.Join(new V[] {}, new Results((V)(R.Get(v_256, (V)"remaining") - R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results(v_257))).First)));
var obj_259 = v_256;
V key_260 = (V)"remaining";
R.Set(obj_259, key_260, values_258[0]);
while (R.Truth(R.And((V)(R.Get(v_256, (V)"remaining") <= (V)0.0d), () => (V)((V)R.Length(R.Get(v_256, (V)"items")) > (V)0.0d)))) {
R.Call(v_217, R.Join(new V[] {v_256}, R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_256, (V)"items")}, new Results((V)1.0d)))));
if (R.Truth((V)((V)R.Length(R.Get(v_256, (V)"items")) > (V)0.0d))) {
var values_261 = R.Join(new V[] {}, new Results((V)(R.Get(v_256, (V)"remaining") + R.Get(R.Get(R.Get(v_256, (V)"items"), (V)1.0d), (V)"duration"))));
var obj_262 = v_256;
V key_263 = (V)"remaining";
R.Set(obj_262, key_263, values_261[0]);
} else {
var values_264 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_265 = v_256;
V key_266 = (V)"remaining";
R.Set(obj_265, key_266, values_264[0]);
var values_267 = R.Join(new V[] {}, new Results(R.Table()));
var obj_268 = v_256;
V key_269 = (V)"poses";
R.Set(obj_268, key_269, values_267[0]);
}
}
return Results.Empty;
})));
R.Set(v_2, (V)"position", new V((RuleFunction)(args_270 => {
V v_271 = args_270[0];
V v_272 = args_270[1];
V v_273 = args_270[2];
V v_274 = args_270[3];
V v_275 = args_270[4];
var values_276 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_271, (V)"poses"), v_272)));
V v_277 = values_276[0];
if (R.Truth(R.Or((V)(!R.Truth(v_277)), () => (V)(R.Get(v_277, (V)"floor_id") != v_273)))) {
return R.Join(new V[] {v_274}, new Results(v_275));
}
var values_278 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_271, (V)"items"), (V)1.0d)));
V v_279 = values_278[0];
var values_280 = R.Join(new V[] {}, new Results(R.And(R.And(v_279, () => R.Get(v_279, (V)"motions")), () => R.Get(R.Get(v_279, (V)"motions"), v_272))));
V v_281 = values_280[0];
if (R.Truth((V)(!R.Truth(v_281)))) {
return R.Join(new V[] {R.Get(v_277, (V)"x")}, new Results(R.Get(v_277, (V)"y")));
}
var values_282 = R.Join(new V[] {}, new Results(R.Get(v_281, (V)"path")));
V v_283 = values_282[0];
if (R.Truth(R.Or(R.Get(v_281, (V)"snap"), () => (V)((V)R.Length(v_283) < (V)2.0d)))) {
return R.Join(new V[] {R.Get(v_277, (V)"x")}, new Results(R.Get(v_277, (V)"y")));
}
var values_284 = R.Join(new V[] {}, new Results((V)((V)R.Length(v_283) - (V)1.0d)));
V v_285 = values_284[0];
var values_286 = R.Join(new V[] {}, new Results((V)(R.Get(v_279, (V)"duration") - R.Get(v_271, (V)"remaining"))));
V v_287 = values_286[0];
var values_288 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(v_287 / R.Get(v_281, (V)"duration"))))))).First * v_285)));
V v_289 = values_288[0];
var values_290 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_285}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results(v_289))).First + (V)1.0d)))));
V v_291 = values_290[0];
var values_292 = R.Join(new V[] {}, new Results((V)(v_289 - (V)(v_291 - (V)1.0d))));
V v_293 = values_292[0];
var values_294 = R.Join(new V[] {}, new Results((V)((V)(v_293 * v_293) * (V)((V)3.0d - (V)((V)2.0d * v_293)))));
v_293 = values_294[0];
return R.Join(new V[] {(V)(R.Get(R.Get(v_283, v_291), (V)"x") + (V)((V)(R.Get(R.Get(v_283, (V)(v_291 + (V)1.0d)), (V)"x") - R.Get(R.Get(v_283, v_291), (V)"x")) * v_293))}, new Results((V)(R.Get(R.Get(v_283, v_291), (V)"y") + (V)((V)(R.Get(R.Get(v_283, (V)(v_291 + (V)1.0d)), (V)"y") - R.Get(R.Get(v_283, v_291), (V)"y")) * v_293))));
return Results.Empty;
})));
R.Set(v_2, (V)"visible_position", new V((RuleFunction)(args_295 => {
V v_296 = args_295[0];
V v_297 = args_295[1];
V v_298 = args_295[2];
V v_299 = args_295[3];
V v_300 = args_295[4];
V v_301 = args_295[5];
var values_302 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"position"), R.Join(new V[] {v_296, v_297, v_298, v_299}, new Results(v_300))));
V v_303 = values_302[0];
V v_304 = values_302[1];
return R.Join(new V[] {v_303, v_304}, new Results(R.And(R.Call(v_301, R.Join(new V[] {v_299}, new Results(v_300))).First, () => R.Call(v_301, R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_303 + (V)0.5d)))).First}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_304 + (V)0.5d)))))).First)));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
