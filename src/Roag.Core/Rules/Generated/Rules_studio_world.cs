// Static C# port of src/studio_world.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 4902556d9637937448655ae095bced03c8994be27e04575816ac50de037eb614
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_world { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_project"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rooms"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schema", (V)"roag-studio-world/1"), new Entry((V)"max_rooms", (V)32.0d), new Entry((V)"max_tiles", (V)32.0d))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)" ", (V)true))));
V v_12 = values_11[0];
foreach (var row_13 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"definitions"))))))) {
V v_14 = row_13[0];
var values_15 = R.Join(new V[] {}, new Results((V)true));
var obj_16 = v_12;
V key_17 = v_14;
R.Set(obj_16, key_17, values_15[0]);
}
var values_18 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"combat", (V)true), new Entry((V)"treasure", (V)true), new Entry((V)"landmark", (V)true), new Entry((V)"secret", (V)true), new Entry((V)"start", (V)true), new Entry((V)"elite", (V)true))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"enemy", (V)true), new Entry((V)"chest", (V)true), new Entry((V)"player", (V)true), new Entry((V)"elite", (V)true), new Entry((V)"landmark", (V)true), new Entry((V)"secret_anchor", (V)true), new Entry((V)"secret_wall", (V)true), new Entry((V)"stair", (V)true))));
V v_21 = values_20[0];
V v_22 = V.Nil;
v_22 = new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
V v_25 = args_23[1];
if (R.Truth((V)(!R.Truth(v_24)))) {
R.Call(rt.Global("error"), R.Join(new V[] {v_25}, new Results((V)2.0d)));
}
return Results.Empty;
}));
V v_26 = V.Nil;
v_26 = new V((RuleFunction)(args_27 => {
V v_28 = args_27[0];
V v_29 = args_27[1];
V v_30 = args_27[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_28))).First == (V)"number"), () => (V)((V)(v_28 % (V)1.0d) == (V)0.0d)), () => (V)(v_28 >= v_29)), () => (V)(v_28 <= v_30))));
return Results.Empty;
}));
V v_31 = V.Nil;
v_31 = new V((RuleFunction)(args_32 => {
V v_33 = args_32[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_33))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_33));
}
var values_34 = R.Join(new V[] {}, new Results(R.Table()));
V v_35 = values_34[0];

foreach (var row_36 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_33)))))) {
V v_37 = row_36[0];
V v_38 = row_36[1];
var values_39 = R.Join(new V[] {}, R.Call(v_31, R.Join(new V[] {}, new Results(v_38))));
var obj_40 = v_35;
V key_41 = v_37;
R.Set(obj_40, key_41, values_39[0]);
}

return R.Join(new V[] {}, new Results(v_35));
return Results.Empty;
}));
var values_42 = R.Join(new V[] {}, new Results(v_31));
var obj_43 = v_10;
V key_44 = (V)"clone";
R.Set(obj_43, key_44, values_42[0]);
V v_45 = V.Nil;
v_45 = new V((RuleFunction)(args_46 => {
V v_47 = args_46[0];
V v_48 = args_46[1];
return R.Join(new V[] {}, new Results(R.Concat(v_47, R.Concat((V)",", v_48))));
return Results.Empty;
}));
V v_49 = V.Nil;
v_49 = new V((RuleFunction)(args_50 => {
V v_51 = args_50[0];
var values_52 = R.Join(new V[] {}, new Results(R.Table()));
V v_53 = values_52[0];

foreach (var row_54 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_51)))))) {
V v_55 = row_54[0];
V v_56 = row_54[1];
var values_57 = R.Join(new V[] {}, new Results(v_56));
var obj_58 = v_53;
V key_59 = (V)((V)R.Length(v_53) + (V)1.0d);
R.Set(obj_58, key_59, values_57[0]);
}

R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_53)));

return R.Join(new V[] {}, new Results(v_53));
return Results.Empty;
}));
R.Set(v_10, (V)"new", new V((RuleFunction)(args_60 => {
V v_61 = args_60[0];
R.Call(v_22, R.Join(new V[] {R.Call(R.Get(v_2, (V)"valid_project_id"), R.Join(new V[] {}, new Results(v_61))).First}, new Results((V)"invalid world project ID")));
var values_62 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schema", R.Get(v_10, (V)"schema")), new Entry((V)"project_id", v_61), new Entry((V)"tiles", R.Table(new Entry((V)1, R.Table(new Entry((V)"id", (V)"studio:tile/floor"), new Entry((V)"name", (V)"Floor"), new Entry((V)"symbol", (V)"."), new Entry((V)"asset_id", (V)""), new Entry((V)"blocks_movement", (V)false), new Entry((V)"blocks_sight", (V)false), new Entry((V)"blocks_projectile", (V)false))), new Entry((V)2, R.Table(new Entry((V)"id", (V)"studio:tile/wall"), new Entry((V)"name", (V)"Wall"), new Entry((V)"symbol", (V)"#"), new Entry((V)"asset_id", (V)""), new Entry((V)"blocks_movement", (V)true), new Entry((V)"blocks_sight", (V)true), new Entry((V)"blocks_projectile", (V)true))), new Entry((V)3, R.Table(new Entry((V)"id", (V)"studio:tile/water"), new Entry((V)"name", (V)"Water"), new Entry((V)"symbol", (V)"~"), new Entry((V)"asset_id", (V)""), new Entry((V)"blocks_movement", (V)true), new Entry((V)"blocks_sight", (V)false), new Entry((V)"blocks_projectile", (V)false))))), new Entry((V)"rooms", R.Table()))));
V v_63 = values_62[0];
return R.Join(new V[] {}, new Results(v_63));
return Results.Empty;
})));
R.Set(v_10, (V)"add_terrain_tiles", new V((RuleFunction)(args_64 => {
V v_65 = args_64[0];
foreach (var row_66 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"order"))))))) {
V v_67 = row_66[0];
V v_68 = row_66[1];
var values_69 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(v_68))));
V v_70 = values_69[0];
var values_71 = R.Join(new V[] {}, new Results(R.Concat((V)"studio:tile/", R.Invoke(R.Get(v_70, (V)"id"), "match", R.Join(new V[] {}, new Results((V)"[^/]+$"))).First)));
V v_72 = values_71[0];
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_10, (V)"tile"), R.Join(new V[] {v_65}, new Results(v_72))).First)))) {
var values_73 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_72), new Entry((V)"name", R.Get(v_70, (V)"name")), new Entry((V)"symbol", v_68), new Entry((V)"asset_id", (V)""), new Entry((V)"blocks_movement", (V)(!R.Truth(R.Get(v_70, (V)"walkable")))), new Entry((V)"blocks_sight", R.Get(v_70, (V)"blocks_vision")), new Entry((V)"blocks_projectile", (V)(!R.Truth(R.Get(v_70, (V)"projectile_passable")))))));
var obj_74 = R.Get(v_65, (V)"tiles");
V key_75 = (V)((V)R.Length(R.Get(v_65, (V)"tiles")) + (V)1.0d);
R.Set(obj_74, key_75, values_73[0]);
}
}
return Results.Empty;
})));
R.Set(v_10, (V)"new_room", new V((RuleFunction)(args_76 => {
V v_77 = args_76[0];
V v_78 = args_76[1];
V v_79 = args_76[2];
V v_80 = args_76[3];
R.Call(v_22, R.Join(new V[] {R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(v_77))).First}, new Results((V)"invalid room ID")));
R.Call(v_22, R.Join(new V[] {R.And(R.And(R.And(R.Call(v_26, R.Join(new V[] {v_79, (V)7.0d}, new Results((V)23.0d))).First, () => R.Call(v_26, R.Join(new V[] {v_80, (V)7.0d}, new Results((V)17.0d))).First), () => (V)((V)(v_79 % (V)2.0d) == (V)1.0d)), () => (V)((V)(v_80 % (V)2.0d) == (V)1.0d))}, new Results((V)"room size must be odd, up to 23\u00c3\u009717 tiles")));
var values_81 = R.Join(new V[] {}, new Results(R.Table()));
V v_82 = values_81[0];

{ double end_85 = R.Num(v_80), step_86 = R.Num((V)1.0d);
for (double i_84 = R.Num((V)1.0d); step_86 > 0 ? i_84 <= end_85 : i_84 >= end_85; i_84 += step_86) {
V v_83 = i_84;
var values_87 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Or((V)(v_83 == (V)1.0d), () => (V)(v_83 == v_80)), () => R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {(V)"#"}, new Results(v_79))).First), () => R.Concat((V)"#", R.Concat(R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {(V)"."}, new Results((V)(v_79 - (V)2.0d)))).First, (V)"#")))));
var obj_88 = v_82;
V key_89 = v_83;
R.Set(obj_88, key_89, values_87[0]);
}
}
var values_90 = R.Join(new V[] {(V)((V)(v_79 + (V)1.0d) / (V)2.0d)}, new Results((V)((V)(v_80 + (V)1.0d) / (V)2.0d)));
V v_91 = values_90[0];
V v_92 = values_90[1];
var values_93 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", v_91), new Entry((V)"y", (V)1.0d), new Entry((V)"facing", (V)"N"), new Entry((V)"width", (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)"x", v_79), new Entry((V)"y", v_92), new Entry((V)"facing", (V)"E"), new Entry((V)"width", (V)1.0d))), new Entry((V)3, R.Table(new Entry((V)"x", v_91), new Entry((V)"y", v_80), new Entry((V)"facing", (V)"S"), new Entry((V)"width", (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)"x", (V)1.0d), new Entry((V)"y", v_92), new Entry((V)"facing", (V)"W"), new Entry((V)"width", (V)1.0d))))));
V v_94 = values_93[0];
foreach (var row_95 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_94)))))) {
V v_96 = row_95[0];
V v_97 = row_95[1];
var values_98 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(v_82, R.Get(v_97, (V)"y")), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_97, (V)"x") - (V)1.0d)))).First, R.Concat((V)".", R.Invoke(R.Get(v_82, R.Get(v_97, (V)"y")), "sub", R.Join(new V[] {}, new Results((V)(R.Get(v_97, (V)"x") + (V)1.0d)))).First))));
var obj_99 = v_82;
V key_100 = R.Get(v_97, (V)"y");
R.Set(obj_99, key_100, values_98[0]);
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_77), new Entry((V)"name", R.Or(v_78, () => v_77)), new Entry((V)"width", v_79), new Entry((V)"height", v_80), new Entry((V)"rows", v_82), new Entry((V)"roles", R.Table(new Entry((V)1, (V)"combat"))), new Entry((V)"biomes", R.Table(new Entry((V)1, R.Get(R.Get(v_6, (V)"order"), (V)1.0d)))), new Entry((V)"weight", (V)1.0d), new Entry((V)"sockets", v_94), new Entry((V)"markers", R.Table(new Entry((V)"enemy", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", v_91), new Entry((V)"y", v_92))))), new Entry((V)"chest", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)(v_79 - (V)2.0d)), new Entry((V)"y", (V)(v_80 - (V)2.0d)))))), new Entry((V)"player", R.Table()), new Entry((V)"elite", R.Table()), new Entry((V)"landmark", R.Table()), new Entry((V)"stair", R.Table()), new Entry((V)"secret_anchor", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)2.0d))))), new Entry((V)"secret_wall", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)1.0d), new Entry((V)"y", (V)2.0d))))))), new Entry((V)"editable", R.Table()), new Entry((V)"protected", R.Table()), new Entry((V)"visual_cells", R.Table()), new Entry((V)"tile_cells", R.Table()), new Entry((V)"rotations", R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)2.0d), new Entry((V)4, (V)3.0d))), new Entry((V)"mirror", (V)true))));
return Results.Empty;
})));
R.Set(v_10, (V)"tile", new V((RuleFunction)(args_101 => {
V v_102 = args_101[0];
V v_103 = args_101[1];
foreach (var row_104 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_102, (V)"tiles"))))))) {
V v_105 = row_104[0];
V v_106 = row_104[1];
if (R.Truth((V)(R.Get(v_106, (V)"id") == v_103))) {
return R.Join(new V[] {}, new Results(v_106));
}
}
return Results.Empty;
})));
R.Set(v_10, (V)"set_cell", new V((RuleFunction)(args_107 => {
V v_108 = args_107[0];
V v_109 = args_107[1];
V v_110 = args_107[2];
V v_111 = args_107[3];
R.Call(v_22, R.Join(new V[] {R.And(R.Call(v_26, R.Join(new V[] {v_109, (V)1.0d}, new Results(R.Get(v_108, (V)"width")))).First, () => R.Call(v_26, R.Join(new V[] {v_110, (V)1.0d}, new Results(R.Get(v_108, (V)"height")))).First)}, new Results((V)"cell outside room")));
R.Call(v_22, R.Join(new V[] {R.Get(v_12, v_111)}, new Results((V)"invalid tile symbol")));
var values_112 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_108, (V)"rows"), v_110)));
V v_113 = values_112[0];
if (R.Truth((V)(R.Invoke(v_113, "sub", R.Join(new V[] {v_109}, new Results(v_109))).First == v_111))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_114 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_113, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_109 - (V)1.0d)))).First, R.Concat(v_111, R.Invoke(v_113, "sub", R.Join(new V[] {}, new Results((V)(v_109 + (V)1.0d)))).First))));
var obj_115 = R.Get(v_108, (V)"rows");
V key_116 = v_110;
R.Set(obj_115, key_116, values_114[0]);
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_10, (V)"set_mask", new V((RuleFunction)(args_117 => {
V v_118 = args_117[0];
V v_119 = args_117[1];
V v_120 = args_117[2];
V v_121 = args_117[3];
V v_122 = args_117[4];
R.Call(v_22, R.Join(new V[] {R.Or((V)(v_119 == (V)"editable"), () => (V)(v_119 == (V)"protected"))}, new Results((V)"invalid mask kind")));
R.Call(v_22, R.Join(new V[] {R.And(R.Call(v_26, R.Join(new V[] {v_120, (V)1.0d}, new Results(R.Get(v_118, (V)"width")))).First, () => R.Call(v_26, R.Join(new V[] {v_121, (V)1.0d}, new Results(R.Get(v_118, (V)"height")))).First)}, new Results((V)"mask outside room")));
var values_123 = R.Join(new V[] {}, new Results(R.Get(v_118, v_119)));
V v_124 = values_123[0];
foreach (var row_125 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_124)))))) {
V v_126 = row_125[0];
V v_127 = row_125[1];
if (R.Truth(R.And((V)(R.Get(v_127, (V)"x") == v_120), () => (V)(R.Get(v_127, (V)"y") == v_121)))) {
if (R.Truth(v_122)) {
return R.Join(new V[] {}, new Results((V)false));
}

R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {v_124}, new Results(v_126)));

