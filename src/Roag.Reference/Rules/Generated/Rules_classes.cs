// Static C# port of src/classes.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 2d8ca355c26516ec20452b288e10a3936c669bd4345a2526571a0ff99a0b9b8f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_classes { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.targeting"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_duelist"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_geomancer"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_companions"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_stormcaller"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_wayfinder"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, new Results(R.Table()));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:actor/adventurer"), new Entry((V)2, (V)"core:actor/ranger"), new Entry((V)3, (V)"core:actor/arcanist"), new Entry((V)4, (V)"core:actor/berserker"), new Entry((V)5, (V)"core:actor/sentinel"), new Entry((V)6, (V)"core:actor/alchemist"), new Entry((V)7, (V)"core:actor/shade"), new Entry((V)8, (V)"core:actor/hookrunner"), new Entry((V)9, (V)"core:actor/trapper"))));
var obj_16 = v_14;
V key_17 = (V)"order";
R.Set(obj_16, key_17, values_15[0]);
var values_18 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(R.Get(v_14, (V)"order"), (V)1.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_14, (V)"order"), (V)1.0d)), new Entry((V)"name", (V)"Adventurer"), new Entry((V)"description", (V)"Balanced sword, three-cell sweep, two-cell Dash"), new Entry((V)"hp", (V)24.0d), new Entry((V)"damage", (V)4.0d), new Entry((V)"slash_damage", (V)3.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)100.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)100.0d), new Entry((V)"sweeping_slash", (V)150.0d), new Entry((V)"dash", (V)130.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/basic_attack"), new Entry((V)"special_id", (V)"core:ability/sweeping_slash"), new Entry((V)"basic_name", (V)"Sword Strike"), new Entry((V)"special_name", (V)"Sweeping Slash"), new Entry((V)"dash_name", (V)"Dash"), new Entry((V)"ui_controls", (V)"F Sword \u00c2\u00b7 R Slash \u00c2\u00b7 X Dash"))), new Entry(R.Get(R.Get(v_14, (V)"order"), (V)2.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_14, (V)"order"), (V)2.0d)), new Entry((V)"name", (V)"Ranger"), new Entry((V)"description", (V)"Four-cell bow, piercing volley, three-cell evasive Dash"), new Entry((V)"hp", (V)20.0d), new Entry((V)"damage", (V)3.0d), new Entry((V)"slash_damage", (V)3.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)115.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)100.0d), new Entry((V)"sweeping_slash", (V)155.0d), new Entry((V)"dash", (V)115.0d))), new Entry((V)"dash_distance", (V)3.0d), new Entry((V)"basic_id", (V)"core:ability/ranger_shot"), new Entry((V)"special_id", (V)"core:ability/ranger_volley"), new Entry((V)"basic_name", (V)"Bow Shot"), new Entry((V)"special_name", (V)"Piercing Volley"), new Entry((V)"dash_name", (V)"Evasive Dash"), new Entry((V)"ui_controls", (V)"F Bow \u00c2\u00b7 R Volley \u00c2\u00b7 X Evade"))), new Entry(R.Get(R.Get(v_14, (V)"order"), (V)3.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_14, (V)"order"), (V)3.0d)), new Entry((V)"name", (V)"Arcanist"), new Entry((V)"description", (V)"Lightning bolt, fire sigil, measured phase step"), new Entry((V)"hp", (V)21.0d), new Entry((V)"damage", (V)3.0d), new Entry((V)"slash_damage", (V)3.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)100.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)110.0d), new Entry((V)"sweeping_slash", (V)170.0d), new Entry((V)"dash", (V)115.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/arcanist_bolt"), new Entry((V)"special_id", (V)"core:ability/arcanist_sigil"), new Entry((V)"basic_name", (V)"Arc Bolt"), new Entry((V)"special_name", (V)"Fire Sigil"), new Entry((V)"dash_name", (V)"Phase Step"), new Entry((V)"ui_controls", (V)"F Bolt \u00c2\u00b7 R Sigil \u00c2\u00b7 X Step"))), new Entry(R.Get(R.Get(v_14, (V)"order"), (V)4.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_14, (V)"order"), (V)4.0d)), new Entry((V)"name", (V)"Berserker"), new Entry((V)"description", (V)"Heavy crush, broad cleave, slower armored traversal"), new Entry((V)"hp", (V)32.0d), new Entry((V)"damage", (V)6.0d), new Entry((V)"slash_damage", (V)5.0d), new Entry((V)"defense", (V)1.0d), new Entry((V)"speed", (V)75.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)115.0d), new Entry((V)"basic_attack", (V)130.0d), new Entry((V)"sweeping_slash", (V)180.0d), new Entry((V)"dash", (V)165.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/berserker_crush"), new Entry((V)"special_id", (V)"core:ability/berserker_cleave"), new Entry((V)"basic_name", (V)"Crushing Blow"), new Entry((V)"special_name", (V)"Broad Cleave"), new Entry((V)"dash_name", (V)"Heavy Lunge"), new Entry((V)"ui_controls", (V)"F Crush \u00c2\u00b7 R Cleave \u00c2\u00b7 X Lunge"))), new Entry(R.Get(R.Get(v_14, (V)"order"), (V)5.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_14, (V)"order"), (V)5.0d)), new Entry((V)"name", (V)"Sentinel"), new Entry((V)"description", (V)"Shield strike and an eight-cell defensive pulse"), new Entry((V)"hp", (V)29.0d), new Entry((V)"damage", (V)4.0d), new Entry((V)"slash_damage", (V)3.0d), new Entry((V)"defense", (V)2.0d), new Entry((V)"speed", (V)90.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)105.0d), new Entry((V)"basic_attack", (V)105.0d), new Entry((V)"sweeping_slash", (V)165.0d), new Entry((V)"dash", (V)145.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/sentinel_strike"), new Entry((V)"special_id", (V)"core:ability/sentinel_pulse"), new Entry((V)"basic_name", (V)"Shield Strike"), new Entry((V)"special_name", (V)"Shield Pulse"), new Entry((V)"dash_name", (V)"Guard Step"), new Entry((V)"ui_controls", (V)"F Strike \u00c2\u00b7 R Pulse \u00c2\u00b7 X Step"))), new Entry(R.Get(R.Get(v_14, (V)"order"), (V)6.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_14, (V)"order"), (V)6.0d)), new Entry((V)"name", (V)"Alchemist"), new Entry((V)"description", (V)"Three-cell vial shot and a targeted cross-shaped blast"), new Entry((V)"hp", (V)22.0d), new Entry((V)"damage", (V)3.0d), new Entry((V)"slash_damage", (V)4.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)105.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)105.0d), new Entry((V)"sweeping_slash", (V)165.0d), new Entry((V)"dash", (V)130.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/alchemist_vial"), new Entry((V)"special_id", (V)"core:ability/alchemist_flask"), new Entry((V)"basic_name", (V)"Vial Shot"), new Entry((V)"special_name", (V)"Blast Flask"), new Entry((V)"dash_name", (V)"Quick Step"), new Entry((V)"ui_controls", (V)"F Vial \u00c2\u00b7 R Flask \u00c2\u00b7 X Step"))), new Entry(R.Get(R.Get(v_14, (V)"order"), (V)7.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_14, (V)"order"), (V)7.0d)), new Entry((V)"name", (V)"Shade"), new Entry((V)"description", (V)"Thrown blade, attack-drawing decoy, one swap"), new Entry((V)"hp", (V)20.0d), new Entry((V)"damage", (V)3.0d), new Entry((V)"slash_damage", (V)0.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)115.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)100.0d), new Entry((V)"sweeping_slash", (V)155.0d), new Entry((V)"dash", (V)115.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/shade_blade"), new Entry((V)"special_id", (V)"core:ability/shade_decoy"), new Entry((V)"basic_name", (V)"Thrown Blade"), new Entry((V)"special_name", (V)"Place Decoy"), new Entry((V)"dash_name", (V)"Shadow Swap"), new Entry((V)"ui_controls", (V)"F Blade \u00c2\u00b7 R Decoy \u00c2\u00b7 X Swap"))), new Entry(R.Get(R.Get(v_14, (V)"order"), (V)8.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_14, (V)"order"), (V)8.0d)), new Entry((V)"name", (V)"Hookrunner"), new Entry((V)"description", (V)"Knife, three-cell hook pull, short sidestep"), new Entry((V)"hp", (V)22.0d), new Entry((V)"damage", (V)4.0d), new Entry((V)"slash_damage", (V)3.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)115.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)95.0d), new Entry((V)"sweeping_slash", (V)145.0d), new Entry((V)"dash", (V)110.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/hookrunner_knife"), new Entry((V)"special_id", (V)"core:ability/hookrunner_hook"), new Entry((V)"basic_name", (V)"Knife"), new Entry((V)"special_name", (V)"Hook Pull"), new Entry((V)"dash_name", (V)"Sidestep"), new Entry((V)"ui_controls", (V)"F Knife \u00c2\u00b7 R Hook \u00c2\u00b7 X Step"))), new Entry(R.Get(R.Get(v_14, (V)"order"), (V)9.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_14, (V)"order"), (V)9.0d)), new Entry((V)"name", (V)"Trapper"), new Entry((V)"description", (V)"Knife, visible snare, short roll"), new Entry((V)"hp", (V)22.0d), new Entry((V)"damage", (V)4.0d), new Entry((V)"slash_damage", (V)3.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)105.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)100.0d), new Entry((V)"sweeping_slash", (V)150.0d), new Entry((V)"dash", (V)120.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/trapper_knife"), new Entry((V)"special_id", (V)"core:ability/trapper_snare"), new Entry((V)"basic_name", (V)"Knife"), new Entry((V)"special_name", (V)"Place Snare"), new Entry((V)"dash_name", (V)"Roll"), new Entry((V)"ui_controls", (V)"F Knife \u00c2\u00b7 R Snare \u00c2\u00b7 X Roll"))))));
var obj_19 = v_14;
V key_20 = (V)"definitions";
R.Set(obj_19, key_20, values_18[0]);
var values_21 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)6, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))))));
V v_22 = values_21[0];
var values_23 = R.Join(new V[] {}, new Results(R.Get(v_4, (V)"id")));
var obj_24 = R.Get(v_14, (V)"order");
V key_25 = (V)((V)R.Length(R.Get(v_14, (V)"order")) + (V)1.0d);
R.Set(obj_24, key_25, values_23[0]);
var values_26 = R.Join(new V[] {}, new Results(R.Get(v_4, (V)"definition")));
var obj_27 = R.Get(v_14, (V)"definitions");
V key_28 = R.Get(v_4, (V)"id");
R.Set(obj_27, key_28, values_26[0]);
var values_29 = R.Join(new V[] {}, new Results(R.Get(v_6, (V)"id")));
var obj_30 = R.Get(v_14, (V)"order");
V key_31 = (V)((V)R.Length(R.Get(v_14, (V)"order")) + (V)1.0d);
R.Set(obj_30, key_31, values_29[0]);
var values_32 = R.Join(new V[] {}, new Results(R.Get(v_6, (V)"definition")));
var obj_33 = R.Get(v_14, (V)"definitions");
V key_34 = R.Get(v_6, (V)"id");
R.Set(obj_33, key_34, values_32[0]);
var values_35 = R.Join(new V[] {}, new Results(R.Get(v_8, (V)"spirit_id")));
var obj_36 = R.Get(v_14, (V)"order");
V key_37 = (V)((V)R.Length(R.Get(v_14, (V)"order")) + (V)1.0d);
R.Set(obj_36, key_37, values_35[0]);
var values_38 = R.Join(new V[] {}, new Results(R.Get(v_8, (V)"spirit")));
var obj_39 = R.Get(v_14, (V)"definitions");
V key_40 = R.Get(v_8, (V)"spirit_id");
R.Set(obj_39, key_40, values_38[0]);
var values_41 = R.Join(new V[] {}, new Results(R.Get(v_8, (V)"tinker_id")));
var obj_42 = R.Get(v_14, (V)"order");
V key_43 = (V)((V)R.Length(R.Get(v_14, (V)"order")) + (V)1.0d);
R.Set(obj_42, key_43, values_41[0]);
var values_44 = R.Join(new V[] {}, new Results(R.Get(v_8, (V)"tinker")));
var obj_45 = R.Get(v_14, (V)"definitions");
V key_46 = R.Get(v_8, (V)"tinker_id");
R.Set(obj_45, key_46, values_44[0]);
var values_47 = R.Join(new V[] {}, new Results(R.Get(v_10, (V)"id")));
var obj_48 = R.Get(v_14, (V)"order");
V key_49 = (V)((V)R.Length(R.Get(v_14, (V)"order")) + (V)1.0d);
R.Set(obj_48, key_49, values_47[0]);
var values_50 = R.Join(new V[] {}, new Results(R.Get(v_10, (V)"definition")));
var obj_51 = R.Get(v_14, (V)"definitions");
V key_52 = R.Get(v_10, (V)"id");
R.Set(obj_51, key_52, values_50[0]);
var values_53 = R.Join(new V[] {}, new Results(R.Get(v_12, (V)"id")));
var obj_54 = R.Get(v_14, (V)"order");
V key_55 = (V)((V)R.Length(R.Get(v_14, (V)"order")) + (V)1.0d);
R.Set(obj_54, key_55, values_53[0]);
var values_56 = R.Join(new V[] {}, new Results(R.Get(v_12, (V)"definition")));
var obj_57 = R.Get(v_14, (V)"definitions");
V key_58 = R.Get(v_12, (V)"id");
R.Set(obj_57, key_58, values_56[0]);
R.Set(v_14, (V)"get", new V((RuleFunction)(args_59 => {
V v_60 = args_59[0];
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_14, (V)"definitions"), v_60)));
return Results.Empty;
})));
R.Set(v_14, (V)"for_player", new V((RuleFunction)(args_61 => {
V v_62 = args_61[0];
return R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_14, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_62, (V)"definition_id")))).First}, new Results((V)"unknown player class"))));
return Results.Empty;
})));
R.Set(v_14, (V)"cost", new V((RuleFunction)(args_63 => {
V v_64 = args_63[0];
V v_65 = args_63[1];
return R.Join(new V[] {}, new Results(R.Get(R.Get(R.Call(R.Get(v_14, (V)"for_player"), R.Join(new V[] {}, new Results(v_64))).First, (V)"costs"), v_65)));
return Results.Empty;
})));
R.Set(v_14, (V)"is_attack", new V((RuleFunction)(args_66 => {
V v_67 = args_66[0];
foreach (var row_68 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_14, (V)"order"))))))) {
V v_69 = row_68[0];
V v_70 = row_68[1];
var values_71 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"get"), R.Join(new V[] {}, new Results(v_70))));
V v_72 = values_71[0];
if (R.Truth(R.Or((V)(v_67 == R.Get(v_72, (V)"basic_id")), () => (V)(v_67 == R.Get(v_72, (V)"special_id"))))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
R.Set(v_14, (V)"immediate_attack", new V((RuleFunction)(args_73 => {
V v_74 = args_73[0];
V v_75 = args_73[1];
V v_76 = args_73[2];
if (R.Truth((V)(v_75 == (V)"basic_attack"))) {
return R.Join(new V[] {}, new Results((V)true));
}
if (R.Truth((V)(v_75 != (V)"sweeping_slash"))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_77 = R.Join(new V[] {}, new Results(R.Get(v_74, (V)"definition_id")));
V v_78 = values_77[0];
if (R.Truth((V)(v_78 == R.Get(v_8, (V)"spirit_id")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_76, (V)"companion_mode") == (V)"strike")));
}
return R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And(R.And((V)(v_78 != R.Get(v_4, (V)"id")), () => (V)(v_78 != R.Get(v_6, (V)"id"))), () => (V)(v_78 != R.Get(v_8, (V)"tinker_id"))), () => (V)(v_78 != R.Get(v_10, (V)"id"))), () => (V)(v_78 != R.Get(v_12, (V)"id"))), () => (V)(v_78 != (V)"core:actor/shade")), () => (V)(v_78 != (V)"core:actor/trapper"))));
return Results.Empty;
})));
V v_79 = V.Nil;
v_79 = new V((RuleFunction)(args_80 => {
V v_81 = args_80[0];
V v_82 = args_80[1];
V v_83 = args_80[2];
V v_84 = args_80[3];
V v_85 = args_80[4];
V v_86 = args_80[5];
var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_82, (V)"map_for"), R.Join(new V[] {}, new Results(v_81))));
V v_88 = values_87[0];
var values_89 = R.Join(new V[] {}, new Results(R.Table()));
V v_90 = values_89[0];
{ double end_93 = R.Num(v_86), step_94 = R.Num((V)1.0d);
for (double i_92 = R.Num((V)1.0d); step_94 > 0 ? i_92 <= end_93 : i_92 >= end_93; i_92 += step_94) {
V v_91 = i_92;
var values_95 = R.Join(new V[] {(V)(R.Get(v_83, (V)"x") + (V)(v_84 * v_91))}, new Results((V)(R.Get(v_83, (V)"y") + (V)(v_85 * v_91))));
V v_96 = values_95[0];
V v_97 = values_95[1];
var values_98 = R.Join(new V[] {(V)false, V.Nil}, new Results(V.Nil));
V v_99 = values_98[0];
V v_100 = values_98[1];
V v_101 = values_98[2];
if (R.Truth(R.And((V)(v_84 != (V)0.0d), () => (V)(v_85 != (V)0.0d)))) {
var values_102 = R.Join(new V[] {(V)(R.Get(v_83, (V)"x") + (V)(v_84 * (V)(v_91 - (V)1.0d)))}, new Results((V)(R.Get(v_83, (V)"y") + (V)(v_85 * (V)(v_91 - (V)1.0d)))));
V v_103 = values_102[0];
V v_104 = values_102[1];
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_82, (V)"projectile_passable"), R.Join(new V[] {v_88, v_96, v_104}, new Results(v_81))).First)))) {
var values_105 = R.Join(new V[] {(V)true, v_96}, new Results(v_104));
v_99 = values_105[0];
v_100 = values_105[1];
v_101 = values_105[2];
} else if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_82, (V)"projectile_passable"), R.Join(new V[] {v_88, v_103, v_97}, new Results(v_81))).First)))) {
var values_106 = R.Join(new V[] {(V)true, v_103}, new Results(v_97));
v_99 = values_106[0];
v_100 = values_106[1];
v_101 = values_106[2];
}
}
if (R.Truth(v_99)) {
var values_107 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_100), new Entry((V)"y", v_101))));
var obj_108 = v_90;
V key_109 = (V)((V)R.Length(v_90) + (V)1.0d);
R.Set(obj_108, key_109, values_107[0]);
break;
}
var values_110 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_96), new Entry((V)"y", v_97))));
var obj_111 = v_90;
V key_112 = (V)((V)R.Length(v_90) + (V)1.0d);
R.Set(obj_111, key_112, values_110[0]);
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_82, (V)"projectile_passable"), R.Join(new V[] {v_88, v_96, v_97}, new Results(v_81))).First)))) {
break;
}
}
}
return R.Join(new V[] {}, new Results(v_90));
return Results.Empty;
}));
R.Set(v_14, (V)"attack", new V((RuleFunction)(args_113 => {
V v_114 = args_113[0];
V v_115 = args_113[1];
V v_116 = args_113[2];
V v_117 = args_113[3];
V v_118 = args_113[4];
V v_119 = args_113[5];
V v_120 = args_113[6];
var values_121 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"for_player"), R.Join(new V[] {}, new Results(v_115))));
V v_122 = values_121[0];
var values_123 = R.Join(new V[] {}, new Results(R.Get(v_115, (V)"position")));
V v_124 = values_123[0];
var values_125 = Results.Empty;
V v_126 = values_125[0];
V v_127 = values_125[1];
V v_128 = values_125[2];
V v_129 = values_125[3];
if (R.Truth((V)(v_116 == (V)"basic_attack"))) {
var values_130 = R.Join(new V[] {}, new Results(R.Get(v_122, (V)"basic_id")));
v_127 = values_130[0];
if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(v_12, (V)"id")))) {
var values_131 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)2.0d))));
v_126 = values_131[0];

