// Static C# port of tests/idea_enemy_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 399222427c2f73e92eb19e87f04067d879c829c4d828d3e42ba36f5c526f2635
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_enemy_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.playback"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_15 = values_14[0];
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_18))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_18));
}
var values_19 = R.Join(new V[] {}, new Results(R.Table()));
V v_20 = values_19[0];

foreach (var row_21 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_18)))))) {
V v_22 = row_21[0];
V v_23 = row_21[1];
var values_24 = R.Join(new V[] {}, R.Call(v_16, R.Join(new V[] {}, new Results(v_23))));
var obj_25 = v_20;
V key_26 = v_22;
R.Set(obj_25, key_26, values_24[0]);
}

return R.Join(new V[] {}, new Results(v_20));
return Results.Empty;
}));
V v_27 = V.Nil;
v_27 = new V((RuleFunction)(args_28 => {

var values_29 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_30 = values_29[0];
var values_31 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_30))));
V v_32 = values_31[0];

var values_33 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_34 = R.Get(v_32, (V)"position");
V key_35 = (V)"x";
var obj_36 = R.Get(v_32, (V)"position");
V key_37 = (V)"y";
R.Set(obj_34, key_35, values_33[0]);
R.Set(obj_36, key_37, values_33[1]);
var values_38 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_30, (V)"core:enemy/heavy_rammer", (V)6.0d, (V)6.0d}, new Results((V)0.0d))));
V v_39 = values_38[0];
return R.Join(new V[] {v_30, v_32}, new Results(v_39));
return Results.Empty;
}));
V v_40 = V.Nil;
v_40 = new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
V v_43 = args_41[1];
var values_44 = R.Join(new V[] {}, new Results(R.Table()));
V v_45 = values_44[0];

foreach (var row_46 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_42)))))) {
V v_47 = row_46[0];
V v_48 = row_46[1];
if (R.Truth((V)(R.Get(v_48, (V)"kind") == v_43))) {
var values_49 = R.Join(new V[] {}, new Results(v_48));
var obj_50 = v_45;
V key_51 = (V)((V)R.Length(v_45) + (V)1.0d);
R.Set(obj_50, key_51, values_49[0]);
}
}
return R.Join(new V[] {}, new Results(v_45));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Rammer AI is read-only and commits a cardinal locked shove with strict due time"}, new Results(new V((RuleFunction)(args_52 => {

var values_53 = R.Join(new V[] {}, R.Call(v_27, Results.Empty));
V v_54 = values_53[0];
V v_55 = values_53[1];
V v_56 = values_53[2];

var values_57 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_54))));
V v_58 = values_57[0];
var values_59 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_54}, new Results(v_56))));
V v_60 = values_59[0];

R.Call(v_3, R.Join(new V[] {v_60}, R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_54}, new Results(v_56)))));

R.Call(v_3, R.Join(new V[] {v_58}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_54)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_60, (V)"shove"), (V)"dx") == (V)1.0d), () => (V)(R.Get(R.Get(v_60, (V)"shove"), (V)"dy") == (V)0.0d)), () => (V)(R.Get(R.Get(R.Get(v_60, (V)"cells"), (V)1.0d), (V)"x") == (V)7.0d)))));
var values_61 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_54}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_62 = values_61[0];
V v_63 = values_61[1];
V v_64 = values_61[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_54, (V)"clock") == (V)100.0d), () => (V)(R.Get(R.Get(v_56, (V)"pending_attack"), (V)"due") == (V)157.0d)), () => (V)(R.Get(v_55, (V)"hp") == (V)24.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(v_40, R.Join(new V[] {v_64}, new Results((V)"WindupStarted"))).First, (V)1.0d), (V)"shove"), (V)"x") == (V)7.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_54}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_54, (V)"clock") == (V)200.0d), () => (V)(R.Get(v_55, (V)"hp") == (V)22.0d)), () => (V)(R.Get(R.Get(v_55, (V)"position"), (V)"x") == (V)8.0d)), () => (V)(R.Get(R.Get(v_55, (V)"position"), (V)"y") == (V)6.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Rammer impact never tracks a target that left its locked cell"}, new Results(new V((RuleFunction)(args_65 => {

var values_66 = R.Join(new V[] {}, R.Call(v_27, Results.Empty));
V v_67 = values_66[0];
V v_68 = values_66[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_67}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_69 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_67}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d)))))));
V v_70 = values_69[0];
V v_71 = values_69[1];
V v_72 = values_69[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_68, (V)"hp") == (V)24.0d), () => (V)(R.Get(R.Get(v_68, (V)"position"), (V)"x") == (V)7.0d)), () => (V)(R.Get(R.Get(v_68, (V)"position"), (V)"y") == (V)5.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_40, R.Join(new V[] {v_72}, new Results((V)"Displaced"))).First) == (V)0.0d), () => (V)((V)R.Length(R.Call(v_40, R.Join(new V[] {v_72}, new Results((V)"DamageTaken"))).First) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"shove rejects walls, occupants, stairs and exits without moving readiness or floor"}, new Results(new V((RuleFunction)(args_73 => {

var values_74 = R.Join(new V[] {}, R.Call(v_27, Results.Empty));
V v_75 = values_74[0];
V v_76 = values_74[1];
V v_77 = values_74[2];

var values_78 = R.Join(new V[] {}, new Results((V)true));
var obj_79 = R.Get(v_75, (V)"meta");
V key_80 = (V)"eligible";
R.Set(obj_79, key_80, values_78[0]);
var values_81 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_75, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_82 = values_81[0];
var values_83 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_75))));
V v_84 = values_83[0];
var values_85 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"shove_landing"), R.Join(new V[] {v_75, R.Get(v_76, (V)"id"), (V)1.0d}, new Results((V)0.0d))));
V v_86 = values_85[0];
V v_87 = values_85[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_86)), () => (V)(v_87 == (V)"occupied")))));