return R.Join(new V[] {}, new Results((V)true));
}
}
if (R.Truth((V)(!R.Truth(v_122)))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_128 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_120), new Entry((V)"y", v_121))));
var obj_129 = v_124;
V key_130 = (V)((V)R.Length(v_124) + (V)1.0d);
R.Set(obj_129, key_130, values_128[0]);

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_10, (V)"set_visual", new V((RuleFunction)(args_131 => {
V v_132 = args_131[0];
V v_133 = args_131[1];
V v_134 = args_131[2];
V v_135 = args_131[3];
R.Call(v_22, R.Join(new V[] {R.And(R.Call(v_26, R.Join(new V[] {v_133, (V)1.0d}, new Results(R.Get(v_132, (V)"width")))).First, () => R.Call(v_26, R.Join(new V[] {v_134, (V)1.0d}, new Results(R.Get(v_132, (V)"height")))).First)}, new Results((V)"visual outside room")));
R.Call(v_22, R.Join(new V[] {R.Or((V)(v_135 == (V)""), () => R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(v_135))).First)}, new Results((V)"invalid visual asset ID")));
var values_136 = R.Join(new V[] {}, R.Call(v_45, R.Join(new V[] {v_133}, new Results(v_134))));
V v_137 = values_136[0];
if (R.Truth((V)(R.Get(R.Get(v_132, (V)"visual_cells"), v_137) == v_135))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_138 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_135 == (V)""), () => V.Nil), () => v_135)));
var obj_139 = R.Get(v_132, (V)"visual_cells");
V key_140 = v_137;
R.Set(obj_139, key_140, values_138[0]);
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_10, (V)"paint_tile", new V((RuleFunction)(args_141 => {
V v_142 = args_141[0];
V v_143 = args_141[1];
V v_144 = args_141[2];
V v_145 = args_141[3];
V v_146 = args_141[4];
var values_147 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_10, (V)"tile"), R.Join(new V[] {v_142}, new Results(v_146))).First}, new Results((V)"unknown tile type"))));
V v_148 = values_147[0];
var values_149 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"set_cell"), R.Join(new V[] {v_143, v_144, v_145}, new Results(R.Get(v_148, (V)"symbol")))));
V v_150 = values_149[0];
var values_151 = R.Join(new V[] {}, R.Call(v_45, R.Join(new V[] {v_144}, new Results(v_145))));
V v_152 = values_151[0];
if (R.Truth((V)(R.Get(R.Get(v_143, (V)"tile_cells"), v_152) != v_146))) {
var values_153 = R.Join(new V[] {}, new Results(v_146));
var obj_154 = R.Get(v_143, (V)"tile_cells");
V key_155 = v_152;
R.Set(obj_154, key_155, values_153[0]);

var values_156 = R.Join(new V[] {}, new Results((V)true));
v_150 = values_156[0];
}
return R.Join(new V[] {}, new Results(v_150));
return Results.Empty;
})));
R.Set(v_10, (V)"add_marker", new V((RuleFunction)(args_157 => {
V v_158 = args_157[0];
V v_159 = args_157[1];
V v_160 = args_157[2];
V v_161 = args_157[3];
R.Call(v_22, R.Join(new V[] {R.Get(v_21, v_159)}, new Results((V)"invalid marker kind")));
R.Call(v_22, R.Join(new V[] {R.And(R.Call(v_26, R.Join(new V[] {v_160, (V)1.0d}, new Results(R.Get(v_158, (V)"width")))).First, () => R.Call(v_26, R.Join(new V[] {v_161, (V)1.0d}, new Results(R.Get(v_158, (V)"height")))).First)}, new Results((V)"marker outside room")));
var values_162 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_158, (V)"markers"), v_159), () => R.Table())));
V v_163 = values_162[0];
var values_164 = R.Join(new V[] {}, new Results(v_163));
var obj_165 = R.Get(v_158, (V)"markers");
V key_166 = v_159;
R.Set(obj_165, key_166, values_164[0]);
foreach (var row_167 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_163)))))) {
V v_168 = row_167[0];
V v_169 = row_167[1];
if (R.Truth(R.And((V)(R.Get(v_169, (V)"x") == v_160), () => (V)(R.Get(v_169, (V)"y") == v_161)))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {v_163}, new Results(v_168)));

