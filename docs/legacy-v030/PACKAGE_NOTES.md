# Complete v0.3.0 package

This archive combines the supplied v0.2.0 project with every v0.3.0 file
from the preceding response. It is a complete project, not a patch.
Runtime source files have not been changed during packaging.

Extract into a new source directory. Back up your existing source and the
LÖVE `deepward_02` save directory before running. Both versions use the
same save identity; do not run them simultaneously. See UPGRADE.md.

The packaging pass verified all 47 files in the syntax-checker manifest,
local module references, exact preservation of the supplied update files,
and ZIP integrity. This pass did not execute Lua or the LÖVE application.
Test outputs under docs/ were supplied with their respective releases;
packaging does not constitute a fresh run of those tests.

Historical documents retained from v0.2.0 (including PROJECT_STATE.md,
CHANGELOG.md, RESEARCH.md, and TEST_REPORT.md) describe that earlier release.
The current release is documented in README.md, UPGRADE.md,
docs/GENERATION.md, and docs/MAP_FORMAT.md.

SOURCE_SHA256.txt has been regenerated for the assembled archive.
