// Static C# port of tests/enemy_squad_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: bec58129a2353c82f6443a04d5fede4abb8c28e339ee49a5ea04a30a937b10c4
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_enemy_squad_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_17 = values_16[0];
V v_18 = V.Nil;
v_18 = new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
V v_21 = args_19[1];
V v_22 = args_19[2];
var values_23 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_24 = values_23[0];
var values_25 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_24))));
V v_26 = values_25[0];

var values_27 = R.Join(new V[] {R.Or(v_21, () => (V)9.0d)}, new Results(R.Or(v_22, () => (V)6.0d)));
var obj_28 = R.Get(v_26, (V)"position");
V key_29 = (V)"x";
var obj_30 = R.Get(v_26, (V)"position");
V key_31 = (V)"y";
R.Set(obj_28, key_29, values_27[0]);
R.Set(obj_30, key_31, values_27[1]);
var values_32 = R.Join(new V[] {(V)100.0d}, new Results((V)100.0d));
var obj_33 = v_26;
V key_34 = (V)"hp";
var obj_35 = v_26;
V key_36 = (V)"max_hp";
R.Set(obj_33, key_34, values_32[0]);
R.Set(obj_35, key_36, values_32[1]);
return R.Join(new V[] {v_24}, new Results(v_26));
return Results.Empty;
}));
V v_37 = V.Nil;
v_37 = new V((RuleFunction)(args_38 => {
V v_39 = args_38[0];
V v_40 = args_38[1];
V v_41 = args_38[2];
V v_42 = args_38[3];
V v_43 = args_38[4];
V v_44 = args_38[5];
var values_45 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_39, R.Concat((V)"core:enemy/", v_40), v_41, v_42}, new Results(R.Or(v_44, () => (V)100000.0d)))));
V v_46 = values_45[0];
var values_47 = R.Join(new V[] {}, new Results((V)"test:squad"));
var obj_48 = v_46;
V key_49 = (V)"squad_id";
R.Set(obj_48, key_49, values_47[0]);

var values_50 = R.Join(new V[] {}, new Results(R.Get(R.Table(new Entry((V)"ward_weaver", (V)"warded_bodyguards"), new Entry((V)"bodyguard_brute", (V)"warded_bodyguards"), new Entry((V)"mend_acolyte", (V)"healer_rushwings"), new Entry((V)"rushwing", (V)"healer_rushwings"), new Entry((V)"snare_scribe", (V)"snares_and_siege"), new Entry((V)"siege_wisp", (V)"snares_and_siege"), new Entry((V)"shield_bearer", (V)"shield_and_archer"), new Entry((V)"pin_archer", (V)"shield_and_archer"), new Entry((V)"banner_captain", (V)"captain_and_followers"), new Entry((V)"banner_follower", (V)"captain_and_followers")), v_40)));
var obj_51 = v_46;
V key_52 = (V)"squad_kind";
R.Set(obj_51, key_52, values_50[0]);
var values_53 = R.Join(new V[] {}, new Results(R.Or(R.And(v_43, () => R.Get(v_43, (V)"id")), () => R.Get(v_46, (V)"id"))));
var obj_54 = v_46;
V key_55 = (V)"squad_leader_id";
R.Set(obj_54, key_55, values_53[0]);
var values_56 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_46, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Or(R.And(v_43, () => R.Get(R.Get(v_43, (V)"position"), (V)"x")), () => v_41)), new Entry((V)"y", R.Or(R.And(v_43, () => R.Get(R.Get(v_43, (V)"position"), (V)"y")), () => v_42)))));
var obj_57 = v_46;
V key_58 = (V)"squad_home";
R.Set(obj_57, key_58, values_56[0]);
return R.Join(new V[] {}, new Results(v_46));
return Results.Empty;
}));
V v_59 = V.Nil;
v_59 = new V((RuleFunction)(args_60 => {
V v_61 = args_60[0];
V v_62 = args_60[1];
var values_63 = R.Join(new V[] {}, new Results(R.Table()));
V v_64 = values_63[0];

foreach (var row_65 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_61)))))) {
V v_66 = row_65[0];
V v_67 = row_65[1];
if (R.Truth((V)(R.Get(v_67, (V)"kind") == v_62))) {
var values_68 = R.Join(new V[] {}, new Results(v_67));
var obj_69 = v_64;
V key_70 = (V)((V)R.Length(v_64) + (V)1.0d);
R.Set(obj_69, key_70, values_68[0]);
}
}
return R.Join(new V[] {}, new Results(v_64));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"approved squads have ten registered roles and exact reviewed sprite assignments"}, new Results(new V((RuleFunction)(args_71 => {

var values_72 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legacy_kenney_visuals"))));
V v_73 = values_72[0];

var values_74 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_visuals"))));
V v_75 = values_74[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_7, (V)"enemies")) == (V)10.0d), () => (V)((V)R.Length(R.Get(v_7, (V)"order")) == (V)5.0d)))));
foreach (var row_76 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"enemies"))))))) {
V v_77 = row_76[0];
V v_78 = row_76[1];
var values_79 = R.Join(new V[] {}, new Results(R.Concat((V)"core:enemy/", R.Get(v_78, (V)1.0d))));
V v_80 = values_79[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_11, (V)"get_enemy"), R.Join(new V[] {}, new Results(v_80))).First, (V)"ai") == (V)"squad"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_73, (V)"squad_actors"), v_80) == R.Get(R.Get(v_7, (V)"tiny"), R.Get(v_78, (V)1.0d))), () => (V)(R.Get(R.Get(v_75, (V)"actors"), v_80) == R.Get(R.Get(v_7, (V)"onebit"), R.Get(v_78, (V)1.0d)))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_7, (V)"tiny"), (V)"ward_weaver") == (V)84.0d), () => (V)(R.Get(R.Get(v_7, (V)"onebit"), (V)"ward_weaver") == (V)319.0d)), () => (V)(R.Get(R.Get(v_7, (V)"tiny"), (V)"snare_scribe") == (V)124.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"completed field protects only linked nearby brutes; LOS, stun and caster death break it"}, new Results(new V((RuleFunction)(args_81 => {

var values_82 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {V.Nil, (V)9.0d}, new Results((V)7.0d))));
V v_83 = values_82[0];
V v_84 = values_82[1];
var values_85 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_83, (V)"ward_weaver", (V)8.0d}, new Results((V)8.0d))));
V v_86 = values_85[0];
var values_87 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_83, (V)"bodyguard_brute", (V)9.0d, (V)8.0d}, new Results(v_86))));
V v_88 = values_87[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"ward_source"), R.Join(new V[] {v_83}, new Results(v_88))).First)))));
var values_89 = R.Join(new V[] {}, new Results((V)true));
var obj_90 = R.Get(v_86, (V)"ai_memory");
V key_91 = (V)"ward_ready";
R.Set(obj_90, key_91, values_89[0]);
var values_92 = R.Join(new V[] {}, new Results(R.Get(v_88, (V)"hp")));
V v_93 = values_92[0];
var values_94 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_83}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d))))));
V v_95 = values_94[0];
V v_96 = values_94[1];
V v_97 = values_94[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_88, (V)"hp") == v_93), () => (V)(R.Get(R.Get(R.Call(v_59, R.Join(new V[] {v_97}, new Results((V)"DamageBlocked"))).First, (V)1.0d), (V)"block_reason") == (V)"forcefield")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"ward_source"), R.Join(new V[] {v_83}, new Results(v_86))).First)))));
var values_98 = R.Join(new V[] {}, new Results((V)13.0d));
var obj_99 = R.Get(v_88, (V)"position");
V key_100 = (V)"x";
R.Set(obj_99, key_100, values_98[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"ward_source"), R.Join(new V[] {v_83}, new Results(v_88))).First)))));
var values_101 = R.Join(new V[] {}, new Results((V)9.0d));
var obj_102 = R.Get(v_88, (V)"position");
V key_103 = (V)"x";
R.Set(obj_102, key_103, values_101[0]);