return R.Join(new V[] {}, new Results((V)false));
}
}
if (R.Truth((V)(v_159 == (V)"stair"))) {
var values_170 = R.Join(new V[] {}, new Results(R.Table()));
var obj_171 = R.Get(v_158, (V)"markers");
V key_172 = (V)"stair";
R.Set(obj_171, key_172, values_170[0]);

var values_173 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_158, (V)"markers"), (V)"stair")));
v_163 = values_173[0];
}
var values_174 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_160), new Entry((V)"y", v_161))));
var obj_175 = v_163;
V key_176 = (V)((V)R.Length(v_163) + (V)1.0d);
R.Set(obj_175, key_176, values_174[0]);

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
V v_177 = V.Nil;
v_177 = new V((RuleFunction)(args_178 => {
V v_179 = args_178[0];
V v_180 = args_178[1];
V v_181 = args_178[2];
var values_182 = R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(v_180 >= (V)1.0d), () => (V)(v_181 >= (V)1.0d)), () => (V)(v_180 <= R.Get(v_179, (V)"width"))), () => (V)(v_181 <= R.Get(v_179, (V)"height"))), () => R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, R.Invoke(R.Get(R.Get(v_179, (V)"rows"), v_181), "sub", R.Join(new V[] {v_180}, new Results(v_180))))).First)));
V v_183 = values_182[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(v_183, () => R.Get(v_183, (V)"walkable")), () => (V)false)));
return Results.Empty;
}));
R.Set(v_10, (V)"validate", new V((RuleFunction)(args_184 => {
V v_185 = args_184[0];
V v_186 = args_184[1];
R.Call(v_22, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_185))).First == (V)"table"), () => (V)(R.Get(v_185, (V)"schema") == R.Get(v_10, (V)"schema")))}, new Results((V)"incompatible world schema")));
R.Call(v_22, R.Join(new V[] {R.Call(R.Get(v_2, (V)"valid_project_id"), R.Join(new V[] {}, new Results(R.Get(v_185, (V)"project_id")))).First}, new Results((V)"invalid world project ID")));
R.Call(v_22, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_185, (V)"tiles")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_185, (V)"tiles")) >= (V)3.0d)), () => (V)((V)R.Length(R.Get(v_185, (V)"tiles")) <= R.Get(v_10, (V)"max_tiles")))}, new Results((V)"invalid tile catalogue")));
var values_187 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_188 = values_187[0];
V v_189 = values_187[1];
foreach (var row_190 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_186, () => R.Table()))))))) {
V v_191 = row_190[0];
V v_192 = row_190[1];
var values_193 = R.Join(new V[] {}, new Results((V)true));
var obj_194 = v_189;
V key_195 = R.Get(v_192, (V)"id");
R.Set(obj_194, key_195, values_193[0]);
}
foreach (var row_196 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_185, (V)"tiles"))))))) {
V v_197 = row_196[0];
V v_198 = row_196[1];
R.Call(v_22, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_198))).First == (V)"table"), () => R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(R.Get(v_198, (V)"id")))).First), () => (V)(!R.Truth(R.Get(v_188, R.Get(v_198, (V)"id")))))}, new Results((V)"duplicate or invalid tile ID")));
var values_199 = R.Join(new V[] {}, new Results((V)true));
var obj_200 = v_188;
V key_201 = R.Get(v_198, (V)"id");
R.Set(obj_200, key_201, values_199[0]);
R.Call(v_22, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_198, (V)"name")))).First == (V)"string"), () => (V)((V)R.Length(R.Get(v_198, (V)"name")) > (V)0.0d)), () => (V)((V)R.Length(R.Get(v_198, (V)"name")) <= (V)80.0d)), () => (V)(!R.Truth(R.Invoke(R.Get(v_198, (V)"name"), "find", R.Join(new V[] {}, new Results((V)"[%c]"))).First)))}, new Results(R.Concat((V)"invalid tile name: ", R.Get(v_198, (V)"id")))));
R.Call(v_22, R.Join(new V[] {R.And(R.Get(v_12, R.Get(v_198, (V)"symbol")), () => (V)(R.Get(v_198, (V)"symbol") != (V)" "))}, new Results(R.Concat((V)"invalid tile symbol: ", R.Get(v_198, (V)"id")))));
R.Call(v_22, R.Join(new V[] {R.Or((V)(R.Get(v_198, (V)"asset_id") == (V)""), () => R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(R.Get(v_198, (V)"asset_id")))).First)}, new Results(R.Concat((V)"invalid tile visual ID: ", R.Get(v_198, (V)"id")))));
if (R.Truth(R.And(v_186, () => (V)(R.Get(v_198, (V)"asset_id") != (V)"")))) {
R.Call(v_22, R.Join(new V[] {R.Get(v_189, R.Get(v_198, (V)"asset_id"))}, new Results(R.Concat((V)"missing tile visual asset: ", R.Get(v_198, (V)"asset_id")))));
}
foreach (var row_202 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"blocks_movement"), new Entry((V)2, (V)"blocks_sight"), new Entry((V)3, (V)"blocks_projectile")))))))) {
V v_203 = row_202[0];
V v_204 = row_202[1];
R.Call(v_22, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_198, v_204)))).First == (V)"boolean")}, new Results(R.Concat((V)"invalid ", R.Concat(v_204, R.Concat((V)": ", R.Get(v_198, (V)"id")))))));
}
var values_205 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_198, (V)"symbol")))));
V v_206 = values_205[0];
var values_207 = R.Join(new V[] {(V)(!R.Truth(R.Get(v_206, (V)"walkable"))), R.Get(v_206, (V)"blocks_vision")}, new Results((V)(!R.Truth(R.Get(v_206, (V)"projectile_passable")))));
V v_208 = values_207[0];
V v_209 = values_207[1];
V v_210 = values_207[2];
R.Call(v_22, R.Join(new V[] {R.And(R.And((V)(R.Get(v_198, (V)"blocks_movement") == v_208), () => (V)(R.Get(v_198, (V)"blocks_sight") == v_209)), () => (V)(R.Get(v_198, (V)"blocks_projectile") == v_210))}, new Results(R.Concat((V)"tile properties disagree with runtime symbol: ", R.Get(v_198, (V)"id")))));
}
R.Call(v_22, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_185, (V)"rooms")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_185, (V)"rooms")) <= R.Get(v_10, (V)"max_rooms")))}, new Results((V)"invalid room list")));
var values_211 = R.Join(new V[] {}, new Results(R.Table()));
V v_212 = values_211[0];
foreach (var row_213 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_185, (V)"rooms"))))))) {
V v_214 = row_213[0];
V v_215 = row_213[1];
var values_216 = R.Join(new V[] {}, new Results(R.Get(v_215, (V)"id")));
V v_217 = values_216[0];
R.Call(v_22, R.Join(new V[] {R.And(R.And(R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(v_217))).First, () => (V)(!R.Truth(R.Get(v_212, v_217)))), () => (V)(!R.Truth(R.Get(R.Get(v_4, (V)"definitions"), v_217))))}, new Results(R.Concat((V)"duplicate or invalid room ID: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_217))).First))));
var values_218 = R.Join(new V[] {}, new Results((V)true));
var obj_219 = v_212;
V key_220 = v_217;
R.Set(obj_219, key_220, values_218[0]);
R.Call(v_22, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"name")))).First == (V)"string"), () => (V)((V)R.Length(R.Get(v_215, (V)"name")) > (V)0.0d)), () => (V)((V)R.Length(R.Get(v_215, (V)"name")) <= (V)80.0d)), () => (V)(!R.Truth(R.Invoke(R.Get(v_215, (V)"name"), "find", R.Join(new V[] {}, new Results((V)"[%c]"))).First)))}, new Results(R.Concat((V)"invalid room name: ", v_217))));
R.Call(v_22, R.Join(new V[] {R.And(R.And(R.And(R.Call(v_26, R.Join(new V[] {R.Get(v_215, (V)"width"), (V)7.0d}, new Results((V)23.0d))).First, () => R.Call(v_26, R.Join(new V[] {R.Get(v_215, (V)"height"), (V)7.0d}, new Results((V)17.0d))).First), () => (V)((V)(R.Get(v_215, (V)"width") % (V)2.0d) == (V)1.0d)), () => (V)((V)(R.Get(v_215, (V)"height") % (V)2.0d) == (V)1.0d))}, new Results(R.Concat((V)"invalid dimensions: ", v_217))));
R.Call(v_22, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"rows")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_215, (V)"rows")) == R.Get(v_215, (V)"height")))}, new Results(R.Concat((V)"invalid rows: ", v_217))));
foreach (var row_221 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"rows"))))))) {
V v_222 = row_221[0];
V v_223 = row_221[1];
R.Call(v_22, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_223))).First == (V)"string"), () => (V)((V)R.Length(v_223) == R.Get(v_215, (V)"width")))}, new Results(R.Concat((V)"invalid row ", R.Concat(v_222, R.Concat((V)": ", v_217))))));
{ double end_226 = R.Num((V)R.Length(v_223)), step_227 = R.Num((V)1.0d);
for (double i_225 = R.Num((V)1.0d); step_227 > 0 ? i_225 <= end_226 : i_225 >= end_226; i_225 += step_227) {
V v_224 = i_225;
R.Call(v_22, R.Join(new V[] {R.Get(v_12, R.Invoke(v_223, "sub", R.Join(new V[] {v_224}, new Results(v_224))).First)}, new Results(R.Concat((V)"invalid row ", R.Concat(v_222, R.Concat((V)" cell ", R.Concat(v_224, R.Concat((V)": ", v_217))))))));
}
}
}
R.Call(v_22, R.Join(new V[] {R.Call(v_177, R.Join(new V[] {v_215, (V)((V)(R.Get(v_215, (V)"width") + (V)1.0d) / (V)2.0d)}, new Results((V)((V)(R.Get(v_215, (V)"height") + (V)1.0d) / (V)2.0d)))).First}, new Results(R.Concat((V)"room center must be walkable: ", v_217))));
R.Call(v_22, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"roles")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_215, (V)"roles")) > (V)0.0d)), () => (V)((V)R.Length(R.Get(v_215, (V)"roles")) <= (V)6.0d))}, new Results(R.Concat((V)"missing role: ", v_217))));
var values_228 = R.Join(new V[] {}, new Results(R.Table()));
V v_229 = values_228[0];
foreach (var row_230 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"roles"))))))) {
V v_231 = row_230[0];
V v_232 = row_230[1];
R.Call(v_22, R.Join(new V[] {R.And(R.Get(v_19, v_232), () => (V)(!R.Truth(R.Get(v_229, v_232))))}, new Results(R.Concat((V)"invalid role: ", v_217))));

var values_233 = R.Join(new V[] {}, new Results((V)true));
var obj_234 = v_229;
V key_235 = v_232;
R.Set(obj_234, key_235, values_233[0]);
}
R.Call(v_22, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"biomes")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_215, (V)"biomes")) > (V)0.0d)), () => (V)((V)R.Length(R.Get(v_215, (V)"biomes")) <= (V)R.Length(R.Get(v_6, (V)"order"))))}, new Results(R.Concat((V)"missing biome: ", v_217))));
var values_236 = R.Join(new V[] {}, new Results(v_229));
V v_237 = values_236[0];
var values_238 = R.Join(new V[] {(V)((V)(R.Get(v_215, (V)"width") + (V)1.0d) / (V)2.0d)}, new Results((V)((V)(R.Get(v_215, (V)"height") + (V)1.0d) / (V)2.0d)));
V v_239 = values_238[0];
V v_240 = values_238[1];
var values_241 = R.Join(new V[] {}, new Results((V)0.0d));
V v_242 = values_241[0];
foreach (var row_243 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_244 = row_243[0];
V v_245 = row_243[1];
if (R.Truth(R.Call(v_177, R.Join(new V[] {v_215, (V)(v_239 + R.Get(v_245, (V)1.0d))}, new Results((V)(v_240 + R.Get(v_245, (V)2.0d))))).First)) {
var values_246 = R.Join(new V[] {}, new Results((V)(v_242 + (V)1.0d)));
v_242 = values_246[0];
}
}
R.Call(v_22, R.Join(new V[] {(V)(v_242 >= R.Or(R.And(R.Or(R.Get(v_237, (V)"start"), () => R.Get(v_237, (V)"elite")), () => (V)4.0d), () => (V)2.0d))}, new Results(R.Concat((V)"center combat clearance too narrow: ", v_217))));
var values_247 = R.Join(new V[] {}, new Results(R.Table()));
v_229 = values_247[0];
foreach (var row_248 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"biomes"))))))) {
V v_249 = row_248[0];
V v_250 = row_248[1];
R.Call(v_22, R.Join(new V[] {R.And(R.Call(R.Get(v_6, (V)"get"), R.Join(new V[] {}, new Results(v_250))).First, () => (V)(!R.Truth(R.Get(v_229, v_250))))}, new Results(R.Concat((V)"invalid biome: ", v_217))));

var values_251 = R.Join(new V[] {}, new Results((V)true));
var obj_252 = v_229;
V key_253 = v_250;
R.Set(obj_252, key_253, values_251[0]);
}
R.Call(v_22, R.Join(new V[] {R.Call(v_26, R.Join(new V[] {R.Get(v_215, (V)"weight"), (V)1.0d}, new Results((V)20.0d))).First}, new Results(R.Concat((V)"invalid weight: ", v_217))));
R.Call(v_22, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"rotations")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_215, (V)"rotations")) > (V)0.0d)), () => (V)((V)R.Length(R.Get(v_215, (V)"rotations")) <= (V)4.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"mirror")))).First == (V)"boolean"))}, new Results(R.Concat((V)"invalid transforms: ", v_217))));
var values_254 = R.Join(new V[] {}, new Results(R.Table()));
v_229 = values_254[0];

