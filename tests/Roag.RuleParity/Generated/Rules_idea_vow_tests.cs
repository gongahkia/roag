// Static C# port of tests/idea_vow_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 15d773bec57b48215e9d14404fb55ee545939325904cdb76b45a1149f100b608
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_vow_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_companions"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_17 = values_16[0];
var values_18 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, new Results((V)"core:boon/pilgrims_vow"));
V v_21 = values_20[0];
V v_22 = V.Nil;
v_22 = new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
var values_25 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", v_24))))));
V v_26 = values_25[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_26, R.Get(v_26, (V)"player_id"), v_21, (V)"rare"}, new Results((V)1.0d)));
return R.Join(new V[] {v_26}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_26))));
return Results.Empty;
}));
V v_27 = V.Nil;
v_27 = new V((RuleFunction)(args_28 => {
V v_29 = args_28[0];
V v_30 = args_28[1];
V v_31 = args_28[2];
var values_32 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_29, (V)"core:enemy/mossbound_guard", R.Or(v_30, () => (V)3.0d), R.Or(v_31, () => (V)2.0d)}, new Results((V)100000.0d))));
V v_33 = values_32[0];
var values_34 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_35 = v_33;
V key_36 = (V)"hp";
R.Set(obj_35, key_36, values_34[0]);

var values_37 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_38 = v_33;
V key_39 = (V)"max_hp";
R.Set(obj_38, key_39, values_37[0]);

return R.Join(new V[] {}, new Results(v_33));
return Results.Empty;
}));
V v_40 = V.Nil;
v_40 = new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
V v_43 = args_41[1];
var values_44 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_42}, new Results(v_43))));
V v_45 = values_44[0];
V v_46 = values_44[1];
V v_47 = values_44[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_45}, new Results(v_46)));

return R.Join(new V[] {}, new Results(v_47));
return Results.Empty;
}));
V v_48 = V.Nil;
v_48 = new V((RuleFunction)(args_49 => {
V v_50 = args_49[0];
V v_51 = args_49[1];
var values_52 = R.Join(new V[] {}, new Results((V)0.0d));
V v_53 = values_52[0];

foreach (var row_54 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_50)))))) {
V v_55 = row_54[0];
V v_56 = row_54[1];
if (R.Truth((V)(R.Get(v_56, (V)"kind") == v_51))) {
var values_57 = R.Join(new V[] {}, new Results((V)(v_53 + (V)1.0d)));
v_53 = values_57[0];
}
}