var values_104 = R.Join(new V[] {}, new Results((V)(R.Get(v_83, (V)"clock") + (V)100.0d)));
var obj_105 = v_86;
V key_106 = (V)"stunned_until";
R.Set(obj_105, key_106, values_104[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"ward_source"), R.Join(new V[] {v_83}, new Results(v_88))).First)))));

var values_107 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_108 = v_86;
V key_109 = (V)"stunned_until";
R.Set(obj_108, key_109, values_107[0]);
var values_110 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_door"), R.Join(new V[] {v_83, (V)8.0d}, new Results((V)7.0d))));
V v_111 = values_110[0];
var values_112 = R.Join(new V[] {(V)8.0d}, new Results((V)6.0d));
var obj_113 = R.Get(v_88, (V)"position");
V key_114 = (V)"x";
var obj_115 = R.Get(v_88, (V)"position");
V key_116 = (V)"y";
R.Set(obj_113, key_114, values_112[0]);
R.Set(obj_115, key_116, values_112[1]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"ward_source"), R.Join(new V[] {v_83}, new Results(v_88))).First)))));

var values_117 = R.Join(new V[] {}, new Results((V)true));
var obj_118 = v_111;
V key_119 = (V)"open";
R.Set(obj_118, key_119, values_117[0]);

var values_120 = R.Join(new V[] {}, new Results((V)false));
var obj_121 = v_111;
V key_122 = (V)"blocks_vision";
R.Set(obj_121, key_122, values_120[0]);

var values_123 = R.Join(new V[] {}, new Results((V)false));
var obj_124 = v_111;
V key_125 = (V)"blocks_projectiles";
R.Set(obj_124, key_125, values_123[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"ward_source"), R.Join(new V[] {v_83}, new Results(v_88)))));
var values_126 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_127 = v_86;
V key_128 = (V)"hp";
R.Set(obj_127, key_128, values_126[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"ward_source"), R.Join(new V[] {v_83}, new Results(v_88))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"guard lure leaves the four-tile field but respects six-tile leash on normal AI turns"}, new Results(new V((RuleFunction)(args_129 => {

var values_130 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {V.Nil, (V)13.0d}, new Results((V)8.0d))));
V v_131 = values_130[0];
V v_132 = values_130[1];
var values_133 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_131, (V)"ward_weaver", (V)7.0d}, new Results((V)8.0d))));
V v_134 = values_133[0];
var values_135 = R.Join(new V[] {}, new Results((V)true));
var obj_136 = R.Get(v_134, (V)"ai_memory");
V key_137 = (V)"ward_ready";
R.Set(obj_136, key_137, values_135[0]);
var values_138 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_131, (V)"bodyguard_brute", (V)11.0d, (V)8.0d}, new Results(v_134))));
V v_139 = values_138[0];
var values_140 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_131))));
V v_141 = values_140[0];
var values_142 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_131}, new Results(v_139))));
V v_143 = values_142[0];
R.Call(v_3, R.Join(new V[] {v_141}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_131)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_143, (V)"kind") == (V)"move"), () => (V)(R.Get(v_143, (V)"dx") == (V)1.0d)))));
var values_144 = R.Join(new V[] {}, new Results((V)12.0d));
var obj_145 = R.Get(v_139, (V)"position");
V key_146 = (V)"x";
R.Set(obj_145, key_146, values_144[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"ward_source"), R.Join(new V[] {v_131}, new Results(v_139))).First)))));
var values_147 = R.Join(new V[] {}, new Results((V)13.0d));
var obj_148 = R.Get(v_139, (V)"position");
V key_149 = (V)"x";
R.Set(obj_148, key_149, values_147[0]);

