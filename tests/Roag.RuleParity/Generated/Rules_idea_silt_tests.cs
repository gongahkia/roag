// Static C# port of tests/idea_silt_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: a7a277345395bbe580bb7f1d7e1d6c8db6a7742aabefacda2863efa9825ea0b0
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_silt_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_silt"))));
V v_11 = values_10[0];

var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_13 = values_12[0];
V v_14 = V.Nil;
v_14 = new V((RuleFunction)(args_15 => {

var values_16 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_17 = values_16[0];

var values_18 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_17))));
V v_19 = values_18[0];

var values_20 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_21 = R.Get(v_19, (V)"position");
V key_22 = (V)"x";
var obj_23 = R.Get(v_19, (V)"position");
V key_24 = (V)"y";
R.Set(obj_21, key_22, values_20[0]);
R.Set(obj_23, key_24, values_20[1]);
var values_25 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_17, R.Get(v_11, (V)"id"), (V)10.0d, (V)6.0d}, new Results((V)50.0d))));
V v_26 = values_25[0];
return R.Join(new V[] {v_17, v_19}, new Results(v_26));
return Results.Empty;
}));
V v_27 = V.Nil;
v_27 = new V((RuleFunction)(args_28 => {
V v_29 = args_28[0];
V v_30 = args_28[1];
var values_31 = R.Join(new V[] {}, new Results(R.Table()));
V v_32 = values_31[0];
{ double end_35 = R.Num(v_30), step_36 = R.Num((V)1.0d);
for (double i_34 = R.Num((V)1.0d); step_36 > 0 ? i_34 <= end_35 : i_34 >= end_35; i_34 += step_36) {
V v_33 = i_34;
var values_37 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_29}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_38 = values_37[0];
V v_39 = values_37[1];
V v_40 = values_37[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_38)));
foreach (var row_41 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_40)))))) {
V v_42 = row_41[0];
V v_43 = row_41[1];
var values_44 = R.Join(new V[] {}, new Results(v_43));
var obj_45 = v_32;
V key_46 = (V)((V)R.Length(v_32) + (V)1.0d);
R.Set(obj_45, key_46, values_44[0]);
}
}
}
return R.Join(new V[] {}, new Results(v_32));
return Results.Empty;
}));
V v_47 = V.Nil;
v_47 = new V((RuleFunction)(args_48 => {

var values_49 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_50 = values_49[0];
V v_51 = values_49[1];
V v_52 = values_49[2];

R.Call(v_27, R.Join(new V[] {v_50}, new Results((V)3.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_52, (V)"position"), (V)"x") == (V)8.0d), () => (V)((V)R.Length(R.Get(v_50, (V)"idea_silt")) == (V)2.0d)), () => (V)(R.Get(R.Get(R.Get(v_50, (V)"idea_silt"), (V)1.0d), (V)"expires") == (V)515.0d)))));
return R.Join(new V[] {v_50, v_51}, new Results(v_52));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Worm decisions are pure and a short charge keeps its committed lane and direction"}, new Results(new V((RuleFunction)(args_53 => {

var values_54 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_55 = values_54[0];
V v_56 = values_54[1];
V v_57 = values_54[2];

var values_58 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_55))));
V v_59 = values_58[0];
{ double end_62 = R.Num((V)30.0d), step_63 = R.Num((V)1.0d);
for (double i_61 = R.Num((V)1.0d); step_63 > 0 ? i_61 <= end_62 : i_61 >= end_62; i_61 += step_63) {
V v_60 = i_61;
var values_64 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_55}, new Results(v_57))));
V v_65 = values_64[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_65, (V)"ability_id") == (V)"core:ability/worm_charge"), () => (V)((V)R.Length(R.Get(v_65, (V)"cells")) == (V)3.0d)), () => (V)(R.Get(R.Get(v_65, (V)"advance"), (V)"dx") == (-(V)1.0d))), () => (V)(R.Get(v_65, (V)"ranged") == (V)true)))));
}
}
R.Call(v_3, R.Join(new V[] {v_59}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_55)))));

R.Call(v_27, R.Join(new V[] {v_55}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_57, (V)"pending_attack"), (V)"due") == (V)215.0d), () => (V)(R.Get(R.Get(v_57, (V)"ai_memory"), (V)"silt_charges") == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_55}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));