var values_132 = R.Join(new V[] {}, new Results((V)true));
v_128 = values_132[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(v_10, (V)"id")))) {
var values_133 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)3.0d))));
v_126 = values_133[0];

var values_134 = R.Join(new V[] {}, new Results((V)true));
v_128 = values_134[0];

var values_135 = R.Join(new V[] {}, new Results((V)"lightning"));
v_129 = values_135[0];
} else if (R.Truth(R.Or((V)(R.Get(v_122, (V)"id") == R.Get(v_8, (V)"spirit_id")), () => (V)(R.Get(v_122, (V)"id") == R.Get(v_8, (V)"tinker_id"))))) {
var values_136 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)3.0d))));
v_126 = values_136[0];

var values_137 = R.Join(new V[] {}, new Results((V)true));
v_128 = values_137[0];

var values_138 = R.Join(new V[] {}, new Results((V)"spirit"));
v_129 = values_138[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(v_6, (V)"id")))) {
var values_139 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)3.0d))));
v_126 = values_139[0];

var values_140 = R.Join(new V[] {}, new Results((V)true));
v_128 = values_140[0];
} else if (R.Truth(R.And((V)(R.Get(v_122, (V)"id") == R.Get(v_4, (V)"id")), () => v_120))) {
var values_141 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)2.0d))));
v_126 = values_141[0];