return R.Join(new V[] {}, new Results(v_53));
return Results.Empty;
}));
V v_58 = V.Nil;
v_58 = new V((RuleFunction)(args_59 => {
V v_60 = args_59[0];
foreach (var row_61 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_60)))))) {
V v_62 = row_61[0];
V v_63 = row_61[1];
if (R.Truth((V)(R.Get(v_63, (V)"kind") == (V)"AttackPerformed"))) {
return R.Join(new V[] {}, new Results(v_63));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"no immediate attack")));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"the real game callbacks compile within LuaJIT upvalue limits after adding vow controls"}, new Results(new V((RuleFunction)(args_64 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(rt.Global("loadfile"), R.Join(new V[] {}, new Results((V)"main.lua")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Pilgrim preview shows exact HP payment and bonus without altering run, RNG or recovery"}, new Results(new V((RuleFunction)(args_65 => {

var values_66 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_67 = values_66[0];
V v_68 = values_66[1];

R.Call(v_27, R.Join(new V[] {}, new Results(v_67)));
var values_69 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_67))));
V v_70 = values_69[0];

var values_71 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_67, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_72 = values_71[0];
{ double end_75 = R.Num((V)30.0d), step_76 = R.Num((V)1.0d);
for (double i_74 = R.Num((V)1.0d); step_76 > 0 ? i_74 <= end_75 : i_74 >= end_75; i_74 += step_76) {
V v_73 = i_74;
var values_77 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_67, (V)"basic_attack", (V)1.0d, (V)0.0d, V.Nil}, new Results(R.Table(new Entry((V)"vow", (V)true))))));
V v_78 = values_77[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_78, (V)"valid"), () => (V)(R.Get(R.Get(v_78, (V)"vow"), (V)"hp_cost") == (V)2.0d)), () => (V)(R.Get(R.Get(v_78, (V)"vow"), (V)"bonus") == (V)6.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_78, (V)"damage") == (V)(R.Get(v_68, (V)"damage") + (V)6.0d)), () => (V)(R.Get(v_78, (V)"cost") == R.Get(v_72, (V)"cost"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_11, (V)"preview_hint"), R.Join(new V[] {v_68, (V)"basic_attack"}, new Results(v_78))).First, "find", R.Join(new V[] {(V)"2 HP", (V)1.0d}, new Results((V)true)))));
}
}
R.Call(v_3, R.Join(new V[] {v_70}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_67)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Pilgrim rejects unaffordable HP and cannot make an attack commitment lethal by payment"}, new Results(new V((RuleFunction)(args_79 => {

foreach (var row_80 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d)))))))) {
V v_81 = row_80[0];
V v_82 = row_80[1];
var values_83 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_84 = values_83[0];
V v_85 = values_83[1];

var values_86 = R.Join(new V[] {}, new Results(v_82));
var obj_87 = v_85;
V key_88 = (V)"hp";
R.Set(obj_87, key_88, values_86[0]);

R.Call(v_27, R.Join(new V[] {}, new Results(v_84)));
var values_89 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_84))));
V v_90 = values_89[0];
var values_91 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_84, (V)"basic_attack", (V)1.0d, (V)0.0d, V.Nil}, new Results(R.Table(new Entry((V)"vow", (V)true))))));
V v_92 = values_91[0];
if (R.Truth((V)(v_82 <= (V)2.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_92, (V)"valid"))), () => R.Invoke(R.Get(v_92, (V)"reason"), "find", R.Join(new V[] {(V)"leave 1 HP", (V)1.0d}, new Results((V)true))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_84}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true))))).First)))));
R.Call(v_3, R.Join(new V[] {v_90}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_84)))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_92, (V)"valid"))));
R.Call(v_40, R.Join(new V[] {v_84}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_85, (V)"hp") == (V)1.0d))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Pilgrim owns no class slot and every existing class can pay on its immediate basic attack"}, new Results(new V((RuleFunction)(args_93 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_11, (V)"order")) == (V)15.0d))));
foreach (var row_94 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_11, (V)"order"))))))) {
V v_95 = row_94[0];
V v_96 = row_94[1];
var values_97 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results(v_96))));
V v_98 = values_97[0];
V v_99 = values_97[1];

var values_100 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_98))));
V v_101 = values_100[0];

