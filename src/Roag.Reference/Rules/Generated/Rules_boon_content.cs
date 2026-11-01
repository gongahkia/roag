// Static C# port of src/boon_content.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 05d944a819ed0551dff5c3fe6d98becbd59140fe50af20edd6af016328e5aa09
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_boon_content { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results((V)"roag-boons/3"));
var obj_4 = v_2;
V key_5 = (V)"version";
R.Set(obj_4, key_5, values_3[0]);
var values_6 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"common"), new Entry((V)2, (V)"uncommon"), new Entry((V)3, (V)"rare"), new Entry((V)4, (V)"legendary"))));
var obj_7 = v_2;
V key_8 = (V)"rarities";
R.Set(obj_7, key_8, values_6[0]);
var values_9 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:boon/storm_conductor"), new Entry((V)2, (V)"core:boon/detonation_bloom"), new Entry((V)3, (V)"core:boon/thorn_mirror"), new Entry((V)4, (V)"core:boon/static_footsteps"), new Entry((V)5, (V)"core:boon/resonant_wounds"), new Entry((V)6, (V)"core:boon/temporal_echo"), new Entry((V)7, (V)"core:boon/crescent_reach"), new Entry((V)8, (V)"core:boon/turncoat_spark"), new Entry((V)9, (V)"core:boon/emberbrand"), new Entry((V)10, (V)"core:boon/flashpoint"), new Entry((V)11, (V)"core:boon/grave_dividend"), new Entry((V)12, (V)"core:boon/opening_cut"), new Entry((V)13, (V)"core:boon/crowdbreaker"), new Entry((V)14, (V)"core:boon/harriers_response"), new Entry((V)15, (V)"core:boon/perfect_guard"), new Entry((V)16, (V)"core:boon/final_spark"), new Entry((V)17, (V)"core:boon/pulse_step"), new Entry((V)18, (V)"core:boon/packed_powder"), new Entry((V)19, (V)"core:boon/longblade_geometry"), new Entry((V)20, (V)"core:boon/phantom_lunge"), new Entry((V)21, (V)"core:boon/guardians_vow"), new Entry((V)22, (V)"core:boon/prospectors_heart"), new Entry((V)23, (V)"core:boon/circuit_chorus"), new Entry((V)24, (V)"core:boon/crownfire"))));
var obj_10 = v_2;
V key_11 = (V)"order";
R.Set(obj_10, key_11, values_9[0]);
var values_12 = R.Join(new V[] {}, new Results((V)20.0d));
var obj_13 = v_2;
V key_14 = (V)"starter_count";
R.Set(obj_13, key_14, values_12[0]);
R.Set(v_2, (V)"starter_order", new V((RuleFunction)(args_15 => {

var values_16 = R.Join(new V[] {}, new Results(R.Table()));
V v_17 = values_16[0];
{ double end_20 = R.Num(R.Get(v_2, (V)"starter_count")), step_21 = R.Num((V)1.0d);
for (double i_19 = R.Num((V)1.0d); step_21 > 0 ? i_19 <= end_20 : i_19 >= end_20; i_19 += step_21) {
V v_18 = i_19;
var values_22 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"order"), v_18)));
var obj_23 = v_17;
V key_24 = v_18;
R.Set(obj_23, key_24, values_22[0]);
}
}
return R.Join(new V[] {}, new Results(v_17));
return Results.Empty;
})));
R.Set(v_2, (V)"normal_order", new V((RuleFunction)(args_25 => {

var values_26 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"starter_order"), Results.Empty));
V v_27 = values_26[0];
foreach (var row_28 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_expansion_content"))).First, (V)"order"))))))) {
V v_29 = row_28[0];
V v_30 = row_28[1];
var values_31 = R.Join(new V[] {}, new Results(v_30));
var obj_32 = v_27;
V key_33 = (V)((V)R.Length(v_27) + (V)1.0d);
R.Set(obj_32, key_33, values_31[0]);
}
var values_34 = R.Join(new V[] {}, new Results((V)"core:boon/aftershock"));
var obj_35 = v_27;
V key_36 = (V)((V)R.Length(v_27) + (V)1.0d);
R.Set(obj_35, key_36, values_34[0]);
var values_37 = R.Join(new V[] {}, new Results((V)"core:boon/reprisal_nova"));
var obj_38 = v_27;
V key_39 = (V)((V)R.Length(v_27) + (V)1.0d);
R.Set(obj_38, key_39, values_37[0]);
var values_40 = R.Join(new V[] {}, new Results((V)"core:boon/ring_of_reversal"));
var obj_41 = v_27;
V key_42 = (V)((V)R.Length(v_27) + (V)1.0d);
R.Set(obj_41, key_42, values_40[0]);
foreach (var row_43 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"order"))))))) {
V v_44 = row_43[0];
V v_45 = row_43[1];
var values_46 = R.Join(new V[] {}, new Results(v_45));
var obj_47 = v_27;
V key_48 = (V)((V)R.Length(v_27) + (V)1.0d);
R.Set(obj_47, key_48, values_46[0]);
}
return R.Join(new V[] {}, new Results(v_27));
return Results.Empty;
})));
V v_49 = V.Nil;
v_49 = new V((RuleFunction)(args_50 => {
V v_51 = args_50[0];
V v_52 = args_50[1];
var values_53 = R.Join(new V[] {}, new Results(R.Table()));
V v_54 = values_53[0];
foreach (var row_55 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"rarities"))))))) {
V v_56 = row_55[0];
V v_57 = row_55[1];
var values_58 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"chance", R.Or(R.And(v_51, () => R.Get(v_51, v_56)), () => (V)0.0d)), new Entry((V)"power", R.Get(v_52, v_56)))));
var obj_59 = v_54;
V key_60 = v_57;
R.Set(obj_59, key_60, values_58[0]);
}
return R.Join(new V[] {}, new Results(v_54));
return Results.Empty;
}));
var values_61 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:boon/storm_conductor", R.Table(new Entry((V)"name", (V)"Storm Conductor"), new Entry((V)"description", (V)"Hits may arc to another hostile creature."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"lightning"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"HitConfirmed"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"target_hostile"))))))), new Entry((V)"stack_policy", (V)"chance_overflow"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {R.Table(new Entry((V)1, (V)20000.0d), new Entry((V)2, (V)60000.0d), new Entry((V)3, (V)180000.0d), new Entry((V)4, (V)500000.0d))}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"damage_nearest"), new Entry((V)"radius", (V)6.0d), new Entry((V)"damage_type", (V)"lightning"), new Entry((V)"proc_coefficient", (V)1.0d))))))), new Entry((V)"core:boon/detonation_bloom", R.Table(new Entry((V)"name", (V)"Detonation Bloom"), new Entry((V)"description", (V)"Credited kills burst around the victim."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"explosion"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"EntityKilled"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"floor"))), new Entry((V)"predicate", R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"area"), new Entry((V)"radius", (V)1.0d), new Entry((V)"damage_type", (V)"explosion"), new Entry((V)"proc_coefficient", (V)1.0d))))))), new Entry((V)"core:boon/thorn_mirror", R.Table(new Entry((V)"name", (V)"Thorn Mirror"), new Entry((V)"description", (V)"Positive damage retaliates against its source."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"defense"), new Entry((V)2, (V)"retaliation"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"DamageTaken"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"target_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"positive"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"damage_source"), new Entry((V)"damage_type", (V)"thorns"), new Entry((V)"proc_coefficient", (V)1.0d))))))), new Entry((V)"core:boon/static_footsteps", R.Table(new Entry((V)"name", (V)"Static Footsteps"), new Entry((V)"description", (V)"Movement may shock a nearby hostile creature."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"movement"), new Entry((V)2, (V)"lightning"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"TileEntered"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)"stack_policy", (V)"chance_overflow"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {R.Table(new Entry((V)1, (V)50000.0d), new Entry((V)2, (V)150000.0d), new Entry((V)3, (V)350000.0d), new Entry((V)4, (V)700000.0d))}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"damage_nearest"), new Entry((V)"radius", (V)2.0d), new Entry((V)"damage_type", (V)"lightning"), new Entry((V)"proc_coefficient", (V)1.0d))))))), new Entry((V)"core:boon/resonant_wounds", R.Table(new Entry((V)"name", (V)"Resonant Wounds"), new Entry((V)"description", (V)"Adjacent hostiles hurt in one chain release a burst."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"resonance"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"DamageTaken"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"radius"), new Entry((V)"radius", (V)8.0d))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"positive"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"target_hostile"))), new Entry((V)3, R.Table(new Entry((V)"op", (V)"pair_adjacent_damage"), new Entry((V)"neighborhood", (V)"eight"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"area"), new Entry((V)"radius", (V)1.0d), new Entry((V)"damage_type", (V)"resonance"), new Entry((V)"proc_coefficient", (V)1.0d))))))), new Entry((V)"core:boon/temporal_echo", R.Table(new Entry((V)"name", (V)"Temporal Echo"), new Entry((V)"description", (V)"Repeat an attack's locked cells after 200 world time."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"time"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"AttackPerformed"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"echo"), new Entry((V)"delay", (V)200.0d), new Entry((V)"damage_type", (V)"echo"), new Entry((V)"proc_coefficient", (V)1.0d))))))), new Entry((V)"core:boon/crescent_reach", R.Table(new Entry((V)"name", (V)"Crescent Reach"), new Entry((V)"description", (V)"Slash gains one forward cell and extra damage."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"transformation"), new Entry((V)2, (V)"offense"))), new Entry((V)"triggers", R.Table()), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"modifiers", R.Table(new Entry((V)1, R.Table(new Entry((V)"ability_id", (V)"core:ability/sweeping_slash"), new Entry((V)"kind", (V)"forward_cell"))), new Entry((V)2, R.Table(new Entry((V)"ability_id", (V)"core:ability/sweeping_slash"), new Entry((V)"kind", (V)"add_damage"))))), new Entry((V)"effects", R.Table()), new Entry((V)"incompatible", R.Table()), new Entry((V)"eligible_classes", R.Table(new Entry((V)1, (V)"core:actor/adventurer"))))), new Entry((V)"core:boon/turncoat_spark", R.Table(new Entry((V)"name", (V)"Turncoat Spark"), new Entry((V)"description", (V)"Observed enemy friendly fire shocks the aggressor."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"lightning"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"DamageTaken"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"radius"), new Entry((V)"radius", (V)8.0d))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"positive"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"source_target_same_faction"))), new Entry((V)3, R.Table(new Entry((V)"op", (V)"source_not_owner"))), new Entry((V)4, R.Table(new Entry((V)"op", (V)"target_hostile"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"damage_source"), new Entry((V)"damage_type", (V)"lightning"), new Entry((V)"proc_coefficient", (V)1.0d))))))), new Entry((V)"core:boon/emberbrand", R.Table(new Entry((V)"name", (V)"Emberbrand"), new Entry((V)"description", (V)"Direct hits ignite foes for three 100-time ticks."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"burning"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"HitConfirmed"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"target_hostile"))), new Entry((V)3, R.Table(new Entry((V)"op", (V)"direct_attack"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)3.0d), new Entry((V)3, (V)4.0d), new Entry((V)4, (V)6.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"apply_status"), new Entry((V)"status", (V)"burning"), new Entry((V)"interval", (V)100.0d), new Entry((V)"ticks", (V)3.0d))))))), new Entry((V)"core:boon/flashpoint", R.Table(new Entry((V)"name", (V)"Flashpoint"), new Entry((V)"description", (V)"Damage to a Burning foe beside another Burning foe bursts."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"burning"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"DamageTaken"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"radius"), new Entry((V)"radius", (V)8.0d))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"positive"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"target_hostile"))), new Entry((V)3, R.Table(new Entry((V)"op", (V)"burning_pair"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"area"), new Entry((V)"radius", (V)1.0d), new Entry((V)"damage_type", (V)"fire"))))))), new Entry((V)"core:boon/grave_dividend", R.Table(new Entry((V)"name", (V)"Grave Dividend"), new Entry((V)"description", (V)"Credited hostile kills restore health."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"sustain"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"EntityKilled"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"floor"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"target_hostile"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"heal_owner"))))))), new Entry((V)"core:boon/opening_cut", R.Table(new Entry((V)"name", (V)"Opening Cut"), new Entry((V)"description", (V)"Directly damaging an unhurt foe deals bonus damage."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"DamageTaken"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"target_hostile"))), new Entry((V)3, R.Table(new Entry((V)"op", (V)"direct_attack"))), new Entry((V)4, R.Table(new Entry((V)"op", (V)"target_was_full"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"damage_target"), new Entry((V)"damage_type", (V)"physical"))))))), new Entry((V)"core:boon/crowdbreaker", R.Table(new Entry((V)"name", (V)"Crowdbreaker"), new Entry((V)"description", (V)"Direct hits gain bonus damage per nearby living hostile."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"spatial"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"DamageTaken"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"target_hostile"))), new Entry((V)3, R.Table(new Entry((V)"op", (V)"direct_attack"))), new Entry((V)4, R.Table(new Entry((V)"op", (V)"adjacent_hostiles"), new Entry((V)"min", (V)1.0d))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"damage_target"), new Entry((V)"damage_type", (V)"physical"), new Entry((V)"scale", (V)"adjacent_hostiles"))))))), new Entry((V)"core:boon/harriers_response", R.Table(new Entry((V)"name", (V)"Harrier's Response"), new Entry((V)"description", (V)"Nearby hostile windups may draw a retaliatory spark."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"defense"), new Entry((V)2, (V)"lightning"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"WindupStarted"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"radius"), new Entry((V)"radius", (V)7.0d))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"source_hostile"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"unique_windup"))))))), new Entry((V)"stack_policy", (V)"chance_overflow"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {R.Table(new Entry((V)1, (V)80000.0d), new Entry((V)2, (V)180000.0d), new Entry((V)3, (V)350000.0d), new Entry((V)4, (V)650000.0d))}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"damage_source"), new Entry((V)"damage_type", (V)"lightning"))))))), new Entry((V)"core:boon/perfect_guard", R.Table(new Entry((V)"name", (V)"Perfect Guard"), new Entry((V)"description", (V)"Fully blocked hostile damage retaliates nearby."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"defense"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"DamageBlocked"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"target_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"source_hostile"))), new Entry((V)3, R.Table(new Entry((V)"op", (V)"source_in_radius"), new Entry((V)"radius", (V)5.0d))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)3.0d), new Entry((V)3, (V)4.0d), new Entry((V)4, (V)6.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"damage_source"), new Entry((V)"damage_type", (V)"thorns"))))))), new Entry((V)"core:boon/final_spark", R.Table(new Entry((V)"name", (V)"Final Spark"), new Entry((V)"description", (V)"Direct hits while at 40% health or less spark again."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"lightning"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"DamageTaken"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"target_hostile"))), new Entry((V)3, R.Table(new Entry((V)"op", (V)"direct_attack"))), new Entry((V)4, R.Table(new Entry((V)"op", (V)"owner_hp_percent"), new Entry((V)"max", (V)40.0d))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"damage_target"), new Entry((V)"damage_type", (V)"lightning"))))))), new Entry((V)"core:boon/pulse_step", R.Table(new Entry((V)"name", (V)"Pulse Step"), new Entry((V)"description", (V)"Enter a tile beside two foes to pulse into both."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"movement"), new Entry((V)2, (V)"spatial"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"TileEntered"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"adjacent_hostiles"), new Entry((V)"min", (V)2.0d))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"adjacent_hostiles"), new Entry((V)"damage_type", (V)"pulse"))))))), new Entry((V)"core:boon/packed_powder", R.Table(new Entry((V)"name", (V)"Packed Powder"), new Entry((V)"description", (V)"Each disjoint three credited or observed hostile deaths blast."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"explosion"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"EntityKilled"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"floor"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"target_hostile"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"distinct_chain_group"), new Entry((V)"size", (V)3.0d))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"area"), new Entry((V)"radius", (V)1.0d), new Entry((V)"damage_type", (V)"explosion"))))))), new Entry((V)"core:boon/longblade_geometry", R.Table(new Entry((V)"name", (V)"Longblade Geometry"), new Entry((V)"description", (V)"Explicit Sword Strike reaches two cells in its line."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"transformation"), new Entry((V)2, (V)"offense"))), new Entry((V)"triggers", R.Table()), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table()), new Entry((V)"modifiers", R.Table(new Entry((V)1, R.Table(new Entry((V)"ability_id", (V)"core:ability/basic_attack"), new Entry((V)"kind", (V)"line_second_cell"))), new Entry((V)2, R.Table(new Entry((V)"ability_id", (V)"core:ability/basic_attack"), new Entry((V)"kind", (V)"add_damage"))))), new Entry((V)"incompatible", R.Table()), new Entry((V)"eligible_classes", R.Table(new Entry((V)1, (V)"core:actor/adventurer"))))), new Entry((V)"core:boon/phantom_lunge", R.Table(new Entry((V)"name", (V)"Phantom Lunge"), new Entry((V)"description", (V)"Dash strikes each hostile beside its traversed path once."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"transformation"), new Entry((V)2, (V)"movement"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"Moved"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"ability_id"), new Entry((V)"value", (V)"core:ability/dash"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"path_adjacent_hostiles"), new Entry((V)"damage_type", (V)"physical"))))), new Entry((V)"modifiers", R.Table(new Entry((V)1, R.Table(new Entry((V)"ability_id", (V)"core:ability/dash"), new Entry((V)"kind", (V)"path_strike"))))), new Entry((V)"incompatible", R.Table()))), new Entry((V)"core:boon/guardians_vow", R.Table(new Entry((V)"name", (V)"Guardian's Vow"), new Entry((V)"description", (V)"A fully blocked hostile hit bursts around you."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"defense"), new Entry((V)2, (V)"explosion"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"DamageBlocked"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"target_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"source_hostile"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"area"), new Entry((V)"radius", (V)1.0d), new Entry((V)"damage_type", (V)"explosion"))))))), new Entry((V)"core:boon/prospectors_heart", R.Table(new Entry((V)"name", (V)"Prospector's Heart"), new Entry((V)"description", (V)"Collecting currency restores health."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"sustain"), new Entry((V)2, (V)"currency"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"CurrencyCollected"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"heal_owner"))))))), new Entry((V)"core:boon/circuit_chorus", R.Table(new Entry((V)"name", (V)"Circuit Chorus"), new Entry((V)"description", (V)"Other boon activations spark a nearby foe."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"lightning"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"BoonActivated"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"other_boon"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"damage_nearest"), new Entry((V)"radius", (V)4.0d), new Entry((V)"damage_type", (V)"lightning"))))))), new Entry((V)"core:boon/crownfire", R.Table(new Entry((V)"name", (V)"Crownfire"), new Entry((V)"description", (V)"Direct first strikes ignite unhurt foes for three ticks."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"burning"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"DamageTaken"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"target_hostile"))), new Entry((V)3, R.Table(new Entry((V)"op", (V)"direct_attack"))), new Entry((V)4, R.Table(new Entry((V)"op", (V)"target_was_full"))), new Entry((V)5, R.Table(new Entry((V)"op", (V)"positive"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)4.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"apply_status"), new Entry((V)"status", (V)"burning"), new Entry((V)"interval", (V)100.0d), new Entry((V)"ticks", (V)3.0d))))))))));
var obj_62 = v_2;
V key_63 = (V)"definitions";
R.Set(obj_62, key_63, values_61[0]);
var values_64 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_expansion_content"))));
V v_65 = values_64[0];
foreach (var row_66 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_65, (V)"order"))))))) {
V v_67 = row_66[0];
V v_68 = row_66[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(R.Get(v_2, (V)"definitions"), v_68)))}, new Results((V)"duplicate expansion boon")));
var values_69 = R.Join(new V[] {}, new Results(v_68));
var obj_70 = R.Get(v_2, (V)"order");
V key_71 = (V)((V)R.Length(R.Get(v_2, (V)"order")) + (V)1.0d);
R.Set(obj_70, key_71, values_69[0]);
var values_72 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_65, (V)"definitions"), v_68)));
var obj_73 = R.Get(v_2, (V)"definitions");
V key_74 = v_68;
R.Set(obj_73, key_74, values_72[0]);
}
var values_75 = R.Join(new V[] {}, new Results((V)"core:boon/aftershock"));
var obj_76 = R.Get(v_2, (V)"order");
V key_77 = (V)((V)R.Length(R.Get(v_2, (V)"order")) + (V)1.0d);
R.Set(obj_76, key_77, values_75[0]);
var values_78 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"name", (V)"Aftershock"), new Entry((V)"description", (V)"A direct hit bursts across the target's adjacent cells."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"offense"), new Entry((V)2, (V)"area"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"DamageTaken"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"source_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"target_hostile"))), new Entry((V)3, R.Table(new Entry((V)"op", (V)"positive"))), new Entry((V)4, R.Table(new Entry((V)"op", (V)"direct_attack"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)3.0d), new Entry((V)3, (V)4.0d), new Entry((V)4, (V)6.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"area"), new Entry((V)"radius", (V)1.0d), new Entry((V)"damage_type", (V)"shock"))))), new Entry((V)"unit", (V)"adjacent burst damage before defense"), new Entry((V)"eligible_classes", R.Table(new Entry((V)1, (V)"core:actor/sentinel"), new Entry((V)2, (V)"core:actor/alchemist"))))));
var obj_79 = R.Get(v_2, (V)"definitions");
V key_80 = (V)"core:boon/aftershock";
R.Set(obj_79, key_80, values_78[0]);
var values_81 = R.Join(new V[] {}, new Results((V)"core:boon/reprisal_nova"));
var obj_82 = R.Get(v_2, (V)"order");
V key_83 = (V)((V)R.Length(R.Get(v_2, (V)"order")) + (V)1.0d);
R.Set(obj_82, key_83, values_81[0]);
var values_84 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"name", (V)"Reprisal Nova"), new Entry((V)"description", (V)"Taking real hostile damage bursts around you."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"defense"), new Entry((V)2, (V)"area"))), new Entry((V)"triggers", R.Table(new Entry((V)1, (V)"DamageTaken"))), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"predicate", R.Table(new Entry((V)"all", R.Table(new Entry((V)1, R.Table(new Entry((V)"op", (V)"target_owner"))), new Entry((V)2, R.Table(new Entry((V)"op", (V)"source_hostile"))), new Entry((V)3, R.Table(new Entry((V)"op", (V)"positive"))))))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)3.0d), new Entry((V)3, (V)4.0d), new Entry((V)4, (V)6.0d))))).First), new Entry((V)"effects", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"area"), new Entry((V)"radius", (V)1.0d), new Entry((V)"damage_type", (V)"pulse"))))), new Entry((V)"unit", (V)"nearby retaliation damage before defense"), new Entry((V)"eligible_classes", R.Table(new Entry((V)1, (V)"core:actor/sentinel"), new Entry((V)2, (V)"core:actor/alchemist"))))));
var obj_85 = R.Get(v_2, (V)"definitions");
V key_86 = (V)"core:boon/reprisal_nova";
R.Set(obj_85, key_86, values_84[0]);
var values_87 = R.Join(new V[] {}, new Results((V)"core:boon/ring_of_reversal"));
var obj_88 = R.Get(v_2, (V)"order");
V key_89 = (V)((V)R.Length(R.Get(v_2, (V)"order")) + (V)1.0d);
R.Set(obj_88, key_89, values_87[0]);
var values_90 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"name", (V)"Ring of Reversal"), new Entry((V)"description", (V)"Escape a locked enemy attack; your next direct hit on that foe gains damage."), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"movement"), new Entry((V)2, (V)"offense"))), new Entry((V)"triggers", R.Table()), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"parameters", R.Call(v_49, R.Join(new V[] {V.Nil}, new Results(R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)3.0d), new Entry((V)3, (V)4.0d), new Entry((V)4, (V)6.0d))))).First), new Entry((V)"effects", R.Table()), new Entry((V)"unit", (V)"bonus damage on the next direct hit against that attacker"))));
var obj_91 = R.Get(v_2, (V)"definitions");
V key_92 = (V)"core:boon/ring_of_reversal";
R.Set(obj_91, key_92, values_90[0]);
var values_93 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))));
V v_94 = values_93[0];
foreach (var row_95 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_94, (V)"order"))))))) {
V v_96 = row_95[0];
V v_97 = row_95[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(R.Get(v_2, (V)"definitions"), v_97)))}, new Results((V)"duplicate idea boon")));
var values_98 = R.Join(new V[] {}, new Results(v_97));
var obj_99 = R.Get(v_2, (V)"order");
V key_100 = (V)((V)R.Length(R.Get(v_2, (V)"order")) + (V)1.0d);
R.Set(obj_99, key_100, values_98[0]);
var values_101 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_94, (V)"definitions"), v_97)));
var obj_102 = R.Get(v_2, (V)"definitions");
V key_103 = v_97;
R.Set(obj_102, key_103, values_101[0]);
}
foreach (var row_104 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_105 = row_104[0];
V v_106 = row_104[1];
var values_107 = R.Join(new V[] {}, new Results(v_106));
var obj_108 = R.Get(R.Get(v_2, (V)"definitions"), v_106);
V key_109 = (V)"id";
R.Set(obj_108, key_109, values_107[0]);
}
var values_110 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"HitConfirmed", (V)true), new Entry((V)"EntityKilled", (V)true), new Entry((V)"DamageTaken", (V)true), new Entry((V)"DamageBlocked", (V)true), new Entry((V)"TileEntered", (V)true), new Entry((V)"AttackPerformed", (V)true), new Entry((V)"WindupStarted", (V)true), new Entry((V)"Moved", (V)true), new Entry((V)"CurrencyCollected", (V)true), new Entry((V)"BoonActivated", (V)true), new Entry((V)"TerrainTriggered", (V)true), new Entry((V)"TerrainBroken", (V)true), new Entry((V)"SupplyCollected", (V)true), new Entry((V)"ChestClaimed", (V)true), new Entry((V)"Waited", (V)true), new Entry((V)"StatusApplied", (V)true), new Entry((V)"StatusCleansed", (V)true), new Entry((V)"DoorOpened", (V)true), new Entry((V)"SecretDiscovered", (V)true), new Entry((V)"BeaconReturned", (V)true), new Entry((V)"SpiritPositioned", (V)true), new Entry((V)"SentryDeployed", (V)true), new Entry((V)"StormGrounded", (V)true), new Entry((V)"MirrorReflected", (V)true))));
V v_111 = values_110[0];
var values_112 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"damage_nearest", (V)true), new Entry((V)"damage_source", (V)true), new Entry((V)"area", (V)true), new Entry((V)"echo", (V)true), new Entry((V)"apply_status", (V)true), new Entry((V)"heal_owner", (V)true), new Entry((V)"damage_target", (V)true), new Entry((V)"adjacent_hostiles", (V)true), new Entry((V)"path_adjacent_hostiles", (V)true), new Entry((V)"prime", (V)true), new Entry((V)"speed_boost", (V)true), new Entry((V)"ward", (V)true), new Entry((V)"mark_damage", (V)true), new Entry((V)"status_neighbors", (V)true), new Entry((V)"cleanse_owner", (V)true), new Entry((V)"refund_purchase", (V)true), new Entry((V)"enrich_drop", (V)true), new Entry((V)"pattern", (V)true), new Entry((V)"displace_target", (V)true), new Entry((V)"stun_target", (V)true), new Entry((V)"ward_to_heal", (V)true), new Entry((V)"consume_burning", (V)true), new Entry((V)"gather_pickups", (V)true), new Entry((V)"extend_burning", (V)true))));
V v_113 = values_112[0];
var values_114 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"source_owner", (V)true), new Entry((V)"target_owner", (V)true), new Entry((V)"target_hostile", (V)true), new Entry((V)"source_not_owner", (V)true), new Entry((V)"positive", (V)true), new Entry((V)"source_target_same_faction", (V)true), new Entry((V)"pair_adjacent_damage", (V)true), new Entry((V)"source_id", (V)true), new Entry((V)"target_id", (V)true), new Entry((V)"source_tag", (V)true), new Entry((V)"target_tag", (V)true), new Entry((V)"source_faction", (V)true), new Entry((V)"target_faction", (V)true), new Entry((V)"direct_attack", (V)true), new Entry((V)"target_was_full", (V)true), new Entry((V)"adjacent_hostiles", (V)true), new Entry((V)"burning_pair", (V)true), new Entry((V)"source_hostile", (V)true), new Entry((V)"source_in_radius", (V)true), new Entry((V)"owner_hp_percent", (V)true), new Entry((V)"distinct_chain_group", (V)true), new Entry((V)"ability_id", (V)true), new Entry((V)"unique_windup", (V)true), new Entry((V)"other_boon", (V)true), new Entry((V)"event_kind", (V)true), new Entry((V)"from_terrain", (V)true), new Entry((V)"path_terrain", (V)true), new Entry((V)"source_on_terrain", (V)true), new Entry((V)"target_near_terrain", (V)true), new Entry((V)"terrain_id", (V)true), new Entry((V)"damage_type", (V)true), new Entry((V)"primed", (V)true), new Entry((V)"special_attack", (V)true), new Entry((V)"target_hp_percent_before", (V)true), new Entry((V)"owner_in_locked_cells", (V)true), new Entry((V)"overlapping_windups", (V)true), new Entry((V)"approaching_owner", (V)true), new Entry((V)"aligned_hostiles", (V)true), new Entry((V)"event_distance", (V)true), new Entry((V)"visible_source", (V)true), new Entry((V)"visible_event", (V)true), new Entry((V)"supply_kind", (V)true), new Entry((V)"supply_excess", (V)true), new Entry((V)"event_amount", (V)true), new Entry((V)"rewarded_drop", (V)true), new Entry((V)"roll_face", (V)true), new Entry((V)"unique_action", (V)true), new Entry((V)"pattern_axis", (V)true), new Entry((V)"actual_dash", (V)true), new Entry((V)"regular_target", (V)true), new Entry((V)"basic_attack", (V)true), new Entry((V)"cooldown_ready", (V)true), new Entry((V)"owner_missing_hp", (V)true), new Entry((V)"active_ward", (V)true), new Entry((V)"burning_target", (V)true), new Entry((V)"owner_axis", (V)true), new Entry((V)"back_to_wall", (V)true), new Entry((V)"event_status", (V)true), new Entry((V)"target_entity_hostile", (V)true), new Entry((V)"on_special_terrain", (V)true))));
V v_115 = values_114[0];
V v_116 = V.Nil;
v_116 = new V((RuleFunction)(args_117 => {
V v_118 = args_117[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_118))).First == (V)"table")}, new Results((V)"invalid boon predicate")));
if (R.Truth(R.Or(R.Get(v_118, (V)"all"), () => R.Get(v_118, (V)"any")))) {
var values_119 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_118, (V)"all"), () => R.Get(v_118, (V)"any"))));
V v_120 = values_119[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_120))).First == (V)"table"), () => (V)((V)R.Length(v_120) > (V)0.0d))}, new Results((V)"empty boon predicate")));
foreach (var row_121 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_120)))))) {
V v_122 = row_121[0];
V v_123 = row_121[1];
R.Call(v_116, R.Join(new V[] {}, new Results(v_123)));
}
} else if (R.Truth(R.Get(v_118, (V)"not_"))) {
R.Call(v_116, R.Join(new V[] {}, new Results(R.Get(v_118, (V)"not_"))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_115, R.Get(v_118, (V)"op"))}, new Results((V)"unknown boon predicate")));
if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"on_special_terrain"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_118, (V)"value") == (V)"heat_vent")}, new Results((V)"invalid special terrain predicate")));
} else if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"event_status"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_118, (V)"value") == (V)"burning")}, new Results((V)"invalid event status")));
} else if (R.Truth(R.Or((V)(R.Get(v_118, (V)"op") == (V)"pattern_axis"), () => (V)(R.Get(v_118, (V)"op") == (V)"owner_axis")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_118, (V)"value") == (V)"cardinal"), () => (V)(R.Get(v_118, (V)"value") == (V)"diagonal"))}, new Results((V)"invalid pattern axis")));
} else if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"roll_face"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_118, (V)"value")))).First == (V)"number"), () => (V)((V)(R.Get(v_118, (V)"value") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_118, (V)"value") >= (V)1.0d)), () => (V)(R.Get(v_118, (V)"value") <= (V)8.0d))}, new Results((V)"invalid dice face predicate")));
} else if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"supply_kind"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_118, (V)"value") == (V)"food"), () => (V)(R.Get(v_118, (V)"value") == (V)"health"))}, new Results((V)"invalid supply predicate")));
} else if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"event_amount"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.Get(R.Table(new Entry((V)"amount", (V)true), new Entry((V)"actual_gain", (V)true), new Entry((V)"price", (V)true)), R.Get(v_118, (V)"field")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_118, (V)"min")))).First == (V)"number")), () => (V)((V)(R.Get(v_118, (V)"min") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_118, (V)"min") >= (V)0.0d))}, new Results((V)"invalid event amount predicate")));
} else if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"pair_adjacent_damage"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_118, (V)"neighborhood") == (V)"cardinal"), () => (V)(R.Get(v_118, (V)"neighborhood") == (V)"eight"))}, new Results((V)"invalid pair neighborhood")));
} else if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"adjacent_hostiles"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_118, (V)"min")))).First == (V)"number"), () => (V)((V)(R.Get(v_118, (V)"min") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_118, (V)"min") >= (V)1.0d))}, new Results((V)"invalid adjacent count")));
} else if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"distinct_chain_group"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_118, (V)"size")))).First == (V)"number"), () => (V)((V)(R.Get(v_118, (V)"size") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_118, (V)"size") >= (V)2.0d))}, new Results((V)"invalid group size")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or((V)(R.Get(v_118, (V)"field") == V.Nil), () => (V)(R.Get(v_118, (V)"field") == (V)"target_id")), () => (V)(R.Get(v_118, (V)"field") == (V)"boon_id"))}, new Results((V)"invalid group field")));
} else if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"source_in_radius"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_118, (V)"radius")))).First == (V)"number"), () => (V)((V)(R.Get(v_118, (V)"radius") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_118, (V)"radius") >= (V)0.0d))}, new Results((V)"invalid source radius")));
} else if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"owner_hp_percent"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_118, (V)"max")))).First == (V)"number"), () => (V)(R.Get(v_118, (V)"max") >= (V)0.0d)), () => (V)(R.Get(v_118, (V)"max") <= (V)100.0d))}, new Results((V)"invalid health threshold")));
} else if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"target_hp_percent_before"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_118, (V)"max")))).First == (V)"number"), () => (V)(R.Get(v_118, (V)"max") >= (V)0.0d)), () => (V)(R.Get(v_118, (V)"max") <= (V)100.0d))}, new Results((V)"invalid target health threshold")));
} else if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"aligned_hostiles"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_118, (V)"min")))).First == (V)"number"), () => (V)((V)(R.Get(v_118, (V)"min") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_118, (V)"min") >= (V)2.0d))}, new Results((V)"invalid formation count")));
} else if (R.Truth((V)(R.Get(v_118, (V)"op") == (V)"event_distance"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_118, (V)"min")))).First == (V)"number"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_118, (V)"max")))).First == (V)"number")), () => (V)(R.Get(v_118, (V)"min") >= (V)0.0d)), () => (V)(R.Get(v_118, (V)"max") >= R.Get(v_118, (V)"min")))}, new Results((V)"invalid event distance")));
} else if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_118, (V)"op") == (V)"source_id"), () => (V)(R.Get(v_118, (V)"op") == (V)"target_id")), () => (V)(R.Get(v_118, (V)"op") == (V)"ability_id")), () => (V)(R.Get(v_118, (V)"op") == (V)"source_tag")), () => (V)(R.Get(v_118, (V)"op") == (V)"target_tag")), () => (V)(R.Get(v_118, (V)"op") == (V)"source_faction")), () => (V)(R.Get(v_118, (V)"op") == (V)"target_faction")), () => (V)(R.Get(v_118, (V)"op") == (V)"event_kind")), () => (V)(R.Get(v_118, (V)"op") == (V)"from_terrain")), () => (V)(R.Get(v_118, (V)"op") == (V)"path_terrain")), () => (V)(R.Get(v_118, (V)"op") == (V)"source_on_terrain")), () => (V)(R.Get(v_118, (V)"op") == (V)"target_near_terrain")), () => (V)(R.Get(v_118, (V)"op") == (V)"terrain_id")), () => (V)(R.Get(v_118, (V)"op") == (V)"damage_type")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_118, (V)"value") != V.Nil)}, new Results((V)"missing predicate value")));
}
}
return Results.Empty;
}));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_124 => {
V v_125 = args_124[0];
V v_126 = args_124[1];
var values_127 = R.Join(new V[] {R.Or(v_125, () => R.Get(v_2, (V)"definitions"))}, new Results(R.Or(v_126, () => R.Get(v_2, (V)"order"))));
v_125 = values_127[0];
v_126 = values_127[1];
var values_128 = R.Join(new V[] {}, new Results(R.Table()));
V v_129 = values_128[0];
foreach (var row_130 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_126)))))) {
V v_131 = row_130[0];
V v_132 = row_130[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_129, v_132)))}, new Results((V)"duplicate boon ID")));
var values_133 = R.Join(new V[] {}, new Results((V)true));
var obj_134 = v_129;
V key_135 = v_132;
R.Set(obj_134, key_135, values_133[0]);
var values_136 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_125, v_132)}, new Results((V)"missing boon definition"))));
V v_137 = values_136[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_132))).First == (V)"string"), () => R.Invoke(v_132, "match", R.Join(new V[] {}, new Results((V)"^core:boon/"))).First)}, new Results((V)"invalid boon ID")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_137, (V)"id") == v_132)}, new Results((V)"boon definition ID mismatch")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"name")))).First == (V)"string"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"description")))).First == (V)"string"))}, new Results((V)"invalid boon text")));
if (R.Truth(R.Get(v_137, (V)"art"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_137, (V)"art"), (V)"index")))).First == (V)"number"), () => (V)((V)(R.Get(R.Get(v_137, (V)"art"), (V)"index") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(R.Get(v_137, (V)"art"), (V)"index") >= (V)0.0d)), () => (V)(R.Get(R.Get(v_137, (V)"art"), (V)"index") < (V)1078.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_137, (V)"art"), (V)"motif")))).First == (V)"string")), () => (V)((V)R.Length(R.Get(R.Get(v_137, (V)"art"), (V)"motif")) > (V)0.0d))}, new Results((V)"invalid boon art")));
}
if (R.Truth(R.Get(v_137, (V)"attack_payment"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"attack_payment")))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_137, (V)"attack_payment"), (V)"hp")))).First == (V)"number")), () => (V)((V)(R.Get(R.Get(v_137, (V)"attack_payment"), (V)"hp") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(R.Get(v_137, (V)"attack_payment"), (V)"hp") > (V)0.0d)), () => (V)(R.Get(R.Get(v_137, (V)"attack_payment"), (V)"hp") <= (V)100.0d))}, new Results((V)"invalid attack payment")));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_137, (V)"unit") == V.Nil), () => R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"unit")))).First == (V)"string"), () => (V)((V)R.Length(R.Get(v_137, (V)"unit")) > (V)0.0d)))}, new Results((V)"invalid boon magnitude text")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"tags")))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"triggers")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"effects")))).First == (V)"table"))}, new Results((V)"invalid boon declaration")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_137, (V)"scope"), () => R.Get(R.Table(new Entry((V)"owner", (V)true), new Entry((V)"nearby", (V)true), new Entry((V)"radius", (V)true), new Entry((V)"floor", (V)true)), R.Get(R.Get(v_137, (V)"scope"), (V)"kind")))}, new Results((V)"invalid observation scope")));
if (R.Truth(R.Or((V)(R.Get(R.Get(v_137, (V)"scope"), (V)"kind") == (V)"radius"), () => (V)(R.Get(R.Get(v_137, (V)"scope"), (V)"kind") == (V)"nearby")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_137, (V)"scope"), (V)"radius")))).First == (V)"number"), () => (V)(R.Get(R.Get(v_137, (V)"scope"), (V)"radius") >= (V)0.0d))}, new Results((V)"invalid observation radius")));
}
if (R.Truth(R.Get(v_137, (V)"predicate"))) {
R.Call(v_116, R.Join(new V[] {}, new Results(R.Get(v_137, (V)"predicate"))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_137, (V)"stack_policy") == (V)"chance_overflow"), () => (V)(R.Get(v_137, (V)"stack_policy") == (V)"add_power"))}, new Results((V)"invalid stack policy")));
var values_138 = R.Join(new V[] {}, new Results(R.Table()));
V v_139 = values_138[0];
foreach (var row_140 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"triggers"))))))) {
V v_141 = row_140[0];
V v_142 = row_140[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_111, v_142), () => (V)(!R.Truth(R.Get(v_139, v_142))))}, new Results((V)"invalid boon trigger")));
var values_143 = R.Join(new V[] {}, new Results((V)true));
var obj_144 = v_139;
V key_145 = v_142;
R.Set(obj_144, key_145, values_143[0]);
}
foreach (var row_146 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"effects"))))))) {
V v_147 = row_146[0];
V v_148 = row_146[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_113, R.Get(v_148, (V)"kind"))}, new Results((V)"invalid boon effect")));
if (R.Truth((V)(R.Get(v_148, (V)"kind") == (V)"displace_target"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_148, (V)"mode") == (V)"pull"), () => (V)(R.Get(v_148, (V)"mode") == (V)"push"))}, new Results((V)"invalid displacement mode")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_148, (V)"collision_damage") == V.Nil), () => (V)(R.Get(v_148, (V)"collision_damage") == (V)true))}, new Results((V)"invalid collision damage")));
} else if (R.Truth((V)(R.Get(v_148, (V)"kind") == (V)"stun_target"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"cap")))).First == (V)"number"), () => (V)((V)(R.Get(v_148, (V)"cap") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_148, (V)"cap") >= (V)1.0d)), () => (V)(R.Get(v_148, (V)"cap") <= (V)100.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"cooldown")))).First == (V)"number")), () => (V)((V)(R.Get(v_148, (V)"cooldown") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_148, (V)"cooldown") >= (V)400.0d)), () => (V)(R.Get(v_148, (V)"cooldown") <= (V)10000.0d))}, new Results((V)"invalid finite stun declaration")));
} else if (R.Truth((V)(R.Get(v_148, (V)"kind") == (V)"extend_burning"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"cap")))).First == (V)"number"), () => (V)((V)(R.Get(v_148, (V)"cap") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_148, (V)"cap") >= (V)3.0d)), () => (V)(R.Get(v_148, (V)"cap") <= (V)8.0d))}, new Results((V)"invalid Burning extension cap")));
}
if (R.Truth((V)(R.Get(v_148, (V)"target") != V.Nil))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Get(v_148, (V)"kind") == (V)"apply_status"), () => (V)(R.Get(v_148, (V)"target") == (V)"nearest")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"radius")))).First == (V)"number")), () => (V)(R.Get(v_148, (V)"radius") >= (V)1.0d)), () => (V)(R.Get(v_148, (V)"radius") <= (V)8.0d))}, new Results((V)"invalid status target")));
}
if (R.Truth((V)(R.Get(v_148, (V)"clear_path") != V.Nil))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_148, (V)"clear_path") == (V)true), () => R.Or((V)(R.Get(v_148, (V)"kind") == (V)"damage_nearest"), () => R.And((V)(R.Get(v_148, (V)"kind") == (V)"apply_status"), () => (V)(R.Get(v_148, (V)"target") == (V)"nearest"))))}, new Results((V)"invalid clear-path effect")));
}
if (R.Truth(R.And((V)(R.Get(v_148, (V)"cooldown") != V.Nil), () => (V)(R.Get(v_148, (V)"kind") != (V)"stun_target")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Get(v_148, (V)"kind") == (V)"heal_owner"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"cooldown")))).First == (V)"number")), () => (V)((V)(R.Get(v_148, (V)"cooldown") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_148, (V)"cooldown") >= (V)400.0d)), () => (V)(R.Get(v_148, (V)"cooldown") <= (V)10000.0d))}, new Results((V)"invalid healing cooldown")));
}
if (R.Truth(R.Or(R.Get(v_148, (V)"pattern"), () => (V)(R.Get(v_148, (V)"kind") == (V)"pattern")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_patterns"))).First, (V)"names"), R.Get(v_148, (V)"pattern")), () => R.Or((V)(R.Get(v_148, (V)"kind") == (V)"pattern"), () => (V)(R.Get(v_148, (V)"kind") == (V)"echo")))}, new Results((V)"invalid boon pattern")));
}
if (R.Truth(R.Get(v_148, (V)"when"))) {
R.Call(v_116, R.Join(new V[] {}, new Results(R.Get(v_148, (V)"when"))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_148, (V)"proc_coefficient") == V.Nil), () => R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"proc_coefficient")))).First == (V)"number"), () => (V)(R.Get(v_148, (V)"proc_coefficient") >= (V)0.0d)), () => (V)(R.Get(v_148, (V)"proc_coefficient") <= (V)10.0d)))}, new Results((V)"invalid proc coefficient")));
if (R.Truth(R.Get(v_148, (V)"radius"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"radius")))).First == (V)"number"), () => (V)((V)(R.Get(v_148, (V)"radius") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_148, (V)"radius") >= (V)0.0d))}, new Results((V)"invalid effect radius")));
}
if (R.Truth((V)(R.Get(v_148, (V)"kind") == (V)"echo"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"delay")))).First == (V)"number"), () => (V)((V)(R.Get(v_148, (V)"delay") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_148, (V)"delay") > (V)0.0d))}, new Results((V)"invalid echo delay")));
}
if (R.Truth((V)(R.Get(v_148, (V)"kind") == (V)"apply_status"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_148, (V)"status") == (V)"burning"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"interval")))).First == (V)"number")), () => (V)((V)(R.Get(v_148, (V)"interval") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_148, (V)"interval") > (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"ticks")))).First == (V)"number")), () => (V)((V)(R.Get(v_148, (V)"ticks") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_148, (V)"ticks") > (V)0.0d))}, new Results((V)"invalid status effect")));
}
if (R.Truth(R.Or(R.Or(R.Or((V)(R.Get(v_148, (V)"kind") == (V)"prime"), () => (V)(R.Get(v_148, (V)"kind") == (V)"speed_boost")), () => (V)(R.Get(v_148, (V)"kind") == (V)"ward")), () => (V)(R.Get(v_148, (V)"kind") == (V)"mark_damage")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"duration")))).First == (V)"number"), () => (V)((V)(R.Get(v_148, (V)"duration") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_148, (V)"duration") > (V)0.0d)), () => (V)(R.Get(v_148, (V)"duration") <= (V)10000.0d))}, new Results((V)"invalid boon duration")));
}
if (R.Truth((V)(R.Get(v_148, (V)"kind") == (V)"mark_damage"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"threshold")))).First == (V)"number"), () => (V)((V)(R.Get(v_148, (V)"threshold") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_148, (V)"threshold") >= (V)2.0d)), () => (V)(R.Get(v_148, (V)"threshold") <= R.Get(v_148, (V)"cap"))), () => (V)(R.Get(v_148, (V)"cap") <= (V)20.0d))}, new Results((V)"invalid mark threshold")));
}
if (R.Truth((V)(R.Get(v_148, (V)"kind") == (V)"status_neighbors"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_148, (V)"status") == (V)"burning")}, new Results((V)"invalid shared status")));
}
if (R.Truth((V)(R.Get(v_148, (V)"kind") == (V)"cleanse_owner"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_148, (V)"status") == (V)"burning")}, new Results((V)"invalid cleansed status")));
}
if (R.Truth((V)(R.Get(v_148, (V)"consume_prime") != V.Nil))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_148, (V)"consume_prime") == (V)true), () => R.Or(R.Or((V)(R.Get(v_148, (V)"kind") == (V)"damage_target"), () => (V)(R.Get(v_148, (V)"kind") == (V)"heal_owner")), () => (V)(R.Get(v_148, (V)"kind") == (V)"area")))}, new Results((V)"invalid primed effect")));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or((V)(R.Get(v_148, (V)"scale") == V.Nil), () => (V)(R.Get(v_148, (V)"scale") == (V)"adjacent_hostiles")), () => R.And((V)(R.Get(v_148, (V)"scale") == (V)"supply_excess"), () => (V)(R.Get(v_148, (V)"kind") == (V)"ward")))}, new Results((V)"invalid effect scale")));
}
if (R.Truth(R.Get(v_137, (V)"pattern_preview"))) {
var values_149 = R.Join(new V[] {}, new Results(R.Get(v_137, (V)"pattern_preview")));
V v_150 = values_149[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_150))).First == (V)"table"), () => R.Get(R.Table(new Entry((V)"hit", (V)true), new Entry((V)"kill", (V)true), new Entry((V)"dash", (V)true), new Entry((V)"wait", (V)true), new Entry((V)"return", (V)true)), R.Get(v_150, (V)"trigger"))), () => (V)((V)R.Length(R.Get(v_137, (V)"effects")) == (V)1.0d)), () => R.Get(R.Get(R.Get(v_137, (V)"effects"), (V)1.0d), (V)"pattern"))}, new Results((V)"invalid pattern preview")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or((V)(R.Get(v_150, (V)"axis") == V.Nil), () => (V)(R.Get(v_150, (V)"axis") == (V)"cardinal")), () => (V)(R.Get(v_150, (V)"axis") == (V)"diagonal"))}, new Results((V)"invalid preview axis")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_150, (V)"special") == V.Nil), () => (V)(R.Get(v_150, (V)"special") == (V)true))}, new Results((V)"invalid special preview")));
}
var values_151 = R.Join(new V[] {(-(V)1.0d)}, new Results((-(V)1.0d)));
V v_152 = values_151[0];
V v_153 = values_151[1];
foreach (var row_154 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"rarities"))))))) {
V v_155 = row_154[0];
V v_156 = row_154[1];
var values_157 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_137, (V)"parameters"), v_156)}, new Results((V)"missing rarity parameters"))));
V v_158 = values_157[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_158, (V)"power")))).First == (V)"number"), () => (V)(R.Get(v_158, (V)"power") >= (V)0.0d))}, new Results((V)"invalid boon power")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_158, (V)"chance")))).First == (V)"number"), () => (V)(R.Get(v_158, (V)"chance") >= (V)0.0d)), () => (V)((V)(R.Get(v_158, (V)"chance") % (V)1.0d) == (V)0.0d))}, new Results((V)"invalid boon chance")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_158, (V)"power") >= v_152), () => (V)(R.Get(v_158, (V)"chance") >= v_153))}, new Results((V)"rarity strength must be monotone")));
var values_159 = R.Join(new V[] {R.Get(v_158, (V)"power")}, new Results(R.Get(v_158, (V)"chance")));
v_152 = values_159[0];
v_153 = values_159[1];
}
foreach (var row_160 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_137, (V)"modifiers"), () => R.Table()))))))) {
V v_161 = row_160[0];
V v_162 = row_160[1];
var values_163 = R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_162, (V)"kind") == (V)"weapon_form"), () => R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_patterns"))).First, (V)"weapon_forms"), R.Get(v_162, (V)"form")))));
V v_164 = values_163[0];
var values_165 = R.Join(new V[] {}, new Results((V)false));
V v_166 = values_165[0];
if (R.Truth((V)(R.Get(v_162, (V)"kind") == (V)"add_damage"))) {
foreach (var row_167 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_patterns"))).First, (V)"weapon_forms"))))))) {
V v_168 = row_167[0];
V v_169 = row_167[1];
if (R.Truth((V)(R.Get(v_169, (V)"ability_id") == R.Get(v_162, (V)"ability_id")))) {
var values_170 = R.Join(new V[] {}, new Results((V)true));
v_166 = values_170[0];

break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.And(v_164, () => (V)(R.Get(v_162, (V)"ability_id") == R.Get(v_164, (V)"ability_id"))), () => v_166), () => R.And((V)(R.Get(v_162, (V)"ability_id") == (V)"core:ability/sweeping_slash"), () => R.Or((V)(R.Get(v_162, (V)"kind") == (V)"forward_cell"), () => (V)(R.Get(v_162, (V)"kind") == (V)"add_damage")))), () => R.And((V)(R.Get(v_162, (V)"ability_id") == (V)"core:ability/basic_attack"), () => R.Or((V)(R.Get(v_162, (V)"kind") == (V)"line_second_cell"), () => (V)(R.Get(v_162, (V)"kind") == (V)"add_damage")))), () => R.And((V)(R.Get(v_162, (V)"ability_id") == (V)"core:ability/dash"), () => R.Or((V)(R.Get(v_162, (V)"kind") == (V)"path_strike"), () => (V)(R.Get(v_162, (V)"kind") == (V)"dash_prime")))), () => R.And((V)(R.Get(v_162, (V)"ability_id") == (V)"core:ability/ranger_shot"), () => R.Or((V)(R.Get(v_162, (V)"kind") == (V)"branch_near"), () => (V)(R.Get(v_162, (V)"kind") == (V)"add_damage")))), () => R.And((V)(R.Get(v_162, (V)"ability_id") == (V)"core:ability/ranger_volley"), () => R.Or((V)(R.Get(v_162, (V)"kind") == (V)"line_extend"), () => (V)(R.Get(v_162, (V)"kind") == (V)"add_damage")))), () => R.And((V)(R.Get(v_162, (V)"ability_id") == (V)"core:ability/arcanist_sigil"), () => R.Or((V)(R.Get(v_162, (V)"kind") == (V)"area_expand"), () => (V)(R.Get(v_162, (V)"kind") == (V)"add_damage")))), () => R.And((V)(R.Get(v_162, (V)"ability_id") == (V)"core:ability/arcanist_bolt"), () => R.Or((V)(R.Get(v_162, (V)"kind") == (V)"status_chain"), () => (V)(R.Get(v_162, (V)"kind") == (V)"add_damage")))), () => R.And((V)(R.Get(v_162, (V)"ability_id") == (V)"core:ability/berserker_crush"), () => R.Or((V)(R.Get(v_162, (V)"kind") == (V)"shockwave"), () => (V)(R.Get(v_162, (V)"kind") == (V)"add_damage"))))}, new Results((V)"invalid ability modifier")));
if (R.Truth(v_164)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Get(v_137, (V)"eligible_classes"), () => (V)((V)R.Length(R.Get(v_137, (V)"eligible_classes")) == (V)1.0d)), () => (V)(R.Get(R.Get(v_137, (V)"eligible_classes"), (V)1.0d) == R.Get(v_164, (V)"class_id")))}, new Results((V)"weapon form class mismatch")));
}
}
if (R.Truth(R.Get(v_137, (V)"eligible_classes"))) {
var values_171 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_172 = values_171[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"eligible_classes")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_137, (V)"eligible_classes")) > (V)0.0d))}, new Results((V)"invalid class eligibility")));
var values_173 = R.Join(new V[] {}, new Results(R.Table()));
V v_174 = values_173[0];
foreach (var row_175 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"eligible_classes"))))))) {
V v_176 = row_175[0];
V v_177 = row_175[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(R.Get(v_172, (V)"get"), R.Join(new V[] {}, new Results(v_177))).First, () => (V)(!R.Truth(R.Get(v_174, v_177))))}, new Results((V)"invalid eligible class")));
var values_178 = R.Join(new V[] {}, new Results((V)true));
var obj_179 = v_174;
V key_180 = v_177;
R.Set(obj_179, key_180, values_178[0]);
}
}
foreach (var row_181 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_137, (V)"incompatible"), () => R.Table()))))))) {
V v_182 = row_181[0];
V v_183 = row_181[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(v_183 != v_132), () => R.Get(v_125, v_183))}, new Results((V)"invalid boon incompatibility")));
}
}
foreach (var row_184 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_125)))))) {
V v_185 = row_184[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_129, v_185)}, new Results((V)"unlisted boon definition")));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_2, (V)"get", new V((RuleFunction)(args_186 => {
V v_187 = args_186[0];
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"definitions"), v_187)));
return Results.Empty;
})));
R.Set(v_2, (V)"offer_text", new V((RuleFunction)(args_188 => {
V v_189 = args_188[0];
V v_190 = args_188[1];
var values_191 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_189))).First}, new Results((V)"unknown boon"))));
V v_192 = values_191[0];
var values_193 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_192, (V)"parameters"), v_190)}, new Results((V)"unknown rarity"))));
V v_194 = values_193[0];
var values_195 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:boon/emberbrand", (V)"damage/tick before defense"), new Entry((V)"core:boon/flashpoint", (V)"burst damage before defense"), new Entry((V)"core:boon/grave_dividend", (V)"health per credited kill"), new Entry((V)"core:boon/opening_cut", (V)"bonus damage before defense"), new Entry((V)"core:boon/crowdbreaker", (V)"damage per adjacent foe"), new Entry((V)"core:boon/harriers_response", (V)"spark damage before defense"), new Entry((V)"core:boon/perfect_guard", (V)"retaliation damage before defense"), new Entry((V)"core:boon/final_spark", (V)"bonus damage before defense"), new Entry((V)"core:boon/pulse_step", (V)"pulse damage before defense"), new Entry((V)"core:boon/packed_powder", (V)"blast damage before defense"), new Entry((V)"core:boon/longblade_geometry", (V)"Sword damage bonus; reach stays 2"), new Entry((V)"core:boon/phantom_lunge", (V)"path strike damage before defense"), new Entry((V)"core:boon/guardians_vow", (V)"adjacent burst damage before defense"), new Entry((V)"core:boon/prospectors_heart", (V)"health per pickup"), new Entry((V)"core:boon/circuit_chorus", (V)"spark damage before defense"), new Entry((V)"core:boon/crownfire", (V)"Burning damage/tick before defense"))));
V v_196 = values_195[0];
var values_197 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_192, (V)"stack_policy") == (V)"chance_overflow"), () => R.Invoke((V)"%.1f%% proc, %d %s", "format", R.Join(new V[] {(V)(R.Get(v_194, (V)"chance") / (V)10000.0d), R.Get(v_194, (V)"power")}, new Results(R.Or(R.Or(R.Get(v_192, (V)"unit"), () => R.Get(v_196, v_189)), () => (V)"power")))).First), () => R.Invoke((V)"%d %s per copy", "format", R.Join(new V[] {R.Get(v_194, (V)"power")}, new Results(R.Or(R.Or(R.Get(v_192, (V)"unit"), () => R.Get(v_196, v_189)), () => (V)"power")))).First)));
V v_198 = values_197[0];
return R.Join(new V[] {}, new Results(R.Concat(R.Get(v_192, (V)"description"), R.Concat((V)" [", R.Concat(v_198, (V)"]")))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