foreach (var row_255 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"rotations"))))))) {
V v_256 = row_255[0];
V v_257 = row_255[1];
R.Call(v_22, R.Join(new V[] {R.And(R.Call(v_26, R.Join(new V[] {v_257, (V)0.0d}, new Results((V)3.0d))).First, () => (V)(!R.Truth(R.Get(v_229, v_257))))}, new Results(R.Concat((V)"invalid rotation: ", v_217))));

var values_258 = R.Join(new V[] {}, new Results((V)true));
var obj_259 = v_229;
V key_260 = v_257;
R.Set(obj_259, key_260, values_258[0]);
}
R.Call(v_22, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"sockets")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_215, (V)"sockets")) == (V)4.0d))}, new Results(R.Concat((V)"room needs four sockets: ", v_217))));
var values_261 = R.Join(new V[] {}, new Results(R.Table()));
v_229 = values_261[0];

var values_262 = R.Join(new V[] {}, new Results(R.Table()));
V v_263 = values_262[0];
foreach (var row_264 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"sockets"))))))) {
V v_265 = row_264[0];
V v_266 = row_264[1];
R.Call(v_22, R.Join(new V[] {R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_266))).First == (V)"table"), () => R.Get(R.Table(new Entry((V)"N", (V)true), new Entry((V)"E", (V)true), new Entry((V)"S", (V)true), new Entry((V)"W", (V)true)), R.Get(v_266, (V)"facing"))), () => (V)(!R.Truth(R.Get(v_229, R.Get(v_266, (V)"facing"))))), () => (V)(R.Get(v_266, (V)"width") == (V)1.0d)), () => R.Call(v_26, R.Join(new V[] {R.Get(v_266, (V)"x"), (V)1.0d}, new Results(R.Get(v_215, (V)"width")))).First), () => R.Call(v_26, R.Join(new V[] {R.Get(v_266, (V)"y"), (V)1.0d}, new Results(R.Get(v_215, (V)"height")))).First)}, new Results(R.Concat((V)"invalid socket: ", v_217))));
var values_267 = R.Join(new V[] {}, new Results((V)true));
var obj_268 = v_229;
V key_269 = R.Get(v_266, (V)"facing");
R.Set(obj_268, key_269, values_267[0]);

var values_270 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_266, (V)"x")), new Entry((V)"y", R.Get(v_266, (V)"y")))));
var obj_271 = v_263;
V key_272 = (V)((V)R.Length(v_263) + (V)1.0d);
R.Set(obj_271, key_272, values_270[0]);
R.Call(v_22, R.Join(new V[] {R.And(R.Or(R.Or(R.Or(R.And((V)(R.Get(v_266, (V)"facing") == (V)"N"), () => (V)(R.Get(v_266, (V)"y") == (V)1.0d)), () => R.And((V)(R.Get(v_266, (V)"facing") == (V)"S"), () => (V)(R.Get(v_266, (V)"y") == R.Get(v_215, (V)"height")))), () => R.And((V)(R.Get(v_266, (V)"facing") == (V)"E"), () => (V)(R.Get(v_266, (V)"x") == R.Get(v_215, (V)"width")))), () => R.And((V)(R.Get(v_266, (V)"facing") == (V)"W"), () => (V)(R.Get(v_266, (V)"x") == (V)1.0d))), () => R.Call(v_177, R.Join(new V[] {v_215, R.Get(v_266, (V)"x")}, new Results(R.Get(v_266, (V)"y")))).First)}, new Results(R.Concat((V)"socket must open at boundary: ", v_217))));
var values_273 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(R.Get(v_266, (V)"facing") == (V)"E"), () => (-(V)1.0d)), () => R.And((V)(R.Get(v_266, (V)"facing") == (V)"W"), () => (V)1.0d)), () => (V)0.0d)));
V v_274 = values_273[0];
var values_275 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(R.Get(v_266, (V)"facing") == (V)"N"), () => (V)1.0d), () => R.And((V)(R.Get(v_266, (V)"facing") == (V)"S"), () => (-(V)1.0d))), () => (V)0.0d)));
V v_276 = values_275[0];
R.Call(v_22, R.Join(new V[] {R.Call(v_177, R.Join(new V[] {v_215, (V)(R.Get(v_266, (V)"x") + v_274)}, new Results((V)(R.Get(v_266, (V)"y") + v_276)))).First}, new Results(R.Concat((V)"socket lacks inner clearance: ", v_217))));
}
R.Call(v_22, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"markers")))).First == (V)"table")}, new Results(R.Concat((V)"invalid markers: ", v_217))));
R.Call(v_22, R.Join(new V[] {R.Or((V)(!R.Truth(R.Get(R.Get(v_215, (V)"markers"), (V)"stair"))), () => (V)((V)R.Length(R.Get(R.Get(v_215, (V)"markers"), (V)"stair")) <= (V)1.0d))}, new Results(R.Concat((V)"room may have only one stair marker: ", v_217))));
R.Call(v_22, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_215, (V)"markers"), (V)"secret_anchor")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(R.Get(v_215, (V)"markers"), (V)"secret_anchor")) > (V)0.0d))}, new Results(R.Concat((V)"room needs future secret anchor: ", v_217))));
foreach (var row_277 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"markers"))))))) {
V v_278 = row_277[0];
V v_279 = row_277[1];
R.Call(v_22, R.Join(new V[] {R.And(R.Get(v_21, v_278), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_279))).First == (V)"table"))}, new Results(R.Concat((V)"invalid marker kind: ", v_217))));
foreach (var row_280 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_279)))))) {
V v_281 = row_280[0];
V v_282 = row_280[1];
R.Call(v_22, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_282))).First == (V)"table"), () => R.Call(v_26, R.Join(new V[] {R.Get(v_282, (V)"x"), (V)1.0d}, new Results(R.Get(v_215, (V)"width")))).First), () => R.Call(v_26, R.Join(new V[] {R.Get(v_282, (V)"y"), (V)1.0d}, new Results(R.Get(v_215, (V)"height")))).First), () => (V)(R.Invoke(R.Get(R.Get(v_215, (V)"rows"), R.Get(v_282, (V)"y")), "sub", R.Join(new V[] {R.Get(v_282, (V)"x")}, new Results(R.Get(v_282, (V)"x")))).First == R.Or(R.And((V)(v_278 == (V)"secret_wall"), () => (V)"#"), () => (V)".")))}, new Results(R.Concat((V)"invalid ", R.Concat(v_278, R.Concat((V)" marker at ", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_282, (V)"x")))).First, R.Concat((V)",", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_282, (V)"y")))).First, R.Concat((V)": ", v_217))))))))));
if (R.Truth(R.Or(R.Or(R.Or((V)(v_278 == (V)"player"), () => (V)(v_278 == (V)"elite")), () => (V)(v_278 == (V)"chest")), () => (V)(v_278 == (V)"stair")))) {
var values_283 = R.Join(new V[] {}, new Results(v_282));
var obj_284 = v_263;
V key_285 = (V)((V)R.Length(v_263) + (V)1.0d);
R.Set(obj_284, key_285, values_283[0]);
}
}
}
foreach (var row_286 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"start", (V)"player"), new Entry((V)"combat", (V)"enemy"), new Entry((V)"treasure", (V)"chest"), new Entry((V)"landmark", (V)"landmark"), new Entry((V)"elite", (V)"elite")))))))) {
V v_287 = row_286[0];
V v_288 = row_286[1];
if (R.Truth(R.Get(v_237, v_287))) {
R.Call(v_22, R.Join(new V[] {(V)((V)R.Length(R.Or(R.Get(R.Get(v_215, (V)"markers"), v_288), () => R.Table())) > (V)0.0d)}, new Results(R.Concat((V)"role ", R.Concat(v_287, R.Concat((V)" needs ", R.Concat(v_288, R.Concat((V)" marker: ", v_217))))))));
}
}
var values_289 = R.Join(new V[] {}, new Results(R.Table()));
V v_290 = values_289[0];