var values_102 = R.Join(new V[] {}, new Results(R.Get(v_99, (V)"hp")));
V v_103 = values_102[0];
var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"effective_ability"), R.Join(new V[] {v_98, (V)"basic_attack", (V)1.0d, (V)0.0d}, new Results((V)true))));
V v_105 = values_104[0];
V v_106 = values_104[1];
var values_107 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_98, (V)"basic_attack", (V)1.0d, (V)0.0d, V.Nil}, new Results(R.Table(new Entry((V)"vow", (V)true))))));
V v_108 = values_107[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_108, (V)"valid")}, new Results(v_96)));
var values_109 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {v_98}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true))))));
V v_110 = values_109[0];
var values_111 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {}, new Results(v_110))));
V v_112 = values_111[0];
var values_113 = R.Join(new V[] {}, new Results((V)(v_106 + (V)6.0d)));
V v_114 = values_113[0];
if (R.Truth(R.Get(v_112, (V)"critical"))) {
var values_115 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)(v_114 * R.Get(v_99, (V)"crit_damage_percent")) / (V)100.0d) + (V)0.5d)))));
v_114 = values_115[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_112, (V)"raw_damage") == v_114), () => (V)(R.Get(v_99, (V)"hp") == (V)(v_103 - (V)2.0d))), () => (V)(R.Get(v_98, (V)"clock") == R.Get(v_108, (V)"cost")))}, new Results(v_96)));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_101, (V)"hp") == (V)((V)100.0d - R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(v_114 - R.Get(v_101, (V)"defense"))))).First))}, new Results(v_96)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_48, R.Join(new V[] {v_110}, new Results((V)"VowPaid"))).First == (V)1.0d))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"one Pilgrim payment empowers the whole primary sweep batch before secondary effects"}, new Results(new V((RuleFunction)(args_116 => {

var values_117 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_118 = values_117[0];
V v_119 = values_117[1];

var values_120 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_121 = R.Get(v_119, (V)"position");
V key_122 = (V)"x";
var obj_123 = R.Get(v_119, (V)"position");
V key_124 = (V)"y";
R.Set(obj_121, key_122, values_120[0]);
R.Set(obj_123, key_124, values_120[1]);

R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_118)));
var values_125 = R.Join(new V[] {}, new Results(R.Table()));
V v_126 = values_125[0];
foreach (var row_127 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_118, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))).First, (V)"cells"))))))) {
V v_128 = row_127[0];
V v_129 = row_127[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_17, (V)"map_for"), R.Join(new V[] {}, new Results(v_118))).First, R.Get(v_129, (V)"x")}, new Results(R.Get(v_129, (V)"y"))))));
var values_130 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {v_118, R.Get(v_129, (V)"x")}, new Results(R.Get(v_129, (V)"y")))));
var obj_131 = v_126;
V key_132 = (V)((V)R.Length(v_126) + (V)1.0d);
R.Set(obj_131, key_132, values_130[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_126) == (V)3.0d))));
var values_133 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"effective_ability"), R.Join(new V[] {v_118, (V)"sweeping_slash", (V)1.0d, (V)0.0d}, new Results((V)true))));
V v_134 = values_133[0];
V v_135 = values_133[1];
var values_136 = R.Join(new V[] {}, new Results(R.Get(v_119, (V)"hp")));
V v_137 = values_136[0];