R.Call(v_3, R.Join(new V[] {v_84}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_75)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_75}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_88 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_75}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_89 = values_88[0];
V v_90 = values_88[1];
V v_91 = values_88[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_76, (V)"position"), (V)"x") == (V)7.0d), () => (V)(R.Get(v_76, (V)"hp") == (V)22.0d)), () => (V)(R.Get(v_75, (V)"clock") == (V)200.0d)), () => R.Get(R.Get(v_75, (V)"meta"), (V)"eligible")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_40, R.Join(new V[] {v_91}, new Results((V)"ShoveBlocked"))).First, (V)1.0d), (V)"reason") == (V)"occupied"))));
var values_92 = R.Join(new V[] {}, new Results((V)9.0d));
var obj_93 = R.Get(v_82, (V)"position");
V key_94 = (V)"x";
R.Set(obj_93, key_94, values_92[0]);
var values_95 = R.Join(new V[] {}, R.Call(v_16, R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"map_for"), R.Join(new V[] {}, new Results(v_75))))));
var obj_96 = v_75;
V key_97 = (V)"generated_map";
R.Set(obj_96, key_97, values_95[0]);
var values_98 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"stairs", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d), new Entry((V)"to_floor_id", (V)"other"), new Entry((V)"to_x", (V)2.0d), new Entry((V)"to_y", (V)2.0d))))))));
var obj_99 = R.Get(v_75, (V)"generated_map");
V key_100 = (V)"placements";
R.Set(obj_99, key_100, values_98[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(rt.Global("select"), R.Join(new V[] {(V)2.0d}, R.Call(R.Get(v_5, (V)"shove_landing"), R.Join(new V[] {v_75, R.Get(v_76, (V)"id"), (V)1.0d}, new Results((V)0.0d))))).First == (V)"stairs"))));
var values_101 = R.Join(new V[] {}, new Results(R.Table()));
var obj_102 = R.Get(R.Get(v_75, (V)"generated_map"), (V)"placements");
V key_103 = (V)"stairs";
R.Set(obj_102, key_103, values_101[0]);
var values_104 = R.Join(new V[] {}, new Results((V)"exit"));
var obj_105 = v_82;
V key_106 = (V)"controller";
R.Set(obj_105, key_106, values_104[0]);

var values_107 = R.Join(new V[] {}, new Results((V)false));
var obj_108 = v_82;
V key_109 = (V)"blocks_movement";
R.Set(obj_108, key_109, values_107[0]);