foreach (var row_291 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_215, (V)"editable")), new Entry((V)2, R.Get(v_215, (V)"protected"))))))))) {
V v_292 = row_291[0];
V v_293 = row_291[1];
R.Call(v_22, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_293))).First == (V)"table")}, new Results(R.Concat((V)"invalid masks: ", v_217))));
foreach (var row_294 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_293)))))) {
V v_295 = row_294[0];
V v_296 = row_294[1];
R.Call(v_22, R.Join(new V[] {R.And(R.Call(v_26, R.Join(new V[] {R.Get(v_296, (V)"x"), (V)1.0d}, new Results(R.Get(v_215, (V)"width")))).First, () => R.Call(v_26, R.Join(new V[] {R.Get(v_296, (V)"y"), (V)1.0d}, new Results(R.Get(v_215, (V)"height")))).First)}, new Results(R.Concat((V)"mask outside room: ", v_217))));
R.Call(v_22, R.Join(new V[] {(V)(R.Invoke(R.Get(R.Get(v_215, (V)"rows"), R.Get(v_296, (V)"y")), "sub", R.Join(new V[] {R.Get(v_296, (V)"x")}, new Results(R.Get(v_296, (V)"x")))).First != (V)" ")}, new Results(R.Concat((V)"mask on void cell: ", v_217))));
var values_297 = R.Join(new V[] {}, R.Call(v_45, R.Join(new V[] {R.Get(v_296, (V)"x")}, new Results(R.Get(v_296, (V)"y")))));
V v_298 = values_297[0];

R.Call(v_22, R.Join(new V[] {(V)(!R.Truth(R.Get(v_290, v_298)))}, new Results(R.Concat((V)"overlapping masks: ", v_217))));
var values_299 = R.Join(new V[] {}, new Results((V)true));
var obj_300 = v_290;
V key_301 = v_298;
R.Set(obj_300, key_301, values_299[0]);
}
}
foreach (var row_302 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"sockets"))))))) {
V v_303 = row_302[0];
V v_304 = row_302[1];
var values_305 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(R.Get(v_304, (V)"facing") == (V)"E"), () => (-(V)1.0d)), () => R.And((V)(R.Get(v_304, (V)"facing") == (V)"W"), () => (V)1.0d)), () => (V)0.0d)));
V v_306 = values_305[0];
var values_307 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(R.Get(v_304, (V)"facing") == (V)"N"), () => (V)1.0d), () => R.And((V)(R.Get(v_304, (V)"facing") == (V)"S"), () => (-(V)1.0d))), () => (V)0.0d)));
V v_308 = values_307[0];
foreach (var row_309 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"editable"))))))) {
V v_310 = row_309[0];
V v_311 = row_309[1];
R.Call(v_22, R.Join(new V[] {R.And((V)(!R.Truth(R.And((V)(R.Get(v_311, (V)"x") == R.Get(v_304, (V)"x")), () => (V)(R.Get(v_311, (V)"y") == R.Get(v_304, (V)"y"))))), () => (V)(!R.Truth(R.And((V)(R.Get(v_311, (V)"x") == (V)(R.Get(v_304, (V)"x") + v_306)), () => (V)(R.Get(v_311, (V)"y") == (V)(R.Get(v_304, (V)"y") + v_308))))))}, new Results(R.Concat((V)"editable socket approach: ", v_217))));
}
}
foreach (var row_312 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"markers"))))))) {
V v_313 = row_312[0];
V v_314 = row_312[1];
foreach (var row_315 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_314)))))) {
V v_316 = row_315[0];
V v_317 = row_315[1];
foreach (var row_318 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"editable"))))))) {
V v_319 = row_318[0];
V v_320 = row_318[1];
R.Call(v_22, R.Join(new V[] {R.Or((V)(R.Get(v_320, (V)"x") != R.Get(v_317, (V)"x")), () => (V)(R.Get(v_320, (V)"y") != R.Get(v_317, (V)"y")))}, new Results(R.Concat((V)"editable critical marker: ", v_217))));
}
}
}
if (R.Truth(R.Or(R.Get(v_237, (V)"start"), () => R.Get(v_237, (V)"elite")))) {
foreach (var row_321 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_322 = row_321[0];
V v_323 = row_321[1];
foreach (var row_324 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"editable"))))))) {
V v_325 = row_324[0];
V v_326 = row_324[1];
R.Call(v_22, R.Join(new V[] {R.Or((V)(R.Get(v_326, (V)"x") != (V)(v_239 + R.Get(v_323, (V)1.0d))), () => (V)(R.Get(v_326, (V)"y") != (V)(v_240 + R.Get(v_323, (V)2.0d))))}, new Results(R.Concat((V)"editable critical center clearance: ", v_217))));
}
}
}
var values_327 = R.Join(new V[] {}, new Results(R.Get(v_263, (V)1.0d)));
V v_328 = values_327[0];
var values_329 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_328, (V)"x")), new Entry((V)"y", R.Get(v_328, (V)"y")))))));
V v_330 = values_329[0];
var values_331 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Call(v_45, R.Join(new V[] {R.Get(v_328, (V)"x")}, new Results(R.Get(v_328, (V)"y")))).First, (V)true))));
V v_332 = values_331[0];
var values_333 = R.Join(new V[] {}, new Results((V)1.0d));
V v_334 = values_333[0];
while (R.Truth((V)(v_334 <= (V)R.Length(v_330)))) {
var values_335 = R.Join(new V[] {}, new Results(R.Get(v_330, v_334)));
V v_336 = values_335[0];
foreach (var row_337 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_338 = row_337[0];
V v_339 = row_337[1];
var values_340 = R.Join(new V[] {(V)(R.Get(v_336, (V)"x") + R.Get(v_339, (V)1.0d))}, new Results((V)(R.Get(v_336, (V)"y") + R.Get(v_339, (V)2.0d))));
V v_341 = values_340[0];
V v_342 = values_340[1];

var values_343 = R.Join(new V[] {}, R.Call(v_45, R.Join(new V[] {v_341}, new Results(v_342))));
V v_344 = values_343[0];
if (R.Truth(R.And(R.Call(v_177, R.Join(new V[] {v_215, v_341}, new Results(v_342))).First, () => (V)(!R.Truth(R.Get(v_332, v_344)))))) {
var values_345 = R.Join(new V[] {}, new Results((V)true));
var obj_346 = v_332;
V key_347 = v_344;
R.Set(obj_346, key_347, values_345[0]);

var values_348 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_341), new Entry((V)"y", v_342))));
var obj_349 = v_330;
V key_350 = (V)((V)R.Length(v_330) + (V)1.0d);
R.Set(obj_349, key_350, values_348[0]);
}
}
var values_351 = R.Join(new V[] {}, new Results((V)(v_334 + (V)1.0d)));
v_334 = values_351[0];
}
foreach (var row_352 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_263)))))) {
V v_353 = row_352[0];
V v_354 = row_352[1];
R.Call(v_22, R.Join(new V[] {R.Get(v_332, R.Call(v_45, R.Join(new V[] {R.Get(v_354, (V)"x")}, new Results(R.Get(v_354, (V)"y")))).First)}, new Results(R.Concat((V)"disconnected socket or anchor at ", R.Concat(R.Get(v_354, (V)"x"), R.Concat((V)",", R.Concat(R.Get(v_354, (V)"y"), R.Concat((V)": ", v_217))))))));
}
var values_355 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"runtime_room"), R.Join(new V[] {}, new Results(v_215))));
V v_356 = values_355[0];
var values_357 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"get"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_215, (V)"biomes"), (V)1.0d)))));
V v_358 = values_357[0];
var values_359 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_358, (V)"id")), new Entry((V)"floor_id", R.Get(v_358, (V)"floor_id")), new Entry((V)"width", (V)140.0d), new Entry((V)"height", (V)90.0d), new Entry((V)"room_min", (V)8.0d), new Entry((V)"room_max", (V)12.0d), new Entry((V)"standard_chests", (V)5.0d), new Entry((V)"retry_limit", (V)2.0d), new Entry((V)"enemy_ids", R.Get(v_358, (V)"enemy_ids")))));
V v_360 = values_359[0];
var values_361 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_4, (V)"validate"), R.Table(new Entry(v_217, v_356)), R.Table(new Entry((V)1, v_217))}, new Results(v_360))));
V v_362 = values_361[0];
V v_363 = values_361[1];
R.Call(v_22, R.Join(new V[] {v_362}, new Results(R.Concat((V)"generator room contract: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_363))).First))));
R.Call(v_22, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"visual_cells")))).First == (V)"table")}, new Results(R.Concat((V)"invalid visual cells: ", v_217))));
foreach (var row_364 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"visual_cells"))))))) {
V v_365 = row_364[0];
V v_366 = row_364[1];
var values_367 = R.Join(new V[] {}, R.Invoke(v_365, "match", R.Join(new V[] {}, new Results((V)"^(%d+),(%d+)$"))));
V v_368 = values_367[0];
V v_369 = values_367[1];

var values_370 = R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_368))));
v_368 = values_370[0];

