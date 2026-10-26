// Static C# port of tests/expansion_legal_playthrough.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: c6a099326967887fcd72eb504fc0aa05d28add9d4e116ae413b49c01182eb156
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_expansion_legal_playthrough { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievements"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, new Results(R.Table()));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_14 = values_13[0];
V v_15 = V.Nil;
v_15 = new V((RuleFunction)(args_16 => {
V v_17 = args_16[0];
V v_18 = args_16[1];
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_17, (V)"x") - R.Get(v_18, (V)"x"))))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_17, (V)"y") - R.Get(v_18, (V)"y"))))))).First == (V)1.0d)));
return Results.Empty;
}));
V v_19 = V.Nil;
v_19 = new V((RuleFunction)(args_20 => {
V v_21 = args_20[0];
V v_22 = args_20[1];
return R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_21, (V)"x") - R.Get(v_22, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_21, (V)"y") - R.Get(v_22, (V)"y"))))).First) == (V)1.0d)));
return Results.Empty;
}));
V v_23 = V.Nil;
v_23 = new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
V v_26 = args_24[1];
V v_27 = args_24[2];
var values_28 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_25))).First, (V)"definition_id")));
V v_29 = values_28[0];
var values_30 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_29 == R.Get(R.Get(v_10, (V)"order"), (V)2.0d)), () => (V)4.0d), () => R.And(R.Or((V)(v_29 == R.Get(R.Get(v_10, (V)"order"), (V)3.0d)), () => (V)(v_29 == R.Get(R.Get(v_10, (V)"order"), (V)6.0d))), () => (V)3.0d)), () => V.Nil)));
V v_31 = values_30[0];
if (R.Truth((V)(!R.Truth(v_31)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_32 = R.Join(new V[] {(V)(R.Get(v_27, (V)"x") - R.Get(v_26, (V)"x"))}, new Results((V)(R.Get(v_27, (V)"y") - R.Get(v_26, (V)"y"))));
V v_33 = values_32[0];
V v_34 = values_32[1];
var values_35 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_33 == (V)0.0d), () => (V)0.0d), () => R.Or(R.And((V)(v_33 > (V)0.0d), () => (V)1.0d), () => (-(V)1.0d)))));
V v_36 = values_35[0];
var values_37 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_34 == (V)0.0d), () => (V)0.0d), () => R.Or(R.And((V)(v_34 > (V)0.0d), () => (V)1.0d), () => (-(V)1.0d)))));
V v_38 = values_37[0];
var values_39 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_33))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_34))))));
V v_40 = values_39[0];
if (R.Truth(R.Or(R.Or((V)(v_40 < (V)1.0d), () => (V)(v_40 > v_31)), () => R.And(R.And((V)(v_33 != (V)0.0d), () => (V)(v_34 != (V)0.0d)), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_33))).First != R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_34))).First))))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_41 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"map_for"), R.Join(new V[] {}, new Results(v_25))));
V v_42 = values_41[0];
{ double end_45 = R.Num(v_40), step_46 = R.Num((V)1.0d);
for (double i_44 = R.Num((V)1.0d); step_46 > 0 ? i_44 <= end_45 : i_44 >= end_45; i_44 += step_46) {
V v_43 = i_44;
var values_47 = R.Join(new V[] {(V)(R.Get(v_26, (V)"x") + (V)(v_36 * v_43))}, new Results((V)(R.Get(v_26, (V)"y") + (V)(v_38 * v_43))));
V v_48 = values_47[0];
V v_49 = values_47[1];
if (R.Truth(R.And((V)(v_36 != (V)0.0d), () => (V)(v_38 != (V)0.0d)))) {
if (R.Truth(R.Or((V)(!R.Truth(R.Call(R.Get(v_6, (V)"projectile_passable"), R.Join(new V[] {v_42, (V)(v_48 - v_36), v_49}, new Results(v_25))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_6, (V)"projectile_passable"), R.Join(new V[] {v_42, v_48, (V)(v_49 - v_38)}, new Results(v_25))).First))))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
}
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_6, (V)"projectile_passable"), R.Join(new V[] {v_42, v_48, v_49}, new Results(v_25))).First)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
}
}
return R.Join(new V[] {v_36}, new Results(v_38));
return Results.Empty;
}));
V v_50 = V.Nil;
v_50 = new V((RuleFunction)(args_51 => {
V v_52 = args_51[0];
V v_53 = args_51[1];
V v_54 = args_51[2];
V v_55 = args_51[3];
var values_56 = R.Join(new V[] {}, new Results(R.Get(v_52, (V)"state")));
V v_57 = values_56[0];
var values_58 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_57))).First, (V)"definition_id")));
V v_59 = values_58[0];
if (R.Truth(R.And(R.And((V)(v_59 != R.Get(R.Get(v_10, (V)"order"), (V)2.0d)), () => (V)(v_59 != R.Get(R.Get(v_10, (V)"order"), (V)3.0d))), () => (V)(v_59 != R.Get(R.Get(v_10, (V)"order"), (V)6.0d))))) {
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"fight"), R.Join(new V[] {v_52, v_53, v_54}, new Results(v_55))));
}
var values_60 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_52, (V)"trace"))));
V v_61 = values_60[0];
{ double end_64 = R.Num(v_54), step_65 = R.Num((V)1.0d);
for (double i_63 = R.Num((V)1.0d); step_65 > 0 ? i_63 <= end_64 : i_63 >= end_64; i_63 += step_65) {
V v_62 = i_63;
if (R.Truth(R.And(v_55, () => R.Call(v_55, Results.Empty).First))) {
return R.Join(new V[] {}, new Results((V)true));
}
var values_66 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"entity"), R.Join(new V[] {v_57}, new Results(v_53))));
V v_67 = values_66[0];
if (R.Truth((V)(!R.Truth(v_67)))) {
return R.Join(new V[] {}, new Results((V)true));
}
if (R.Truth(R.Get(v_57, (V)"game_over"))) {
return R.Join(new V[] {(V)false}, new Results((V)"dead"));
}
var values_68 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_57))).First, (V)"position")));
V v_69 = values_68[0];
if (R.Truth(R.Call(R.Get(v_2, (V)"escape"), R.Join(new V[] {v_52, R.Get(v_67, (V)"position"), V.Nil, (V)true}, new Results((V)true))).First)) {

} else if (R.Truth(R.Call(R.Get(v_2, (V)"movement_cycle"), R.Join(new V[] {v_52}, new Results(v_61))).First)) {
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_2, (V)"attack_in_reach"), R.Join(new V[] {}, new Results(v_52))).First)))) {
var values_70 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"action"), R.Join(new V[] {v_52}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_71 = values_70[0];
V v_72 = values_70[1];
if (R.Truth((V)(!R.Truth(v_71)))) {
return R.Join(new V[] {(V)false}, new Results(v_72));
}
}
} else {
var values_73 = R.Join(new V[] {}, R.Call(v_23, R.Join(new V[] {v_57, v_69}, new Results(R.Get(v_67, (V)"position")))));
V v_74 = values_73[0];
V v_75 = values_73[1];
if (R.Truth(v_74)) {
var values_76 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"action"), R.Join(new V[] {v_52}, R.Call(R.Get(v_2, (V)"attack_intent"), R.Join(new V[] {v_52, v_74}, new Results(v_75))))));
V v_77 = values_76[0];
V v_78 = values_76[1];
if (R.Truth((V)(!R.Truth(v_77)))) {
return R.Join(new V[] {(V)false}, new Results(v_78));
}
} else if (R.Truth(R.And((V)(v_59 == R.Get(R.Get(v_10, (V)"order"), (V)3.0d)), () => R.Call(R.Get(v_2, (V)"attack_in_reach"), R.Join(new V[] {}, new Results(v_52))).First))) {

} else {
var values_81 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"path"), R.Join(new V[] {v_57}, new Results(new V((RuleFunction)(args_79 => {
V v_80 = args_79[0];
return R.Join(new V[] {}, new Results((V)(R.Call(v_23, R.Join(new V[] {v_57, v_80}, new Results(R.Get(v_67, (V)"position")))).First != V.Nil)));
return Results.Empty;
}))))));
V v_82 = values_81[0];
if (R.Truth((V)(!R.Truth(v_82)))) {
return R.Join(new V[] {(V)false}, new Results((V)"no ranged firing position"));
}
var values_83 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"action"), R.Join(new V[] {v_52}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_82, (V)1.0d)), new Entry((V)"dy", R.Get(v_82, (V)2.0d)))))));
V v_84 = values_83[0];
V v_85 = values_83[1];
if (R.Truth((V)(!R.Truth(v_84)))) {
return R.Join(new V[] {(V)false}, new Results(v_85));
}
}
}
}
}
return R.Join(new V[] {(V)false}, new Results((V)"ranged combat action limit"));
return Results.Empty;
}));
V v_86 = V.Nil;
v_86 = new V((RuleFunction)(args_87 => {
V v_88 = args_87[0];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"nearest"), R.Join(new V[] {v_88}, new Results(new V((RuleFunction)(args_89 => {
V v_90 = args_89[0];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_90, (V)"controller") == (V)"ai"), () => (V)(!R.Truth(R.Get(v_90, (V)"invulnerable")))), () => (V)(R.Get(v_90, (V)"id") != R.Get(R.Get(v_88, (V)"stage"), (V)"elite_id")))));
return Results.Empty;
}))))));
return Results.Empty;
}));
V v_91 = V.Nil;
v_91 = new V((RuleFunction)(args_92 => {
V v_93 = args_92[0];
V v_94 = args_92[1];
var values_95 = R.Join(new V[] {}, new Results(R.Get(v_93, (V)"state")));
V v_96 = values_95[0];
var values_97 = R.Join(new V[] {}, new Results(R.Get(v_96, (V)"active_floor_id")));
V v_98 = values_97[0];
var values_99 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(R.Get(R.Get(v_96, (V)"generated_map"), (V)"placements"), (V)"stairs"), (V)1.0d)}, new Results((V)"paired floor has no stair"))));
V v_100 = values_99[0];
var values_101 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_93, (V)"trace"))));
V v_102 = values_101[0];
var values_103 = R.Join(new V[] {}, new Results((V)false));
V v_104 = values_103[0];
{ double end_107 = R.Num(v_94), step_108 = R.Num((V)1.0d);
for (double i_106 = R.Num((V)1.0d); step_108 > 0 ? i_106 <= end_107 : i_106 >= end_107; i_106 += step_108) {
V v_105 = i_106;
if (R.Truth((V)(R.Get(v_96, (V)"active_floor_id") != v_98))) {
return R.Join(new V[] {}, new Results((V)true));
}
if (R.Truth(R.Get(v_96, (V)"game_over"))) {
return R.Join(new V[] {(V)false}, new Results((V)"dead on way to stair"));
}
var values_109 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_96))));
V v_110 = values_109[0];
if (R.Truth(R.Call(R.Get(v_2, (V)"movement_cycle"), R.Join(new V[] {v_93}, new Results(v_102))).First)) {
var values_111 = R.Join(new V[] {}, new Results((V)true));
v_104 = values_111[0];
}
if (R.Truth(R.Or(R.Call(R.Get(v_2, (V)"escape"), R.Join(new V[] {v_93, v_100}, new Results(v_104))).First, () => R.Call(R.Get(v_2, (V)"attack_adjacent"), R.Join(new V[] {v_93}, new Results(R.Get(R.Get(v_96, (V)"stage"), (V)"elite_id")))).First))) {

} else {
var values_114 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"path"), R.Join(new V[] {v_96}, new Results(new V((RuleFunction)(args_112 => {
V v_113 = args_112[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_113, (V)"x") == R.Get(v_100, (V)"x")), () => (V)(R.Get(v_113, (V)"y") == R.Get(v_100, (V)"y")))));
return Results.Empty;
}))))));
V v_115 = values_114[0];
if (R.Truth((V)(!R.Truth(v_115)))) {
return R.Join(new V[] {(V)false}, new Results((V)"no legal path to stair"));
}
var values_116 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"action"), R.Join(new V[] {v_93}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_115, (V)1.0d)), new Entry((V)"dy", R.Get(v_115, (V)2.0d)))))));
V v_117 = values_116[0];
V v_118 = values_116[1];
if (R.Truth((V)(!R.Truth(v_117)))) {
return R.Join(new V[] {(V)false}, new Results(v_118));
}
}
}
}
return R.Join(new V[] {(V)false}, new Results((V)"stair action limit"));
return Results.Empty;
}));
V v_119 = V.Nil;
v_119 = new V((RuleFunction)(args_120 => {
V v_121 = args_120[0];
var values_122 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"grave_dividend", (V)60.0d), new Entry((V)"thorn_mirror", (V)50.0d), new Entry((V)"emberbrand", (V)45.0d), new Entry((V)"storm_conductor", (V)40.0d), new Entry((V)"perfect_guard", (V)35.0d), new Entry((V)"temporal_echo", (V)30.0d), new Entry((V)"detonation_bloom", (V)26.0d), new Entry((V)"opening_cut", (V)25.0d), new Entry((V)"rhythm_of_steel", (V)24.0d))));
V v_123 = values_122[0];
var values_124 = R.Join(new V[] {}, R.Invoke(R.Get(v_121, (V)"boon_id"), "match", R.Join(new V[] {}, new Results((V)"([^/]+)$"))));
V v_125 = values_124[0];
var values_126 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"common", (V)1.0d), new Entry((V)"uncommon", (V)3.0d), new Entry((V)"rare", (V)6.0d), new Entry((V)"legendary", (V)10.0d))));
V v_127 = values_126[0];
return R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_123, v_125), () => (V)0.0d) + R.Or(R.Get(v_127, R.Get(v_121, (V)"rarity")), () => (V)0.0d))));
return Results.Empty;
}));
V v_128 = V.Nil;
v_128 = new V((RuleFunction)(args_129 => {
V v_130 = args_129[0];
V v_131 = args_129[1];
var values_132 = R.Join(new V[] {}, new Results(R.Get(v_130, (V)"state")));
V v_133 = values_132[0];
if (R.Truth((V)(R.Get(v_133, (V)"currency") < (V)5.0d))) {
return R.Join(new V[] {(V)false}, new Results((V)"unaffordable"));
}
var values_136 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"nearest"), R.Join(new V[] {v_133}, new Results(new V((RuleFunction)(args_134 => {
V v_135 = args_134[0];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_135, (V)"controller") == (V)"chest"), () => (V)(!R.Truth(R.Get(v_135, (V)"claimed")))), () => (V)(R.Get(v_135, (V)"price") > (V)0.0d))));
return Results.Empty;
}))))));
V v_137 = values_136[0];
if (R.Truth((V)(!R.Truth(v_137)))) {
return R.Join(new V[] {(V)false}, new Results((V)"no chest"));
}
var values_140 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"navigate"), R.Join(new V[] {v_130, new V((RuleFunction)(args_138 => {
V v_139 = args_138[0];
return R.Join(new V[] {}, R.Call(v_19, R.Join(new V[] {v_139}, new Results(R.Get(v_137, (V)"position")))));
return Results.Empty;
}))}, new Results(v_131))));
V v_141 = values_140[0];
V v_142 = values_140[1];
if (R.Truth((V)(!R.Truth(v_141)))) {
return R.Join(new V[] {(V)false}, new Results(v_142));
}
var values_143 = R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"chest_options"), R.Join(new V[] {v_133}, new Results(R.Get(v_137, (V)"id")))))).First, (V)"offers")));
V v_144 = values_143[0];
var values_145 = R.Join(new V[] {(V)1.0d}, new Results((-R.Get(rt.Global("math"), (V)"huge"))));
V v_146 = values_145[0];
V v_147 = values_145[1];
foreach (var row_148 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_144)))))) {
V v_149 = row_148[0];
V v_150 = row_148[1];
var values_151 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_130, (V)"offer_policy") == (V)"first"), () => (-v_149)), () => R.Call(v_119, R.Join(new V[] {}, new Results(v_150))).First)));
V v_152 = values_151[0];
if (R.Truth((V)(v_152 > v_147))) {
var values_153 = R.Join(new V[] {v_149}, new Results(v_152));
v_146 = values_153[0];
v_147 = values_153[1];
}
}
var values_154 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"action"), R.Join(new V[] {v_130}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_137, (V)"id")), new Entry((V)"offer_index", v_146))))));
v_141 = values_154[0];
v_142 = values_154[1];
if (R.Truth((V)(!R.Truth(v_141)))) {
return R.Join(new V[] {(V)false}, new Results(v_142));
}
var values_155 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"stage", R.Get(R.Get(v_133, (V)"stage"), (V)"index")), new Entry((V)"floor", R.Get(v_133, (V)"active_floor_index")), new Entry((V)"boon_id", R.Get(R.Get(v_144, v_146), (V)"boon_id")), new Entry((V)"rarity", R.Get(R.Get(v_144, v_146), (V)"rarity")), new Entry((V)"price", R.Get(v_137, (V)"price")))));
var obj_156 = R.Get(v_130, (V)"purchases");
V key_157 = (V)((V)R.Length(R.Get(v_130, (V)"purchases")) + (V)1.0d);
R.Set(obj_156, key_157, values_155[0]);
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
V v_158 = V.Nil;
v_158 = new V((RuleFunction)(args_159 => {
V v_160 = args_159[0];
V v_161 = args_159[1];
var values_162 = R.Join(new V[] {}, new Results(R.Get(v_160, (V)"state")));
V v_163 = values_162[0];
V v_164 = V.Nil;
v_164 = new V((RuleFunction)(args_165 => {

{ double end_168 = R.Num((V)16.0d), step_169 = R.Num((V)1.0d);
for (double i_167 = R.Num((V)1.0d); step_169 > 0 ? i_167 <= end_168 : i_167 >= end_168; i_167 += step_169) {
V v_166 = i_167;
if (R.Truth((V)(R.Get(v_163, (V)"currency") >= (V)5.0d))) {
return R.Join(new V[] {}, new Results((V)true));
}
var values_170 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_163))).First, (V)"position")));
V v_171 = values_170[0];
var values_172 = Results.Empty;
V v_173 = values_172[0];
V v_174 = values_172[1];
foreach (var row_175 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_163, (V)"drops"))))))) {
V v_176 = row_175[0];
V v_177 = row_175[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_177, (V)"kind") == (V)"currency"), () => (V)(!R.Truth(R.Get(v_177, (V)"collected")))), () => (V)(R.Get(R.Get(v_177, (V)"position"), (V)"floor_id") == R.Get(v_163, (V)"active_floor_id"))))) {
var values_178 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_171, (V)"x") - R.Get(R.Get(v_177, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_171, (V)"y") - R.Get(R.Get(v_177, (V)"position"), (V)"y"))))).First)));
V v_179 = values_178[0];
if (R.Truth(R.Or((V)(!R.Truth(v_173)), () => (V)(v_179 < v_174)))) {
var values_180 = R.Join(new V[] {v_177}, new Results(v_179));
v_173 = values_180[0];
v_174 = values_180[1];
}
}
}
if (R.Truth((V)(!R.Truth(v_173)))) {
return R.Join(new V[] {}, new Results((V)true));
}
var values_181 = R.Join(new V[] {R.Get(R.Get(v_173, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_173, (V)"position"), (V)"y")));
V v_182 = values_181[0];
V v_183 = values_181[1];
if (R.Truth((V)(v_174 == (V)0.0d))) {
var values_184 = R.Join(new V[] {}, new Results((V)false));
V v_185 = values_184[0];
foreach (var row_186 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_14)))))) {
V v_187 = row_186[0];
V v_188 = row_186[1];
var values_189 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"preview"), R.Join(new V[] {v_163, (V)"move", R.Get(v_188, (V)1.0d)}, new Results(R.Get(v_188, (V)2.0d)))));
V v_190 = values_189[0];
if (R.Truth(R.And(R.Get(v_190, (V)"valid"), () => (V)(R.Get(v_190, (V)"distance") == (V)1.0d)))) {
var values_191 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"action"), R.Join(new V[] {v_160}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_188, (V)1.0d)), new Entry((V)"dy", R.Get(v_188, (V)2.0d)))))));
V v_192 = values_191[0];
if (R.Truth(v_192)) {
var values_193 = R.Join(new V[] {}, new Results((V)true));
v_185 = values_193[0];

break;
}
}
}
if (R.Truth((V)(!R.Truth(v_185)))) {
return R.Join(new V[] {(V)false}, new Results((V)"cannot step off currency"));
}
}
var values_196 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"navigate"), R.Join(new V[] {v_160, new V((RuleFunction)(args_194 => {
V v_195 = args_194[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_195, (V)"x") == v_182), () => (V)(R.Get(v_195, (V)"y") == v_183))));
return Results.Empty;
}))}, new Results((V)200.0d))));
V v_197 = values_196[0];
V v_198 = values_196[1];
if (R.Truth((V)(!R.Truth(v_197)))) {
return R.Join(new V[] {(V)false}, new Results(v_198));
}
}
}
return R.Join(new V[] {(V)(R.Get(v_163, (V)"currency") >= (V)5.0d)}, new Results(R.Or(R.And((V)(R.Get(v_163, (V)"currency") >= (V)5.0d), () => V.Nil), () => (V)"currency pickup limit")));
return Results.Empty;
}));
{ double end_201 = R.Num(v_161), step_202 = R.Num((V)1.0d);
for (double i_200 = R.Num((V)1.0d); step_202 > 0 ? i_200 <= end_201 : i_200 >= end_201; i_200 += step_202) {
V v_199 = i_200;
var values_203 = R.Join(new V[] {}, R.Call(v_164, Results.Empty));
V v_204 = values_203[0];
V v_205 = values_203[1];
if (R.Truth((V)(!R.Truth(v_204)))) {
return R.Join(new V[] {(V)false}, new Results(v_205));
}
if (R.Truth((V)(R.Get(v_163, (V)"currency") >= (V)5.0d))) {
return R.Join(new V[] {}, new Results((V)true));
}
var values_206 = R.Join(new V[] {}, R.Call(v_86, R.Join(new V[] {}, new Results(v_163))));
V v_207 = values_206[0];
if (R.Truth((V)(!R.Truth(v_207)))) {
return R.Join(new V[] {(V)false}, new Results((V)"no living ordinary enemy"));
}
var values_209 = R.Join(new V[] {}, R.Call(v_50, R.Join(new V[] {v_160, R.Get(v_207, (V)"id"), (V)180.0d}, new Results(new V((RuleFunction)(args_208 => {

return R.Join(new V[] {}, new Results((V)(R.Get(v_163, (V)"currency") >= (V)5.0d)));
return Results.Empty;
}))))));
V v_210 = values_209[0];
V v_211 = values_209[1];
if (R.Truth((V)(!R.Truth(v_210)))) {
return R.Join(new V[] {(V)false}, new Results(v_211));
}
}
}
var values_212 = R.Join(new V[] {}, R.Call(v_164, Results.Empty));
V v_213 = values_212[0];
V v_214 = values_212[1];
if (R.Truth((V)(!R.Truth(v_213)))) {
return R.Join(new V[] {(V)false}, new Results(v_214));
}
return R.Join(new V[] {(V)(R.Get(v_163, (V)"currency") >= (V)5.0d)}, new Results(R.Or(R.And((V)(R.Get(v_163, (V)"currency") >= (V)5.0d), () => V.Nil), () => (V)"no currency after kills")));
return Results.Empty;
}));
V v_215 = V.Nil;
v_215 = new V((RuleFunction)(args_216 => {
V v_217 = args_216[0];
var values_218 = R.Join(new V[] {}, new Results(R.Get(v_217, (V)"state")));
V v_219 = values_218[0];
if (R.Truth((V)(R.Get(v_219, (V)"active_floor_index") != (V)1.0d))) {
return R.Join(new V[] {}, new Results((V)true));
}
if (R.Truth(R.And((V)(R.Get(R.Get(v_219, (V)"stage"), (V)"index") == (V)1.0d), () => (V)((V)R.Length(R.Get(v_217, (V)"purchases")) == (V)0.0d)))) {
var values_220 = R.Join(new V[] {}, R.Call(v_158, R.Join(new V[] {v_217}, new Results((V)8.0d))));
V v_221 = values_220[0];
V v_222 = values_220[1];
if (R.Truth((V)(!R.Truth(v_221)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"early economy: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_222))).First)));
}
var values_223 = R.Join(new V[] {}, R.Call(v_128, R.Join(new V[] {v_217}, new Results((V)300.0d))));
V v_224 = values_223[0];
V v_225 = values_223[1];
if (R.Truth((V)(!R.Truth(v_224)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"first purchase: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_225))).First)));
}
} else if (R.Truth((V)(R.Get(v_219, (V)"currency") >= (V)5.0d))) {
R.Call(v_128, R.Join(new V[] {v_217}, new Results((V)250.0d)));
}
return R.Join(new V[] {}, R.Call(v_91, R.Join(new V[] {v_217}, new Results((V)500.0d))));
return Results.Empty;
}));
V v_226 = V.Nil;
v_226 = new V((RuleFunction)(args_227 => {
V v_228 = args_227[0];
var values_229 = R.Join(new V[] {}, new Results(R.Get(v_228, (V)"state")));
V v_230 = values_229[0];
var values_231 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_4, (V)"entity"), R.Join(new V[] {v_230}, new Results(R.Get(R.Get(v_230, (V)"stage"), (V)"elite_id")))).First}, new Results((V)"missing biome elite"))));
V v_232 = values_231[0];
var values_235 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"navigate"), R.Join(new V[] {v_228, new V((RuleFunction)(args_233 => {
V v_234 = args_233[0];
return R.Join(new V[] {}, new Results(R.Or((V)(R.Call(v_23, R.Join(new V[] {v_230, v_234}, new Results(R.Get(v_232, (V)"position")))).First != V.Nil), () => R.Call(v_15, R.Join(new V[] {v_234}, new Results(R.Get(v_232, (V)"position")))).First)));
return Results.Empty;
}))}, new Results((V)1500.0d))));
V v_236 = values_235[0];
V v_237 = values_235[1];
if (R.Truth((V)(!R.Truth(v_236)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"elite approach: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_237))).First)));
}
var values_238 = R.Join(new V[] {}, R.Call(v_50, R.Join(new V[] {v_228, R.Get(v_232, (V)"id")}, new Results((V)300.0d))));
v_236 = values_238[0];
v_237 = values_238[1];
if (R.Truth((V)(!R.Truth(v_236)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"elite combat: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_237))).First)));
}
var values_239 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"entity"), R.Join(new V[] {v_230}, new Results(R.Get(R.Get(v_230, (V)"stage"), (V)"elite_reward_id")))));
V v_240 = values_239[0];
if (R.Truth(v_240)) {
var values_243 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"navigate"), R.Join(new V[] {v_228, new V((RuleFunction)(args_241 => {
V v_242 = args_241[0];
return R.Join(new V[] {}, R.Call(v_19, R.Join(new V[] {v_242}, new Results(R.Get(v_240, (V)"position")))));
return Results.Empty;
}))}, new Results((V)80.0d))));
v_236 = values_243[0];
v_237 = values_243[1];
if (R.Truth((V)(!R.Truth(v_236)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"elite reward approach: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_237))).First)));
}
var values_244 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"action"), R.Join(new V[] {v_228}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_240, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))));
v_236 = values_244[0];
v_237 = values_244[1];
if (R.Truth((V)(!R.Truth(v_236)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"elite reward: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_237))).First)));
}
}
var values_245 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_4, (V)"entity"), R.Join(new V[] {v_230}, new Results(R.Get(R.Get(v_230, (V)"stage"), (V)"exit_id")))).First}, new Results((V)"missing exit"))));
V v_246 = values_245[0];
var values_249 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"navigate"), R.Join(new V[] {v_228, new V((RuleFunction)(args_247 => {
V v_248 = args_247[0];
return R.Join(new V[] {}, R.Call(v_19, R.Join(new V[] {v_248}, new Results(R.Get(v_246, (V)"position")))));
return Results.Empty;
}))}, new Results((V)130.0d))));
v_236 = values_249[0];
v_237 = values_249[1];
if (R.Truth((V)(!R.Truth(v_236)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"exit approach: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_237))).First)));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
R.Set(v_12, (V)"attempt", new V((RuleFunction)(args_250 => {
V v_251 = args_250[0];
V v_252 = args_250[1];
var values_253 = R.Join(new V[] {}, new Results(R.Or(v_252, () => R.Table())));
v_252 = values_253[0];
var values_254 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_252, (V)"class_id"), () => R.Get(R.Get(v_10, (V)"order"), (V)1.0d))));
V v_255 = values_254[0];
var values_256 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_251}, new Results(R.Table(new Entry((V)"multifloor", (V)true), new Entry((V)"class_id", v_255), new Entry((V)"available_boons", R.Call(R.Get(v_8, (V)"pool"), R.Join(new V[] {}, new Results(R.Table()))).First))))));
V v_257 = values_256[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_252, (V)"offer_policy") == V.Nil), () => (V)(R.Get(v_252, (V)"offer_policy") == (V)"first"))}, new Results((V)"unknown offer policy")));
var values_258 = R.Join(new V[] {}, new Results(R.Get(v_252, (V)"offer_policy")));
var obj_259 = v_257;
V key_260 = (V)"offer_policy";
R.Set(obj_259, key_260, values_258[0]);
var values_261 = R.Join(new V[] {}, new Results(R.Table()));
var obj_262 = v_257;
V key_263 = (V)"purchases";
R.Set(obj_262, key_263, values_261[0]);
var values_264 = R.Join(new V[] {}, new Results(R.Get(v_257, (V)"state")));
V v_265 = values_264[0];
var values_266 = R.Join(new V[] {}, new Results(R.Table()));
V v_267 = values_266[0];
var values_268 = R.Join(new V[] {}, new Results(v_267));
var obj_269 = v_257;
V key_270 = (V)"stages";
R.Set(obj_269, key_270, values_268[0]);
{ double end_273 = R.Num((V)3.0d), step_274 = R.Num((V)1.0d);
for (double i_272 = R.Num((V)1.0d); step_274 > 0 ? i_272 <= end_273 : i_272 >= end_273; i_272 += step_274) {
V v_271 = i_272;
if (R.Truth(R.Get(v_265, (V)"game_over"))) {
return R.Join(new V[] {V.Nil, R.Concat((V)"dead before stage ", v_271)}, new Results(v_257));
}
if (R.Truth(R.Or((V)(R.Get(R.Get(v_265, (V)"stage"), (V)"index") != v_271), () => (V)(R.Get(v_265, (V)"active_floor_index") != (V)1.0d)))) {
return R.Join(new V[] {V.Nil, R.Concat((V)"wrong stage/floor at entry ", v_271)}, new Results(v_257));
}
var values_275 = R.Join(new V[] {}, R.Call(v_215, R.Join(new V[] {}, new Results(v_257))));
V v_276 = values_275[0];
V v_277 = values_275[1];
if (R.Truth((V)(!R.Truth(v_276)))) {
return R.Join(new V[] {V.Nil, R.Concat((V)"stage ", R.Concat(v_271, R.Concat((V)" upper: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_277))).First)))}, new Results(v_257));
}
if (R.Truth((V)(R.Get(v_265, (V)"active_floor_index") != (V)2.0d))) {
return R.Join(new V[] {V.Nil, R.Concat((V)"stage ", R.Concat(v_271, (V)" stair did not reach lower floor"))}, new Results(v_257));
}
var values_278 = R.Join(new V[] {}, R.Call(v_226, R.Join(new V[] {}, new Results(v_257))));
v_276 = values_278[0];
v_277 = values_278[1];
if (R.Truth((V)(!R.Truth(v_276)))) {
return R.Join(new V[] {V.Nil, R.Concat((V)"stage ", R.Concat(v_271, R.Concat((V)" lower: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_277))).First)))}, new Results(v_257));
}
var values_279 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"biome_id", R.Get(R.Get(v_265, (V)"stage"), (V)"biome_id")), new Entry((V)"actions", (V)R.Length(R.Get(v_257, (V)"trace"))), new Entry((V)"hp", R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_265))).First, (V)"hp")), new Entry((V)"clock", R.Get(v_265, (V)"clock")), new Entry((V)"floor", R.Get(v_265, (V)"active_floor_id")))));
var obj_280 = v_267;
V key_281 = (V)((V)R.Length(v_267) + (V)1.0d);
R.Set(obj_280, key_281, values_279[0]);
if (R.Truth((V)(R.Get(v_252, (V)"stop_after_stage") == v_271))) {
return R.Join(new V[] {}, new Results(v_257));
}
var values_282 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"entity"), R.Join(new V[] {v_265}, new Results(R.Get(R.Get(v_265, (V)"stage"), (V)"exit_id")))))));
V v_283 = values_282[0];
var values_284 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"route_options"), R.Join(new V[] {v_265}, new Results(R.Get(v_283, (V)"id")))))));
V v_285 = values_284[0];
var values_286 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Or(R.Get(v_252, (V)"routes"), () => R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)1.0d))), v_271), () => (V)1.0d)));
V v_287 = values_286[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_285, (V)"routes"), v_287)}, new Results(R.Concat((V)"missing route ", v_287))));
var values_288 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"action"), R.Join(new V[] {v_257}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_283, (V)"id")), new Entry((V)"route_index", v_287))))));
v_276 = values_288[0];
v_277 = values_288[1];
if (R.Truth((V)(!R.Truth(v_276)))) {
return R.Join(new V[] {V.Nil, R.Concat((V)"stage ", R.Concat(v_271, R.Concat((V)" route: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_277))).First)))}, new Results(v_257));
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_265, (V)"finale"), () => (V)(!R.Truth(R.Get(v_265, (V)"victory"))))}, new Results((V)"final sanctum entered without an ordinary boss fight")));
var values_289 = R.Join(new V[] {}, R.Call(v_50, R.Join(new V[] {v_257, R.Get(R.Get(v_265, (V)"finale"), (V)"boss_id")}, new Results((V)400.0d))));
V v_290 = values_289[0];
V v_291 = values_289[1];
if (R.Truth((V)(!R.Truth(v_290)))) {
return R.Join(new V[] {V.Nil, R.Concat((V)"final boss: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_291))).First)}, new Results(v_257));
}
if (R.Truth((V)(!R.Truth(R.Get(v_265, (V)"victory"))))) {
return R.Join(new V[] {V.Nil, (V)"boss fell without Victory"}, new Results(v_257));
}
return R.Join(new V[] {}, new Results(v_257));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_12));
return Results.Empty;
} }