var values_150 = R.Join(new V[] {}, new Results((V)14.0d));
var obj_151 = R.Get(v_132, (V)"position");
V key_152 = (V)"x";
R.Set(obj_151, key_152, values_150[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_131}, new Results(v_139))).First, (V)"kind") == (V)"windup"))));
var values_153 = R.Join(new V[] {}, new Results((V)14.0d));
var obj_154 = R.Get(v_139, (V)"position");
V key_155 = (V)"x";
R.Set(obj_154, key_155, values_153[0]);

var values_156 = R.Join(new V[] {}, new Results((V)13.0d));
var obj_157 = R.Get(v_132, (V)"position");
V key_158 = (V)"x";
R.Set(obj_157, key_158, values_156[0]);
var values_159 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_131}, new Results(v_139))));
v_143 = values_159[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_143, (V)"kind") == (V)"move")}, new Results((V)"outside leash must return rather than attack")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ward activation is a cancellable locked channel and resumes byte-identically"}, new Results(new V((RuleFunction)(args_160 => {

var values_161 = R.Join(new V[] {}, R.Call(v_18, Results.Empty));
V v_162 = values_161[0];
var values_163 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_162, (V)"ward_weaver", (V)9.0d, (V)8.0d, V.Nil}, new Results((V)0.0d))));
V v_164 = values_163[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_162}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_164, (V)"pending_attack"), (V)"support"), (V)"kind") == (V)"ward"), () => (V)(!R.Truth(R.Get(R.Get(v_164, (V)"ai_memory"), (V)"ward_ready")))))));
var values_165 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_162))));
V v_166 = values_165[0];
var values_167 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, new Results(v_166))))))));
V v_168 = values_167[0];
var values_169 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_162}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_170 = values_169[0];
V v_171 = values_169[1];
V v_172 = values_169[2];
var values_173 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_168}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_174 = values_173[0];
V v_175 = values_173[1];
V v_176 = values_173[2];
R.Call(v_3, R.Join(new V[] {v_172}, new Results(v_176)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_162))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_168)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_164, (V)"ai_memory"), (V)"ward_ready"), () => (V)((V)R.Length(R.Call(v_59, R.Join(new V[] {v_172}, new Results((V)"SquadWardRaised"))).First) == (V)1.0d)))));
var values_177 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, new Results(v_166))));
V v_178 = values_177[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_178, R.Get(v_164, (V)"id")}, new Results((V)300.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_178}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Get(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_178}, new Results(R.Get(v_164, (V)"id")))).First, (V)"ai_memory"), (V)"ward_ready"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"primary multi-target damage evaluates the ward before the full HP batch commits"}, new Results(new V((RuleFunction)(args_179 => {

var values_180 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {V.Nil, (V)8.0d}, new Results((V)7.0d))));
V v_181 = values_180[0];
V v_182 = values_180[1];

var values_183 = R.Join(new V[] {(V)30.0d}, new Results((V)30.0d));
var obj_184 = v_182;
V key_185 = (V)"damage";
var obj_186 = v_182;
V key_187 = (V)"slash_damage";
R.Set(obj_184, key_185, values_183[0]);
R.Set(obj_186, key_187, values_183[1]);
var values_188 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_181, (V)"ward_weaver", (V)8.0d}, new Results((V)8.0d))));
V v_189 = values_188[0];
var values_190 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_181, (V)"bodyguard_brute", (V)9.0d, (V)8.0d}, new Results(v_189))));
V v_191 = values_190[0];
var values_192 = R.Join(new V[] {}, new Results((V)true));
var obj_193 = R.Get(v_189, (V)"ai_memory");
V key_194 = (V)"ward_ready";
R.Set(obj_193, key_194, values_192[0]);
var values_195 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_181}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d))))));
V v_196 = values_195[0];
V v_197 = values_195[1];
V v_198 = values_195[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_181}, new Results(R.Get(v_189, (V)"id")))).First)), () => (V)(R.Get(v_191, (V)"hp") == R.Get(v_191, (V)"max_hp"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_59, R.Join(new V[] {v_198}, new Results((V)"DamageBatchResolved"))).First) >= (V)1.0d), () => (V)(!R.Truth(R.Call(R.Get(v_7, (V)"ward_source"), R.Join(new V[] {v_181}, new Results(v_191))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_181}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_181}, new Results(R.Get(v_191, (V)"id")))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"directional shield uses authoritative facing and leaves rear, DOT and secondary damage open"}, new Results(new V((RuleFunction)(args_199 => {

var values_200 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {V.Nil, (V)9.0d}, new Results((V)7.0d))));
V v_201 = values_200[0];
V v_202 = values_200[1];
var values_203 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_201, (V)"shield_bearer", (V)9.0d}, new Results((V)8.0d))));
V v_204 = values_203[0];
var values_205 = R.Join(new V[] {(V)0.0d}, new Results((-(V)1.0d)));
var obj_206 = R.Get(v_204, (V)"ai_memory");
V key_207 = (V)"shield_dx";
var obj_208 = R.Get(v_204, (V)"ai_memory");
V key_209 = (V)"shield_dy";
R.Set(obj_206, key_207, values_205[0]);
R.Set(obj_208, key_209, values_205[1]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"block"), R.Join(new V[] {v_201, v_204, v_202}, new Results((V)true))).First == (V)"directional shield"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"block"), R.Join(new V[] {v_201, v_204, v_202}, new Results((V)false))).First)))));
var values_210 = R.Join(new V[] {(V)10.0d}, new Results((V)8.0d));
var obj_211 = R.Get(v_202, (V)"position");
V key_212 = (V)"x";
var obj_213 = R.Get(v_202, (V)"position");
V key_214 = (V)"y";
R.Set(obj_211, key_212, values_210[0]);
R.Set(obj_213, key_214, values_210[1]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"block"), R.Join(new V[] {v_201, v_204, v_202}, new Results((V)true))).First)))));
var values_215 = R.Join(new V[] {(V)9.0d}, new Results((V)9.0d));
var obj_216 = R.Get(v_202, (V)"position");
V key_217 = (V)"x";
var obj_218 = R.Get(v_202, (V)"position");
V key_219 = (V)"y";
R.Set(obj_216, key_217, values_215[0]);
R.Set(obj_218, key_219, values_215[1]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"block"), R.Join(new V[] {v_201, v_204, v_202}, new Results((V)true))).First)))));
var values_220 = R.Join(new V[] {}, new Results((V)7.0d));
var obj_221 = R.Get(v_202, (V)"position");
V key_222 = (V)"y";
R.Set(obj_221, key_222, values_220[0]);
var values_223 = R.Join(new V[] {}, new Results(R.Get(v_204, (V)"hp")));
V v_224 = values_223[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_201}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_204, (V)"hp") == v_224))));
var values_225 = R.Join(new V[] {}, new Results((V)(R.Get(v_201, (V)"clock") + (V)200.0d)));
var obj_226 = v_204;
V key_227 = (V)"stunned_until";
R.Set(obj_226, key_227, values_225[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_201}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_204, (V)"hp") < v_224))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"captain rally is frozen into future follower commitments and removed on leader death"}, new Results(new V((RuleFunction)(args_228 => {

var values_229 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {V.Nil, (V)9.0d}, new Results((V)7.0d))));
V v_230 = values_229[0];
V v_231 = values_229[1];
var values_232 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_230, (V)"banner_captain", (V)8.0d}, new Results((V)8.0d))));
V v_233 = values_232[0];
var values_234 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_230, (V)"banner_follower", (V)9.0d, (V)8.0d, v_233}, new Results((V)0.0d))));
V v_235 = values_234[0];
var values_236 = R.Join(new V[] {}, new Results((V)50.0d));
var obj_237 = v_235;
V key_238 = (V)"speed";
R.Set(obj_237, key_238, values_236[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_230}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_235, (V)"pending_attack"), (V)"damage") == (V)2.0d))));
var values_239 = R.Join(new V[] {}, new Results((V)30.0d));
var obj_240 = v_231;
V key_241 = (V)"damage";
R.Set(obj_240, key_241, values_239[0]);
var values_242 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_230}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)1.0d))))));
V v_243 = values_242[0];
V v_244 = values_242[1];
V v_245 = values_242[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_230}, new Results(R.Get(v_233, (V)"id")))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_7, (V)"rally_source"), R.Join(new V[] {v_230}, new Results(v_235))).First))))));
var values_246 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {v_245}, new Results((V)"DamageTaken"))));
V v_247 = values_246[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_247, (V)R.Length(v_247)), (V)"target_id") == R.Get(v_231, (V)"id")), () => (V)(R.Get(R.Get(v_247, (V)R.Length(v_247)), (V)"attempted_damage") == (V)2.0d)))));
{ double end_250 = R.Num((V)3.0d), step_251 = R.Num((V)1.0d);
for (double i_249 = R.Num((V)1.0d); step_251 > 0 ? i_249 <= end_250 : i_249 >= end_250; i_249 += step_251) {
V v_248 = i_249;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_230}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
if (R.Truth(R.Get(v_235, (V)"pending_attack"))) {
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_235, (V)"pending_attack"), () => (V)(R.Get(R.Get(v_235, (V)"pending_attack"), (V)"damage") == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"healer targets a locked wounded ally and separate attackers choose different approaches"}, new Results(new V((RuleFunction)(args_252 => {

var values_253 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {V.Nil, (V)9.0d}, new Results((V)5.0d))));
V v_254 = values_253[0];
V v_255 = values_253[1];
var values_256 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_254, (V)"mend_acolyte", (V)9.0d}, new Results((V)8.0d))));
V v_257 = values_256[0];
var values_258 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_254, (V)"rushwing", (V)8.0d, (V)7.0d}, new Results(v_257))));
V v_259 = values_258[0];
var values_260 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_254, (V)"rushwing", (V)10.0d, (V)7.0d}, new Results(v_257))));
V v_261 = values_260[0];
var values_262 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_263 = v_259;
V key_264 = (V)"hp";
R.Set(obj_263, key_264, values_262[0]);
var values_265 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_254}, new Results(v_257))));
V v_266 = values_265[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_266, (V)"support"), (V)"kind") == (V)"heal"), () => (V)(R.Get(R.Get(v_266, (V)"support"), (V)"target_id") == R.Get(v_259, (V)"id"))), () => (V)(R.Get(R.Get(v_266, (V)"support"), (V)"power") == (V)2.0d)))));
var values_267 = R.Join(new V[] {}, new Results(R.Get(v_259, (V)"max_hp")));
var obj_268 = v_259;
V key_269 = (V)"hp";
R.Set(obj_268, key_269, values_267[0]);
var values_270 = R.Join(new V[] {R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_254}, new Results(v_259))).First}, R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_254}, new Results(v_261))));
V v_271 = values_270[0];
V v_272 = values_270[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_271, (V)"kind") == (V)"move"), () => (V)(R.Get(v_272, (V)"kind") == (V)"move")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)((V)(R.Get(R.Get(v_259, (V)"position"), (V)"x") + R.Get(v_271, (V)"dx")) != (V)(R.Get(R.Get(v_261, (V)"position"), (V)"x") + R.Get(v_272, (V)"dx"))), () => (V)((V)(R.Get(R.Get(v_259, (V)"position"), (V)"y") + R.Get(v_271, (V)"dy")) != (V)(R.Get(R.Get(v_261, (V)"position"), (V)"y") + R.Get(v_272, (V)"dy")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"snare placement is locked, capped, visible data and leaves an untrapped escape"}, new Results(new V((RuleFunction)(args_273 => {

var values_274 = R.Join(new V[] {}, R.Call(v_18, Results.Empty));
V v_275 = values_274[0];
V v_276 = values_274[1];
var values_277 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_275, (V)"snare_scribe", (V)9.0d, (V)8.0d, V.Nil}, new Results((V)0.0d))));
V v_278 = values_277[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_275}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_278, (V)"pending_attack"), (V)"support"), (V)"kind") == (V)"snare"))));
var values_279 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_275))))));
V v_280 = values_279[0];
var values_281 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_275}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_282 = values_281[0];
V v_283 = values_281[1];
V v_284 = values_281[2];
var values_285 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_280}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_286 = values_285[0];
V v_287 = values_285[1];
V v_288 = values_285[2];
R.Call(v_3, R.Join(new V[] {v_284}, new Results(v_288)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_275))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_280)))));
var values_289 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_275, (V)"enemy_snares"), (V)1.0d)))));
V v_290 = values_289[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_59, R.Join(new V[] {v_284}, new Results((V)"EnemySnarePlaced"))).First) == (V)1.0d))));
var values_291 = R.Join(new V[] {R.Get(v_275, (V)"clock")}, new Results(R.Get(v_276, (V)"hp")));
V v_292 = values_291[0];
V v_293 = values_291[1];
var values_294 = R.Join(new V[] {(V)(R.Get(v_290, (V)"x") - R.Get(R.Get(v_276, (V)"position"), (V)"x"))}, new Results((V)(R.Get(v_290, (V)"y") - R.Get(R.Get(v_276, (V)"position"), (V)"y"))));
V v_295 = values_294[0];
V v_296 = values_294[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_295))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_296))).First) == (V)1.0d))));
var values_297 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_275}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_295), new Entry((V)"dy", v_296))))));
V v_298 = values_297[0];
V v_299 = values_297[1];
V v_300 = values_297[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_276, (V)"hp") == (V)(v_293 - (V)1.0d)), () => (V)(R.Get(v_275, (V)"clock") == (V)(v_292 + (V)140.0d))), () => (V)((V)R.Length(R.Get(v_275, (V)"enemy_snares")) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_59, R.Join(new V[] {v_300}, new Results((V)"EnemySnareTriggered"))).First) == (V)1.0d))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_275))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_275)))))))));
var values_301 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"source_id", R.Get(v_278, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_276, (V)"position"), (V)"floor_id")), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d))), new Entry((V)2, R.Table(new Entry((V)"source_id", R.Get(v_278, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_276, (V)"position"), (V)"floor_id")), new Entry((V)"x", (V)10.0d), new Entry((V)"y", (V)6.0d))))));
var obj_302 = v_275;
V key_303 = (V)"enemy_snares";
R.Set(obj_302, key_303, values_301[0]);
var values_304 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_275}, new Results(v_278))));
V v_305 = values_304[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(!R.Truth(R.Get(v_305, (V)"support"))), () => (V)(R.Get(R.Get(v_305, (V)"support"), (V)"kind") != (V)"snare")))));
var values_306 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_275))));
V v_307 = values_306[0];