var values_110 = R.Join(new V[] {}, new Results((V)8.0d));
var obj_111 = R.Get(v_82, (V)"position");
V key_112 = (V)"x";
R.Set(obj_111, key_112, values_110[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(rt.Global("select"), R.Join(new V[] {(V)2.0d}, R.Call(R.Get(v_5, (V)"shove_landing"), R.Join(new V[] {v_75, R.Get(v_76, (V)"id"), (V)1.0d}, new Results((V)0.0d))))).First == (V)"exit"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(rt.Global("select"), R.Join(new V[] {(V)2.0d}, R.Call(R.Get(v_5, (V)"shove_landing"), R.Join(new V[] {v_75, R.Get(v_76, (V)"id"), (V)2.0d}, new Results((V)0.0d))))).First == (V)"invalid direction"))));
var values_113 = R.Join(new V[] {(V)2.0d}, new Results((V)2.0d));
var obj_114 = R.Get(v_76, (V)"position");
V key_115 = (V)"x";
var obj_116 = R.Get(v_76, (V)"position");
V key_117 = (V)"y";
R.Set(obj_114, key_115, values_113[0]);
R.Set(obj_116, key_117, values_113[1]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(rt.Global("select"), R.Join(new V[] {(V)2.0d}, R.Call(R.Get(v_5, (V)"shove_landing"), R.Join(new V[] {v_75, R.Get(v_76, (V)"id"), (-(V)1.0d)}, new Results((V)0.0d))))).First == (V)"wall"))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"forced entry triggers brambles once, collects supplies and keeps the action gate"}, new Results(new V((RuleFunction)(args_118 => {

var values_119 = R.Join(new V[] {}, R.Call(v_27, Results.Empty));
V v_120 = values_119[0];
V v_121 = values_119[1];

var values_122 = R.Join(new V[] {}, R.Call(v_16, R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"map_for"), R.Join(new V[] {}, new Results(v_120))))));
var obj_123 = v_120;
V key_124 = (V)"generated_map";
R.Set(obj_123, key_124, values_122[0]);
var values_125 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_120, (V)"generated_map"), (V)"rows"), (V)6.0d)));
V v_126 = values_125[0];
var values_127 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_126, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)7.0d))).First, R.Concat((V)"b", R.Invoke(v_126, "sub", R.Join(new V[] {}, new Results((V)9.0d))).First))));
var obj_128 = R.Get(R.Get(v_120, (V)"generated_map"), (V)"rows");
V key_129 = (V)6.0d;
R.Set(obj_128, key_129, values_127[0]);
var values_130 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_131 = values_130[0];
var values_132 = R.Join(new V[] {}, new Results((V)12.0d));
var obj_133 = v_121;
V key_134 = (V)"hp";
R.Set(obj_133, key_134, values_132[0]);
R.Call(R.Get(v_131, (V)"spawn_supply"), R.Join(new V[] {v_120, (V)"food", R.Get(v_120, (V)"active_floor_id"), (V)8.0d, (V)6.0d}, new Results(new V((RuleFunction)(args_135 => {


return Results.Empty;
})))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_120}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_136 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_120}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_137 = values_136[0];
V v_138 = values_136[1];
V v_139 = values_136[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_40, R.Join(new V[] {v_139}, new Results((V)"TerrainTriggered"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_40, R.Join(new V[] {v_139}, new Results((V)"SupplyCollected"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_121, (V)"hp") == (V)13.0d), () => (V)(R.Get(v_120, (V)"clock") == (V)200.0d)), () => R.Get(R.Get(R.Get(v_120, (V)"drops"), (V)1.0d), (V)"collected")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a forced snare entry delays the queued player recovery exactly once"}, new Results(new V((RuleFunction)(args_140 => {

var values_141 = R.Join(new V[] {}, R.Call(v_27, Results.Empty));
V v_142 = values_141[0];
V v_143 = values_141[1];
var values_144 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_142, (V)"core:enemy/snare_scribe", (V)10.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_145 = values_144[0];
var values_146 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"source_id", R.Get(v_145, (V)"id")), new Entry((V)"floor_id", R.Get(v_142, (V)"active_floor_id")), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d))))));
var obj_147 = v_142;
V key_148 = (V)"enemy_snares";
R.Set(obj_147, key_148, values_146[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_142}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_149 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_142}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_150 = values_149[0];
V v_151 = values_149[1];
V v_152 = values_149[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_143, (V)"position"), (V)"x") == (V)8.0d), () => (V)(R.Get(v_143, (V)"hp") == (V)21.0d)), () => (V)(R.Get(v_142, (V)"clock") == (V)240.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_40, R.Join(new V[] {v_152}, new Results((V)"EnemySnareTriggered"))).First) == (V)1.0d), () => (V)(!R.Truth(R.Get(v_143, (V)"snare_delay")))), () => (V)((V)R.Length(R.Get(v_142, (V)"enemy_snares")) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_142}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d))))))));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_142, (V)"clock") == (V)340.0d)}, new Results((V)"snare delay applied twice")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Rammer friendly fire shoves an ally and interrupts that ally\u00e2\u0080\u0099s windup"}, new Results(new V((RuleFunction)(args_153 => {