var values_371 = R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_369))));
v_369 = values_371[0];
R.Call(v_22, R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(v_368, () => v_369), () => R.Call(v_26, R.Join(new V[] {v_368, (V)1.0d}, new Results(R.Get(v_215, (V)"width")))).First), () => R.Call(v_26, R.Join(new V[] {v_369, (V)1.0d}, new Results(R.Get(v_215, (V)"height")))).First), () => (V)(R.Invoke(R.Get(R.Get(v_215, (V)"rows"), v_369), "sub", R.Join(new V[] {v_368}, new Results(v_368))).First != (V)" ")), () => R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(v_366))).First)}, new Results(R.Concat((V)"invalid visual cell: ", v_217))));
if (R.Truth(v_186)) {
R.Call(v_22, R.Join(new V[] {R.Get(v_189, v_366)}, new Results(R.Concat((V)"missing room visual asset: ", v_366))));
}
}
R.Call(v_22, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"tile_cells")))).First == (V)"table")}, new Results(R.Concat((V)"invalid tile cells: ", v_217))));
foreach (var row_372 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"tile_cells"))))))) {
V v_373 = row_372[0];
V v_374 = row_372[1];
var values_375 = R.Join(new V[] {}, R.Invoke(v_373, "match", R.Join(new V[] {}, new Results((V)"^(%d+),(%d+)$"))));
V v_376 = values_375[0];
V v_377 = values_375[1];

var values_378 = R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_376))));
v_376 = values_378[0];

var values_379 = R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_377))));
v_377 = values_379[0];
var values_380 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"tile"), R.Join(new V[] {v_185}, new Results(v_374))));
V v_381 = values_380[0];
R.Call(v_22, R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(v_376, () => v_377), () => R.Call(v_26, R.Join(new V[] {v_376, (V)1.0d}, new Results(R.Get(v_215, (V)"width")))).First), () => R.Call(v_26, R.Join(new V[] {v_377, (V)1.0d}, new Results(R.Get(v_215, (V)"height")))).First), () => v_381), () => (V)(R.Invoke(R.Get(R.Get(v_215, (V)"rows"), v_377), "sub", R.Join(new V[] {v_376}, new Results(v_376))).First == R.Get(v_381, (V)"symbol")))}, new Results(R.Concat((V)"invalid tile cell: ", R.Concat(v_217, R.Concat((V)" ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_373))).First))))));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_10, (V)"runtime_room", new V((RuleFunction)(args_382 => {
V v_383 = args_382[0];
V v_384 = args_382[1];
var values_385 = R.Join(new V[] {}, R.Call(v_31, R.Join(new V[] {}, new Results(R.Get(v_383, (V)"visual_cells")))));
V v_386 = values_385[0];
if (R.Truth(v_384)) {
foreach (var row_387 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_383, (V)"tile_cells"))))))) {
V v_388 = row_387[0];
V v_389 = row_387[1];
var values_390 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"tile"), R.Join(new V[] {v_384}, new Results(v_389))));
V v_391 = values_390[0];
if (R.Truth(R.And(R.And(v_391, () => (V)(R.Get(v_391, (V)"asset_id") != (V)"")), () => (V)(!R.Truth(R.Get(v_386, v_388)))))) {
var values_392 = R.Join(new V[] {}, new Results(R.Get(v_391, (V)"asset_id")));
var obj_393 = v_386;
V key_394 = v_388;
R.Set(obj_393, key_394, values_392[0]);
}
}
}
var values_395 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_383, (V)"id")), new Entry((V)"roles", R.Call(v_31, R.Join(new V[] {}, new Results(R.Get(v_383, (V)"roles")))).First), new Entry((V)"rows", R.Call(v_31, R.Join(new V[] {}, new Results(R.Get(v_383, (V)"rows")))).First), new Entry((V)"sockets", R.Call(v_31, R.Join(new V[] {}, new Results(R.Get(v_383, (V)"sockets")))).First), new Entry((V)"markers", R.Call(v_31, R.Join(new V[] {}, new Results(R.Get(v_383, (V)"markers")))).First), new Entry((V)"editable", R.Call(v_31, R.Join(new V[] {}, new Results(R.Get(v_383, (V)"editable")))).First), new Entry((V)"rotations", R.Call(v_31, R.Join(new V[] {}, new Results(R.Get(v_383, (V)"rotations")))).First), new Entry((V)"mirror", R.Get(v_383, (V)"mirror")), new Entry((V)"protected_default", (V)true), new Entry((V)"studio_visual_cells", v_386), new Entry((V)"studio_protected", R.Call(v_31, R.Join(new V[] {}, new Results(R.Get(v_383, (V)"protected")))).First), new Entry((V)"studio_weight", R.Get(v_383, (V)"weight")), new Entry((V)"studio_biomes", R.Call(v_31, R.Join(new V[] {}, new Results(R.Get(v_383, (V)"biomes")))).First))));
V v_396 = values_395[0];
return R.Join(new V[] {}, new Results(v_396));
return Results.Empty;
})));
V v_397 = V.Nil;
v_397 = new V((RuleFunction)(args_398 => {
V v_399 = args_398[0];
return R.Join(new V[] {}, R.Invoke(R.Invoke(R.Invoke(R.Invoke(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_399))).First, "gsub", R.Join(new V[] {(V)"%%"}, new Results((V)"%%25"))).First, "gsub", R.Join(new V[] {(V)"\t"}, new Results((V)"%%09"))).First, "gsub", R.Join(new V[] {(V)"\n"}, new Results((V)"%%0A"))).First, "gsub", R.Join(new V[] {(V)"\r"}, new Results((V)"%%0D"))));
return Results.Empty;
}));
V v_400 = V.Nil;
v_400 = new V((RuleFunction)(args_401 => {
V v_402 = args_401[0];
R.Call(v_22, R.Join(new V[] {R.And((V)(!R.Truth(R.Invoke(v_402, "find", R.Join(new V[] {}, new Results((V)"%%[^0-9A-F]"))).First)), () => (V)(!R.Truth(R.Invoke(v_402, "find", R.Join(new V[] {}, new Results((V)"%%[0-9A-F][^0-9A-F]"))).First)))}, new Results((V)"invalid world escape")));
return R.Join(new V[] {}, new Results(R.Invoke(v_402, "gsub", R.Join(new V[] {(V)"%%(%x%x)"}, new Results(new V((RuleFunction)(args_403 => {
V v_404 = args_403[0];
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"char"), R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {v_404}, new Results((V)16.0d))))));
return Results.Empty;
}))))).First));
return Results.Empty;
}));
V v_405 = V.Nil;
v_405 = new V((RuleFunction)(args_406 => {
V v_407 = args_406[0];
var values_408 = R.Join(new V[] {}, new Results(R.Table()));
V v_409 = values_408[0];

foreach (var row_410 in R.Iterate(R.Join(new V[] {}, R.Invoke(R.Concat(v_407, (V)"\t"), "gmatch", R.Join(new V[] {}, new Results((V)"(.-)\t")))))) {
V v_411 = row_410[0];
var values_412 = R.Join(new V[] {}, R.Call(v_400, R.Join(new V[] {}, new Results(v_411))));
var obj_413 = v_409;
V key_414 = (V)((V)R.Length(v_409) + (V)1.0d);
R.Set(obj_413, key_414, values_412[0]);
}

return R.Join(new V[] {}, new Results(v_409));
return Results.Empty;
}));
V v_415 = V.Nil;
v_415 = new V((RuleFunction)(args_416 => {
V v_417 = args_416[0];
var values_418 = R.Join(new V[] {}, new Results(R.Table(new Entry(1, R.Tail(args_416, 1)))));
V v_419 = values_418[0];

{ double end_422 = R.Num((V)R.Length(v_419)), step_423 = R.Num((V)1.0d);
for (double i_421 = R.Num((V)1.0d); step_423 > 0 ? i_421 <= end_422 : i_421 >= end_422; i_421 += step_423) {
V v_420 = i_421;
var values_424 = R.Join(new V[] {}, R.Call(v_397, R.Join(new V[] {}, new Results(R.Get(v_419, v_420)))));
var obj_425 = v_419;
V key_426 = v_420;
R.Set(obj_425, key_426, values_424[0]);
}
}

var values_427 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_419}, new Results((V)"\t"))));
var obj_428 = v_417;
V key_429 = (V)((V)R.Length(v_417) + (V)1.0d);
R.Set(obj_428, key_429, values_427[0]);
return Results.Empty;
}));
V v_430 = V.Nil;
v_430 = new V((RuleFunction)(args_431 => {
V v_432 = args_431[0];
var values_433 = R.Join(new V[] {}, new Results(R.Table()));
V v_434 = values_433[0];

foreach (var row_435 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_432)))))) {
V v_436 = row_435[0];
V v_437 = row_435[1];
var values_438 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_437, (V)"x"), R.Concat((V)",", R.Get(v_437, (V)"y")))));
var obj_439 = v_434;
V key_440 = (V)((V)R.Length(v_434) + (V)1.0d);
R.Set(obj_439, key_440, values_438[0]);
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_434)));

