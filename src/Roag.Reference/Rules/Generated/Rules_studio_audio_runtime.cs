// Static C# port of src/studio_audio_runtime.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: c500f6890b6a25f6002e33f2f9cd4e72dc9a0d37832a153eec71dd718f57bbe4
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_audio_runtime { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_audio"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_audio_store"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_audio_samples"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.audio_cues"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, new Results(R.Table()));
V v_12 = values_11[0];
V v_13 = V.Nil;
v_13 = new V((RuleFunction)(args_14 => {
V v_15 = args_14[0];
return R.Join(new V[] {}, new Results(R.And(v_15, () => R.Invoke(v_15, "match", R.Join(new V[] {}, new Results((V)"([^/]+)$"))).First)));
return Results.Empty;
}));
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
if (R.Truth(R.And(v_19, () => R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(v_19))).First))) {
var values_20 = R.Join(new V[] {}, new Results(v_19));
var obj_21 = v_18;
V key_22 = (V)((V)R.Length(v_18) + (V)1.0d);
R.Set(obj_21, key_22, values_20[0]);
}
return Results.Empty;
}));
V v_23 = V.Nil;
v_23 = new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
return R.Join(new V[] {}, new Results(R.And(R.Get(v_25, (V)"source_tags"), () => R.Get(R.Get(v_25, (V)"source_tags"), (V)1.0d))));
return Results.Empty;
}));
V v_26 = V.Nil;
v_26 = new V((RuleFunction)(args_27 => {
V v_28 = args_27[0];
return R.Join(new V[] {}, new Results(R.Get(v_28, (V)"target_definition_id")));
return Results.Empty;
}));
R.Set(v_12, (V)"cues_for_event", new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
V v_31 = args_29[1];
var values_32 = R.Join(new V[] {}, new Results(R.Table()));
V v_33 = values_32[0];
if (R.Truth(R.Or((V)(!R.Truth(v_30)), () => (V)(!R.Truth(R.Get(v_30, (V)"kind")))))) {
return R.Join(new V[] {}, new Results(v_33));
}
var values_34 = R.Join(new V[] {}, new Results(R.And(v_31, () => R.Get(v_31, (V)"player_id"))));
V v_35 = values_34[0];
var values_36 = R.Join(new V[] {}, R.Call(v_23, R.Join(new V[] {}, new Results(v_30))));
V v_37 = values_36[0];
var values_38 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {}, new Results(v_30))));
V v_39 = values_38[0];
if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"Moved"))) {
if (R.Truth((V)(R.Get(v_30, (V)"source_id") == v_35))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/player/", R.Or(R.And(R.And(R.Get(v_30, (V)"path"), () => (V)((V)R.Length(R.Get(v_30, (V)"path")) > (V)1.0d)), () => (V)"dash"), () => (V)"move")))));
} else {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/enemy/move")));
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/enemy/", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(v_37))))).First, (V)"/move")))));
}
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"AttackPerformed"))) {
if (R.Truth((V)(R.Get(v_30, (V)"source_id") == v_35))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/player/attack")));
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/ability/", R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(R.Get(v_30, (V)"ability_id")))))).First))));
} else {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/enemy/attack")));
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/enemy/", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(v_37))))).First, (V)"/attack")))));
}
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"WindupStarted"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/enemy/telegraph")));
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/enemy/", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(v_37))))).First, (V)"/telegraph")))));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"DamageTaken"))) {
if (R.Truth((V)(R.Get(v_30, (V)"target_id") == v_35))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/player/damage")));
} else {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/enemy/hit")));
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/enemy/", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(v_39))))).First, (V)"/hit")))));
}
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"EntityKilled"))) {
if (R.Truth((V)(R.Get(v_30, (V)"target_id") == v_35))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/player/death")));
} else {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/enemy/death")));
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/enemy/", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(v_39))))).First, (V)"/death")))));
}
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"BoonActivated"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/boon/activate")));
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/boon/", R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(R.Get(v_30, (V)"boon_id")))))).First))));
} else if (R.Truth(R.And((V)(R.Get(v_30, (V)"kind") == (V)"StatusTick"), () => (V)(R.Get(v_30, (V)"status") == (V)"burning")))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/status/burning")));
} else if (R.Truth(R.And((V)(R.Get(v_30, (V)"kind") == (V)"TileEntered"), () => (V)(R.Get(v_30, (V)"source_id") == v_35)))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/terrain/step")));
if (R.Truth(R.And(R.And(v_31, () => R.Get(v_31, (V)"generated_map")), () => (V)(R.Get(v_30, (V)"floor_id") == R.Get(v_31, (V)"active_floor_id"))))) {
var values_40 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"at"), R.Join(new V[] {R.Get(v_31, (V)"generated_map"), R.Get(v_30, (V)"x")}, new Results(R.Get(v_30, (V)"y")))));
V v_41 = values_40[0];
if (R.Truth(v_41)) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/terrain/", R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(R.Get(v_41, (V)"id")))))).First))));
}
}
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"TerrainTriggered"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/terrain/hazard")));
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/terrain/", R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(R.Get(v_30, (V)"terrain_id")))))).First))));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"TerrainBroken"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/terrain/", R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(R.Get(v_30, (V)"terrain_id")))))).First))));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"FloorChanged"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/terrain/stairs")));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"DoorOpened"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/world/door")));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"ChestClaimed"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/world/chest")));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"SecretDiscovered"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/world/secret")));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"CurrencyCollected"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/world/currency")));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"StatIncreased"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/ui/level_up")));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"AchievementEarned"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/ui/achievement")));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"EliteEngaged"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/boss/arrival")));
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/elite/", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(v_37))))).First, (V)"/arrival")))));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"FinalSanctumEntered"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/boss/arrival")));
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/elite/hollow_regent/arrival")));
} else if (R.Truth(R.Or((V)(R.Get(v_30, (V)"kind") == (V)"ElitePhaseChanged"), () => (V)(R.Get(v_30, (V)"kind") == (V)"RegentPhaseChanged")))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/boss/phase")));
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/elite/", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(v_37))))).First, (V)"/phase")))));
} else if (R.Truth(R.Or((V)(R.Get(v_30, (V)"kind") == (V)"EliteDefeated"), () => (V)(R.Get(v_30, (V)"kind") == (V)"RegentDefeated")))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/boss/defeat")));
var values_42 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(R.Get(v_30, (V)"kind") == (V)"RegentDefeated"), () => (V)"core:enemy/hollow_regent"), () => R.And(R.And(v_31, () => R.Get(v_31, (V)"stage")), () => R.Get(R.Get(v_31, (V)"stage"), (V)"elite_definition_id"))), () => v_37)));
V v_43 = values_42[0];
R.Call(v_16, R.Join(new V[] {v_33}, new Results(R.Concat((V)"core:cue/elite/", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {}, new Results(v_43))))).First, (V)"/defeat")))));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"Victory"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/ui/victory")));
} else if (R.Truth((V)(R.Get(v_30, (V)"kind") == (V)"Defeat"))) {
R.Call(v_16, R.Join(new V[] {v_33}, new Results((V)"core:cue/ui/defeat")));
}
return R.Join(new V[] {}, new Results(v_33));
return Results.Empty;
})));
V v_44 = V.Nil;
v_44 = new V((RuleFunction)(args_45 => {
V v_46 = args_45[0];
var values_47 = R.Join(new V[] {}, new Results(R.Table()));
V v_48 = values_47[0];

foreach (var row_49 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_46, (V)"bindings"))))))) {
V v_50 = row_49[0];
V v_51 = row_49[1];
var values_52 = R.Join(new V[] {}, new Results(v_51));
var obj_53 = v_48;
V key_54 = R.Get(v_51, (V)"cue_id");
R.Set(obj_53, key_54, values_52[0]);
}
return R.Join(new V[] {}, new Results(v_48));
return Results.Empty;
}));
V v_55 = V.Nil;
v_55 = new V((RuleFunction)(args_56 => {
V v_57 = args_56[0];
if (R.Truth(R.Or(R.Invoke(v_57, "find", R.Join(new V[] {(V)"/music/", (V)1.0d}, new Results((V)true))).First, () => R.Invoke(v_57, "match", R.Join(new V[] {}, new Results((V)"/music$"))).First))) {
return R.Join(new V[] {}, new Results((V)"music"));
}
var values_58 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(v_57))));
V v_59 = values_58[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(v_59, () => R.Get(v_59, (V)"category")), () => (V)"world")));
return Results.Empty;
}));
V v_60 = V.Nil;
v_60 = new V((RuleFunction)(args_61 => {
V v_62 = args_61[0];
V v_63 = args_61[1];
var values_64 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"inspect_published"), R.Join(new V[] {}, new Results(v_62))));
V v_65 = values_64[0];
V v_66 = values_64[1];
var values_67 = Results.Empty;
V v_68 = values_67[0];
var values_69 = R.Join(new V[] {}, new Results((V)(v_65 != V.Nil)));
V v_70 = values_69[0];
if (R.Truth((V)(!R.Truth(v_65)))) {
if (R.Truth(R.And((V)(v_66 != (V)"absent"), () => (V)(v_66 != (V)"corrupt audio")))) {
return R.Join(new V[] {V.Nil}, new Results(R.Concat((V)"rejected: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_66))).First)));
}
var values_71 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"showcase"), R.Join(new V[] {}, new Results((V)"builtin_audio"))));
v_65 = values_71[0];
v_68 = values_71[1];
var values_72 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_66 == (V)"absent"), () => (V)"original built-in samples"), () => (V)"original built-in samples (published audio corrupt)")));
v_66 = values_72[0];
}
var values_73 = R.Join(new V[] {}, new Results(R.Table()));
V v_74 = values_73[0];
foreach (var row_75 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_65, (V)"sounds"))))))) {
V v_76 = row_75[0];
V v_77 = row_75[1];
var values_78 = R.Join(new V[] {}, new Results(R.Or(R.And(v_68, () => R.Get(v_68, R.Get(v_77, (V)"path"))), () => R.Call(v_62, R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"published_path"), R.Join(new V[] {}, new Results(v_77))))).First)));
V v_79 = values_78[0];
if (R.Truth(R.Or(R.Or((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_79))).First != (V)"string"), () => (V)((V)R.Length(v_79) != R.Get(v_77, (V)"bytes"))), () => (V)(R.Call(R.Get(v_2, (V)"checksum"), R.Join(new V[] {}, new Results(v_79))).First != R.Get(v_77, (V)"checksum"))))) {
return R.Join(new V[] {V.Nil}, new Results(R.Concat((V)"missing/corrupt audio: ", R.Get(v_77, (V)"id"))));
}
var values_80 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"format")}, new Results(v_79))));
V v_81 = values_80[0];
V v_82 = values_80[1];
if (R.Truth((V)(!R.Truth(v_81)))) {
return R.Join(new V[] {V.Nil}, new Results(R.Concat((V)"invalid audio ", R.Concat(R.Get(v_77, (V)"id"), R.Concat((V)": ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_82))).First)))));
}
var values_83 = R.Join(new V[] {}, new Results((V)false));
V v_84 = values_83[0];
foreach (var row_85 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_65, (V)"bindings"))))))) {
V v_86 = row_85[0];
V v_87 = row_85[1];
if (R.Truth(R.Or(R.Invoke(R.Get(v_87, (V)"cue_id"), "find", R.Join(new V[] {(V)"/music/", (V)1.0d}, new Results((V)true))).First, () => R.Invoke(R.Get(v_87, (V)"cue_id"), "find", R.Join(new V[] {(V)"/ambient/", (V)1.0d}, new Results((V)true))).First))) {
foreach (var row_88 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_87, (V)"variants"))))))) {
V v_89 = row_88[0];
V v_90 = row_88[1];
if (R.Truth((V)(R.Get(v_90, (V)"asset_id") == R.Get(v_77, (V)"id")))) {
var values_91 = R.Join(new V[] {}, new Results((V)true));
v_84 = values_91[0];

break;
}
}
}
if (R.Truth(v_84)) {
break;
}
}
var values_92 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_63, v_79, v_77}, new Results(R.Or(R.And(v_84, () => (V)"stream"), () => (V)"static")))));
V v_93 = values_92[0];
V v_94 = values_92[1];
if (R.Truth(R.Or((V)(!R.Truth(v_93)), () => (V)(!R.Truth(v_94))))) {
return R.Join(new V[] {V.Nil}, new Results(R.Concat((V)"audio decode failed ", R.Concat(R.Get(v_77, (V)"id"), R.Concat((V)": ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_94))).First)))));
}
var values_95 = R.Join(new V[] {}, new Results(v_94));
var obj_96 = v_74;
V key_97 = R.Get(v_77, (V)"id");
R.Set(obj_96, key_97, values_95[0]);
}
var values_98 = R.Join(new V[] {}, R.Call(v_44, R.Join(new V[] {}, new Results(v_65))));
V v_99 = values_98[0];
var values_100 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_65, (V)"bindings"))));
V v_101 = values_100[0];
var values_102 = R.Join(new V[] {}, new Results((V)0.0d));
V v_103 = values_102[0];
if (R.Truth(v_70)) {
var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"showcase"), R.Join(new V[] {}, new Results((V)"builtin_audio"))));
V v_105 = values_104[0];
V v_106 = values_104[1];
var values_107 = R.Join(new V[] {}, new Results(R.Table()));
V v_108 = values_107[0];
foreach (var row_109 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_105, (V)"bindings"))))))) {
V v_110 = row_109[0];
V v_111 = row_109[1];
if (R.Truth((V)(!R.Truth(R.Get(v_99, R.Get(v_111, (V)"cue_id")))))) {
var values_112 = R.Join(new V[] {}, new Results(v_111));
var obj_113 = v_99;
V key_114 = R.Get(v_111, (V)"cue_id");
R.Set(obj_113, key_114, values_112[0]);
var values_115 = R.Join(new V[] {}, new Results((V)true));
var obj_116 = v_108;
V key_117 = R.Get(R.Get(R.Get(v_111, (V)"variants"), (V)1.0d), (V)"asset_id");
R.Set(obj_116, key_117, values_115[0]);
var values_118 = R.Join(new V[] {}, new Results((V)(v_103 + (V)1.0d)));
v_103 = values_118[0];
}
}
foreach (var row_119 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_105, (V)"sounds"))))))) {
V v_120 = row_119[0];
V v_121 = row_119[1];
if (R.Truth(R.And(R.Get(v_108, R.Get(v_121, (V)"id")), () => (V)(!R.Truth(R.Get(v_74, R.Get(v_121, (V)"id"))))))) {
var values_122 = R.Join(new V[] {}, new Results(R.Or(R.Invoke(R.Get(v_121, (V)"id"), "match", R.Join(new V[] {}, new Results((V)"original_music$"))).First, () => R.Invoke(R.Get(v_121, (V)"id"), "match", R.Join(new V[] {}, new Results((V)"original_ambience$"))).First)));
V v_123 = values_122[0];
var values_124 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_63, R.Get(v_106, R.Get(v_121, (V)"path")), v_121}, new Results(R.Or(R.And(v_123, () => (V)"stream"), () => (V)"static")))));
V v_125 = values_124[0];
V v_126 = values_124[1];
if (R.Truth(R.Or((V)(!R.Truth(v_125)), () => (V)(!R.Truth(v_126))))) {
return R.Join(new V[] {V.Nil}, new Results(R.Concat((V)"original audio decode failed ", R.Concat(R.Get(v_121, (V)"id"), R.Concat((V)": ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_126))).First)))));
}
var values_127 = R.Join(new V[] {}, new Results(v_126));
var obj_128 = v_74;
V key_129 = R.Get(v_121, (V)"id");
R.Set(obj_128, key_129, values_127[0]);
}
}
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"pack", v_65), new Entry((V)"status", v_66), new Entry((V)"templates", v_74), new Entry((V)"bindings", v_99), new Entry((V)"custom_binding_count", R.Or(R.And(v_70, () => v_101), () => (V)0.0d)), new Entry((V)"fallback_binding_count", v_103), new Entry((V)"signature", R.Call(R.Get(v_2, (V)"checksum"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"encode"), R.Join(new V[] {}, new Results(v_65))))).First))));
return Results.Empty;
}));
R.Set(v_12, (V)"new", new V((RuleFunction)(args_130 => {
V v_131 = args_130[0];
V v_132 = args_130[1];
var values_133 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"read", v_131), new Entry((V)"make_source", v_132), new Entry((V)"elapsed", (V)0.0d), new Entry((V)"voices", R.Table()), new Entry((V)"music", V.Nil), new Entry((V)"retiring_music", V.Nil), new Entry((V)"scene_id", V.Nil), new Entry((V)"ambient_id", V.Nil), new Entry((V)"ambience", V.Nil), new Entry((V)"retiring_ambience", V.Nil), new Entry((V)"variant_counter", (V)0.0d), new Entry((V)"last_played", R.Table()), new Entry((V)"status", (V)"absent"), new Entry((V)"paused", (V)false), new Entry((V)"max_total_voices", (V)24.0d), new Entry((V)"played_count", (V)0.0d), new Entry((V)"user_music", (V)1.0d), new Entry((V)"user_effects", (V)1.0d))}, new Results(R.Table(new Entry((V)"__index", v_12))))));
V v_134 = values_133[0];
R.Invoke(v_134, "refresh", Results.Empty);
return R.Join(new V[] {}, new Results(v_134));
return Results.Empty;
})));
R.Set(v_12, "set_user_mix", new V((RuleFunction)(args_135 => {
V v_136 = args_135[0];
V v_137 = args_135[1];
V v_138 = args_135[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_137))).First == (V)"number"), () => (V)(v_137 >= (V)0.0d)), () => (V)(v_137 <= (V)1.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_138))).First == (V)"number")), () => (V)(v_138 >= (V)0.0d)), () => (V)(v_138 <= (V)1.0d))}, new Results((V)"invalid user audio mix")));
var values_139 = R.Join(new V[] {v_137}, new Results(v_138));
var obj_140 = v_136;
V key_141 = (V)"user_music";
var obj_142 = v_136;
V key_143 = (V)"user_effects";
R.Set(obj_140, key_141, values_139[0]);
R.Set(obj_142, key_143, values_139[1]);
foreach (var row_144 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_136, (V)"voices"))))))) {
V v_145 = row_144[0];
V v_146 = row_144[1];
R.Invoke(R.Get(v_146, (V)"source"), "setVolume", R.Join(new V[] {}, new Results((V)(R.Get(v_146, (V)"base_volume") * v_138))));
}
foreach (var row_147 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"music", R.Get(v_136, (V)"music")), new Entry((V)"old_music", R.Get(v_136, (V)"retiring_music")), new Entry((V)"ambience", R.Get(v_136, (V)"ambience")), new Entry((V)"old_ambience", R.Get(v_136, (V)"retiring_ambience"))))))))) {
V v_148 = row_147[0];
V v_149 = row_147[1];
if (R.Truth(v_149)) {
var values_150 = R.Join(new V[] {}, new Results((V)(R.Get(v_149, (V)"base_volume") * v_137)));
var obj_151 = v_149;
V key_152 = (V)"target";
R.Set(obj_151, key_152, values_150[0]);
if (R.Truth((V)(R.Get(v_149, (V)"duration") == (V)0.0d))) {
R.Invoke(R.Get(v_149, (V)"source"), "setVolume", R.Join(new V[] {}, new Results(R.Get(v_149, (V)"target"))));
} else if (R.Truth(R.Or((V)(!R.Truth(R.Get(v_149, (V)"delay"))), () => (V)(R.Get(v_149, (V)"delay") == (V)0.0d)))) {
R.Invoke(R.Get(v_149, (V)"source"), "setVolume", R.Join(new V[] {}, new Results((V)(R.Get(v_149, (V)"target") * R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, new Results((V)(R.Or(R.Get(v_149, (V)"elapsed"), () => (V)0.0d) / R.Get(v_149, (V)"duration"))))).First))));
}
}
}
return Results.Empty;
})));
R.Set(v_12, "refresh", new V((RuleFunction)(args_153 => {
V v_154 = args_153[0];
var values_155 = R.Join(new V[] {}, R.Call(v_60, R.Join(new V[] {R.Get(v_154, (V)"read")}, new Results(R.Get(v_154, (V)"make_source")))));
V v_156 = values_155[0];
V v_157 = values_155[1];
if (R.Truth((V)(!R.Truth(v_156)))) {
if (R.Truth((V)(!R.Truth(R.Get(v_154, (V)"pack"))))) {
var values_159 = R.Join(new V[] {}, R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(v_60, R.Join(new V[] {new V((RuleFunction)(args_158 => {

return R.Join(new V[] {}, new Results(V.Nil));
return Results.Empty;
}))}, new Results(R.Get(v_154, (V)"make_source")))))));
V v_160 = values_159[0];
if (R.Truth(v_160)) {
var values_161 = R.Join(new V[] {}, new Results(v_160));
v_156 = values_161[0];
var values_162 = R.Join(new V[] {}, new Results(R.Concat((V)"original built-in samples (published audio rejected: ", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_157))).First, (V)")"))));
var obj_163 = v_156;
V key_164 = (V)"status";
R.Set(obj_163, key_164, values_162[0]);
}
}
}
if (R.Truth((V)(!R.Truth(v_156)))) {
var values_165 = R.Join(new V[] {}, new Results(R.Concat((V)"rejected; kept previous audio: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_157))).First)));
var obj_166 = v_154;
V key_167 = (V)"status";
R.Set(obj_166, key_167, values_165[0]);
return R.Join(new V[] {(V)false}, new Results(R.Get(v_154, (V)"status")));
}
if (R.Truth((V)(R.Get(v_154, (V)"signature") == R.Get(v_156, (V)"signature")))) {
var values_168 = R.Join(new V[] {}, new Results(R.Concat((V)"unchanged \u00c2\u00b7 ", R.Get(v_156, (V)"status"))));
var obj_169 = v_154;
V key_170 = (V)"status";
R.Set(obj_169, key_170, values_168[0]);
return R.Join(new V[] {(V)true}, new Results(R.Get(v_154, (V)"status")));
}
R.Invoke(v_154, "stop_all", Results.Empty);
var values_171 = R.Join(new V[] {}, new Results(R.Get(v_154, (V)"paused")));
V v_172 = values_171[0];
var values_173 = R.Join(new V[] {}, new Results((V)false));
var obj_174 = v_154;
V key_175 = (V)"paused";
R.Set(obj_174, key_175, values_173[0]);
var values_176 = R.Join(new V[] {R.Get(v_156, (V)"pack"), R.Get(v_156, (V)"templates"), R.Get(v_156, (V)"bindings")}, new Results(R.Get(v_156, (V)"signature")));
var obj_177 = v_154;
V key_178 = (V)"pack";
var obj_179 = v_154;
V key_180 = (V)"templates";
var obj_181 = v_154;
V key_182 = (V)"bindings";
var obj_183 = v_154;
V key_184 = (V)"signature";
R.Set(obj_177, key_178, values_176[0]);
R.Set(obj_179, key_180, values_176[1]);
R.Set(obj_181, key_182, values_176[2]);
R.Set(obj_183, key_184, values_176[3]);
var values_185 = R.Join(new V[] {}, new Results(R.Get(v_156, (V)"custom_binding_count")));
var obj_186 = v_154;
V key_187 = (V)"custom_binding_count";
R.Set(obj_186, key_187, values_185[0]);
var values_188 = R.Join(new V[] {}, new Results(R.Get(v_156, (V)"fallback_binding_count")));
var obj_189 = v_154;
V key_190 = (V)"fallback_binding_count";
R.Set(obj_189, key_190, values_188[0]);
var values_191 = R.Join(new V[] {}, new Results(R.Concat((V)"refreshed \u00c2\u00b7 ", R.Get(v_156, (V)"status"))));
var obj_192 = v_154;
V key_193 = (V)"status";
R.Set(obj_192, key_193, values_191[0]);
var values_194 = R.Join(new V[] {R.Get(v_154, (V)"scene_id")}, new Results(R.Get(v_154, (V)"ambient_id")));
V v_195 = values_194[0];
V v_196 = values_194[1];
var values_197 = R.Join(new V[] {V.Nil}, new Results(V.Nil));
var obj_198 = v_154;
V key_199 = (V)"scene_id";
var obj_200 = v_154;
V key_201 = (V)"ambient_id";
R.Set(obj_198, key_199, values_197[0]);
R.Set(obj_200, key_201, values_197[1]);
if (R.Truth(v_195)) {
R.Invoke(v_154, "set_scene", R.Join(new V[] {}, new Results(v_195)));
}
if (R.Truth(v_196)) {
R.Invoke(v_154, "set_ambience", R.Join(new V[] {}, new Results(v_196)));
}
if (R.Truth(v_172)) {
R.Invoke(v_154, "set_paused", R.Join(new V[] {}, new Results((V)true)));
}
return R.Join(new V[] {(V)true}, new Results(R.Get(v_154, (V)"status")));
return Results.Empty;
})));
R.Set(v_12, "stop_all", new V((RuleFunction)(args_202 => {
V v_203 = args_202[0];
foreach (var row_204 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_203, (V)"voices"))))))) {
V v_205 = row_204[0];
V v_206 = row_204[1];
R.Invoke(R.Get(v_206, (V)"source"), "stop", Results.Empty);
}
var values_207 = R.Join(new V[] {}, new Results(R.Table()));
var obj_208 = v_203;
V key_209 = (V)"voices";
R.Set(obj_208, key_209, values_207[0]);
if (R.Truth(R.Get(v_203, (V)"music"))) {
R.Invoke(R.Get(R.Get(v_203, (V)"music"), (V)"source"), "stop", Results.Empty);
}
if (R.Truth(R.Get(v_203, (V)"retiring_music"))) {
R.Invoke(R.Get(R.Get(v_203, (V)"retiring_music"), (V)"source"), "stop", Results.Empty);
}
var values_210 = R.Join(new V[] {V.Nil}, new Results(V.Nil));
var obj_211 = v_203;
V key_212 = (V)"music";
var obj_213 = v_203;
V key_214 = (V)"retiring_music";
R.Set(obj_211, key_212, values_210[0]);
R.Set(obj_213, key_214, values_210[1]);
if (R.Truth(R.Get(v_203, (V)"ambience"))) {
R.Invoke(R.Get(R.Get(v_203, (V)"ambience"), (V)"source"), "stop", Results.Empty);
}
if (R.Truth(R.Get(v_203, (V)"retiring_ambience"))) {
R.Invoke(R.Get(R.Get(v_203, (V)"retiring_ambience"), (V)"source"), "stop", Results.Empty);
}
var values_215 = R.Join(new V[] {V.Nil}, new Results(V.Nil));
var obj_216 = v_203;
V key_217 = (V)"ambience";
var obj_218 = v_203;
V key_219 = (V)"retiring_ambience";
R.Set(obj_216, key_217, values_215[0]);
R.Set(obj_218, key_219, values_215[1]);
return Results.Empty;
})));
R.Set(v_12, "select_variant", new V((RuleFunction)(args_220 => {
V v_221 = args_220[0];
V v_222 = args_220[1];
var values_223 = R.Join(new V[] {}, new Results((V)(R.Get(v_221, (V)"variant_counter") + (V)1.0d)));
var obj_224 = v_221;
V key_225 = (V)"variant_counter";
R.Set(obj_224, key_225, values_223[0]);
var values_226 = R.Join(new V[] {}, new Results((V)0.0d));
V v_227 = values_226[0];
foreach (var row_228 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_222, (V)"variants"))))))) {
V v_229 = row_228[0];
V v_230 = row_228[1];
var values_231 = R.Join(new V[] {}, new Results((V)(v_227 + R.Get(v_230, (V)"weight"))));
v_227 = values_231[0];
}
var values_232 = R.Join(new V[] {}, new Results((V)((V)((V)(R.Get(v_221, (V)"variant_counter") - (V)1.0d) % v_227) + (V)1.0d)));
V v_233 = values_232[0];
foreach (var row_234 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_222, (V)"variants"))))))) {
V v_235 = row_234[0];
V v_236 = row_234[1];
var values_237 = R.Join(new V[] {}, new Results((V)(v_233 - R.Get(v_236, (V)"weight"))));
v_233 = values_237[0];
if (R.Truth((V)(v_233 <= (V)0.0d))) {
return R.Join(new V[] {}, new Results(R.Get(v_236, (V)"asset_id")));
}
}
return Results.Empty;
})));
V v_238 = V.Nil;
v_238 = new V((RuleFunction)(args_239 => {
V v_240 = args_239[0];
return R.Join(new V[] {}, R.Invoke(v_240, "clone", Results.Empty));
return Results.Empty;
}));
R.Set(v_12, "emit", new V((RuleFunction)(args_241 => {
V v_242 = args_241[0];
V v_243 = args_241[1];
V v_244 = args_241[2];
V v_245 = args_241[3];
var values_246 = R.Join(new V[] {}, new Results(R.And(R.Get(v_242, (V)"bindings"), () => R.Get(R.Get(v_242, (V)"bindings"), v_243))));
V v_247 = values_246[0];
if (R.Truth((V)(!R.Truth(v_247)))) {
return R.Join(new V[] {(V)false}, new Results((V)"unbound"));
}
var values_248 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_242, (V)"pack"), (V)"mix"), R.Call(v_55, R.Join(new V[] {}, new Results(v_243))).First)));
V v_249 = values_248[0];
if (R.Truth(R.Or(R.Or(R.Or((V)(!R.Truth(v_249)), () => R.Get(v_249, (V)"mute")), () => (V)(R.Get(v_249, (V)"volume") == (V)0.0d)), () => (V)(R.Get(v_242, (V)"user_effects") == (V)0.0d)))) {
return R.Join(new V[] {(V)false}, new Results((V)"muted"));
}
var values_250 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_242, (V)"last_played"), v_243), () => (-R.Get(rt.Global("math"), (V)"huge")))));
V v_251 = values_250[0];
if (R.Truth((V)((V)(R.Get(v_242, (V)"elapsed") - v_251) < (V)(R.Get(v_247, (V)"cooldown_ms") / (V)1000.0d)))) {
return R.Join(new V[] {(V)false}, new Results((V)"cooldown"));
}
var values_252 = R.Join(new V[] {}, new Results((V)0.0d));
V v_253 = values_252[0];
{ double end_256 = R.Num((V)1.0d), step_257 = R.Num((-(V)1.0d));
for (double i_255 = R.Num((V)R.Length(R.Get(v_242, (V)"voices"))); step_257 > 0 ? i_255 <= end_256 : i_255 >= end_256; i_255 += step_257) {
V v_254 = i_255;
if (R.Truth((V)(!R.Truth(R.Invoke(R.Get(R.Get(R.Get(v_242, (V)"voices"), v_254), (V)"source"), "isPlaying", Results.Empty).First)))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_242, (V)"voices")}, new Results(v_254)));
} else if (R.Truth((V)(R.Get(R.Get(R.Get(v_242, (V)"voices"), v_254), (V)"cue_id") == v_243))) {
var values_258 = R.Join(new V[] {}, new Results((V)(v_253 + (V)1.0d)));
v_253 = values_258[0];
}
}
}
if (R.Truth((V)(v_253 >= R.Get(v_247, (V)"max_voices")))) {
return R.Join(new V[] {(V)false}, new Results((V)"voice cap"));
}
if (R.Truth((V)((V)R.Length(R.Get(v_242, (V)"voices")) >= R.Get(v_242, (V)"max_total_voices")))) {
var values_259 = Results.Empty;
V v_260 = values_259[0];
V v_261 = values_259[1];
foreach (var row_262 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_242, (V)"voices"))))))) {
V v_263 = row_262[0];
V v_264 = row_262[1];
if (R.Truth(R.Or((V)(!R.Truth(v_261)), () => (V)(R.Get(v_264, (V)"priority") < v_261)))) {
var values_265 = R.Join(new V[] {}, new Results(v_263));
v_260 = values_265[0];

var values_266 = R.Join(new V[] {}, new Results(R.Get(v_264, (V)"priority")));
v_261 = values_266[0];
}
}
if (R.Truth(R.And(v_261, () => (V)(v_261 <= R.Get(v_247, (V)"priority"))))) {
R.Invoke(R.Get(R.Get(R.Get(v_242, (V)"voices"), v_260), (V)"source"), "stop", Results.Empty);

R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_242, (V)"voices")}, new Results(v_260)));
} else {
return R.Join(new V[] {(V)false}, new Results((V)"global voice cap"));
}
}
var values_267 = R.Join(new V[] {}, new Results((V)1.0d));
V v_268 = values_267[0];
if (R.Truth(R.And(R.And((V)(R.Get(v_247, (V)"distance") > (V)0.0d), () => v_244), () => v_245))) {
var values_269 = R.Join(new V[] {(V)(R.Get(v_244, (V)"x") - R.Get(v_245, (V)"x"))}, new Results((V)(R.Get(v_244, (V)"y") - R.Get(v_245, (V)"y"))));
V v_270 = values_269[0];
V v_271 = values_269[1];
var values_272 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"sqrt"), R.Join(new V[] {}, new Results((V)((V)(v_270 * v_270) + (V)(v_271 * v_271))))));
V v_273 = values_272[0];
if (R.Truth((V)(v_273 > R.Get(v_247, (V)"distance")))) {
return R.Join(new V[] {(V)false}, new Results((V)"out of range"));
}
var values_274 = R.Join(new V[] {}, new Results((V)((V)1.0d - (V)(v_273 / (V)(R.Get(v_247, (V)"distance") + (V)1.0d)))));
v_268 = values_274[0];
}
var values_275 = R.Join(new V[] {}, R.Invoke(v_242, "select_variant", R.Join(new V[] {}, new Results(v_247))));
V v_276 = values_275[0];
var values_277 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_238}, new Results(R.Get(R.Get(v_242, (V)"templates"), v_276)))));
V v_278 = values_277[0];
V v_279 = values_277[1];
if (R.Truth((V)(!R.Truth(v_278)))) {
var values_280 = R.Join(new V[] {}, new Results(R.Concat((V)"audio clone failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_279))).First)));
var obj_281 = v_242;
V key_282 = (V)"status";
R.Set(obj_281, key_282, values_280[0]);

return R.Join(new V[] {(V)false}, new Results(R.Get(v_242, (V)"status")));
}
var values_283 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_247, (V)"volume") * R.Get(v_249, (V)"volume")) * v_268)));
V v_284 = values_283[0];
R.Invoke(v_279, "setVolume", R.Join(new V[] {}, new Results((V)(v_284 * R.Get(v_242, (V)"user_effects")))));
R.Invoke(v_279, "setPitch", R.Join(new V[] {}, new Results(R.Get(v_247, (V)"pitch"))));
R.Invoke(v_279, "setLooping", R.Join(new V[] {}, new Results(R.Get(v_247, (V)"loop"))));
var values_285 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_279, (V)"play")}, new Results(v_279))));
V v_286 = values_285[0];
V v_287 = values_285[1];
if (R.Truth((V)(!R.Truth(v_286)))) {
var values_288 = R.Join(new V[] {}, new Results(R.Concat((V)"audio play failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_287))).First)));
var obj_289 = v_242;
V key_290 = (V)"status";
R.Set(obj_289, key_290, values_288[0]);

return R.Join(new V[] {(V)false}, new Results(R.Get(v_242, (V)"status")));
}
var values_291 = R.Join(new V[] {}, new Results(R.Get(v_242, (V)"elapsed")));
var obj_292 = R.Get(v_242, (V)"last_played");
V key_293 = v_243;
R.Set(obj_292, key_293, values_291[0]);
var values_294 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"source", v_279), new Entry((V)"cue_id", v_243), new Entry((V)"priority", R.Get(v_247, (V)"priority")), new Entry((V)"base_volume", v_284))));
var obj_295 = R.Get(v_242, (V)"voices");
V key_296 = (V)((V)R.Length(R.Get(v_242, (V)"voices")) + (V)1.0d);
R.Set(obj_295, key_296, values_294[0]);
var values_297 = R.Join(new V[] {}, new Results((V)(R.Get(v_242, (V)"played_count") + (V)1.0d)));
var obj_298 = v_242;
V key_299 = (V)"played_count";
R.Set(obj_298, key_299, values_297[0]);
return R.Join(new V[] {(V)true}, new Results(v_276));
return Results.Empty;
})));
R.Set(v_12, "event", new V((RuleFunction)(args_300 => {
V v_301 = args_300[0];
V v_302 = args_300[1];
V v_303 = args_300[2];
var values_304 = R.Join(new V[] {}, new Results(R.And(v_303, () => R.Get(v_303, (V)"player_id"))));
V v_305 = values_304[0];
var values_306 = Results.Empty;
V v_307 = values_306[0];
if (R.Truth(R.And(v_303, () => v_305))) {
foreach (var row_308 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_303, (V)"entities"), () => R.Table()))))))) {
V v_309 = row_308[0];
V v_310 = row_308[1];
if (R.Truth((V)(R.Get(v_310, (V)"id") == v_305))) {
var values_311 = R.Join(new V[] {}, new Results(R.Get(v_310, (V)"position")));
v_307 = values_311[0];

break;
}
}
}
var values_312 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_302, (V)"x"), () => R.Table(new Entry((V)"x", R.Get(v_302, (V)"x")), new Entry((V)"y", R.Get(v_302, (V)"y")))), () => R.Get(v_302, (V)"source_position"))));
V v_313 = values_312[0];
var values_314 = R.Join(new V[] {}, new Results(R.Table()));
V v_315 = values_314[0];
foreach (var row_316 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"cues_for_event"), R.Join(new V[] {v_302}, new Results(v_303)))))))) {
V v_317 = row_316[0];
V v_318 = row_316[1];
var values_319 = R.Join(new V[] {}, R.Invoke(v_301, "emit", R.Join(new V[] {v_318, v_313}, new Results(v_307))));
V v_320 = values_319[0];
V v_321 = values_319[1];
if (R.Truth(v_320)) {
var values_322 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"cue_id", v_318), new Entry((V)"asset_id", v_321))));
var obj_323 = v_315;
V key_324 = (V)((V)R.Length(v_315) + (V)1.0d);
R.Set(obj_323, key_324, values_322[0]);
}
}
return R.Join(new V[] {}, new Results(v_315));
return Results.Empty;
})));
R.Set(v_12, "set_scene", new V((RuleFunction)(args_325 => {
V v_326 = args_325[0];
V v_327 = args_325[1];
if (R.Truth((V)(R.Get(v_326, (V)"scene_id") == v_327))) {
return R.Join(new V[] {(V)false}, new Results((V)"already playing scene"));
}
var values_328 = R.Join(new V[] {}, new Results(v_327));
var obj_329 = v_326;
V key_330 = (V)"scene_id";
R.Set(obj_329, key_330, values_328[0]);
var values_331 = R.Join(new V[] {}, new Results(R.And(R.Get(v_326, (V)"bindings"), () => R.Get(R.Get(v_326, (V)"bindings"), v_327))));
V v_332 = values_331[0];
if (R.Truth((V)(!R.Truth(v_332)))) {
if (R.Truth(R.Get(v_326, (V)"music"))) {
R.Invoke(R.Get(R.Get(v_326, (V)"music"), (V)"source"), "stop", Results.Empty);
}
var values_333 = R.Join(new V[] {}, new Results(V.Nil));
var obj_334 = v_326;
V key_335 = (V)"music";
R.Set(obj_334, key_335, values_333[0]);
return R.Join(new V[] {(V)false}, new Results((V)"scene unbound"));
}
var values_336 = R.Join(new V[] {}, R.Invoke(v_326, "select_variant", R.Join(new V[] {}, new Results(v_332))));
V v_337 = values_336[0];
var values_338 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_238}, new Results(R.Get(R.Get(v_326, (V)"templates"), v_337)))));
V v_339 = values_338[0];
V v_340 = values_338[1];
if (R.Truth((V)(!R.Truth(v_339)))) {
var values_341 = R.Join(new V[] {}, new Results(R.Concat((V)"music clone failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_340))).First)));
var obj_342 = v_326;
V key_343 = (V)"status";
R.Set(obj_342, key_343, values_341[0]);

return R.Join(new V[] {(V)false}, new Results(R.Get(v_326, (V)"status")));
}
var values_344 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(R.Get(v_326, (V)"pack"), (V)"mix"), R.Call(v_55, R.Join(new V[] {}, new Results(v_327))).First), () => R.Table(new Entry((V)"volume", (V)1.0d), new Entry((V)"mute", (V)false)))));
V v_345 = values_344[0];
var values_346 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_345, (V)"mute"), () => (V)0.0d), () => (V)(R.Get(v_332, (V)"volume") * R.Get(v_345, (V)"volume")))));
V v_347 = values_346[0];
var values_348 = R.Join(new V[] {}, new Results((V)(v_347 * R.Get(v_326, (V)"user_music"))));
V v_349 = values_348[0];
var values_350 = R.Join(new V[] {}, new Results(R.Get(v_332, (V)"transition")));
V v_351 = values_350[0];
var values_352 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_351, (V)"mode") == (V)"instant"), () => (V)0.0d), () => (V)(R.Get(v_351, (V)"duration_ms") / (V)1000.0d))));
V v_353 = values_352[0];
var values_354 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_351, (V)"mode") == (V)"fade"), () => (V)(v_353 / (V)2.0d)), () => v_353)));
V v_355 = values_354[0];
var values_356 = R.Join(new V[] {}, new Results(R.Or(R.And(R.And((V)(R.Get(v_351, (V)"mode") == (V)"fade"), () => R.Get(v_326, (V)"music")), () => (V)(v_353 / (V)2.0d)), () => (V)0.0d)));
V v_357 = values_356[0];
R.Invoke(v_340, "setLooping", R.Join(new V[] {}, new Results(R.Get(v_332, (V)"loop"))));
R.Invoke(v_340, "setPitch", R.Join(new V[] {}, new Results(R.Get(v_332, (V)"pitch"))));
R.Invoke(v_340, "setVolume", R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_355 > (V)0.0d), () => (V)0.0d), () => v_349))));
var values_358 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_340, (V)"play")}, new Results(v_340))));
V v_359 = values_358[0];
V v_360 = values_358[1];
if (R.Truth((V)(!R.Truth(v_359)))) {
var values_361 = R.Join(new V[] {}, new Results(R.Concat((V)"music play failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_360))).First)));
var obj_362 = v_326;
V key_363 = (V)"status";
R.Set(obj_362, key_363, values_361[0]);

return R.Join(new V[] {(V)false}, new Results(R.Get(v_326, (V)"status")));
}
if (R.Truth(R.Get(v_326, (V)"retiring_music"))) {
R.Invoke(R.Get(R.Get(v_326, (V)"retiring_music"), (V)"source"), "stop", Results.Empty);
}
var values_364 = R.Join(new V[] {}, new Results(R.Get(v_326, (V)"music")));
var obj_365 = v_326;
V key_366 = (V)"retiring_music";
R.Set(obj_365, key_366, values_364[0]);
if (R.Truth(R.Get(v_326, (V)"retiring_music"))) {
var values_367 = R.Join(new V[] {}, new Results(v_355));
var obj_368 = R.Get(v_326, (V)"retiring_music");
V key_369 = (V)"retire_duration";
R.Set(obj_368, key_369, values_367[0]);
}
if (R.Truth(R.And(R.Get(v_326, (V)"retiring_music"), () => (V)(R.Get(v_351, (V)"mode") == (V)"instant")))) {
R.Invoke(R.Get(R.Get(v_326, (V)"retiring_music"), (V)"source"), "stop", Results.Empty);

var values_370 = R.Join(new V[] {}, new Results(V.Nil));
var obj_371 = v_326;
V key_372 = (V)"retiring_music";
R.Set(obj_371, key_372, values_370[0]);
}
var values_373 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"source", v_340), new Entry((V)"target", v_349), new Entry((V)"base_volume", v_347), new Entry((V)"duration", v_355), new Entry((V)"delay", v_357), new Entry((V)"elapsed", (V)0.0d))));
var obj_374 = v_326;
V key_375 = (V)"music";
R.Set(obj_374, key_375, values_373[0]);
var values_376 = R.Join(new V[] {}, new Results((V)(R.Get(v_326, (V)"played_count") + (V)1.0d)));
var obj_377 = v_326;
V key_378 = (V)"played_count";
R.Set(obj_377, key_378, values_376[0]);
return R.Join(new V[] {(V)true}, new Results(v_337));
return Results.Empty;
})));
R.Set(v_12, "set_ambience", new V((RuleFunction)(args_379 => {
V v_380 = args_379[0];
V v_381 = args_379[1];
if (R.Truth((V)(R.Get(v_380, (V)"ambient_id") == v_381))) {
return R.Join(new V[] {(V)false}, new Results((V)"already playing ambience"));
}
var values_382 = R.Join(new V[] {}, new Results(v_381));
var obj_383 = v_380;
V key_384 = (V)"ambient_id";
R.Set(obj_383, key_384, values_382[0]);
var values_385 = R.Join(new V[] {}, new Results(R.And(R.Get(v_380, (V)"bindings"), () => R.Get(R.Get(v_380, (V)"bindings"), v_381))));
V v_386 = values_385[0];
if (R.Truth((V)(!R.Truth(v_386)))) {
if (R.Truth(R.Get(v_380, (V)"ambience"))) {
R.Invoke(R.Get(R.Get(v_380, (V)"ambience"), (V)"source"), "stop", Results.Empty);
}
var values_387 = R.Join(new V[] {}, new Results(V.Nil));
var obj_388 = v_380;
V key_389 = (V)"ambience";
R.Set(obj_388, key_389, values_387[0]);
return R.Join(new V[] {(V)false}, new Results((V)"ambience unbound"));
}
var values_390 = R.Join(new V[] {}, R.Invoke(v_380, "select_variant", R.Join(new V[] {}, new Results(v_386))));
V v_391 = values_390[0];
var values_392 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_238}, new Results(R.Get(R.Get(v_380, (V)"templates"), v_391)))));
V v_393 = values_392[0];
V v_394 = values_392[1];
if (R.Truth((V)(!R.Truth(v_393)))) {
var values_395 = R.Join(new V[] {}, new Results(R.Concat((V)"ambience clone failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_394))).First)));
var obj_396 = v_380;
V key_397 = (V)"status";
R.Set(obj_396, key_397, values_395[0]);
return R.Join(new V[] {(V)false}, new Results(R.Get(v_380, (V)"status")));
}
var values_398 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(R.Get(v_380, (V)"pack"), (V)"mix"), R.Call(v_55, R.Join(new V[] {}, new Results(v_381))).First), () => R.Table(new Entry((V)"volume", (V)1.0d), new Entry((V)"mute", (V)false)))));
V v_399 = values_398[0];
var values_400 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_399, (V)"mute"), () => (V)0.0d), () => (V)(R.Get(v_386, (V)"volume") * R.Get(v_399, (V)"volume")))));
V v_401 = values_400[0];
var values_402 = R.Join(new V[] {}, new Results((V)(v_401 * R.Get(v_380, (V)"user_music"))));
V v_403 = values_402[0];
var values_404 = R.Join(new V[] {}, new Results(R.Get(v_386, (V)"transition")));
V v_405 = values_404[0];
var values_406 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_405, (V)"mode") == (V)"instant"), () => (V)0.0d), () => (V)(R.Get(v_405, (V)"duration_ms") / (V)1000.0d))));
V v_407 = values_406[0];
var values_408 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_405, (V)"mode") == (V)"fade"), () => (V)(v_407 / (V)2.0d)), () => v_407)));
V v_409 = values_408[0];
var values_410 = R.Join(new V[] {}, new Results(R.Or(R.And(R.And((V)(R.Get(v_405, (V)"mode") == (V)"fade"), () => R.Get(v_380, (V)"ambience")), () => (V)(v_407 / (V)2.0d)), () => (V)0.0d)));
V v_411 = values_410[0];
R.Invoke(v_394, "setLooping", R.Join(new V[] {}, new Results(R.Get(v_386, (V)"loop"))));
R.Invoke(v_394, "setPitch", R.Join(new V[] {}, new Results(R.Get(v_386, (V)"pitch"))));
R.Invoke(v_394, "setVolume", R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_409 > (V)0.0d), () => (V)0.0d), () => v_403))));
var values_412 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_394, (V)"play")}, new Results(v_394))));
V v_413 = values_412[0];
V v_414 = values_412[1];
if (R.Truth((V)(!R.Truth(v_413)))) {
var values_415 = R.Join(new V[] {}, new Results(R.Concat((V)"ambience play failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_414))).First)));
var obj_416 = v_380;
V key_417 = (V)"status";
R.Set(obj_416, key_417, values_415[0]);
return R.Join(new V[] {(V)false}, new Results(R.Get(v_380, (V)"status")));
}
if (R.Truth(R.Get(v_380, (V)"retiring_ambience"))) {
R.Invoke(R.Get(R.Get(v_380, (V)"retiring_ambience"), (V)"source"), "stop", Results.Empty);
}
var values_418 = R.Join(new V[] {}, new Results(R.Get(v_380, (V)"ambience")));
var obj_419 = v_380;
V key_420 = (V)"retiring_ambience";
R.Set(obj_419, key_420, values_418[0]);
if (R.Truth(R.Get(v_380, (V)"retiring_ambience"))) {
var values_421 = R.Join(new V[] {}, new Results(v_409));
var obj_422 = R.Get(v_380, (V)"retiring_ambience");
V key_423 = (V)"retire_duration";
R.Set(obj_422, key_423, values_421[0]);
}
if (R.Truth(R.And(R.Get(v_380, (V)"retiring_ambience"), () => (V)(v_407 == (V)0.0d)))) {
R.Invoke(R.Get(R.Get(v_380, (V)"retiring_ambience"), (V)"source"), "stop", Results.Empty);

var values_424 = R.Join(new V[] {}, new Results(V.Nil));
var obj_425 = v_380;
V key_426 = (V)"retiring_ambience";
R.Set(obj_425, key_426, values_424[0]);
}
var values_427 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"source", v_394), new Entry((V)"target", v_403), new Entry((V)"base_volume", v_401), new Entry((V)"duration", v_409), new Entry((V)"delay", v_411), new Entry((V)"elapsed", (V)0.0d))));
var obj_428 = v_380;
V key_429 = (V)"ambience";
R.Set(obj_428, key_429, values_427[0]);
var values_430 = R.Join(new V[] {}, new Results((V)(R.Get(v_380, (V)"played_count") + (V)1.0d)));
var obj_431 = v_380;
V key_432 = (V)"played_count";
R.Set(obj_431, key_432, values_430[0]);
return R.Join(new V[] {(V)true}, new Results(v_391));
return Results.Empty;
})));
R.Set(v_12, "update", new V((RuleFunction)(args_433 => {
V v_434 = args_433[0];
V v_435 = args_433[1];
if (R.Truth(R.Get(v_434, (V)"paused"))) {
return Results.Empty;
}
var values_436 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Or(v_435, () => (V)0.0d)}, new Results((V)0.25d))))));
v_435 = values_436[0];
var values_437 = R.Join(new V[] {}, new Results((V)(R.Get(v_434, (V)"elapsed") + v_435)));
var obj_438 = v_434;
V key_439 = (V)"elapsed";
R.Set(obj_438, key_439, values_437[0]);
if (R.Truth(R.And(R.Get(v_434, (V)"music"), () => (V)(R.Get(R.Get(v_434, (V)"music"), (V)"duration") > (V)0.0d)))) {
var values_440 = R.Join(new V[] {}, new Results(R.Get(v_434, (V)"music")));
V v_441 = values_440[0];
if (R.Truth(R.And(R.Get(v_441, (V)"delay"), () => (V)(R.Get(v_441, (V)"delay") > (V)0.0d)))) {
var values_442 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(R.Get(v_441, (V)"delay") - v_435)))));
var obj_443 = v_441;
V key_444 = (V)"delay";
R.Set(obj_443, key_444, values_442[0]);
} else {
var values_445 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_441, (V)"duration")}, new Results((V)(R.Get(v_441, (V)"elapsed") + v_435)))));
var obj_446 = v_441;
V key_447 = (V)"elapsed";
R.Set(obj_446, key_447, values_445[0]);
R.Invoke(R.Get(v_441, (V)"source"), "setVolume", R.Join(new V[] {}, new Results((V)((V)(R.Get(v_441, (V)"target") * R.Get(v_441, (V)"elapsed")) / R.Get(v_441, (V)"duration")))));
}
}
if (R.Truth(R.Get(v_434, (V)"retiring_music"))) {
var values_448 = R.Join(new V[] {}, new Results(R.Get(v_434, (V)"retiring_music")));
V v_449 = values_448[0];
var values_450 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_449, (V)"retire_duration"), () => (V)0.0d)));
V v_451 = values_450[0];
var values_452 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_449, (V)"elapsed"), () => (V)0.0d) + v_435)));
var obj_453 = v_449;
V key_454 = (V)"elapsed";
R.Set(obj_453, key_454, values_452[0]);
if (R.Truth(R.Or((V)(v_451 <= (V)0.0d), () => (V)(R.Get(v_449, (V)"elapsed") >= v_451)))) {
R.Invoke(R.Get(v_449, (V)"source"), "stop", Results.Empty);

var values_455 = R.Join(new V[] {}, new Results(V.Nil));
var obj_456 = v_434;
V key_457 = (V)"retiring_music";
R.Set(obj_456, key_457, values_455[0]);
} else {
R.Invoke(R.Get(v_449, (V)"source"), "setVolume", R.Join(new V[] {}, new Results((V)(R.Get(v_449, (V)"target") * (V)((V)1.0d - (V)(R.Get(v_449, (V)"elapsed") / v_451))))));
}
}
if (R.Truth(R.And(R.Get(v_434, (V)"ambience"), () => (V)(R.Get(R.Get(v_434, (V)"ambience"), (V)"duration") > (V)0.0d)))) {
var values_458 = R.Join(new V[] {}, new Results(R.Get(v_434, (V)"ambience")));
V v_459 = values_458[0];
if (R.Truth(R.And(R.Get(v_459, (V)"delay"), () => (V)(R.Get(v_459, (V)"delay") > (V)0.0d)))) {
var values_460 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(R.Get(v_459, (V)"delay") - v_435)))));
var obj_461 = v_459;
V key_462 = (V)"delay";
R.Set(obj_461, key_462, values_460[0]);
} else {
var values_463 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_459, (V)"duration")}, new Results((V)(R.Get(v_459, (V)"elapsed") + v_435)))));
var obj_464 = v_459;
V key_465 = (V)"elapsed";
R.Set(obj_464, key_465, values_463[0]);
R.Invoke(R.Get(v_459, (V)"source"), "setVolume", R.Join(new V[] {}, new Results((V)((V)(R.Get(v_459, (V)"target") * R.Get(v_459, (V)"elapsed")) / R.Get(v_459, (V)"duration")))));
}
}
if (R.Truth(R.Get(v_434, (V)"retiring_ambience"))) {
var values_466 = R.Join(new V[] {}, new Results(R.Get(v_434, (V)"retiring_ambience")));
V v_467 = values_466[0];
var values_468 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_467, (V)"retire_duration"), () => (V)0.0d)));
V v_469 = values_468[0];
var values_470 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_467, (V)"elapsed"), () => (V)0.0d) + v_435)));
var obj_471 = v_467;
V key_472 = (V)"elapsed";
R.Set(obj_471, key_472, values_470[0]);
if (R.Truth(R.Or((V)(v_469 <= (V)0.0d), () => (V)(R.Get(v_467, (V)"elapsed") >= v_469)))) {
R.Invoke(R.Get(v_467, (V)"source"), "stop", Results.Empty);

var values_473 = R.Join(new V[] {}, new Results(V.Nil));
var obj_474 = v_434;
V key_475 = (V)"retiring_ambience";
R.Set(obj_474, key_475, values_473[0]);
} else {
R.Invoke(R.Get(v_467, (V)"source"), "setVolume", R.Join(new V[] {}, new Results((V)(R.Get(v_467, (V)"target") * (V)((V)1.0d - (V)(R.Get(v_467, (V)"elapsed") / v_469))))));
}
}
return Results.Empty;
})));
R.Set(v_12, "set_paused", new V((RuleFunction)(args_476 => {
V v_477 = args_476[0];
V v_478 = args_476[1];
if (R.Truth((V)(R.Get(v_477, (V)"paused") == v_478))) {
return Results.Empty;
}
var values_479 = R.Join(new V[] {}, new Results(v_478));
var obj_480 = v_477;
V key_481 = (V)"paused";
R.Set(obj_480, key_481, values_479[0]);
V v_482 = V.Nil;
v_482 = new V((RuleFunction)(args_483 => {
V v_484 = args_483[0];
if (R.Truth((V)(!R.Truth(v_484)))) {
return Results.Empty;
}
if (R.Truth(v_478)) {
var values_485 = R.Join(new V[] {}, R.Invoke(R.Get(v_484, (V)"source"), "isPlaying", Results.Empty));
var obj_486 = v_484;
V key_487 = (V)"resume";
R.Set(obj_486, key_487, values_485[0]);
if (R.Truth(R.Get(v_484, (V)"resume"))) {
R.Invoke(R.Get(v_484, (V)"source"), "pause", Results.Empty);
}
} else if (R.Truth(R.Get(v_484, (V)"resume"))) {
R.Invoke(R.Get(v_484, (V)"source"), "play", Results.Empty);

var values_488 = R.Join(new V[] {}, new Results(V.Nil));
var obj_489 = v_484;
V key_490 = (V)"resume";
R.Set(obj_489, key_490, values_488[0]);
}
return Results.Empty;
}));
R.Call(v_482, R.Join(new V[] {}, new Results(R.Get(v_477, (V)"music"))));

R.Call(v_482, R.Join(new V[] {}, new Results(R.Get(v_477, (V)"retiring_music"))));
R.Call(v_482, R.Join(new V[] {}, new Results(R.Get(v_477, (V)"ambience"))));

R.Call(v_482, R.Join(new V[] {}, new Results(R.Get(v_477, (V)"retiring_ambience"))));
foreach (var row_491 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_477, (V)"voices"))))))) {
V v_492 = row_491[0];
V v_493 = row_491[1];
R.Call(v_482, R.Join(new V[] {}, new Results(v_493)));
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_12));
return Results.Empty;
} }