var values_138 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {v_118}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true))))));
V v_139 = values_138[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_48, R.Join(new V[] {v_139}, new Results((V)"VowPaid"))).First == (V)1.0d), () => (V)(R.Call(v_48, R.Join(new V[] {v_139}, new Results((V)"DamageBatchResolved"))).First == (V)1.0d)), () => (V)(R.Get(v_119, (V)"hp") == (V)(v_137 - (V)2.0d))))));
foreach (var row_140 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_126)))))) {
V v_141 = row_140[0];
V v_142 = row_140[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_142, (V)"hp") == (V)((V)100.0d - R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)((V)(v_135 + (V)6.0d) - R.Get(v_142, (V)"defense"))))).First)))));
}
var values_143 = Results.Empty;
V v_144 = values_143[0];
foreach (var row_145 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_139)))))) {
V v_146 = row_145[0];
V v_147 = row_145[1];
if (R.Truth((V)(R.Get(v_147, (V)"kind") == (V)"DamageBatchResolved"))) {
var values_148 = R.Join(new V[] {}, new Results(v_147));
v_144 = values_148[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_144, (V)"outcomes")) == (V)3.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Pilgrim bonus is included before one critical roll and preserves critical RNG consumption"}, new Results(new V((RuleFunction)(args_149 => {

var values_150 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_151 = values_150[0];
V v_152 = values_150[1];

var values_153 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_154 = values_153[0];
V v_155 = values_153[1];

R.Call(v_27, R.Join(new V[] {}, new Results(v_151)));

R.Call(v_27, R.Join(new V[] {}, new Results(v_154)));
var values_156 = R.Join(new V[] {}, new Results((V)1000000.0d));
var obj_157 = v_152;
V key_158 = (V)"crit_chance_ppm";
R.Set(obj_157, key_158, values_156[0]);

var values_159 = R.Join(new V[] {}, new Results((V)1000000.0d));
var obj_160 = v_155;
V key_161 = (V)"crit_chance_ppm";
R.Set(obj_160, key_161, values_159[0]);
var values_162 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"effective_ability"), R.Join(new V[] {v_151, (V)"basic_attack", (V)1.0d, (V)0.0d}, new Results((V)true))));
V v_163 = values_162[0];
V v_164 = values_162[1];
var values_165 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {v_151}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true))))));
V v_166 = values_165[0];
var values_167 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {v_154}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_168 = values_167[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Call(v_58, R.Join(new V[] {}, new Results(v_166))).First, (V)"critical"), () => R.Get(R.Call(v_58, R.Join(new V[] {}, new Results(v_168))).First, (V)"critical")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(v_58, R.Join(new V[] {}, new Results(v_166))).First, (V)"raw_damage") == R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)((V)(v_164 + (V)6.0d) * R.Get(v_152, (V)"crit_damage_percent")) / (V)100.0d) + (V)0.5d)))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_151, (V)"rng"), (V)"state") == R.Get(R.Get(v_154, (V)"rng"), (V)"state")), () => (V)(R.Get(v_151, (V)"clock") == R.Get(v_154, (V)"clock"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Pilgrim HP payment is a cost rather than a wardable hit, retaliation or healing toggle"}, new Results(new V((RuleFunction)(args_169 => {

var values_170 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_171 = values_170[0];
V v_172 = values_170[1];

R.Call(v_27, R.Join(new V[] {}, new Results(v_171)));

var values_173 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_174 = v_172;
V key_175 = (V)"hp";
R.Set(obj_174, key_175, values_173[0]);

var values_176 = R.Join(new V[] {}, new Results((V)true));
var obj_177 = v_172;
V key_178 = (V)"invulnerable";
R.Set(obj_177, key_178, values_176[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_171, R.Get(v_172, (V)"id"), (V)"core:boon/betrayal_ward", (V)"rare"}, new Results((V)1.0d)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_171, R.Get(v_172, (V)"id"), (V)"core:boon/pain_dividend", (V)"rare"}, new Results((V)1.0d)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_171, R.Get(v_172, (V)"id"), (V)"core:boon/thorn_mirror", (V)"rare"}, new Results((V)1.0d)));
var values_179 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:boon/betrayal_ward", R.Table(new Entry((V)"ward", (V)3.0d), new Entry((V)"expires_at", (V)500.0d))))));
var obj_180 = v_172;
V key_181 = (V)"boon_memory";
R.Set(obj_180, key_181, values_179[0]);
var values_182 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {v_171}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true))))));
V v_183 = values_182[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_172, (V)"hp") == (V)8.0d), () => (V)(R.Get(R.Get(R.Get(v_172, (V)"boon_memory"), (V)"core:boon/betrayal_ward"), (V)"ward") == (V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Get(v_172, (V)"boon_memory"), (V)"core:boon/pain_dividend"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_48, R.Join(new V[] {v_183}, new Results((V)"Healed"))).First == (V)0.0d), () => (V)(R.Call(v_48, R.Join(new V[] {v_183}, new Results((V)"BoonActivated"))).First == (V)0.0d)))));
foreach (var row_184 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_183)))))) {
V v_185 = row_184[0];
V v_186 = row_184[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.And((V)(R.Get(v_186, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_186, (V)"target_id") == R.Get(v_172, (V)"id"))))))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Pilgrim requires ownership, rejects forged flags and never pays on movement, waits or chests"}, new Results(new V((RuleFunction)(args_187 => {

var values_188 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_189 = values_188[0];

var values_190 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_189))));
V v_191 = values_190[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_189}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true))))).First)))));
R.Call(v_3, R.Join(new V[] {v_191}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_189)))));
var values_192 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_193 = values_192[0];

var values_194 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_chest"), R.Join(new V[] {v_193, (V)"core:chest/standard", (V)3.0d}, new Results((V)2.0d))));
V v_195 = values_194[0];

var values_196 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_197 = v_193;
V key_198 = (V)"currency";
R.Set(obj_197, key_198, values_196[0]);
foreach (var row_199 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d), new Entry((V)"vow", (V)true))), new Entry((V)4, R.Table(new Entry((V)"kind", (V)"wait"), new Entry((V)"vow", (V)true))), new Entry((V)5, R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"vow", (V)true))), new Entry((V)6, R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_195, (V)"id")), new Entry((V)"offer_index", (V)1.0d), new Entry((V)"vow", (V)true)))))))))) {
V v_200 = row_199[0];
V v_201 = row_199[1];
var values_202 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_193))));
v_191 = values_202[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_193}, new Results(v_201))).First)))));

