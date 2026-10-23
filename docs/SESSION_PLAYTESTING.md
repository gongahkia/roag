# Session recording and agent playtesting

Implementation milestone in progress. Ordinary world sessions will automatically retain bounded local diagnostics; an explicit isolated playtest mode will expose the real mouse input pipeline to a versioned observation/action protocol. The recorder must never alter world randomness, save compatibility, collision or exploration rules. Human sessions will not launch an LLM or upload recordings.

The optional tester will use a caller-selected Codex model and reasoning level, structured decisions, bounded actions/time and evidence-linked feedback. Automated assertions and a model's judgments will be reported separately. Existing native acceptance remains the regression baseline.

Research: [OpenAI's non-interactive documentation](https://learn.chatgpt.com/docs/non-interactive-mode) documents `codex exec`, JSON events, schema-constrained final output and reuse of CLI authentication. The installed CLI is checked before an optional model run. [GPT-5.6 Luna documentation](https://developers.openai.com/api/docs/models/gpt-5.6-luna) documents image input and low reasoning support; availability still depends on the caller's account. The integration will preserve the requested model ID rather than silently substituting one.