R.Call(v_27, R.Join(new V[] {v_55}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_56, (V)"hp") == (V)24.0d), () => (V)(R.Get(R.Get(v_56, (V)"position"), (V)"y") == (V)7.0d)), () => (V)(R.Get(R.Get(v_57, (V)"position"), (V)"x") == (V)8.0d)), () => (V)(R.Get(R.Get(v_57, (V)"position"), (V)"y") == (V)6.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_55, (V)"idea_silt")) == (V)2.0d), () => (V)(R.Get(R.Get(R.Get(v_55, (V)"idea_silt"), (V)1.0d), (V)"x") == (V)10.0d)), () => (V)(R.Get(R.Get(R.Get(v_55, (V)"idea_silt"), (V)2.0d), (V)"x") == (V)9.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"the trail covers only cells actually vacated and expires without rewriting underlying row bytes"}, new Results(new V((RuleFunction)(args_66 => {

var values_67 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_68 = values_67[0];
V v_69 = values_67[1];
V v_70 = values_67[2];

var values_71 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_13, (V)"map_for"), R.Join(new V[] {}, new Results(v_68))).First, (V)"rows")))));
V v_72 = values_71[0];
R.Call(v_27, R.Join(new V[] {v_68}, new Results((V)3.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_13, (V)"map_for"), R.Join(new V[] {}, new Results(v_68))).First, (V)"rows")))).First == v_72))));
foreach (var row_73 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_68, (V)"idea_silt"))))))) {
V v_74 = row_73[0];
V v_75 = row_73[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_75, (V)"y") == (V)6.0d), () => (V)(R.Get(v_75, (V)"x") != R.Get(R.Get(v_70, (V)"position"), (V)"x"))))));
}
R.Call(v_27, R.Join(new V[] {v_68}, new Results((V)3.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_68, (V)"idea_silt")) == (V)0.0d), () => (V)(R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_13, (V)"map_for"), R.Join(new V[] {}, new Results(v_68))).First, (V)"rows")))).First == v_72)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_68)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a living blocker stops charge damage and movement before any untraversed ground becomes bog"}, new Results(new V((RuleFunction)(args_76 => {

var values_77 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_78 = values_77[0];
V v_79 = values_77[1];
V v_80 = values_77[2];

var values_81 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_78, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_82 = values_81[0];
R.Call(v_27, R.Join(new V[] {v_78}, new Results((V)3.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_82, (V)"hp") == (V)9.0d), () => (V)(R.Get(v_79, (V)"hp") == (V)24.0d)), () => (V)(R.Get(R.Get(v_80, (V)"position"), (V)"x") == (V)10.0d)), () => (V)((V)R.Length(R.Or(R.Get(v_78, (V)"idea_silt"), () => R.Table())) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"walking and Dash pay the previewed temporary bog costs and never damage the entrant"}, new Results(new V((RuleFunction)(args_83 => {

var values_84 = R.Join(new V[] {}, R.Call(v_47, Results.Empty));
V v_85 = values_84[0];
V v_86 = values_84[1];
V v_87 = values_84[2];

var values_88 = R.Join(new V[] {(V)9.0d}, new Results((V)7.0d));
var obj_89 = R.Get(v_86, (V)"position");
V key_90 = (V)"x";
var obj_91 = R.Get(v_86, (V)"position");
V key_92 = (V)"y";
R.Set(obj_89, key_90, values_88[0]);
R.Set(obj_91, key_92, values_88[1]);
var values_93 = R.Join(new V[] {}, new Results(R.Get(v_86, (V)"hp")));
V v_94 = values_93[0];

var values_95 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_85, (V)"move", (V)0.0d}, new Results((-(V)1.0d)))));
V v_96 = values_95[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_96, (V)"valid"), () => (V)(R.Get(v_96, (V)"cost") == (V)140.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_85}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d))))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_85, (V)"clock") == (V)440.0d), () => (V)(R.Get(v_86, (V)"hp") == v_94)))));
var values_97 = R.Join(new V[] {}, R.Call(v_47, Results.Empty));
v_85 = values_97[0];
v_86 = values_97[1];
v_87 = values_97[2];