var values_308 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_307, (V)"enemy_snares"), (V)1.0d)));
var obj_309 = R.Get(v_307, (V)"enemy_snares");
V key_310 = (V)3.0d;
R.Set(obj_309, key_310, values_308[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_307))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"artillery locks a compact square and archer never selects a friendly-blocked shot"}, new Results(new V((RuleFunction)(args_311 => {

var values_312 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {V.Nil, (V)9.0d}, new Results((V)6.0d))));
V v_313 = values_312[0];
V v_314 = values_312[1];
var values_315 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_313, (V)"siege_wisp", (V)9.0d}, new Results((V)9.0d))));
V v_316 = values_315[0];
var values_317 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_313}, new Results(v_316))));
V v_318 = values_317[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_318, (V)"ability_id") == (V)"core:ability/siege_square"), () => (V)((V)R.Length(R.Get(v_318, (V)"cells")) == (V)4.0d)), () => (V)(R.Get(v_318, (V)"ranged") == (V)"area")))));
var values_319 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_313, (V)"pin_archer", (V)10.0d}, new Results((V)9.0d))));
V v_320 = values_319[0];
var values_321 = R.Join(new V[] {(V)10.0d}, new Results((V)6.0d));
var obj_322 = R.Get(v_314, (V)"position");
V key_323 = (V)"x";
var obj_324 = R.Get(v_314, (V)"position");
V key_325 = (V)"y";
R.Set(obj_322, key_323, values_321[0]);
R.Set(obj_324, key_325, values_321[1]);
var values_326 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_313, (V)"shield_bearer", (V)10.0d, (V)8.0d}, new Results(V.Nil))));
V v_327 = values_326[0];
var values_328 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_313}, new Results(v_320))));
v_318 = values_328[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_318, (V)"ability_id") != (V)"core:ability/pin_shot"))));
var values_329 = R.Join(new V[] {}, new Results((V)11.0d));
var obj_330 = R.Get(v_327, (V)"position");
V key_331 = (V)"x";
R.Set(obj_330, key_331, values_329[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_313}, new Results(v_320))).First, (V)"ability_id") == (V)"core:ability/pin_shot"))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"generated squads fit open counterplay patches, seed replay and old snapshots never repopulate"}, new Results(new V((RuleFunction)(args_332 => {

var values_333 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_334 = values_333[0];
V v_335 = values_333[1];
var values_336 = R.Join(new V[] {}, new Results((V)0.0d));
V v_337 = values_336[0];
foreach (var row_338 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_17, (V)"order"))))))) {
V v_339 = row_338[0];
V v_340 = row_338[1];
{ double end_343 = R.Num((V)30.0d), step_344 = R.Num((V)1.0d);
for (double i_342 = R.Num((V)1.0d); step_344 > 0 ? i_342 <= end_343 : i_342 >= end_343; i_342 += step_344) {
V v_341 = i_342;
var values_345 = R.Join(new V[] {}, new Results((V)(v_340 == R.Get(R.Get(v_17, (V)"order"), (V)1.0d))));
V v_346 = values_345[0];
var values_347 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"stage_options", R.Table(new Entry((V)"index", R.Or(R.And(v_346, () => (V)1.0d), () => (V)2.0d)), new Entry((V)"biome_id", v_340), new Entry((V)"route_history", R.Or(R.And(v_346, () => R.Table()), () => R.Table(new Entry(1, new Results(R.Invoke(v_340, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First))))))))));
V v_348 = values_347[0];
var values_349 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_341}, new Results(v_348))));
V v_350 = values_349[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_350, (V)"rng"), (V)"state") == R.Get(R.Call(R.Get(v_15, (V)"new"), R.Join(new V[] {}, new Results(v_341))).First, (V)"state")))));
var values_351 = Results.Empty;
V v_352 = values_351[0];
V v_353 = values_351[1];
foreach (var row_354 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_350, (V)"entities"))))))) {
V v_355 = row_354[0];
V v_356 = row_354[1];
if (R.Truth(R.Get(v_356, (V)"squad_id"))) {
var values_357 = R.Join(new V[] {}, new Results((V)(R.Or(v_353, () => (V)0.0d) + (V)1.0d)));
v_353 = values_357[0];
var values_358 = R.Join(new V[] {}, new Results((V)true));
var obj_359 = v_334;
V key_360 = R.Get(v_356, (V)"squad_kind");
R.Set(obj_359, key_360, values_358[0]);
if (R.Truth((V)(R.Get(v_356, (V)"id") == R.Get(v_356, (V)"squad_leader_id")))) {
var values_361 = R.Join(new V[] {}, new Results(v_356));
v_352 = values_361[0];

var values_362 = R.Join(new V[] {}, new Results((V)(v_337 + (V)1.0d)));
v_337 = values_362[0];
}
}
}
if (R.Truth(v_352)) {
if (R.Truth((V)(R.Get(v_352, (V)"squad_kind") == (V)"captain_and_followers"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_353 >= (V)3.0d), () => (V)(v_353 <= (V)6.0d)))));