var values_154 = R.Join(new V[] {}, R.Call(v_27, Results.Empty));
V v_155 = values_154[0];
V v_156 = values_154[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_155}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_157 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_155, (V)"core:enemy/mossbound_guard", (V)7.0d, (V)5.0d}, new Results((V)100.0d))));
V v_158 = values_157[0];
var values_159 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_160 = R.Get(v_158, (V)"position");
V key_161 = (V)"x";
var obj_162 = R.Get(v_158, (V)"position");
V key_163 = (V)"y";
R.Set(obj_160, key_161, values_159[0]);
R.Set(obj_162, key_163, values_159[1]);
var values_164 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_155}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d)))))));
V v_165 = values_164[0];
V v_166 = values_164[1];
V v_167 = values_164[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_156, (V)"position"), (V)"x") == (V)7.0d), () => (V)(R.Get(R.Get(v_156, (V)"position"), (V)"y") == (V)5.0d)), () => (V)(R.Get(v_156, (V)"hp") == (V)24.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_158, (V)"position"), (V)"x") == (V)8.0d), () => (V)(R.Get(R.Get(v_158, (V)"position"), (V)"y") == (V)6.0d)), () => (V)(!R.Truth(R.Get(v_158, (V)"pending_attack")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_40, R.Join(new V[] {v_167}, new Results((V)"WindupCancelled"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_40, R.Join(new V[] {v_167}, new Results((V)"DamageTaken"))).First, (V)1.0d), (V)"target_id") == R.Get(v_158, (V)"id")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pending shove round-trips through disk codec and corrupted direction rejects"}, new Results(new V((RuleFunction)(args_168 => {

var values_169 = R.Join(new V[] {}, R.Call(v_27, Results.Empty));
V v_170 = values_169[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_170}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_171 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_170))));
V v_172 = values_171[0];
var values_173 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_172))))))));
V v_174 = values_173[0];
var values_175 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_170}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_176 = values_175[0];
V v_177 = values_175[1];
V v_178 = values_175[2];

var values_179 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_174}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_180 = values_179[0];
V v_181 = values_179[1];
V v_182 = values_179[2];
R.Call(v_3, R.Join(new V[] {v_178}, new Results(v_182)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_170))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_174)))));
foreach (var row_183 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_172, (V)"entities"))))))) {
V v_184 = row_183[0];
V v_185 = row_183[1];
if (R.Truth(R.Get(v_185, (V)"pending_attack"))) {
var values_186 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_187 = R.Get(R.Get(v_185, (V)"pending_attack"), (V)"shove");
V key_188 = (V)"dx";
R.Set(obj_187, key_188, values_186[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_172))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"shove playback slides the victim without changing the authoritative snapshot"}, new Results(new V((RuleFunction)(args_189 => {

var values_190 = R.Join(new V[] {}, R.Call(v_27, Results.Empty));
V v_191 = values_190[0];
V v_192 = values_190[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_191}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_193 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_191}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_194 = values_193[0];
V v_195 = values_193[1];
V v_196 = values_193[2];
var values_197 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_191))));
V v_198 = values_197[0];