var values_98 = R.Join(new V[] {(V)11.0d}, new Results((V)6.0d));
var obj_99 = R.Get(v_86, (V)"position");
V key_100 = (V)"x";
var obj_101 = R.Get(v_86, (V)"position");
V key_102 = (V)"y";
R.Set(obj_99, key_100, values_98[0]);
R.Set(obj_101, key_102, values_98[1]);

var values_103 = R.Join(new V[] {}, new Results(R.Get(v_86, (V)"hp")));
v_94 = values_103[0];
var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_85, (V)"dash", (-(V)1.0d)}, new Results((V)0.0d))));
v_96 = values_104[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_96, (V)"valid"), () => (V)(R.Get(v_96, (V)"distance") == (V)2.0d)), () => (V)(R.Get(v_96, (V)"cost") == (V)170.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_85}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_85, (V)"clock") == (V)470.0d), () => (V)(R.Get(v_86, (V)"hp") == v_94)), () => (V)(R.Get(R.Get(v_86, (V)"position"), (V)"x") == (V)9.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ordinary enemy movement pays the same bog recovery once at its actual move time"}, new Results(new V((RuleFunction)(args_105 => {

var values_106 = R.Join(new V[] {}, R.Call(v_47, Results.Empty));
V v_107 = values_106[0];
V v_108 = values_106[1];
V v_109 = values_106[2];

var values_110 = R.Join(new V[] {(V)11.0d}, new Results((V)6.0d));
var obj_111 = R.Get(v_108, (V)"position");
V key_112 = (V)"x";
var obj_113 = R.Get(v_108, (V)"position");
V key_114 = (V)"y";
R.Set(obj_111, key_112, values_110[0]);
R.Set(obj_113, key_114, values_110[1]);
var values_115 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_107, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)7.0d}, new Results((V)301.0d))));
V v_116 = values_115[0];
R.Call(v_27, R.Join(new V[] {v_107}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_116, (V)"position"), (V)"x") == (V)9.0d), () => (V)(R.Get(R.Get(v_116, (V)"position"), (V)"y") == (V)6.0d)))));
var values_117 = R.Join(new V[] {}, new Results((V)false));
V v_118 = values_117[0];
foreach (var row_119 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_107, (V)"schedule"))))))) {
V v_120 = row_119[0];
V v_121 = row_119[1];
if (R.Truth(R.And((V)(R.Get(v_121, (V)"actor_id") == R.Get(v_116, (V)"id")), () => (V)(R.Get(v_121, (V)"kind") == (V)"actor_ready")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_121, (V)"due") == (V)((V)301.0d + R.Call(R.Get(v_5, (V)"effective_cost"), R.Join(new V[] {(V)140.0d}, new Results(R.Get(v_116, (V)"speed")))).First)))));

var values_122 = R.Join(new V[] {}, new Results((V)true));
v_118 = values_122[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_118)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stun spends and cancels a pending charge while stairs and water cannot become a charge lane"}, new Results(new V((RuleFunction)(args_123 => {

var values_124 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_125 = values_124[0];
V v_126 = values_124[1];
V v_127 = values_124[2];

R.Call(v_27, R.Join(new V[] {v_125}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_125, R.Get(v_127, (V)"id")}, new Results((V)500.0d)))));

R.Call(v_27, R.Join(new V[] {v_125}, new Results((V)3.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_127, (V)"ai_memory"), (V)"silt_charges") == (V)1.0d), () => (V)((V)R.Length(R.Or(R.Get(v_125, (V)"idea_silt"), () => R.Table())) == (V)0.0d)))));
foreach (var row_128 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"stair"), new Entry((V)2, (V)"water")))))))) {
V v_129 = row_128[0];
V v_130 = row_128[1];
var values_131 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
v_125 = values_131[0];
v_126 = values_131[1];
v_127 = values_131[2];

var values_132 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"map_for"), R.Join(new V[] {}, new Results(v_125))))))));
var obj_133 = v_125;
V key_134 = (V)"generated_map";
R.Set(obj_133, key_134, values_132[0]);
if (R.Truth((V)(v_130 == (V)"stair"))) {
var values_135 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"stairs", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)6.0d))))))));
var obj_136 = R.Get(v_125, (V)"generated_map");
V key_137 = (V)"placements";
R.Set(obj_136, key_137, values_135[0]);
} else {
var values_138 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_125, (V)"generated_map"), (V)"rows"), (V)6.0d)));
V v_139 = values_138[0];

