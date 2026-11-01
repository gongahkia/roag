// Static C# port of src/idea_interaction_content.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 96fff2b336baaa33d34196a121e424e527aea8e8e372087e09862a2053b54ddd
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_interaction_content { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"version", (V)1.0d), new Entry((V)"action_cost", (V)100.0d), new Entry((V)"fixture_delay", (V)100.0d), new Entry((V)"fixture_damage", (V)4.0d), new Entry((V)"service_cap", (V)2.0d), new Entry((V)"assembly_cap", (V)1.0d), new Entry((V)"utility_range", (V)3.0d), new Entry((V)"return_range", (V)4.0d))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"offering_font"), new Entry((V)2, (V)"ration_cache"), new Entry((V)3, (V)"cleanse_flask"), new Entry((V)4, (V)"escape_scroll"), new Entry((V)5, (V)"return_token"), new Entry((V)6, (V)"charm_well"), new Entry((V)7, (V)"attunement_forge"), new Entry((V)8, (V)"travelling_trader"), new Entry((V)9, (V)"memorial"), new Entry((V)10, (V)"bell"), new Entry((V)11, (V)"falling_fixture"), new Entry((V)12, (V)"route_sign"), new Entry((V)13, (V)"camp"), new Entry((V)14, (V)"archive_contract"))));
var obj_4 = v_2;
V key_5 = (V)"order";
R.Set(obj_4, key_5, values_3[0]);
var values_6 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"offering_font", (V)"Offering Font"), new Entry((V)"ration_cache", (V)"Ration Cache"), new Entry((V)"cleanse_flask", (V)"Cleansing Flask"), new Entry((V)"escape_scroll", (V)"Escape Scroll"), new Entry((V)"return_token", (V)"Return Token"), new Entry((V)"charm_well", (V)"Charm Well"), new Entry((V)"attunement_forge", (V)"Attunement Forge"), new Entry((V)"travelling_trader", (V)"Supply Trader"), new Entry((V)"memorial", (V)"Memorial"), new Entry((V)"bell", (V)"Bell"), new Entry((V)"bell_clue", (V)"Bell Plaque"), new Entry((V)"falling_fixture", (V)"Falling Fixture"), new Entry((V)"route_sign", (V)"Route Sign"), new Entry((V)"camp", (V)"Camp"), new Entry((V)"archive_contract", (V)"Archivist Contract"), new Entry((V)"return_marker", (V)"Token Marker"))));
var obj_7 = v_2;
V key_8 = (V)"names";
R.Set(obj_7, key_8, values_6[0]);
var values_9 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"offering_font", R.Table(new Entry((V)1, (V)"tiny"), new Entry((V)2, (V)74.0d), new Entry((V)3, (V)690.0d))), new Entry((V)"ration_cache", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)23.0d), new Entry((V)3, (V)650.0d))), new Entry((V)"cleanse_flask", R.Table(new Entry((V)1, (V)"tiny"), new Entry((V)2, (V)114.0d), new Entry((V)3, (V)580.0d))), new Entry((V)"escape_scroll", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)898.0d), new Entry((V)3, (V)139.0d))), new Entry((V)"return_token", R.Table(new Entry((V)1, (V)"onebit"), new Entry((V)2, (V)574.0d), new Entry((V)3, (V)574.0d))), new Entry((V)"charm_well", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)853.0d), new Entry((V)3, (V)435.0d))), new Entry((V)"attunement_forge", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)13.0d), new Entry((V)3, (V)400.0d))), new Entry((V)"travelling_trader", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)239.0d), new Entry((V)3, (V)768.0d))), new Entry((V)"memorial", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)564.0d), new Entry((V)3, (V)72.0d))), new Entry((V)"bell", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)681.0d), new Entry((V)3, (V)867.0d))), new Entry((V)"bell_clue", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)896.0d), new Entry((V)3, (V)392.0d))), new Entry((V)"falling_fixture", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)475.0d), new Entry((V)3, (V)549.0d))), new Entry((V)"route_sign", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)19.0d), new Entry((V)3, (V)674.0d))), new Entry((V)"camp", R.Table(new Entry((V)1, (V)"micro"), new Entry((V)2, (V)117.0d), new Entry((V)3, (V)508.0d))), new Entry((V)"archive_contract", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)901.0d), new Entry((V)3, (V)392.0d))), new Entry((V)"return_marker", R.Table(new Entry((V)1, (V)"onebit"), new Entry((V)2, (V)574.0d), new Entry((V)3, (V)574.0d))))));
var obj_10 = v_2;
V key_11 = (V)"art";
R.Set(obj_10, key_11, values_9[0]);
R.Set(v_2, (V)"binding", new V((RuleFunction)(args_12 => {
V v_13 = args_12[0];
return R.Join(new V[] {}, new Results(R.Concat((V)"core:object/interaction_", v_13)));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