var values_142 = R.Join(new V[] {}, new Results((V)"piercing_line"));
v_128 = values_142[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(R.Get(v_14, (V)"order"), (V)2.0d)))) {
var values_143 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)4.0d))));
v_126 = values_143[0];

var values_144 = R.Join(new V[] {}, new Results((V)true));
v_128 = values_144[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(R.Get(v_14, (V)"order"), (V)3.0d)))) {
var values_145 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)3.0d))));
v_126 = values_145[0];
var values_146 = R.Join(new V[] {}, new Results((V)true));
v_128 = values_146[0];

var values_147 = R.Join(new V[] {}, new Results((V)"lightning"));
v_129 = values_147[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(R.Get(v_14, (V)"order"), (V)6.0d)))) {
var values_148 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)3.0d))));
v_126 = values_148[0];
var values_149 = R.Join(new V[] {}, new Results((V)true));
v_128 = values_149[0];

var values_150 = R.Join(new V[] {}, new Results((V)"acid"));
v_129 = values_150[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(R.Get(v_14, (V)"order"), (V)7.0d)))) {
var values_151 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)2.0d))));
v_126 = values_151[0];

var values_152 = R.Join(new V[] {}, new Results((V)true));
v_128 = values_152[0];
} else {
var values_153 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"cells"), R.Join(new V[] {(V)"basic_attack", R.Get(v_124, (V)"x"), R.Get(v_124, (V)"y"), v_117}, new Results(v_118))));
v_126 = values_153[0];
}
} else {
var values_154 = R.Join(new V[] {}, new Results(R.Get(v_122, (V)"special_id")));
v_127 = values_154[0];
if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(v_12, (V)"id")))) {
var values_155 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)(R.Get(v_124, (V)"x") + v_117)), new Entry((V)"y", (V)(R.Get(v_124, (V)"y") + v_118)))))));
v_126 = values_155[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(v_10, (V)"id")))) {
var values_156 = R.Join(new V[] {}, new Results(R.Table()));
v_126 = values_156[0];

var values_157 = R.Join(new V[] {}, R.Call(R.Get(v_119, (V)"map_for"), R.Join(new V[] {}, new Results(v_114))));
V v_158 = values_157[0];
var values_159 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)2.0d))));
V v_160 = values_159[0];