return R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_434}, new Results((V)";"))));
return Results.Empty;
}));
V v_441 = V.Nil;
v_441 = new V((RuleFunction)(args_442 => {
V v_443 = args_442[0];
var values_444 = R.Join(new V[] {}, new Results(R.Table()));
V v_445 = values_444[0];

if (R.Truth((V)(v_443 == (V)""))) {
return R.Join(new V[] {}, new Results(v_445));
}
foreach (var row_446 in R.Iterate(R.Join(new V[] {}, R.Invoke(v_443, "gmatch", R.Join(new V[] {}, new Results((V)"[^;]+")))))) {
V v_447 = row_446[0];
var values_448 = R.Join(new V[] {}, R.Invoke(v_447, "match", R.Join(new V[] {}, new Results((V)"^(%d+),(%d+)$"))));
V v_449 = values_448[0];
V v_450 = values_448[1];

R.Call(v_22, R.Join(new V[] {v_449}, new Results((V)"invalid point")));
var values_451 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_449))).First), new Entry((V)"y", R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_450))).First))));
var obj_452 = v_445;
V key_453 = (V)((V)R.Length(v_445) + (V)1.0d);
R.Set(obj_452, key_453, values_451[0]);
}

return R.Join(new V[] {}, new Results(v_445));
return Results.Empty;
}));
R.Set(v_10, (V)"encode", new V((RuleFunction)(args_454 => {
V v_455 = args_454[0];
R.Call(R.Get(v_10, (V)"validate"), R.Join(new V[] {}, new Results(v_455)));
var values_456 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"ROAG-STUDIO-WORLD/1"))));
V v_457 = values_456[0];

R.Call(v_415, R.Join(new V[] {v_457, (V)"project"}, new Results(R.Get(v_455, (V)"project_id"))));
var values_458 = R.Join(new V[] {}, R.Call(v_31, R.Join(new V[] {}, new Results(R.Get(v_455, (V)"tiles")))));
V v_459 = values_458[0];

R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_459}, new Results(new V((RuleFunction)(args_460 => {
V v_461 = args_460[0];
V v_462 = args_460[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_461, (V)"id") < R.Get(v_462, (V)"id"))));
return Results.Empty;
})))));
foreach (var row_463 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_459)))))) {
V v_464 = row_463[0];
V v_465 = row_463[1];
R.Call(v_415, R.Join(new V[] {v_457, (V)"tile", R.Get(v_465, (V)"id"), R.Get(v_465, (V)"name"), R.Get(v_465, (V)"symbol"), R.Get(v_465, (V)"asset_id"), R.Or(R.And(R.Get(v_465, (V)"blocks_movement"), () => (V)1.0d), () => (V)0.0d), R.Or(R.And(R.Get(v_465, (V)"blocks_sight"), () => (V)1.0d), () => (V)0.0d)}, new Results(R.Or(R.And(R.Get(v_465, (V)"blocks_projectile"), () => (V)1.0d), () => (V)0.0d))));
}
var values_466 = R.Join(new V[] {}, R.Call(v_31, R.Join(new V[] {}, new Results(R.Get(v_455, (V)"rooms")))));
V v_467 = values_466[0];

R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_467}, new Results(new V((RuleFunction)(args_468 => {
V v_469 = args_468[0];
V v_470 = args_468[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_469, (V)"id") < R.Get(v_470, (V)"id"))));
return Results.Empty;
})))));
foreach (var row_471 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_467)))))) {
V v_472 = row_471[0];
V v_473 = row_471[1];
R.Call(v_415, R.Join(new V[] {v_457, (V)"room", R.Get(v_473, (V)"id"), R.Get(v_473, (V)"name"), R.Get(v_473, (V)"width"), R.Get(v_473, (V)"height"), R.Get(v_473, (V)"weight"), R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Call(v_49, R.Join(new V[] {}, new Results(R.Get(v_473, (V)"roles")))).First}, new Results((V)","))).First, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Call(v_49, R.Join(new V[] {}, new Results(R.Get(v_473, (V)"biomes")))).First}, new Results((V)","))).First, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Get(v_473, (V)"rotations")}, new Results((V)","))).First}, new Results(R.Or(R.And(R.Get(v_473, (V)"mirror"), () => (V)1.0d), () => (V)0.0d))));
foreach (var row_474 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_473, (V)"rows"))))))) {
V v_475 = row_474[0];
V v_476 = row_474[1];
R.Call(v_415, R.Join(new V[] {v_457, (V)"row", R.Get(v_473, (V)"id"), v_475}, new Results(v_476)));
}
foreach (var row_477 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_473, (V)"sockets"))))))) {
V v_478 = row_477[0];
V v_479 = row_477[1];
R.Call(v_415, R.Join(new V[] {v_457, (V)"socket", R.Get(v_473, (V)"id"), R.Get(v_479, (V)"facing"), R.Get(v_479, (V)"x"), R.Get(v_479, (V)"y")}, new Results(R.Get(v_479, (V)"width"))));
}
var values_480 = R.Join(new V[] {}, new Results(R.Table()));
V v_481 = values_480[0];

foreach (var row_482 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_473, (V)"markers"))))))) {
V v_483 = row_482[0];
var values_484 = R.Join(new V[] {}, new Results(v_483));
var obj_485 = v_481;
V key_486 = (V)((V)R.Length(v_481) + (V)1.0d);
R.Set(obj_485, key_486, values_484[0]);
}

R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_481)));
foreach (var row_487 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_481)))))) {
V v_488 = row_487[0];
V v_489 = row_487[1];
R.Call(v_415, R.Join(new V[] {v_457, (V)"marker", R.Get(v_473, (V)"id"), v_489}, R.Call(v_430, R.Join(new V[] {}, new Results(R.Get(R.Get(v_473, (V)"markers"), v_489))))));
}
R.Call(v_415, R.Join(new V[] {v_457, (V)"mask", R.Get(v_473, (V)"id"), (V)"editable"}, R.Call(v_430, R.Join(new V[] {}, new Results(R.Get(v_473, (V)"editable"))))));
R.Call(v_415, R.Join(new V[] {v_457, (V)"mask", R.Get(v_473, (V)"id"), (V)"protected"}, R.Call(v_430, R.Join(new V[] {}, new Results(R.Get(v_473, (V)"protected"))))));
var values_490 = R.Join(new V[] {}, new Results(R.Table()));
V v_491 = values_490[0];

foreach (var row_492 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_473, (V)"visual_cells"))))))) {
V v_493 = row_492[0];
var values_494 = R.Join(new V[] {}, new Results(v_493));
var obj_495 = v_491;
V key_496 = (V)((V)R.Length(v_491) + (V)1.0d);
R.Set(obj_495, key_496, values_494[0]);
}

R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_491)));
foreach (var row_497 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_491)))))) {
V v_498 = row_497[0];
V v_499 = row_497[1];
R.Call(v_415, R.Join(new V[] {v_457, (V)"visual", R.Get(v_473, (V)"id"), v_499}, new Results(R.Get(R.Get(v_473, (V)"visual_cells"), v_499))));
}
var values_500 = R.Join(new V[] {}, new Results(R.Table()));
v_491 = values_500[0];

foreach (var row_501 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_473, (V)"tile_cells"))))))) {
V v_502 = row_501[0];
var values_503 = R.Join(new V[] {}, new Results(v_502));
var obj_504 = v_491;
V key_505 = (V)((V)R.Length(v_491) + (V)1.0d);
R.Set(obj_504, key_505, values_503[0]);
}

R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_491)));
foreach (var row_506 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_491)))))) {
V v_507 = row_506[0];
V v_508 = row_506[1];
R.Call(v_415, R.Join(new V[] {v_457, (V)"tilecell", R.Get(v_473, (V)"id"), v_508}, new Results(R.Get(R.Get(v_473, (V)"tile_cells"), v_508))));
}
}
return R.Join(new V[] {}, new Results(R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_457}, new Results((V)"\n"))).First, (V)"\n")));
return Results.Empty;
})));
R.Set(v_10, (V)"decode", new V((RuleFunction)(args_509 => {
V v_510 = args_509[0];
R.Call(v_22, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_510))).First == (V)"string"), () => (V)((V)R.Length(v_510) <= (V)((V)1024.0d * (V)1024.0d)))}, new Results((V)"world source too large")));
var values_511 = R.Join(new V[] {}, new Results(R.Table()));
V v_512 = values_511[0];

foreach (var row_513 in R.Iterate(R.Join(new V[] {}, R.Invoke(R.Concat(v_510, (V)"\n"), "gmatch", R.Join(new V[] {}, new Results((V)"(.-)\n")))))) {
V v_514 = row_513[0];
if (R.Truth((V)(v_514 != (V)""))) {
var values_515 = R.Join(new V[] {}, new Results(v_514));
var obj_516 = v_512;
V key_517 = (V)((V)R.Length(v_512) + (V)1.0d);
R.Set(obj_516, key_517, values_515[0]);
}
}
R.Call(v_22, R.Join(new V[] {(V)(R.Get(v_512, (V)1.0d) == (V)"ROAG-STUDIO-WORLD/1")}, new Results((V)"incompatible world source")));
var values_518 = R.Join(new V[] {}, R.Call(v_405, R.Join(new V[] {}, new Results(R.Or(R.Get(v_512, (V)2.0d), () => (V)"")))));
V v_519 = values_518[0];

