// Static C# port of src/audio_cues.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 20b22316b282b06fc7cfaeb26e32dad560612450ba617b4b183d8b60604d9a3f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_audio_cues { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table()));
V v_10 = values_9[0];
var values_11 = Results.Empty;
V v_12 = values_11[0];
V v_13 = values_11[1];
V v_14 = V.Nil;
v_14 = new V((RuleFunction)(args_15 => {
V v_16 = args_15[0];
return R.Join(new V[] {}, R.Invoke(v_16, "match", R.Join(new V[] {}, new Results((V)"/([^/]+)$"))));
return Results.Empty;
}));
R.Set(v_10, (V)"room_cue", new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(v_19 == (V)"ambient"), () => (V)(v_19 == (V)"music"))}, new Results((V)"invalid room audio kind")));
var values_20 = R.Join(new V[] {}, new Results((V)0.0d));
V v_21 = values_20[0];
{ double end_24 = R.Num((V)R.Length(v_18)), step_25 = R.Num((V)1.0d);
for (double i_23 = R.Num((V)1.0d); step_25 > 0 ? i_23 <= end_24 : i_23 >= end_24; i_23 += step_25) {
V v_22 = i_23;
var values_26 = R.Join(new V[] {}, new Results((V)((V)((V)(v_21 * (V)33.0d) + R.Invoke(v_18, "byte", R.Join(new V[] {}, new Results(v_22))).First) % (V)4294967296.0d)));
v_21 = values_26[0];
}
}
return R.Join(new V[] {}, new Results(R.Concat((V)"studio:cue/room/", R.Concat(R.Call(R.Get(rt.Global("string"), (V)"format"), R.Join(new V[] {(V)"%08x"}, new Results(v_21))).First, R.Concat((V)"/", R.Concat(R.Invoke(R.Call(v_14, R.Join(new V[] {}, new Results(v_18))).First, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)40.0d))).First, R.Concat((V)"/", v_19)))))));
return Results.Empty;
})));
R.Set(v_10, (V)"list", new V((RuleFunction)(args_27 => {
V v_28 = args_27[0];
if (R.Truth(R.And((V)(!R.Truth(v_28)), () => v_12))) {
return R.Join(new V[] {}, new Results(v_12));
}
var values_29 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_30 = values_29[0];
V v_31 = values_29[1];
V v_32 = V.Nil;
v_32 = new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
V v_35 = args_33[1];
V v_36 = args_33[2];
V v_37 = args_33[3];
V v_38 = args_33[4];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_31, v_34)))}, new Results((V)"duplicate audio cue")));
var values_39 = R.Join(new V[] {}, new Results((V)true));
var obj_40 = v_31;
V key_41 = v_34;
R.Set(obj_40, key_41, values_39[0]);
var values_42 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_34), new Entry((V)"name", v_35), new Entry((V)"category", v_36), new Entry((V)"context_id", R.Or(v_37, () => (V)"")), new Entry((V)"event", R.Or(v_38, () => (V)"")))));
var obj_43 = v_30;
V key_44 = (V)((V)R.Length(v_30) + (V)1.0d);
R.Set(obj_43, key_44, values_42[0]);
return Results.Empty;
}));
foreach (var row_45 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"player/move"), new Entry((V)2, (V)"Player move"), new Entry((V)3, (V)"player"), new Entry((V)4, (V)"Moved"))), new Entry((V)2, R.Table(new Entry((V)1, (V)"player/attack"), new Entry((V)2, (V)"Player attack"), new Entry((V)3, (V)"player"), new Entry((V)4, (V)"AttackPerformed"))), new Entry((V)3, R.Table(new Entry((V)1, (V)"player/dash"), new Entry((V)2, (V)"Player Dash"), new Entry((V)3, (V)"player"), new Entry((V)4, (V)"Moved"))), new Entry((V)4, R.Table(new Entry((V)1, (V)"player/damage"), new Entry((V)2, (V)"Player damage"), new Entry((V)3, (V)"player"), new Entry((V)4, (V)"DamageTaken"))), new Entry((V)5, R.Table(new Entry((V)1, (V)"player/death"), new Entry((V)2, (V)"Player death"), new Entry((V)3, (V)"player"), new Entry((V)4, (V)"Died"))), new Entry((V)6, R.Table(new Entry((V)1, (V)"enemy/move"), new Entry((V)2, (V)"Enemy movement"), new Entry((V)3, (V)"enemy"), new Entry((V)4, (V)"Moved"))), new Entry((V)7, R.Table(new Entry((V)1, (V)"enemy/telegraph"), new Entry((V)2, (V)"Enemy telegraph"), new Entry((V)3, (V)"enemy"), new Entry((V)4, (V)"WindupStarted"))), new Entry((V)8, R.Table(new Entry((V)1, (V)"enemy/attack"), new Entry((V)2, (V)"Enemy attack"), new Entry((V)3, (V)"enemy"), new Entry((V)4, (V)"AttackPerformed"))), new Entry((V)9, R.Table(new Entry((V)1, (V)"enemy/hit"), new Entry((V)2, (V)"Enemy hit"), new Entry((V)3, (V)"enemy"), new Entry((V)4, (V)"DamageTaken"))), new Entry((V)10, R.Table(new Entry((V)1, (V)"enemy/death"), new Entry((V)2, (V)"Enemy death"), new Entry((V)3, (V)"enemy"), new Entry((V)4, (V)"Died"))), new Entry((V)11, R.Table(new Entry((V)1, (V)"boss/arrival"), new Entry((V)2, (V)"Boss arrival"), new Entry((V)3, (V)"boss"), new Entry((V)4, (V)"FinalSanctumEntered"))), new Entry((V)12, R.Table(new Entry((V)1, (V)"boss/phase"), new Entry((V)2, (V)"Boss phase"), new Entry((V)3, (V)"boss"), new Entry((V)4, (V)"RegentPhaseChanged"))), new Entry((V)13, R.Table(new Entry((V)1, (V)"boss/defeat"), new Entry((V)2, (V)"Boss defeat"), new Entry((V)3, (V)"boss"), new Entry((V)4, (V)"RegentDefeated"))), new Entry((V)14, R.Table(new Entry((V)1, (V)"boon/activate"), new Entry((V)2, (V)"Any boon activation"), new Entry((V)3, (V)"boon"), new Entry((V)4, (V)"BoonActivated"))), new Entry((V)15, R.Table(new Entry((V)1, (V)"status/burning"), new Entry((V)2, (V)"Burning tick"), new Entry((V)3, (V)"boon"), new Entry((V)4, (V)"StatusTick"))), new Entry((V)16, R.Table(new Entry((V)1, (V)"terrain/step"), new Entry((V)2, (V)"Terrain footstep"), new Entry((V)3, (V)"terrain"), new Entry((V)4, (V)"TileEntered"))), new Entry((V)17, R.Table(new Entry((V)1, (V)"terrain/hazard"), new Entry((V)2, (V)"Terrain hazard"), new Entry((V)3, (V)"terrain"), new Entry((V)4, (V)"TerrainTriggered"))), new Entry((V)18, R.Table(new Entry((V)1, (V)"terrain/stairs"), new Entry((V)2, (V)"Stairs"), new Entry((V)3, (V)"terrain"), new Entry((V)4, (V)"FloorChanged"))), new Entry((V)19, R.Table(new Entry((V)1, (V)"world/door"), new Entry((V)2, (V)"Door opens"), new Entry((V)3, (V)"world"), new Entry((V)4, (V)"DoorOpened"))), new Entry((V)20, R.Table(new Entry((V)1, (V)"world/chest"), new Entry((V)2, (V)"Chest claimed"), new Entry((V)3, (V)"world"), new Entry((V)4, (V)"ChestClaimed"))), new Entry((V)21, R.Table(new Entry((V)1, (V)"world/secret"), new Entry((V)2, (V)"Secret discovered"), new Entry((V)3, (V)"world"), new Entry((V)4, (V)"SecretDiscovered"))), new Entry((V)22, R.Table(new Entry((V)1, (V)"world/currency"), new Entry((V)2, (V)"Currency collected"), new Entry((V)3, (V)"world"), new Entry((V)4, (V)"CurrencyCollected"))), new Entry((V)23, R.Table(new Entry((V)1, (V)"world/pickup"), new Entry((V)2, (V)"World pickup"), new Entry((V)3, (V)"world"), new Entry((V)4, (V)"CurrencyCollected"))), new Entry((V)24, R.Table(new Entry((V)1, (V)"ui/hover"), new Entry((V)2, (V)"UI hover"), new Entry((V)3, (V)"ui"), new Entry((V)4, (V)""))), new Entry((V)25, R.Table(new Entry((V)1, (V)"ui/confirm"), new Entry((V)2, (V)"UI confirm"), new Entry((V)3, (V)"ui"), new Entry((V)4, (V)""))), new Entry((V)26, R.Table(new Entry((V)1, (V)"ui/cancel"), new Entry((V)2, (V)"UI cancel"), new Entry((V)3, (V)"ui"), new Entry((V)4, (V)""))), new Entry((V)27, R.Table(new Entry((V)1, (V)"ui/level_up"), new Entry((V)2, (V)"Level up"), new Entry((V)3, (V)"ui"), new Entry((V)4, (V)"StatIncreased"))), new Entry((V)28, R.Table(new Entry((V)1, (V)"ui/achievement"), new Entry((V)2, (V)"Achievement"), new Entry((V)3, (V)"ui"), new Entry((V)4, (V)"AchievementEarned"))), new Entry((V)29, R.Table(new Entry((V)1, (V)"ui/victory"), new Entry((V)2, (V)"Victory"), new Entry((V)3, (V)"ui"), new Entry((V)4, (V)"Victory"))), new Entry((V)30, R.Table(new Entry((V)1, (V)"ui/defeat"), new Entry((V)2, (V)"Defeat"), new Entry((V)3, (V)"ui"), new Entry((V)4, (V)"Defeat"))), new Entry((V)31, R.Table(new Entry((V)1, (V)"music/title"), new Entry((V)2, (V)"Title music"), new Entry((V)3, (V)"music"), new Entry((V)4, (V)""))), new Entry((V)32, R.Table(new Entry((V)1, (V)"music/final_boss"), new Entry((V)2, (V)"Final boss music"), new Entry((V)3, (V)"music"), new Entry((V)4, (V)""))), new Entry((V)33, R.Table(new Entry((V)1, (V)"music/victory"), new Entry((V)2, (V)"Victory music"), new Entry((V)3, (V)"music"), new Entry((V)4, (V)""))), new Entry((V)34, R.Table(new Entry((V)1, (V)"music/defeat"), new Entry((V)2, (V)"Defeat music"), new Entry((V)3, (V)"music"), new Entry((V)4, (V)"")))))))))) {
V v_46 = row_45[0];
V v_47 = row_45[1];
R.Call(v_32, R.Join(new V[] {R.Concat((V)"core:cue/", R.Get(v_47, (V)1.0d)), R.Get(v_47, (V)2.0d), R.Get(v_47, (V)3.0d), (V)""}, new Results(R.Get(v_47, (V)4.0d))));
}
foreach (var row_48 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_4, (V)"order"))))))) {
V v_49 = row_48[0];
V v_50 = row_48[1];
var values_51 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(v_50))));
V v_52 = values_51[0];
R.Call(v_32, R.Join(new V[] {R.Concat((V)"core:cue/class/", R.Concat(R.Call(v_14, R.Join(new V[] {}, new Results(v_50))).First, (V)"/select")), R.Concat(R.Get(v_52, (V)"name"), (V)" selected"), (V)"class", v_50}, new Results((V)"title")));
foreach (var row_53 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_52, (V)"basic_id")), new Entry((V)2, R.Get(v_52, (V)"special_id"))))))))) {
V v_54 = row_53[0];
V v_55 = row_53[1];
R.Call(v_32, R.Join(new V[] {R.Concat((V)"core:cue/ability/", R.Call(v_14, R.Join(new V[] {}, new Results(v_55))).First), R.Concat(R.Get(v_52, (V)"name"), R.Concat((V)" ", R.Or(R.And((V)(v_55 == R.Get(v_52, (V)"basic_id")), () => R.Get(v_52, (V)"basic_name")), () => R.Get(v_52, (V)"special_name")))), (V)"ability", v_55}, new Results((V)"AttackPerformed")));
}
}
var values_56 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_57 = values_56[0];
V v_58 = values_56[1];
foreach (var row_59 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_60 = row_59[0];
V v_61 = row_59[1];
var values_62 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_61))));
V v_63 = values_62[0];
var values_64 = R.Join(new V[] {}, R.Call(v_14, R.Join(new V[] {}, new Results(v_61))));
V v_65 = values_64[0];
R.Call(v_32, R.Join(new V[] {R.Concat((V)"core:cue/music/explore/", v_65), R.Concat(R.Get(v_63, (V)"name"), (V)" exploration"), (V)"music", v_61}, new Results((V)"stage")));
R.Call(v_32, R.Join(new V[] {R.Concat((V)"core:cue/music/elite/", v_65), R.Concat(R.Get(v_63, (V)"name"), (V)" elite"), (V)"music", v_61}, new Results((V)"elite")));
R.Call(v_32, R.Join(new V[] {R.Concat((V)"core:cue/ambient/", v_65), R.Concat(R.Get(v_63, (V)"name"), (V)" ambience"), (V)"biome", v_61}, new Results((V)"stage")));
var values_66 = R.Join(new V[] {}, new Results((V)true));
var obj_67 = v_57;
V key_68 = R.Get(v_63, (V)"elite_id");
R.Set(obj_67, key_68, values_66[0]);
var values_69 = R.Join(new V[] {}, new Results((V)true));
var obj_70 = v_58;
V key_71 = R.Get(v_63, (V)"elite_id");
R.Set(obj_70, key_71, values_69[0]);
foreach (var row_72 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_63, (V)"enemy_ids"))))))) {
V v_73 = row_72[0];
V v_74 = row_72[1];
var values_75 = R.Join(new V[] {}, new Results((V)true));
var obj_76 = v_57;
V key_77 = v_74;
R.Set(obj_76, key_77, values_75[0]);
}
foreach (var row_78 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_63, (V)"pressure_enemy_ids"), () => R.Table()))))))) {
V v_79 = row_78[0];
V v_80 = row_78[1];
var values_81 = R.Join(new V[] {}, new Results((V)true));
var obj_82 = v_57;
V key_83 = v_80;
R.Set(obj_82, key_83, values_81[0]);
}
}
var values_84 = R.Join(new V[] {}, new Results((V)true));
var obj_85 = v_57;
V key_86 = (V)"core:enemy/hollow_regent";
R.Set(obj_85, key_86, values_84[0]);
var values_87 = R.Join(new V[] {}, new Results((V)true));
var obj_88 = v_57;
V key_89 = (V)"core:enemy/extraction_pursuer";
R.Set(obj_88, key_89, values_87[0]);
var values_90 = R.Join(new V[] {}, new Results(R.Table()));
V v_91 = values_90[0];