var values_363 = R.Join(new V[] {}, new Results((V)true));
var obj_364 = v_335;
V key_365 = (V)(v_353 - (V)1.0d);
R.Set(obj_364, key_365, values_363[0]);
} else if (R.Truth((V)(R.Get(v_352, (V)"squad_kind") == (V)"warded_bodyguards"))) {
var values_366 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(R.Get(v_352, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_352, (V)"position"), (V)"y")))))));
V v_367 = values_366[0];
foreach (var row_368 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_350, (V)"entities"))))))) {
V v_369 = row_368[0];
V v_370 = row_368[1];
if (R.Truth(R.And((V)(R.Get(v_370, (V)"squad_leader_id") == R.Get(v_352, (V)"id")), () => (V)(R.Get(v_370, (V)"id") != R.Get(v_352, (V)"id"))))) {
var values_371 = R.Join(new V[] {}, new Results(R.Get(v_370, (V)"position")));
var obj_372 = v_367;
V key_373 = (V)((V)R.Length(v_367) + (V)1.0d);
R.Set(obj_372, key_373, values_371[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"flank_reachable"), R.Join(new V[] {R.Get(v_350, (V)"generated_map")}, new Results(v_367)))));
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_350))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_350)))))))));
}
}
}
}
foreach (var row_374 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"order"))))))) {
V v_375 = row_374[0];
V v_376 = row_374[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_334, v_376)}, new Results(R.Concat((V)"unplaced squad ", v_376))));
}
{ double end_379 = R.Num((V)5.0d), step_380 = R.Num((V)1.0d);
for (double i_378 = R.Num((V)2.0d); step_380 > 0 ? i_378 <= end_379 : i_378 >= end_379; i_378 += step_380) {
V v_377 = i_378;
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_335, v_377)}, new Results(R.Concat((V)"missing seeded follower count ", v_377))));
}
}
R.Call(rt.Global("print"), R.Join(new V[] {}, R.Invoke((V)"SQUAD CORPUS 210 floors: %d squads; five combinations, all 2\u00e2\u0080\u00935 follower sizes", "format", R.Join(new V[] {}, new Results(v_337)))));
var values_381 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_generated"), R.Join(new V[] {}, new Results((V)401.0d))));
V v_382 = values_381[0];
var values_383 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_382))));
V v_384 = values_383[0];
R.Call(v_3, R.Join(new V[] {v_384}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, new Results(v_384)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"invulnerable guards cannot seal the only generated approach to their wizard"}, new Results(new V((RuleFunction)(args_385 => {

var values_386 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"start_x", (V)2.0d), new Entry((V)"start_y", (V)3.0d), new Entry((V)"rows", R.Table(new Entry((V)1, (V)"#######"), new Entry((V)2, (V)"#######"), new Entry((V)3, (V)"#.....#"), new Entry((V)4, (V)"#######"), new Entry((V)5, (V)"#######"))))));
V v_387 = values_386[0];
var values_388 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)5.0d), new Entry((V)"y", (V)3.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)3.0d))))));
V v_389 = values_388[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"flank_reachable"), R.Join(new V[] {v_387}, new Results(v_389))).First)))));
var values_390 = R.Join(new V[] {}, new Results((V)"#.....#"));
var obj_391 = R.Get(v_387, (V)"rows");
V key_392 = (V)2.0d;
R.Set(obj_391, key_392, values_390[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"flank_reachable"), R.Join(new V[] {v_387}, new Results(v_389)))));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(R.Get(v_7, (V)"flank_reachable"), R.Join(new V[] {v_387, v_389}, new Results(R.Table(new Entry((V)"3,2", (V)true))))).First))}, new Results((V)"bypass crossed an existing locked prop or permanent pillar")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"completed healer channel restores one wounded ally but does not follow a displaced target"}, new Results(new V((RuleFunction)(args_393 => {

var values_394 = R.Join(new V[] {}, R.Call(v_18, Results.Empty));
V v_395 = values_394[0];
var values_396 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_395, (V)"mend_acolyte", (V)9.0d, (V)8.0d, V.Nil}, new Results((V)0.0d))));
V v_397 = values_396[0];
var values_398 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_395, (V)"rushwing", (V)8.0d, (V)8.0d}, new Results(v_397))));
V v_399 = values_398[0];