var values_199 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), Results.Empty));
V v_200 = values_199[0];
R.Call(R.Get(v_13, (V)"add"), R.Join(new V[] {v_200, v_196}, new Results(R.Get(v_192, (V)"id"))));
var values_201 = R.Join(new V[] {}, new Results((V)false));
V v_202 = values_201[0];
foreach (var row_203 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_200, (V)"items"))))))) {
V v_204 = row_203[0];
V v_205 = row_203[1];
var values_206 = R.Join(new V[] {}, new Results(R.And(R.Get(v_205, (V)"motions"), () => R.Get(R.Get(v_205, (V)"motions"), R.Get(v_192, (V)"id")))));
V v_207 = values_206[0];
if (R.Truth(v_207)) {
var values_208 = R.Join(new V[] {}, new Results((V)true));
v_202 = values_208[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_207, (V)"snap"))), () => (V)((V)R.Length(R.Get(v_207, (V)"path")) == (V)2.0d)))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_202)));
{ double end_211 = R.Num((V)50.0d), step_212 = R.Num((V)1.0d);
for (double i_210 = R.Num((V)1.0d); step_212 > 0 ? i_210 <= end_211 : i_210 >= end_211; i_210 += step_212) {
V v_209 = i_210;
R.Call(R.Get(v_13, (V)"update"), R.Join(new V[] {v_200}, new Results((V)0.02d)));
}
}
R.Call(v_3, R.Join(new V[] {v_198}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_191)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"natural stage replacements are bounded, deterministic and preserved on restore"}, new Results(new V((RuleFunction)(args_213 => {

var values_214 = R.Join(new V[] {}, new Results((V)0.0d));
V v_215 = values_214[0];
foreach (var row_216 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_15, (V)"order"))))))) {
V v_217 = row_216[0];
V v_218 = row_216[1];
{ double end_221 = R.Num((V)5.0d), step_222 = R.Num((V)1.0d);
for (double i_220 = R.Num((V)1.0d); step_222 > 0 ? i_220 <= end_221 : i_220 >= end_221; i_220 += step_222) {
V v_219 = i_220;
var values_223 = R.Join(new V[] {}, new Results((V)(v_218 == R.Get(R.Get(v_15, (V)"order"), (V)1.0d))));
V v_224 = values_223[0];
var values_225 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_219}, new Results(R.Table(new Entry((V)"multifloor", (V)true), new Entry((V)"stage_options", R.Table(new Entry((V)"biome_id", v_218), new Entry((V)"index", R.Or(R.And(v_224, () => (V)1.0d), () => (V)2.0d)), new Entry((V)"route_history", R.Or(R.And(v_224, () => R.Table()), () => R.Table(new Entry(1, new Results(R.Invoke(v_218, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First))))))))))));
V v_226 = values_225[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_226, (V)"generated_map"), (V)"biome_id") == v_218))));
var values_227 = R.Join(new V[] {}, new Results(R.Table()));
V v_228 = values_227[0];
foreach (var row_229 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_226, (V)"entities"))))))) {
V v_230 = row_229[0];
V v_231 = row_229[1];
if (R.Truth((V)(R.Get(v_231, (V)"definition_id") == (V)"core:enemy/heavy_rammer"))) {
var values_232 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_228, R.Get(R.Get(v_231, (V)"position"), (V)"floor_id")), () => (V)0.0d) + (V)1.0d)));
var obj_233 = v_228;
V key_234 = R.Get(R.Get(v_231, (V)"position"), (V)"floor_id");
R.Set(obj_233, key_234, values_232[0]);
var values_235 = R.Join(new V[] {}, new Results((V)(v_215 + (V)1.0d)));
v_215 = values_235[0];
}
}
foreach (var row_236 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_228)))))) {
V v_237 = row_236[0];
V v_238 = row_236[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_238 <= (V)1.0d))));
}
var values_239 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_226))))))))));
V v_240 = values_239[0];
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_226))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_240)))));
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_215 > (V)0.0d)}, new Results((V)"Rammer was never consumed by actual stage population")));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {(V)18.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {(V)18.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"legal travel driver breaks opposed-lane dodge loops without assistance"}, new Results(new V((RuleFunction)(args_241 => {

var values_242 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_243 = values_242[0];
var values_244 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/arcanist"))))));
V v_245 = values_244[0];
var values_246 = R.Join(new V[] {}, R.Call(v_16, R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"map_for"), R.Join(new V[] {}, new Results(v_245))))));
var obj_247 = v_245;
V key_248 = (V)"generated_map";
R.Set(obj_247, key_248, values_246[0]);
var values_249 = R.Join(new V[] {}, new Results(R.Get(v_245, (V)"generated_map")));
V v_250 = values_249[0];
{ double end_253 = R.Num(R.Get(v_250, (V)"height")), step_254 = R.Num((V)1.0d);
for (double i_252 = R.Num((V)1.0d); step_254 > 0 ? i_252 <= end_253 : i_252 >= end_253; i_252 += step_254) {
V v_251 = i_252;
var values_255 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {(V)"#"}, new Results(R.Get(v_250, (V)"width")))));
var obj_256 = R.Get(v_250, (V)"rows");
V key_257 = v_251;
R.Set(obj_256, key_257, values_255[0]);
}
}
var values_258 = R.Join(new V[] {}, new Results((V)"#.............#"));
var obj_259 = R.Get(v_250, (V)"rows");
V key_260 = (V)6.0d;
R.Set(obj_259, key_260, values_258[0]);