var values_161 = R.Join(new V[] {}, new Results(R.Get(v_160, (V)2.0d)));
V v_162 = values_161[0];
if (R.Truth(R.And(R.And(R.And(v_162, () => (V)(R.Get(v_162, (V)"x") == (V)(R.Get(v_124, (V)"x") + (V)((V)2.0d * v_117)))), () => (V)(R.Get(v_162, (V)"y") == (V)(R.Get(v_124, (V)"y") + (V)((V)2.0d * v_118)))), () => R.Call(R.Get(v_119, (V)"projectile_passable"), R.Join(new V[] {v_158, R.Get(v_162, (V)"x"), R.Get(v_162, (V)"y")}, new Results(v_114))).First))) {
foreach (var row_163 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_164 = row_163[0];
V v_165 = row_163[1];
var values_166 = R.Join(new V[] {(V)(R.Get(v_162, (V)"x") + R.Get(v_165, (V)1.0d))}, new Results((V)(R.Get(v_162, (V)"y") + R.Get(v_165, (V)2.0d))));
V v_167 = values_166[0];
V v_168 = values_166[1];
if (R.Truth(R.Call(R.Get(v_119, (V)"projectile_passable"), R.Join(new V[] {v_158, v_167, v_168}, new Results(v_114))).First)) {
var values_169 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_167), new Entry((V)"y", v_168))));
var obj_170 = v_126;
V key_171 = (V)((V)R.Length(v_126) + (V)1.0d);
R.Set(obj_170, key_171, values_169[0]);
}
}
}
} else if (R.Truth(R.Or((V)(R.Get(v_122, (V)"id") == R.Get(v_8, (V)"spirit_id")), () => (V)(R.Get(v_122, (V)"id") == R.Get(v_8, (V)"tinker_id"))))) {
var values_172 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"cells"), R.Join(new V[] {v_114, v_115, v_117, v_118}, new Results(v_119))));
v_126 = values_172[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(v_6, (V)"id")))) {
var values_173 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)(R.Get(v_124, (V)"x") + v_117)), new Entry((V)"y", (V)(R.Get(v_124, (V)"y") + v_118)))))));
v_126 = values_173[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(v_4, (V)"id")))) {
var values_174 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"cells"), R.Join(new V[] {(V)"sweeping_slash", R.Get(v_124, (V)"x"), R.Get(v_124, (V)"y"), v_117}, new Results(v_118))));
v_126 = values_174[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(R.Get(v_14, (V)"order"), (V)2.0d)))) {
var values_175 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)5.0d))));
v_126 = values_175[0];
var values_176 = R.Join(new V[] {}, new Results((V)"piercing_line"));
v_128 = values_176[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(R.Get(v_14, (V)"order"), (V)3.0d)))) {
var values_177 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)2.0d))));
V v_178 = values_177[0];
var values_179 = R.Join(new V[] {}, new Results(R.Get(v_178, (V)R.Length(v_178))));
V v_180 = values_179[0];
var values_181 = R.Join(new V[] {}, new Results(R.Table()));
v_126 = values_181[0];
if (R.Truth(R.And(R.And(R.And(v_180, () => (V)(R.Get(v_180, (V)"x") == (V)(R.Get(v_124, (V)"x") + (V)((V)2.0d * v_117)))), () => (V)(R.Get(v_180, (V)"y") == (V)(R.Get(v_124, (V)"y") + (V)((V)2.0d * v_118)))), () => R.Call(R.Get(v_119, (V)"projectile_passable"), R.Join(new V[] {R.Call(R.Get(v_119, (V)"map_for"), R.Join(new V[] {}, new Results(v_114))).First, R.Get(v_180, (V)"x"), R.Get(v_180, (V)"y")}, new Results(v_114))).First))) {
var values_182 = R.Join(new V[] {}, R.Call(R.Get(v_119, (V)"map_for"), R.Join(new V[] {}, new Results(v_114))));
V v_183 = values_182[0];
{ double end_186 = R.Num((V)1.0d), step_187 = R.Num((V)1.0d);
for (double i_185 = R.Num((-(V)1.0d)); step_187 > 0 ? i_185 <= end_186 : i_185 >= end_186; i_185 += step_187) {
V v_184 = i_185;
{ double end_190 = R.Num((V)1.0d), step_191 = R.Num((V)1.0d);
for (double i_189 = R.Num((-(V)1.0d)); step_191 > 0 ? i_189 <= end_190 : i_189 >= end_190; i_189 += step_191) {
V v_188 = i_189;
var values_192 = R.Join(new V[] {(V)(R.Get(v_180, (V)"x") + v_188)}, new Results((V)(R.Get(v_180, (V)"y") + v_184)));
V v_193 = values_192[0];
V v_194 = values_192[1];
if (R.Truth(R.Call(R.Get(v_119, (V)"projectile_passable"), R.Join(new V[] {v_183, v_193, v_194}, new Results(v_114))).First)) {
var values_195 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_193), new Entry((V)"y", v_194))));
var obj_196 = v_126;
V key_197 = (V)((V)R.Length(v_126) + (V)1.0d);
R.Set(obj_196, key_197, values_195[0]);
}
}
}
}
}
} else {
var values_198 = R.Join(new V[] {}, new Results(v_178));
v_126 = values_198[0];
}
var values_199 = R.Join(new V[] {}, new Results((V)"area"));
v_128 = values_199[0];

