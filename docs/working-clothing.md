# Regional working clothing

The existing eighteen garments remain obtainable. Eighteen additional pieces
bring the total to 36, six alternatives per protected body location. The paper
doll still has six armour locations, not a new anatomy system. Equip with `E`
from the pack; the selected item shows protection, weight, condition and its
trade-off. Worn and readied items leave grid cells but not carried weight.

| Location | New clothing | Concrete purpose and cost |
|---|---|---|
| Head | reed brim; kiln face wrap; ridge visor | Salt protection; smoke filtering and heat-resistant cloth; four pierce protection but two less sight/ranged reach and metal noise |
| Torso | cork-backed coat; kiln apron; winter felt coat | Current resistance only while light/laden; one heat harm absorbed with six extra condition wear; warm blunt padding gaining four weight when wet |
| Arms | reed splints; quarry sleeves; watch vambraces | Strong dry guard but absorbent bindings; lime/thorn coverage; point protection and wet tool grip |
| Hands | potter mitts; archer tabs; split-hide palms | Heat/lime resistance with stiffness; nearly unarmoured wet grip; salt-resistant grip and cut protection |
| Legs | reed gaiters; quarry chaps; frost leggings | Light narrow thorn/spray cover; stiff lime-resistant work cover; warm broad padding gaining two weight when wet |
| Feet | peat pattens; felt overboots; ice cleats | Quiet mud travel without sharp-ground protection; quiet warmth but water weight; ice/scree grip with metal noise |

Cut, pierce and blunt protection apply at the struck location; narrow coverage
benefits from guarding. Zero-condition equipment no longer protects or lends
terrain tags. Damaged pieces remain physical possessions. Three accumulated
mobility points slow difficult terrain and upward climbing. They are not
hidden percentage modifiers. Wet grip preserves a reinforced guard's morale
effect, not universal immunity to attacks.

Heatproof items themselves lose less condition to fire; torso heatproof
clothing additionally takes one point of courier fire harm. The kiln apron
can still fail and does not protect from stronger fire indefinitely. Lime and
salt slurry abrade susceptible physical metal/weapon items; appropriate worn
protection resists the courier's grit status. Ice cleats improve footing but
do not strengthen thin ice. A cork coat does not make an overloaded courier
float. Rain still uses a coarse above-ground rule rather than a roof model.

Each fresh frontier's existing eight stores includes its work clothing;
regional visits also let the merchant carry one deterministic spare alongside
the established three lots. There are no extra containers or free refills.
Migration preserves existing chest rewards, depleted stores, exact equipment
and losses. The initial household's ready-to-explore equipment is unchanged.

Production hooks: `inventory.py` handles physical definitions, protection,
wet mass and terrain conditions; `actions.py` consumes grip, sound and range;
`world.py` consumes restricted visor sight; `materials.py` handles shared
item wear, slurry, smoke and heat; `frontiers.py` and merchant actions provide
finite acquisition. `tests/test_work_clothing.py` exercises these paths.