var values_400 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_401 = v_399;
V key_402 = (V)"hp";
R.Set(obj_401, key_402, values_400[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_395}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_397, (V)"pending_attack"), (V)"support"), (V)"target_id") == R.Get(v_399, (V)"id")))));
var values_403 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_395))))));
V v_404 = values_403[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"displace"), R.Join(new V[] {v_404, R.Get(v_399, (V)"id"), (V)7.0d}, new Results((V)8.0d)))));
var values_405 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_395}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_406 = values_405[0];
V v_407 = values_405[1];
V v_408 = values_405[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_399, (V)"hp") == (V)3.0d), () => (V)((V)R.Length(R.Call(v_59, R.Join(new V[] {v_408}, new Results((V)"Healed"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_404}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_404}, new Results(R.Get(v_399, (V)"id")))).First, (V)"hp") == (V)1.0d)}, new Results((V)"locked heal followed its former target")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"squad artillery retains locked cells and canonical enemy-on-enemy damage batching"}, new Results(new V((RuleFunction)(args_409 => {

var values_410 = R.Join(new V[] {}, R.Call(v_18, Results.Empty));
V v_411 = values_410[0];
V v_412 = values_410[1];
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_411, (V)"core:enemy/siege_wisp", (V)9.0d, (V)9.0d}, new Results((V)0.0d)));
var values_413 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_411, (V)"core:enemy/mossbound_guard", (V)10.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_414 = values_413[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_411}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_415 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_411))))));
V v_416 = values_415[0];
var values_417 = R.Join(new V[] {}, new Results(R.Table()));
V v_418 = values_417[0];
{ double end_421 = R.Num((V)2.0d), step_422 = R.Num((V)1.0d);
for (double i_420 = R.Num((V)1.0d); step_422 > 0 ? i_420 <= end_421 : i_420 >= end_421; i_420 += step_422) {
V v_419 = i_420;
var values_423 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_411}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_424 = values_423[0];
V v_425 = values_423[1];
V v_426 = values_423[2];
var values_427 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_416}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_428 = values_427[0];
V v_429 = values_427[1];
V v_430 = values_427[2];
R.Call(v_3, R.Join(new V[] {v_426}, new Results(v_430)));
foreach (var row_431 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_426)))))) {
V v_432 = row_431[0];
V v_433 = row_431[1];
var values_434 = R.Join(new V[] {}, new Results(v_433));
var obj_435 = v_418;
V key_436 = (V)((V)R.Length(v_418) + (V)1.0d);
R.Set(obj_435, key_436, values_434[0]);
}
}
}
var values_437 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_59, R.Join(new V[] {v_418}, new Results((V)"DamageBatchResolved"))).First, (V)1.0d)));
V v_438 = values_437[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_438, () => (V)((V)R.Length(R.Get(v_438, (V)"outcomes")) == (V)2.0d)), () => (V)(R.Get(v_412, (V)"hp") == (V)98.0d)), () => (V)(R.Get(v_414, (V)"hp") == (V)9.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_411))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_416)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"caster death removes its bounded snares and existing saves gain no new encounters"}, new Results(new V((RuleFunction)(args_439 => {

var values_440 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {V.Nil, (V)9.0d}, new Results((V)7.0d))));
V v_441 = values_440[0];
V v_442 = values_440[1];

var values_443 = R.Join(new V[] {}, new Results((V)30.0d));
var obj_444 = v_442;
V key_445 = (V)"damage";
R.Set(obj_444, key_445, values_443[0]);
var values_446 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_441, (V)"snare_scribe", (V)9.0d}, new Results((V)8.0d))));
V v_447 = values_446[0];
var values_448 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"source_id", R.Get(v_447, (V)"id")), new Entry((V)"floor_id", R.Get(v_441, (V)"active_floor_id")), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d))))));
var obj_449 = v_441;
V key_450 = (V)"enemy_snares";
R.Set(obj_449, key_450, values_448[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_441)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_441}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_441, (V)"enemy_snares")) == (V)0.0d), () => (V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_441}, new Results(R.Get(v_447, (V)"id")))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_441)))))));
var values_451 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_generated"), R.Join(new V[] {}, new Results((V)41.0d))));
V v_452 = values_451[0];
var values_453 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_452))));
V v_454 = values_453[0];
var values_455 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, new Results(v_454))))))));
V v_456 = values_455[0];
R.Call(v_3, R.Join(new V[] {v_454}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_456)))));
foreach (var row_457 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_456, (V)"entities"))))))) {
V v_458 = row_457[0];
V v_459 = row_457[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_459, (V)"squad_id"))))));
}
var values_460 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_441))));
V v_461 = values_460[0];
var values_462 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"source_id", R.Get(v_447, (V)"id")), new Entry((V)"floor_id", R.Get(v_441, (V)"active_floor_id")), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d))))));
var obj_463 = v_461;
V key_464 = (V)"enemy_snares";
R.Set(obj_463, key_464, values_462[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_461))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"shield bash lowers protection through its windup and recovery for corridor counterplay"}, new Results(new V((RuleFunction)(args_465 => {

var values_466 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {V.Nil, (V)9.0d}, new Results((V)7.0d))));
V v_467 = values_466[0];
V v_468 = values_466[1];
var values_469 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_467, (V)"shield_bearer", (V)9.0d, (V)8.0d, V.Nil}, new Results((V)0.0d))));
V v_470 = values_469[0];
var values_471 = R.Join(new V[] {(V)0.0d}, new Results((-(V)1.0d)));
var obj_472 = R.Get(v_470, (V)"ai_memory");
V key_473 = (V)"shield_dx";
var obj_474 = R.Get(v_470, (V)"ai_memory");
V key_475 = (V)"shield_dy";
R.Set(obj_472, key_473, values_471[0]);
R.Set(obj_474, key_475, values_471[1]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"block"), R.Join(new V[] {v_467, v_470, v_468}, new Results((V)true)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_467}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_470, (V)"pending_attack"), () => R.Get(R.Get(v_470, (V)"ai_memory"), (V)"shield_lowered")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"block"), R.Join(new V[] {v_467, v_470, v_468}, new Results((V)true))).First)))));
var values_476 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_467))))))))));
V v_477 = values_476[0];
{ double end_480 = R.Num((V)3.0d), step_481 = R.Num((V)1.0d);
for (double i_479 = R.Num((V)1.0d); step_481 > 0 ? i_479 <= end_480 : i_479 >= end_480; i_479 += step_481) {
V v_478 = i_479;
var values_482 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d))));
V v_483 = values_482[0];
var values_484 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_467}, new Results(v_483))));
V v_485 = values_484[0];
V v_486 = values_484[1];
V v_487 = values_484[2];
var values_488 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_477}, new Results(v_483))));
V v_489 = values_488[0];
V v_490 = values_488[1];
V v_491 = values_488[2];
R.Call(v_3, R.Join(new V[] {v_487}, new Results(v_491)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_467}, new Results(R.Get(v_470, (V)"id")))).First))}, new Results((V)"frontal corridor enemy had no punish window")));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_467))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_477)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
