# Campaign RNG v1 — specification reference vectors

These are independently calculated integer reference values for the exact P02 algorithm. They are not results of executing the local repository. Add equivalent assertions to the actual Lua tests.

## State recurrence from seed 1

`state = (state * 48271) % 2147483647`

| Draw | State |
|---|---|
| 1 | 48271 |
| 2 | 182605794 |
| 3 | 1291394886 |
| 4 | 1914720637 |
| 5 | 2078669041 |
| 6 | 407355683 |
| 7 | 1105902161 |
| 8 | 854716505 |
| 9 | 564586691 |
| 10 | 1596680831 |

## Namespace derivation from master seed 12345

Start h at the master seed; process the exact ASCII bytes with `(h*131+b)%2147483647`; map a final zero to one.

| Namespace | Derived seed |
|---|---|
| `region/body/1/terrain/v1` | 1792981176 |
| `region/body/2/terrain/v1` | 123199946 |
| `region/body/3/terrain/v1` | 600902363 |
| `person/1/expertise/fieldwork/v1` | 1157295232 |
| `person/1/expertise/teaching/v1` | 1172168658 |

## Arithmetic bounds and test notes

Maximum recurrence product at an allowed state: `103661183076066`.
Maximum namespace-fold pre-modulus value for an ASCII byte at most 127: `281320357753`.
Both are below `2^53 = 9007199254740992`.

The namespace hash can collide; stable namespaces prevent draw-order coupling but do not create a collision-proof identifier. IDs are separate explicit fields.

Test a bounded-integer rejection boundary through a controllable test draw source or a targeted known state; do not rely on a small random sample to establish that rejection sampling is implemented. Validate input limits rather than coercing seed zero, fractional values, or nonfinite numbers.
