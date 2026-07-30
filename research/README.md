# Jomon offline-policy research

This is an external Python 3.11+ experiment harness, not an application dependency. Generate development JSONL with `npm run autoplay:dataset:development -- --output development.jsonl`, then train with `python3 offline_policy.py train --dataset development.jsonl --output bc.json --algorithm behavior-cloning` or `--algorithm recurrent-ppo-style --seed 17`. Training rejects held-out data and visible records with hidden-state fields. Artifacts pin data/feature versions, SHA-256, seed, hyperparameters, environment, and checkpoint identity.

`recurrent-ppo-style` uses the bounded visible history encoder and PPO clipping metadata as an offline baseline scaffold; it does not claim online PPO training or ship inference. Evaluate either partition with `python3 offline_policy.py evaluate --checkpoint bc.json --dataset held-out.jsonl --partition held-out --output evaluation.json`. Evaluation artifacts use the AP-06 objective identifier. Run `python3 -m unittest test_offline_policy.py` for deterministic smoke, held-out isolation, and metadata validation.

The experiment naming follows [PPO](https://arxiv.org/abs/1707.06347) and recurrent policies for partially observed environments ([Romac and Béraud, 2019](https://arxiv.org/abs/1903.04311)); neither paper is treated as evidence that this policy should ship.