var values_140 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_139, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)8.0d))).First, R.Concat((V)"~", R.Invoke(v_139, "sub", R.Join(new V[] {}, new Results((V)10.0d))).First))));
var obj_141 = R.Get(R.Get(v_125, (V)"generated_map"), (V)"rows");
V key_142 = (V)6.0d;
R.Set(obj_141, key_142, values_140[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_125}, new Results(v_127))).First, (V)"ability_id") != (V)"core:ability/worm_charge"))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"spent charge budgets do not reset and old trails remain finite after their owner dies"}, new Results(new V((RuleFunction)(args_143 => {

var values_144 = R.Join(new V[] {}, R.Call(v_47, Results.Empty));
V v_145 = values_144[0];
V v_146 = values_144[1];
V v_147 = values_144[2];

var values_148 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_149 = R.Get(v_147, (V)"ai_memory");
V key_150 = (V)"silt_charges";
R.Set(obj_149, key_150, values_148[0]);
var values_151 = R.Join(new V[] {(V)11.0d}, new Results((V)6.0d));
var obj_152 = R.Get(v_146, (V)"position");
V key_153 = (V)"x";
var obj_154 = R.Get(v_146, (V)"position");
V key_155 = (V)"y";
R.Set(obj_152, key_153, values_151[0]);
R.Set(obj_154, key_155, values_151[1]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_145}, new Results(v_147))).First, (V)"ability_id") != (V)"core:ability/worm_charge"))));
var values_156 = R.Join(new V[] {}, R.Call(v_47, Results.Empty));
v_145 = values_156[0];
v_146 = values_156[1];
v_147 = values_156[2];

var values_157 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_158 = v_147;
V key_159 = (V)"hp";
R.Set(obj_158, key_159, values_157[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_145}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_145}, new Results(R.Get(v_147, (V)"id")))).First)), () => (V)((V)R.Length(R.Get(v_145, (V)"idea_silt")) == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_145)))))));

R.Call(v_27, R.Join(new V[] {v_145}, new Results((V)2.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_145, (V)"idea_silt")) == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pending charge and live bog save continuations preserve exact rows RNG schedule and expiry"}, new Results(new V((RuleFunction)(args_160 => {

foreach (var row_161 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)3.0d)))))))) {
V v_162 = row_161[0];
V v_163 = row_161[1];
var values_164 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_165 = values_164[0];
V v_166 = values_164[1];
V v_167 = values_164[2];

R.Call(v_27, R.Join(new V[] {v_165}, new Results(v_163)));
var values_168 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_165))));
V v_169 = values_168[0];