foreach (var row_92 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_57)))))) {
V v_93 = row_92[0];
var values_94 = R.Join(new V[] {}, new Results(v_93));
var obj_95 = v_91;
V key_96 = (V)((V)R.Length(v_91) + (V)1.0d);
R.Set(obj_95, key_96, values_94[0]);
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_91)));
foreach (var row_97 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_91)))))) {
V v_98 = row_97[0];
V v_99 = row_97[1];
var values_100 = R.Join(new V[] {}, R.Call(v_14, R.Join(new V[] {}, new Results(v_99))));
V v_101 = values_100[0];
foreach (var row_102 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"move"), new Entry((V)2, (V)"moves"), new Entry((V)3, (V)"Moved"))), new Entry((V)2, R.Table(new Entry((V)1, (V)"telegraph"), new Entry((V)2, (V)"winds up"), new Entry((V)3, (V)"WindupStarted"))), new Entry((V)3, R.Table(new Entry((V)1, (V)"attack"), new Entry((V)2, (V)"attacks"), new Entry((V)3, (V)"AttackPerformed"))), new Entry((V)4, R.Table(new Entry((V)1, (V)"hit"), new Entry((V)2, (V)"is hit"), new Entry((V)3, (V)"DamageTaken"))), new Entry((V)5, R.Table(new Entry((V)1, (V)"death"), new Entry((V)2, (V)"dies"), new Entry((V)3, (V)"Died")))))))))) {
V v_103 = row_102[0];
V v_104 = row_102[1];
R.Call(v_32, R.Join(new V[] {R.Concat((V)"core:cue/enemy/", R.Concat(v_101, R.Concat((V)"/", R.Get(v_104, (V)1.0d)))), R.Concat(R.Invoke(v_101, "gsub", R.Join(new V[] {(V)"_"}, new Results((V)" "))).First, R.Concat((V)" ", R.Get(v_104, (V)2.0d))), (V)"enemy", v_99}, new Results(R.Get(v_104, (V)3.0d))));
}
if (R.Truth(R.Or((V)(v_99 == (V)"core:enemy/hollow_regent"), () => R.Get(v_58, v_99)))) {
foreach (var row_105 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"arrival"), new Entry((V)2, (V)"arrives"))), new Entry((V)2, R.Table(new Entry((V)1, (V)"phase"), new Entry((V)2, (V)"changes phase"))), new Entry((V)3, R.Table(new Entry((V)1, (V)"defeat"), new Entry((V)2, (V)"falls")))))))))) {
V v_106 = row_105[0];
V v_107 = row_105[1];
R.Call(v_32, R.Join(new V[] {R.Concat((V)"core:cue/elite/", R.Concat(v_101, R.Concat((V)"/", R.Get(v_107, (V)1.0d)))), R.Concat(R.Invoke(v_101, "gsub", R.Join(new V[] {(V)"_"}, new Results((V)" "))).First, R.Concat((V)" ", R.Get(v_107, (V)2.0d))), (V)"elite", v_99}, new Results(R.Get(v_107, (V)1.0d))));
}
}
}
foreach (var row_108 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_6, (V)"order"))))))) {
V v_109 = row_108[0];
V v_110 = row_108[1];
R.Call(v_32, R.Join(new V[] {R.Concat((V)"core:cue/boon/", R.Call(v_14, R.Join(new V[] {}, new Results(v_110))).First), R.Concat(R.Get(R.Call(R.Get(v_6, (V)"get"), R.Join(new V[] {}, new Results(v_110))).First, (V)"name"), (V)" activates"), (V)"boon", v_110}, new Results((V)"BoonActivated")));
}
foreach (var row_111 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"order"))))))) {
V v_112 = row_111[0];
V v_113 = row_111[1];
var values_114 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(v_113))));
V v_115 = values_114[0];
R.Call(v_32, R.Join(new V[] {R.Concat((V)"core:cue/terrain/", R.Call(v_14, R.Join(new V[] {}, new Results(R.Get(v_115, (V)"id")))).First), R.Get(v_115, (V)"name"), (V)"terrain", R.Get(v_115, (V)"id")}, new Results((V)"TerrainTriggered")));
}
foreach (var row_116 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(v_28, () => R.Get(v_28, (V)"rooms")), () => R.Table()))))))) {
V v_117 = row_116[0];
V v_118 = row_116[1];
R.Call(v_32, R.Join(new V[] {R.Call(R.Get(v_10, (V)"room_cue"), R.Join(new V[] {R.Get(v_118, (V)"id")}, new Results((V)"ambient"))).First, R.Concat(R.Get(v_118, (V)"name"), (V)" ambience"), (V)"room", R.Get(v_118, (V)"id")}, new Results((V)"stage")));
R.Call(v_32, R.Join(new V[] {R.Call(R.Get(v_10, (V)"room_cue"), R.Join(new V[] {R.Get(v_118, (V)"id")}, new Results((V)"music"))).First, R.Concat(R.Get(v_118, (V)"name"), (V)" music"), (V)"room", R.Get(v_118, (V)"id")}, new Results((V)"stage")));
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_30}, new Results(new V((RuleFunction)(args_119 => {
V v_120 = args_119[0];
V v_121 = args_119[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_120, (V)"id") < R.Get(v_121, (V)"id"))));
return Results.Empty;
})))));
if (R.Truth((V)(!R.Truth(v_28)))) {
var values_122 = R.Join(new V[] {v_30}, new Results(R.Table()));
v_12 = values_122[0];
v_13 = values_122[1];
foreach (var row_123 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_30)))))) {
V v_124 = row_123[0];
V v_125 = row_123[1];
var values_126 = R.Join(new V[] {}, new Results(v_125));
var obj_127 = v_13;
V key_128 = R.Get(v_125, (V)"id");
R.Set(obj_127, key_128, values_126[0]);
}
}
return R.Join(new V[] {}, new Results(v_30));
return Results.Empty;
})));
R.Set(v_10, (V)"get", new V((RuleFunction)(args_129 => {
V v_130 = args_129[0];
V v_131 = args_129[1];
if (R.Truth(R.And(R.And((V)(!R.Truth(v_131)), () => v_13), () => R.Get(v_13, v_130)))) {
return R.Join(new V[] {}, new Results(R.Get(v_13, v_130)));
}
foreach (var row_132 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"list"), R.Join(new V[] {}, new Results(v_131)))))))) {
V v_133 = row_132[0];
V v_134 = row_132[1];
if (R.Truth((V)(R.Get(v_134, (V)"id") == v_130))) {
return R.Join(new V[] {}, new Results(v_134));
}
}
if (R.Truth(R.And((V)(!R.Truth(v_131)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_130))).First == (V)"string")))) {
if (R.Truth(R.Or(R.Invoke(v_130, "match", R.Join(new V[] {}, new Results((V)"^studio:cue/room/%x%x%x%x%x%x%x%x/[a-z0-9_%-]+/ambient$"))).First, () => R.Invoke(v_130, "match", R.Join(new V[] {}, new Results((V)"^studio:cue/room/%x%x%x%x%x%x%x%x/[a-z0-9_%-]+/music$"))).First))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_130), new Entry((V)"name", v_130), new Entry((V)"category", (V)"room"), new Entry((V)"context_id", (V)""), new Entry((V)"event", (V)"stage"))));
}
}
return Results.Empty;
})));
R.Set(v_10, (V)"by_category", new V((RuleFunction)(args_135 => {
V v_136 = args_135[0];
V v_137 = args_135[1];
var values_138 = R.Join(new V[] {}, new Results(R.Table()));
V v_139 = values_138[0];
foreach (var row_140 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"list"), R.Join(new V[] {}, new Results(v_137)))))))) {
V v_141 = row_140[0];
V v_142 = row_140[1];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_136)), () => (V)(v_136 == (V)"all")), () => (V)(R.Get(v_142, (V)"category") == v_136)))) {
var values_143 = R.Join(new V[] {}, new Results(v_142));
var obj_144 = v_139;
V key_145 = (V)((V)R.Length(v_139) + (V)1.0d);
R.Set(obj_144, key_145, values_143[0]);
}
}
return R.Join(new V[] {}, new Results(v_139));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_10));
return Results.Empty;
} }