var values_261 = R.Join(new V[] {}, new Results((V)"######.########"));
var obj_262 = R.Get(v_250, (V)"rows");
V key_263 = (V)5.0d;
R.Set(obj_262, key_263, values_261[0]);
var values_264 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_245))));
V v_265 = values_264[0];

var values_266 = R.Join(new V[] {(V)7.0d}, new Results((V)5.0d));
var obj_267 = R.Get(v_265, (V)"position");
V key_268 = (V)"x";
var obj_269 = R.Get(v_265, (V)"position");
V key_270 = (V)"y";
R.Set(obj_267, key_268, values_266[0]);
R.Set(obj_269, key_270, values_266[1]);
var values_271 = R.Join(new V[] {}, new Results((V)110.0d));
var obj_272 = R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_245, (V)"core:enemy/thornspitter", (V)3.0d, (V)6.0d}, new Results((V)20.0d))).First;
V key_273 = (V)"speed";
R.Set(obj_272, key_273, values_271[0]);
var values_274 = R.Join(new V[] {}, new Results((V)110.0d));
var obj_275 = R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_245, (V)"core:enemy/thornspitter", (V)12.0d, (V)6.0d}, new Results((V)20.0d))).First;
V key_276 = (V)"speed";
R.Set(obj_275, key_276, values_274[0]);
var values_277 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_245, (V)"meta"), (V)"eligible")));
V v_278 = values_277[0];
var values_279 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", v_245), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table()))));
V v_280 = values_279[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_243, (V)"navigate"), R.Join(new V[] {v_280, new V((RuleFunction)(args_281 => {
V v_282 = args_281[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_282, (V)"x") == (V)11.0d), () => (V)(R.Get(v_282, (V)"y") == (V)6.0d))));
return Results.Empty;
}))}, new Results((V)60.0d)))));
var values_283 = R.Join(new V[] {}, new Results((V)false));
V v_284 = values_283[0];
foreach (var row_285 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_280, (V)"trace"))))))) {
V v_286 = row_285[0];
V v_287 = row_285[1];
if (R.Truth((V)(R.Get(R.Get(v_287, (V)"intent"), (V)"kind") == (V)"dash"))) {
var values_288 = R.Join(new V[] {}, new Results((V)true));
v_284 = values_288[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(v_284, () => (V)(R.Get(v_265, (V)"hp") < R.Get(v_265, (V)"max_hp"))), () => (V)(R.Get(v_265, (V)"hp") > (V)0.0d)), () => (V)(R.Get(R.Get(v_245, (V)"meta"), (V)"eligible") == v_278))}, new Results((V)"driver must accept ordinary risk, without HP/position assistance")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"legal driver holds against fully armoured strikes but still dodges shove threats"}, new Results(new V((RuleFunction)(args_289 => {

var values_290 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_291 = values_290[0];
var values_292 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_293 = values_292[0];

var values_294 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_293))));
V v_295 = values_294[0];

var values_296 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_297 = R.Get(v_295, (V)"position");
V key_298 = (V)"x";
var obj_299 = R.Get(v_295, (V)"position");
V key_300 = (V)"y";
R.Set(obj_297, key_298, values_296[0]);
R.Set(obj_299, key_300, values_296[1]);

var values_301 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_302 = v_295;
V key_303 = (V)"defense";
R.Set(obj_302, key_303, values_301[0]);
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_293, (V)"core:enemy/mossbound_guard", (V)6.0d, (V)6.0d}, new Results((V)50.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_293}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"pending_strikes"), R.Join(new V[] {}, new Results(v_293))).First, (V)1.0d))));
var values_304 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_293))));
V v_305 = values_304[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_291, (V)"escape"), R.Join(new V[] {R.Table(new Entry((V)"state", v_293), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table())), V.Nil}, new Results((V)true))).First)))));

R.Call(v_3, R.Join(new V[] {v_305}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_293)))));
var values_306 = R.Join(new V[] {}, R.Call(v_27, Results.Empty));
V v_307 = values_306[0];
V v_308 = values_306[1];