R.Call(v_22, R.Join(new V[] {R.And((V)(R.Get(v_519, (V)1.0d) == (V)"project"), () => (V)((V)R.Length(v_519) == (V)2.0d))}, new Results((V)"invalid world project record")));
var values_520 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schema", R.Get(v_10, (V)"schema")), new Entry((V)"project_id", R.Get(v_519, (V)2.0d)), new Entry((V)"tiles", R.Table()), new Entry((V)"rooms", R.Table()))));
V v_521 = values_520[0];
var values_522 = R.Join(new V[] {}, new Results(R.Table()));
V v_523 = values_522[0];
{ double end_526 = R.Num((V)R.Length(v_512)), step_527 = R.Num((V)1.0d);
for (double i_525 = R.Num((V)3.0d); step_527 > 0 ? i_525 <= end_526 : i_525 >= end_526; i_525 += step_527) {
V v_524 = i_525;
var values_528 = R.Join(new V[] {}, R.Call(v_405, R.Join(new V[] {}, new Results(R.Get(v_512, v_524)))));
V v_529 = values_528[0];

var values_530 = R.Join(new V[] {}, new Results(R.Get(v_529, (V)1.0d)));
V v_531 = values_530[0];
if (R.Truth((V)(v_531 == (V)"tile"))) {
R.Call(v_22, R.Join(new V[] {(V)((V)R.Length(v_529) == (V)8.0d)}, new Results((V)"invalid tile record")));
var values_532 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_529, (V)2.0d)), new Entry((V)"name", R.Get(v_529, (V)3.0d)), new Entry((V)"symbol", R.Get(v_529, (V)4.0d)), new Entry((V)"asset_id", R.Get(v_529, (V)5.0d)), new Entry((V)"blocks_movement", (V)(R.Get(v_529, (V)6.0d) == (V)"1")), new Entry((V)"blocks_sight", (V)(R.Get(v_529, (V)7.0d) == (V)"1")), new Entry((V)"blocks_projectile", (V)(R.Get(v_529, (V)8.0d) == (V)"1")))));
var obj_533 = R.Get(v_521, (V)"tiles");
V key_534 = (V)((V)R.Length(R.Get(v_521, (V)"tiles")) + (V)1.0d);
R.Set(obj_533, key_534, values_532[0]);
{ double end_537 = R.Num((V)8.0d), step_538 = R.Num((V)1.0d);
for (double i_536 = R.Num((V)6.0d); step_538 > 0 ? i_536 <= end_537 : i_536 >= end_537; i_536 += step_538) {
V v_535 = i_536;
R.Call(v_22, R.Join(new V[] {R.Or((V)(R.Get(v_529, v_535) == (V)"0"), () => (V)(R.Get(v_529, v_535) == (V)"1"))}, new Results((V)"invalid tile flag")));
}
}
} else if (R.Truth((V)(v_531 == (V)"room"))) {
R.Call(v_22, R.Join(new V[] {R.And((V)((V)R.Length(v_529) == (V)10.0d), () => (V)(!R.Truth(R.Get(v_523, R.Get(v_529, (V)2.0d)))))}, new Results((V)"invalid room record")));
V v_539 = V.Nil;
v_539 = new V((RuleFunction)(args_540 => {
V v_541 = args_540[0];
var values_542 = R.Join(new V[] {}, new Results(R.Table()));
V v_543 = values_542[0];

foreach (var row_544 in R.Iterate(R.Join(new V[] {}, R.Invoke(v_541, "gmatch", R.Join(new V[] {}, new Results((V)"[^,]+")))))) {
V v_545 = row_544[0];
var values_546 = R.Join(new V[] {}, new Results(v_545));
var obj_547 = v_543;
V key_548 = (V)((V)R.Length(v_543) + (V)1.0d);
R.Set(obj_547, key_548, values_546[0]);
}

return R.Join(new V[] {}, new Results(v_543));
return Results.Empty;
}));
var values_549 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_529, (V)2.0d)), new Entry((V)"name", R.Get(v_529, (V)3.0d)), new Entry((V)"width", R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_529, (V)4.0d)))).First), new Entry((V)"height", R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_529, (V)5.0d)))).First), new Entry((V)"weight", R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_529, (V)6.0d)))).First), new Entry((V)"roles", R.Call(v_539, R.Join(new V[] {}, new Results(R.Get(v_529, (V)7.0d)))).First), new Entry((V)"biomes", R.Call(v_539, R.Join(new V[] {}, new Results(R.Get(v_529, (V)8.0d)))).First), new Entry((V)"rotations", R.Table()), new Entry((V)"mirror", (V)(R.Get(v_529, (V)10.0d) == (V)"1")), new Entry((V)"rows", R.Table()), new Entry((V)"sockets", R.Table()), new Entry((V)"markers", R.Table()), new Entry((V)"editable", R.Table()), new Entry((V)"protected", R.Table()), new Entry((V)"visual_cells", R.Table()), new Entry((V)"tile_cells", R.Table()))));
V v_550 = values_549[0];
R.Call(v_22, R.Join(new V[] {R.Or((V)(R.Get(v_529, (V)10.0d) == (V)"0"), () => (V)(R.Get(v_529, (V)10.0d) == (V)"1"))}, new Results((V)"invalid mirror flag")));
foreach (var row_551 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_539, R.Join(new V[] {}, new Results(R.Get(v_529, (V)9.0d))))))))) {
V v_552 = row_551[0];
V v_553 = row_551[1];
var values_554 = R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_553))));
var obj_555 = R.Get(v_550, (V)"rotations");
V key_556 = (V)((V)R.Length(R.Get(v_550, (V)"rotations")) + (V)1.0d);
R.Set(obj_555, key_556, values_554[0]);
}
var values_557 = R.Join(new V[] {}, new Results(v_550));
var obj_558 = R.Get(v_521, (V)"rooms");
V key_559 = (V)((V)R.Length(R.Get(v_521, (V)"rooms")) + (V)1.0d);
R.Set(obj_558, key_559, values_557[0]);

var values_560 = R.Join(new V[] {}, new Results(v_550));
var obj_561 = v_523;
V key_562 = R.Get(v_550, (V)"id");
R.Set(obj_561, key_562, values_560[0]);
} else {
var values_563 = R.Join(new V[] {}, new Results(R.Get(v_523, R.Get(v_529, (V)2.0d))));
V v_564 = values_563[0];

R.Call(v_22, R.Join(new V[] {v_564}, new Results((V)"record before room")));
if (R.Truth((V)(v_531 == (V)"row"))) {
R.Call(v_22, R.Join(new V[] {R.And((V)((V)R.Length(v_529) == (V)4.0d), () => (V)(!R.Truth(R.Get(R.Get(v_564, (V)"rows"), R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_529, (V)3.0d)))).First))))}, new Results((V)"invalid row record")));

var values_565 = R.Join(new V[] {}, new Results(R.Get(v_529, (V)4.0d)));
var obj_566 = R.Get(v_564, (V)"rows");
V key_567 = R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_529, (V)3.0d)))).First;
R.Set(obj_566, key_567, values_565[0]);
} else if (R.Truth((V)(v_531 == (V)"socket"))) {
R.Call(v_22, R.Join(new V[] {(V)((V)R.Length(v_529) == (V)6.0d)}, new Results((V)"invalid socket record")));

var values_568 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"facing", R.Get(v_529, (V)3.0d)), new Entry((V)"x", R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_529, (V)4.0d)))).First), new Entry((V)"y", R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_529, (V)5.0d)))).First), new Entry((V)"width", R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_529, (V)6.0d)))).First))));
var obj_569 = R.Get(v_564, (V)"sockets");
V key_570 = (V)((V)R.Length(R.Get(v_564, (V)"sockets")) + (V)1.0d);
R.Set(obj_569, key_570, values_568[0]);
} else if (R.Truth((V)(v_531 == (V)"marker"))) {
R.Call(v_22, R.Join(new V[] {R.And((V)((V)R.Length(v_529) == (V)4.0d), () => (V)(!R.Truth(R.Get(R.Get(v_564, (V)"markers"), R.Get(v_529, (V)3.0d)))))}, new Results((V)"invalid marker record")));

var values_571 = R.Join(new V[] {}, R.Call(v_441, R.Join(new V[] {}, new Results(R.Get(v_529, (V)4.0d)))));
var obj_572 = R.Get(v_564, (V)"markers");
V key_573 = R.Get(v_529, (V)3.0d);
R.Set(obj_572, key_573, values_571[0]);
} else if (R.Truth((V)(v_531 == (V)"mask"))) {
R.Call(v_22, R.Join(new V[] {R.And((V)((V)R.Length(v_529) == (V)4.0d), () => R.Or((V)(R.Get(v_529, (V)3.0d) == (V)"editable"), () => (V)(R.Get(v_529, (V)3.0d) == (V)"protected")))}, new Results((V)"invalid mask record")));

var values_574 = R.Join(new V[] {}, R.Call(v_441, R.Join(new V[] {}, new Results(R.Get(v_529, (V)4.0d)))));
var obj_575 = v_564;
V key_576 = R.Get(v_529, (V)3.0d);
R.Set(obj_575, key_576, values_574[0]);
} else if (R.Truth((V)(v_531 == (V)"visual"))) {
R.Call(v_22, R.Join(new V[] {R.And((V)((V)R.Length(v_529) == (V)4.0d), () => (V)(!R.Truth(R.Get(R.Get(v_564, (V)"visual_cells"), R.Get(v_529, (V)3.0d)))))}, new Results((V)"invalid visual record")));

var values_577 = R.Join(new V[] {}, new Results(R.Get(v_529, (V)4.0d)));
var obj_578 = R.Get(v_564, (V)"visual_cells");
V key_579 = R.Get(v_529, (V)3.0d);
R.Set(obj_578, key_579, values_577[0]);
} else if (R.Truth((V)(v_531 == (V)"tilecell"))) {
R.Call(v_22, R.Join(new V[] {R.And((V)((V)R.Length(v_529) == (V)4.0d), () => (V)(!R.Truth(R.Get(R.Get(v_564, (V)"tile_cells"), R.Get(v_529, (V)3.0d)))))}, new Results((V)"invalid tile cell record")));

var values_580 = R.Join(new V[] {}, new Results(R.Get(v_529, (V)4.0d)));
var obj_581 = R.Get(v_564, (V)"tile_cells");
V key_582 = R.Get(v_529, (V)3.0d);
R.Set(obj_581, key_582, values_580[0]);
} else {
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results(R.Concat((V)"unknown world record: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_531))).First))));
}
}
}
}
R.Call(R.Get(v_10, (V)"validate"), R.Join(new V[] {}, new Results(v_521)));

return R.Join(new V[] {}, new Results(v_521));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_10));
return Results.Empty;
} }
