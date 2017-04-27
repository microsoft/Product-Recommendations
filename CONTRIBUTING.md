Contributing to the Product Recommendations pre-configured Solution
======================

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

Please follow the contribution guidelines below:

Coding Style Changes
--------------------
Please:
* **DO NOT** send PRs for style changes. For example, do not send PRs that are focused on changing usage of ```Int32``` to ```int```.
* **DO NOT** send PRs for upgrading code to use newer language features, though it's ok to use newer language features as part of new code that's written.  For example, it's ok to use expression-bodied members as part of new code you write, but do not send a PR focused on changing existing properties or methods to use the feature.
* **DO** give priority to the current style of the project or file you're changing even if it diverges from the general guidelines.

Public API Changes
-----------
* **DO NOT** submit such PRs until the APIs have been approved with the team (mlapi@microsoft.com)

Pull Requests
-------------

* **DO** submit all code changes via pull requests (PRs) rather than through a direct commit. PRs will be reviewed and potentially merged by the repo maintainers after a peer review that includes at least one maintainer.
* **DO NOT** submit "work in progress" PRs.  A PR should only be submitted when it is considered ready for review and subsequent merging by the contributor.
* **DO** give PRs short-but-descriptive names (e.g. "Improve code coverage for web job tests by 10%", not "Fix #1234")
* **DO** refer to any relevant issues, and include [keywords](https://help.github.com/articles/closing-issues-via-commit-messages/) that automatically close issues when the PR is merged.
* **DO** tag any users that should know about and/or review the change.
* **DO** ensure each commit successfully builds.  The entire PR must pass all unit tests before it'll be merged.
* **DO** address PR feedback in an additional commit(s) rather than ammending the existing commits, and only rebase/squash them when necessary.  This makes it easier for reviewers to track changes. If necessary, squashing should be handled by the merger using the ["squash and merge"](https://github.com/blog/2141-squash-your-commits) feature, and should only be done by the contributor upon request.
* **DO NOT** fix merge conflicts using a merge commit. Prefer `git rebase`.