var values_200 = R.Join(new V[] {}, new Results((V)"fire"));
v_129 = values_200[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(R.Get(v_14, (V)"order"), (V)4.0d)))) {
var values_201 = R.Join(new V[] {}, new Results(R.Table()));
v_126 = values_201[0];
var values_202 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"direction_index"), R.Join(new V[] {v_117}, new Results(v_118))))));
V v_203 = values_202[0];
{ double end_206 = R.Num((V)2.0d), step_207 = R.Num((V)1.0d);
for (double i_205 = R.Num((-(V)2.0d)); step_207 > 0 ? i_205 <= end_206 : i_205 >= end_206; i_205 += step_207) {
V v_204 = i_205;
var values_208 = R.Join(new V[] {}, new Results(R.Get(v_22, (V)((V)((V)((V)(v_203 + v_204) - (V)1.0d) % (V)8.0d) + (V)1.0d))));
V v_209 = values_208[0];
var values_210 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)(R.Get(v_124, (V)"x") + R.Get(v_209, (V)1.0d))), new Entry((V)"y", (V)(R.Get(v_124, (V)"y") + R.Get(v_209, (V)2.0d))))));
var obj_211 = v_126;
V key_212 = (V)((V)R.Length(v_126) + (V)1.0d);
R.Set(obj_211, key_212, values_210[0]);
}
}
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(R.Get(v_14, (V)"order"), (V)5.0d)))) {
var values_213 = R.Join(new V[] {}, new Results(R.Table()));
v_126 = values_213[0];
foreach (var row_214 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_22)))))) {
V v_215 = row_214[0];
V v_216 = row_214[1];
var values_217 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)(R.Get(v_124, (V)"x") + R.Get(v_216, (V)1.0d))), new Entry((V)"y", (V)(R.Get(v_124, (V)"y") + R.Get(v_216, (V)2.0d))))));
var obj_218 = v_126;
V key_219 = (V)((V)R.Length(v_126) + (V)1.0d);
R.Set(obj_218, key_219, values_217[0]);
}
var values_220 = R.Join(new V[] {}, new Results((V)"pulse"));
v_129 = values_220[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(R.Get(v_14, (V)"order"), (V)6.0d)))) {
var values_221 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)3.0d))));
V v_222 = values_221[0];
var values_223 = R.Join(new V[] {}, new Results(R.Get(v_222, (V)R.Length(v_222))));
V v_224 = values_223[0];
var values_225 = R.Join(new V[] {}, new Results(R.Table()));
v_126 = values_225[0];
var values_226 = R.Join(new V[] {}, R.Call(R.Get(v_119, (V)"map_for"), R.Join(new V[] {}, new Results(v_114))));
V v_227 = values_226[0];
if (R.Truth(R.And(R.And(R.And(v_224, () => (V)(R.Get(v_224, (V)"x") == (V)(R.Get(v_124, (V)"x") + (V)((V)3.0d * v_117)))), () => (V)(R.Get(v_224, (V)"y") == (V)(R.Get(v_124, (V)"y") + (V)((V)3.0d * v_118)))), () => R.Call(R.Get(v_119, (V)"projectile_passable"), R.Join(new V[] {v_227, R.Get(v_224, (V)"x"), R.Get(v_224, (V)"y")}, new Results(v_114))).First))) {
foreach (var row_228 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_229 = row_228[0];
V v_230 = row_228[1];
var values_231 = R.Join(new V[] {(V)(R.Get(v_224, (V)"x") + R.Get(v_230, (V)1.0d))}, new Results((V)(R.Get(v_224, (V)"y") + R.Get(v_230, (V)2.0d))));
V v_232 = values_231[0];
V v_233 = values_231[1];
if (R.Truth(R.Call(R.Get(v_119, (V)"projectile_passable"), R.Join(new V[] {v_227, v_232, v_233}, new Results(v_114))).First)) {
var values_234 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_232), new Entry((V)"y", v_233))));
var obj_235 = v_126;
V key_236 = (V)((V)R.Length(v_126) + (V)1.0d);
R.Set(obj_235, key_236, values_234[0]);
}
}
} else {
var values_237 = R.Join(new V[] {}, new Results(v_222));
v_126 = values_237[0];
}
var values_238 = R.Join(new V[] {}, new Results((V)"area"));
v_128 = values_238[0];

