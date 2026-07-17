# `Jomon`

![](./asset/logo/jomon.png)

## Local clearance gate

Run `npm run clearance:local` before declaring a release. It validates 1,000 deterministic full campaigns, writes the failure corpus to `clearance/`, and upserts the `autoplay-clearance` GitHub issue. Set `REPORT_ISSUE=0` to keep the report local.