var values_309 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_310 = v_308;
V key_311 = (V)"defense";
R.Set(obj_310, key_311, values_309[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_307}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_291, (V)"escape"), R.Join(new V[] {R.Table(new Entry((V)"state", v_307), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table())), V.Nil}, new Results((V)true)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(R.Get(R.Get(v_308, (V)"position"), (V)"x") != (V)7.0d), () => (V)(R.Get(R.Get(v_308, (V)"position"), (V)"y") != (V)6.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"legal driver respects projectile corners and uses an area attack against a protected formation"}, new Results(new V((RuleFunction)(args_312 => {

var values_313 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_314 = values_313[0];
var values_315 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/alchemist"))))));
V v_316 = values_315[0];
var values_317 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_316))));
V v_318 = values_317[0];

var values_319 = R.Join(new V[] {(V)6.0d}, new Results((V)6.0d));
var obj_320 = R.Get(v_318, (V)"position");
V key_321 = (V)"x";
var obj_322 = R.Get(v_318, (V)"position");
V key_323 = (V)"y";
R.Set(obj_320, key_321, values_319[0]);
R.Set(obj_322, key_323, values_319[1]);
var values_324 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_316, (V)"core:enemy/mossbound_guard", (V)7.0d, (V)7.0d}, new Results((V)100000.0d))));
V v_325 = values_324[0];
var values_326 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", v_316), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table()))));
V v_327 = values_326[0];

var values_328 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_316))));
V v_329 = values_328[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_314, (V)"attack_adjacent"), R.Join(new V[] {}, new Results(v_327))).First)))));

R.Call(v_3, R.Join(new V[] {v_329}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_316)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_314, (V)"action"), R.Join(new V[] {v_327}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_314, (V)"attack_adjacent"), R.Join(new V[] {}, new Results(v_327))).First, () => (V)(R.Get(v_325, (V)"hp") < R.Get(v_325, (V)"max_hp"))))));
var values_330 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/alchemist"))))));
v_316 = values_330[0];

var values_331 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_316))));
v_318 = values_331[0];
var values_332 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_333 = R.Get(v_318, (V)"position");
V key_334 = (V)"x";
var obj_335 = R.Get(v_318, (V)"position");
V key_336 = (V)"y";
R.Set(obj_333, key_334, values_332[0]);
R.Set(obj_335, key_336, values_332[1]);
var values_337 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_316, (V)"core:enemy/ward_weaver", (V)10.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_338 = values_337[0];
var values_339 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_316, (V)"core:enemy/bodyguard_brute", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_340 = values_339[0];
var values_341 = R.Join(new V[] {}, new Results(R.Get(v_338, (V)"id")));
var obj_342 = v_338;
V key_343 = (V)"squad_id";
R.Set(obj_342, key_343, values_341[0]);

var values_344 = R.Join(new V[] {}, new Results((V)"warded_bodyguards"));
var obj_345 = v_338;
V key_346 = (V)"squad_kind";
R.Set(obj_345, key_346, values_344[0]);
var values_347 = R.Join(new V[] {}, new Results((V)true));
var obj_348 = R.Get(v_338, (V)"ai_memory");
V key_349 = (V)"ward_ready";
R.Set(obj_348, key_349, values_347[0]);
var values_350 = R.Join(new V[] {}, new Results(R.Get(v_338, (V)"id")));
var obj_351 = v_340;
V key_352 = (V)"squad_id";
R.Set(obj_351, key_352, values_350[0]);

var values_353 = R.Join(new V[] {}, new Results(R.Get(v_338, (V)"id")));
var obj_354 = v_340;
V key_355 = (V)"squad_leader_id";
R.Set(obj_354, key_355, values_353[0]);

var values_356 = R.Join(new V[] {}, new Results(R.Get(v_338, (V)"squad_kind")));
var obj_357 = v_340;
V key_358 = (V)"squad_kind";
R.Set(obj_357, key_358, values_356[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))).First, (V)"ward_source"), R.Join(new V[] {v_316}, new Results(v_340))).First == v_338))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_314, (V)"attack_in_reach"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", v_316), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table())))))));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_338, (V)"hp") == (V)2.0d), () => (V)(R.Get(v_340, (V)"hp") == R.Get(v_340, (V)"max_hp")))}, new Results((V)"area attack must reach caster while retaining the primary batch field rule")));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