R.Call(v_3, R.Join(new V[] {v_191}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_193)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"bump attacks are ordinary and cannot silently spend Pilgrim HP"}, new Results(new V((RuleFunction)(args_203 => {

var values_204 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_205 = values_204[0];
V v_206 = values_204[1];

R.Call(v_27, R.Join(new V[] {}, new Results(v_205)));

var values_207 = R.Join(new V[] {}, new Results(R.Get(v_206, (V)"hp")));
V v_208 = values_207[0];
var values_209 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {v_205}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_210 = values_209[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_206, (V)"hp") == v_208), () => (V)(R.Call(v_48, R.Join(new V[] {v_210}, new Results((V)"VowPaid"))).First == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(v_58, R.Join(new V[] {}, new Results(v_210))).First, (V)"raw_damage") == R.Get(v_206, (V)"damage")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Pilgrim cancellation and an empty miss do not create a reusable empowered charge"}, new Results(new V((RuleFunction)(args_211 => {

var values_212 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_213 = values_212[0];
V v_214 = values_212[1];

var values_215 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_213))));
V v_216 = values_215[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_213}, new Results(R.Table(new Entry((V)"kind", (V)"cancel"))))).First)))));

R.Call(v_3, R.Join(new V[] {v_216}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_213)))));
var values_217 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {v_213}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true))))));
V v_218 = values_217[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_48, R.Join(new V[] {v_218}, new Results((V)"VowPaid"))).First == (V)1.0d), () => (V)(R.Get(v_214, (V)"hp") == (V)22.0d)))));
var values_219 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {v_213}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
v_218 = values_219[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_48, R.Join(new V[] {v_218}, new Results((V)"VowPaid"))).First == (V)0.0d), () => (V)(R.Get(R.Call(v_58, R.Join(new V[] {}, new Results(v_218))).First, (V)"raw_damage") == R.Get(v_214, (V)"damage"))), () => (V)(R.Get(v_214, (V)"hp") == (V)22.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"non-attack special skills refuse Pilgrim payment even when ordinary placement is valid"}, new Results(new V((RuleFunction)(args_220 => {

foreach (var row_221 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:actor/duelist"), new Entry((V)2, (V)"core:actor/geomancer"), new Entry((V)3, (V)"core:actor/tinkerer"), new Entry((V)4, (V)"core:actor/wayfinder"), new Entry((V)5, (V)"core:actor/shade"), new Entry((V)6, (V)"core:actor/trapper"), new Entry((V)7, (V)"core:actor/spiritbinder")))))))) {
V v_222 = row_221[0];
V v_223 = row_221[1];
var values_224 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results(v_223))));
V v_225 = values_224[0];
V v_226 = values_224[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_11, (V)"immediate_attack"), R.Join(new V[] {v_226, (V)"sweeping_slash"}, new Results(R.Table(new Entry((V)"companion_mode", (V)"place"))))).First)))));
var values_227 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_225))));
V v_228 = values_227[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_225, (V)"sweeping_slash", (V)1.0d, (V)0.0d, V.Nil}, new Results(R.Table(new Entry((V)"vow", (V)true))))).First, (V)"valid"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_225}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true))))).First)))));
R.Call(v_3, R.Join(new V[] {v_228}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_225)))));
}
var values_229 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results((V)"core:actor/stormcaller"))));
V v_230 = values_229[0];
V v_231 = values_229[1];

var values_232 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_233 = v_231;
V key_234 = (V)"storm_charge";
R.Set(obj_233, key_234, values_232[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_11, (V)"immediate_attack"), R.Join(new V[] {v_231, (V)"sweeping_slash"}, new Results(R.Table()))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_230, (V)"sweeping_slash", (V)1.0d, (V)0.0d, V.Nil}, new Results(R.Table(new Entry((V)"vow", (V)true))))).First, (V)"valid"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a real commanded Spirit attack pays on its owner and adds the bonus exactly once"}, new Results(new V((RuleFunction)(args_235 => {