var values_239 = R.Join(new V[] {}, new Results((V)"explosion"));
v_129 = values_239[0];
} else if (R.Truth(R.Or((V)(R.Get(v_122, (V)"id") == R.Get(R.Get(v_14, (V)"order"), (V)7.0d)), () => (V)(R.Get(v_122, (V)"id") == R.Get(R.Get(v_14, (V)"order"), (V)9.0d))))) {
var values_240 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)(R.Get(v_124, (V)"x") + v_117)), new Entry((V)"y", (V)(R.Get(v_124, (V)"y") + v_118)))))));
v_126 = values_240[0];
} else if (R.Truth((V)(R.Get(v_122, (V)"id") == R.Get(R.Get(v_14, (V)"order"), (V)8.0d)))) {
var values_241 = R.Join(new V[] {}, R.Call(v_79, R.Join(new V[] {v_114, v_119, v_124, v_117, v_118}, new Results((V)3.0d))));
v_126 = values_241[0];

var values_242 = R.Join(new V[] {}, new Results((V)true));
v_128 = values_242[0];
} else {
var values_243 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"cells"), R.Join(new V[] {(V)"sweeping_slash", R.Get(v_124, (V)"x"), R.Get(v_124, (V)"y"), v_117}, new Results(v_118))));
v_126 = values_243[0];
}
}
return R.Join(new V[] {v_126, v_127, v_128}, new Results(v_129));
return Results.Empty;
})));
V v_244 = V.Nil;
v_244 = new V((RuleFunction)(args_245 => {
V v_246 = args_245[0];
V v_247 = args_245[1];
V v_248 = args_245[2];
if (R.Truth(R.Get(v_248, (V)"vow"))) {
return R.Join(new V[] {}, R.Invoke((V)"VOW: %d HP -> +%d damage%s \u00c2\u00b7 P off \u00c2\u00b7 Enter", "format", R.Join(new V[] {R.Get(R.Get(v_248, (V)"vow"), (V)"hp_cost"), R.Get(R.Get(v_248, (V)"vow"), (V)"bonus")}, new Results(R.Or(R.And(R.Get(v_248, (V)"fate_power"), () => (V)" \u00c2\u00b7 die on commit"), () => (V)"")))));
}
if (R.Truth(R.Get(v_248, (V)"fate_power"))) {
return R.Join(new V[] {}, new Results((V)"Die rolls on commit \u00c2\u00b7 Enter attack \u00c2\u00b7 Esc cancel"));
}
if (R.Truth((V)(v_247 == (V)"dash"))) {
return R.Join(new V[] {}, new Results((V)"Enter to move \u00c2\u00b7 Esc cancel"));
}
if (R.Truth((V)(v_247 != (V)"sweeping_slash"))) {
return R.Join(new V[] {}, new Results((V)"Enter to strike \u00c2\u00b7 Esc cancel"));
}
var values_249 = R.Join(new V[] {}, new Results(R.Get(v_246, (V)"definition_id")));
V v_250 = values_249[0];
var values_251 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(v_4, (V)"id"), (V)"Enter to parry \u00c2\u00b7 no attack damage"), new Entry(R.Get(v_6, (V)"id"), (V)"Enter to raise one finite block"), new Entry(R.Get(v_8, (V)"tinker_id"), (V)"Enter to deploy three-charge sentry"), new Entry(R.Get(v_10, (V)"id"), (V)"Enter to spend charge on locked cross"), new Entry(R.Get(v_12, (V)"id"), (V)"Enter to plant return beacon"))));
V v_252 = values_251[0];
if (R.Truth((V)(v_250 == R.Get(v_8, (V)"spirit_id")))) {
return R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_248, (V)"companion_mode") == (V)"strike"), () => (V)"Enter to order one paid spirit strike"), () => (V)"Enter to position spirit")));
}
return R.Join(new V[] {}, new Results(R.Or(R.Get(v_252, v_250), () => (V)"Enter to strike \u00c2\u00b7 Esc cancel")));
return Results.Empty;
}));
R.Set(v_14, (V)"preview_hint", new V((RuleFunction)(args_253 => {
V v_254 = args_253[0];
V v_255 = args_253[1];
V v_256 = args_253[2];
var values_257 = R.Join(new V[] {}, R.Call(v_244, R.Join(new V[] {v_254, v_255}, new Results(v_256))));
V v_258 = values_257[0];
if (R.Truth(R.Get(v_256, (V)"weapon_hint"))) {
var values_259 = R.Join(new V[] {}, new Results(R.Concat(v_258, R.Concat((V)" \u00c2\u00b7 ", R.Get(v_256, (V)"weapon_hint")))));
v_258 = values_259[0];
}
return R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_256, (V)"boon_hint"), () => R.Concat(v_258, R.Concat((V)" \u00c2\u00b7 ", R.Get(v_256, (V)"boon_hint")))), () => v_258)));
return Results.Empty;
})));
R.Set(v_14, (V)"status", new V((RuleFunction)(args_260 => {
V v_261 = args_260[0];
V v_262 = args_260[1];
var values_263 = R.Join(new V[] {}, new Results(R.Get(v_262, (V)"definition_id")));
V v_264 = values_263[0];
if (R.Truth((V)(v_264 == R.Get(v_6, (V)"id")))) {
foreach (var row_265 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_261, (V)"entities"))))))) {
V v_266 = row_265[0];
V v_267 = row_265[1];
if (R.Truth((V)(R.Get(v_267, (V)"controller") == R.Get(v_6, (V)"controller")))) {
return R.Join(new V[] {R.Or(R.And((V)(R.Get(R.Get(v_267, (V)"position"), (V)"floor_id") == R.Get(R.Get(v_262, (V)"position"), (V)"floor_id")), () => R.Invoke((V)"BULWARK %d TIME", "format", R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(R.Get(v_267, (V)"expires") - R.Get(v_261, (V)"clock"))))))).First), () => (V)"BULWARK ON OTHER FLOOR")}, new Results((V)"R replace \u00c2\u00b7 expires in world time"));
}
}
return R.Join(new V[] {(V)"NO BULWARK"}, new Results((V)"R place block \u00c2\u00b7 routes stay open"));
} else if (R.Truth((V)(v_264 == R.Get(v_8, (V)"spirit_id")))) {
var values_268 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"owned"), R.Join(new V[] {v_261}, new Results((V)"spirit"))));
V v_269 = values_268[0];
return R.Join(new V[] {R.Or(R.And(v_269, () => (V)"SPIRIT PLACED"), () => (V)"NO BOUND SPIRIT")}, new Results((V)"R position or order adjacent strike"));
} else if (R.Truth((V)(v_264 == R.Get(v_8, (V)"tinker_id")))) {
var values_270 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"owned"), R.Join(new V[] {v_261}, new Results((V)"sentry"))));
V v_271 = values_270[0];
return R.Join(new V[] {R.Or(R.And(v_271, () => R.Invoke((V)"SENTRY %d/3", "format", R.Join(new V[] {}, new Results(R.Get(v_271, (V)"sentry_shots")))).First), () => (V)"NO SENTRY")}, new Results(R.Or(R.And(R.And(v_271, () => (V)(R.Get(v_271, (V)"sentry_shots") == (V)0.0d)), () => (V)"Spent \u00c2\u00b7 R replaces sentry"), () => (V)"Bolts have friendly fire \u00c2\u00b7 R replace")));
} else if (R.Truth((V)(v_264 == R.Get(v_10, (V)"id")))) {
return R.Join(new V[] {R.Invoke((V)"CHARGE %d/3", "format", R.Join(new V[] {}, new Results(R.Or(R.Get(v_262, (V)"storm_charge"), () => (V)0.0d)))).First}, new Results(R.Or(R.And(R.Get(v_262, (V)"storm_ward"), () => (V)"Grounded ward ready"), () => (V)"R discharge \u00c2\u00b7 X ground ward")));
} else if (R.Truth((V)(v_264 == R.Get(v_12, (V)"id")))) {
return R.Join(new V[] {R.Or(R.And(R.Get(R.Get(v_261, (V)"class_special"), (V)"beacon"), () => (V)"BEACON PLANTED"), () => (V)"NO BEACON")}, new Results((V)"R plant \u00c2\u00b7 X room-bound return / Dash"));
}
return Results.Empty;
})));
R.Set(v_14, (V)"validate", new V((RuleFunction)(args_272 => {

var values_273 = R.Join(new V[] {}, new Results(R.Table()));
V v_274 = values_273[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Get(v_14, (V)"order")) == (V)15.0d)}, new Results((V)"expected fifteen starting classes")));
foreach (var row_275 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_14, (V)"order"))))))) {
V v_276 = row_275[0];
V v_277 = row_275[1];
var values_278 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_14, (V)"get"), R.Join(new V[] {}, new Results(v_277))).First}, new Results((V)"missing class"))));
V v_279 = values_278[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_279, (V)"id") == v_277), () => (V)(!R.Truth(R.Get(v_274, v_277)))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_279, (V)"name")))).First == (V)"string")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_279, (V)"description")))).First == (V)"string")), () => (V)(R.Get(v_279, (V)"dash_distance") >= (V)1.0d)), () => (V)(R.Get(v_279, (V)"dash_distance") <= (V)3.0d)), () => (V)(R.Get(v_279, (V)"speed") > (V)0.0d)), () => (V)(R.Get(v_279, (V)"hp") > (V)0.0d))}, new Results((V)"invalid class identity")));
foreach (var row_280 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"move"), new Entry((V)2, (V)"basic_attack"), new Entry((V)3, (V)"sweeping_slash"), new Entry((V)4, (V)"dash")))))))) {
V v_281 = row_280[0];
V v_282 = row_280[1];
var values_283 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_279, (V)"costs"), v_282)));
V v_284 = values_283[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_284))).First == (V)"number"), () => (V)((V)(v_284 % (V)1.0d) == (V)0.0d)), () => (V)(v_284 > (V)0.0d))}, new Results((V)"invalid class action cost")));
}
var values_285 = R.Join(new V[] {}, new Results((V)true));
var obj_286 = v_274;
V key_287 = v_277;
R.Set(obj_286, key_287, values_285[0]);
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_14));
return Results.Empty;
} }