var values_170 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(v_169))))))));
V v_171 = values_170[0];
{ double end_174 = R.Num((V)5.0d), step_175 = R.Num((V)1.0d);
for (double i_173 = R.Num((V)1.0d); step_175 > 0 ? i_173 <= end_174 : i_173 >= end_174; i_173 += step_175) {
V v_172 = i_173;
R.Call(v_3, R.Join(new V[] {R.Call(v_27, R.Join(new V[] {v_165}, new Results((V)1.0d))).First}, R.Call(v_27, R.Join(new V[] {v_171}, new Results((V)1.0d)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_165))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_171)))));
}
}
}
var values_176 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"clock", (V)900.0d), new Entry((V)"entities", R.Table()), new Entry((V)"schedule", R.Table()), new Entry((V)"floor_pause", R.Table(new Entry((V)"a", R.Table(new Entry((V)"suspended_at", (V)300.0d), new Entry((V)"schedule", R.Table()))))), new Entry((V)"idea_silt", R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", (V)"a"), new Entry((V)"expires", (V)515.0d))), new Entry((V)2, R.Table(new Entry((V)"floor_id", (V)"b"), new Entry((V)"expires", (V)1000.0d))))))));
V v_177 = values_176[0];
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))).First, (V)"resume"), R.Join(new V[] {v_177}, new Results((V)"a")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_177, (V)"idea_silt"), (V)1.0d), (V)"expires") == (V)1115.0d), () => (V)(R.Get(R.Get(R.Get(v_177, (V)"idea_silt"), (V)2.0d), (V)"expires") == (V)1000.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"corrupt expiry ownership overlap charge budgets and unlocked pending lanes are rejected"}, new Results(new V((RuleFunction)(args_178 => {

var values_179 = R.Join(new V[] {}, R.Call(v_47, Results.Empty));
V v_180 = values_179[0];
V v_181 = values_179[1];
V v_182 = values_179[2];

var values_183 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_180))));
V v_184 = values_183[0];
foreach (var row_208 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, new V((RuleFunction)(args_185 => {
V v_186 = args_185[0];
var values_187 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_188 = R.Get(R.Get(v_186, (V)"idea_silt"), (V)1.0d);
V key_189 = (V)"expires";
R.Set(obj_188, key_189, values_187[0]);
return Results.Empty;
}))), new Entry((V)2, new V((RuleFunction)(args_190 => {
V v_191 = args_190[0];
var values_192 = R.Join(new V[] {}, new Results(R.Get(v_191, (V)"player_id")));
var obj_193 = R.Get(R.Get(v_191, (V)"idea_silt"), (V)1.0d);
V key_194 = (V)"source_id";
R.Set(obj_193, key_194, values_192[0]);
return Results.Empty;
}))), new Entry((V)3, new V((RuleFunction)(args_195 => {
V v_196 = args_195[0];
var values_197 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_196, (V)"idea_silt"), (V)1.0d), (V)"x")));
var obj_198 = R.Get(R.Get(v_196, (V)"idea_silt"), (V)2.0d);
V key_199 = (V)"x";
R.Set(obj_198, key_199, values_197[0]);
return Results.Empty;
}))), new Entry((V)4, new V((RuleFunction)(args_200 => {
V v_201 = args_200[0];
foreach (var row_202 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_201, (V)"entities"))))))) {
V v_203 = row_202[0];
V v_204 = row_202[1];
if (R.Truth((V)(R.Get(v_204, (V)"id") == R.Get(v_182, (V)"id")))) {
var values_205 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_206 = R.Get(v_204, (V)"ai_memory");
V key_207 = (V)"silt_charges";
R.Set(obj_206, key_207, values_205[0]);
}
}
return Results.Empty;
})))))))))) {
V v_209 = row_208[0];
V v_210 = row_208[1];
var values_211 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(v_184))))));
V v_212 = values_211[0];

R.Call(v_210, R.Join(new V[] {}, new Results(v_212)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_212))).First)))));
}
var values_213 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
v_180 = values_213[0];
v_181 = values_213[1];
v_182 = values_213[2];

R.Call(v_27, R.Join(new V[] {v_180}, new Results((V)1.0d)));

var values_214 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_180))));
V v_215 = values_214[0];
foreach (var row_216 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"entities"))))))) {
V v_217 = row_216[0];
V v_218 = row_216[1];
if (R.Truth((V)(R.Get(v_218, (V)"id") == R.Get(v_182, (V)"id")))) {
var values_219 = R.Join(new V[] {}, new Results((V)7.0d));
var obj_220 = R.Get(R.Get(R.Get(v_218, (V)"pending_attack"), (V)"target_cells"), (V)1.0d);
V key_221 = (V)"y";
R.Set(obj_220, key_221, values_219[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_215))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"the global trail cap refuses further cells without changing ground RNG or existing lifetimes"}, new Results(new V((RuleFunction)(args_222 => {

var values_223 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_224 = values_223[0];
V v_225 = values_223[1];
V v_226 = values_223[2];

var values_227 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_228 = R.Get(v_226, (V)"ai_memory");
V key_229 = (V)"silt_charges";
R.Set(obj_228, key_229, values_227[0]);
var values_230 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_226), new Entry((V)2, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_224, R.Get(v_11, (V)"id"), (V)11.0d, (V)6.0d}, new Results((V)100000.0d))).First), new Entry(3, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_224, R.Get(v_11, (V)"id"), (V)12.0d, (V)6.0d}, new Results((V)100000.0d)))))));
V v_231 = values_230[0];
foreach (var row_232 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_231)))))) {
V v_233 = row_232[0];
V v_234 = row_232[1];
var values_235 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_236 = R.Get(v_234, (V)"ai_memory");
V key_237 = (V)"silt_charges";
R.Set(obj_236, key_237, values_235[0]);
}
var values_238 = R.Join(new V[] {}, new Results((V)7.0d));
var obj_239 = v_224;
V key_240 = (V)"next_action_id";
R.Set(obj_239, key_240, values_238[0]);