var values_236 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results((V)"core:actor/spiritbinder"))));
V v_237 = values_236[0];
V v_238 = values_236[1];
R.Call(R.Get(v_13, (V)"place"), R.Join(new V[] {v_237, v_238, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))}, new Results(new V((RuleFunction)(args_239 => {


return Results.Empty;
})))));
var values_240 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {v_237, (V)4.0d}, new Results((V)2.0d))));
V v_241 = values_240[0];
var values_242 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_237, (V)"sweeping_slash", (V)1.0d, (V)0.0d, V.Nil}, new Results(R.Table(new Entry((V)"vow", (V)true))))));
V v_243 = values_242[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_243, (V)"valid"), () => (V)(R.Get(v_243, (V)"companion_mode") == (V)"strike")))));
var values_244 = R.Join(new V[] {}, new Results(R.Get(v_238, (V)"hp")));
V v_245 = values_244[0];

var values_246 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {v_237}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true))))));
V v_247 = values_246[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_238, (V)"hp") == (V)(v_245 - (V)2.0d)), () => (V)(R.Call(v_48, R.Join(new V[] {v_247}, new Results((V)"VowPaid"))).First == (V)1.0d)), () => (V)(R.Call(v_48, R.Join(new V[] {v_247}, new Results((V)"AttackPerformed"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_241, (V)"hp") == (V)((V)100.0d - R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)((V)(R.Get(v_238, (V)"slash_damage") + (V)6.0d) - R.Get(v_241, (V)"defense"))))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Pilgrim does not empower delayed Echo for free and paid state resumes exactly"}, new Results(new V((RuleFunction)(args_248 => {

var values_249 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_250 = values_249[0];
V v_251 = values_249[1];

R.Call(v_27, R.Join(new V[] {}, new Results(v_250)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_250, R.Get(v_251, (V)"id"), (V)"core:boon/temporal_echo", (V)"rare"}, new Results((V)1.0d)));
R.Call(v_40, R.Join(new V[] {v_250}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_250, (V)"delayed_effects"))))));
var values_252 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_19, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_19, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_250))))))))));
V v_253 = values_252[0];
var values_254 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {v_250}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_255 = values_254[0];

var values_256 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {v_253}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_257 = values_256[0];
R.Call(v_3, R.Join(new V[] {v_255}, new Results(v_257)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_250))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_253)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_251, (V)"hp") == (V)22.0d), () => (V)(R.Call(v_48, R.Join(new V[] {v_255}, new Results((V)"VowPaid"))).First == (V)0.0d)))));
var values_258 = R.Join(new V[] {}, new Results((V)false));
V v_259 = values_258[0];
foreach (var row_260 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_255)))))) {
V v_261 = row_260[0];
V v_262 = row_260[1];
if (R.Truth(R.And((V)(R.Get(v_262, (V)"kind") == (V)"DamageTaken"), () => (V)(!R.Truth(R.Get(v_262, (V)"direct_attack")))))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_262, (V)"attempted_damage") == R.Get(R.Get(R.Get(R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results((V)"core:boon/temporal_echo"))).First, (V)"parameters"), (V)"rare"), (V)"power")))));
var values_263 = R.Join(new V[] {}, new Results((V)true));
v_259 = values_263[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_259)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"mixed Pilgrim rarities add power while HP price stays fixed and the declaration validates"}, new Results(new V((RuleFunction)(args_264 => {

var values_265 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_266 = values_265[0];
V v_267 = values_265[1];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_266, R.Get(v_267, (V)"id"), v_21, (V)"common"}, new Results((V)5.0d)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_266, R.Get(v_267, (V)"id"), v_21, (V)"legendary"}, new Results((V)4.0d)));
var values_268 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"attack_vow"), R.Join(new V[] {}, new Results(v_267))));
V v_269 = values_268[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_269, (V)"hp_cost") == (V)2.0d), () => (V)(R.Get(v_269, (V)"bonus") == (V)((V)((V)15.0d + (V)6.0d) + (V)32.0d))))));
var values_270 = R.Join(new V[] {}, R.Call(R.Get(v_19, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_19, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results(v_21))))))));
V v_271 = values_270[0];

var values_272 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_273 = R.Get(v_271, (V)"attack_payment");
V key_274 = (V)"hp";
R.Set(obj_273, key_274, values_272[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_9, (V)"validate"), R.Table(new Entry(v_21, v_271))}, new Results(R.Table(new Entry((V)1, v_21))))).First)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_266))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_266)))))))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