var values_241 = R.Join(new V[] {}, new Results(R.Table()));
var obj_242 = v_224;
V key_243 = (V)"idea_silt";
R.Set(obj_242, key_243, values_241[0]);
{ double end_246 = R.Num((V)12.0d), step_247 = R.Num((V)1.0d);
for (double i_245 = R.Num((V)1.0d); step_247 > 0 ? i_245 <= end_246 : i_245 >= end_246; i_245 += step_247) {
V v_244 = i_245;
var values_248 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"action_id", R.Call(R.Get(rt.Global("math"), (V)"ceil"), R.Join(new V[] {}, new Results((V)(v_244 / (V)2.0d)))).First), new Entry((V)"source_id", R.Get(R.Get(v_231, R.Call(R.Get(rt.Global("math"), (V)"ceil"), R.Join(new V[] {}, new Results((V)(v_244 / (V)4.0d)))).First), (V)"id")), new Entry((V)"floor_id", R.Get(v_224, (V)"active_floor_id")), new Entry((V)"x", (V)(v_244 + (V)1.0d)), new Entry((V)"y", (V)2.0d), new Entry((V)"expires", (V)300.0d))));
var obj_249 = R.Get(v_224, (V)"idea_silt");
V key_250 = v_244;
R.Set(obj_249, key_250, values_248[0]);
}
}
var values_251 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"map_for"), R.Join(new V[] {}, new Results(v_224))));
V v_252 = values_251[0];

R.Call(R.Get(v_11, (V)"validate"), R.Join(new V[] {v_224}, new Results(R.Table(new Entry(R.Get(v_252, (V)"floor_id"), v_252)))));
var values_253 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_224))));
V v_254 = values_253[0];
R.Call(R.Get(v_11, (V)"leave"), R.Join(new V[] {v_224, v_226, R.Table(new Entry((V)"ability_id", (V)"core:ability/worm_charge"), new Entry((V)"action_id", (V)6.0d)), (V)10.0d, (V)6.0d, R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)6.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d)))), v_252}, new Results(new V((RuleFunction)(args_255 => {

R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"cap should emit nothing")));
return Results.Empty;
})))));
R.Call(v_3, R.Join(new V[] {v_254}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_224)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_224, (V)"idea_silt")) == (V)12.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"real generated Worms resolve their approved body and native silt cue without missing bindings"}, new Results(new V((RuleFunction)(args_256 => {

var values_257 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_seeds"))).First, (V)"find"), R.Join(new V[] {}, new Results(R.Get(v_11, (V)"id")))));
V v_258 = values_257[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_258)))))));
var values_259 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_260 = values_259[0];
foreach (var row_261 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_262 = row_261[0];
V v_263 = row_261[1];
var values_264 = R.Join(new V[] {(V)false}, new Results((V)false));
V v_265 = values_264[0];
V v_266 = values_264[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_260, (V)"packs"), v_263), (V)"missing")) == (V)0.0d))));
foreach (var row_267 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_260, (V)"packs"), v_263), (V)"bindings"))))))) {
V v_268 = row_267[0];
V v_269 = row_267[1];
if (R.Truth((V)(R.Get(v_269, (V)"id") == R.Get(v_11, (V)"id")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_269, (V)"sheet") == (V)"onebit"), () => (V)(R.Get(v_269, (V)"index") == (V)371.0d)))));

var values_270 = R.Join(new V[] {}, new Results((V)true));
v_265 = values_270[0];
}
if (R.Truth((V)(R.Get(v_269, (V)"id") == (V)"core:object/idea_silt_trail"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_269, (V)"sheet") == (V)"onebit"), () => (V)(R.Get(v_269, (V)"index") == (V)268.0d)))));

var values_271 = R.Join(new V[] {}, new Results((V)true));
v_266 = values_271[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_265, () => v_266))));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
