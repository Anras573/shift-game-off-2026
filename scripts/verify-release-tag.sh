#!/usr/bin/env bash
# Fails a release that would ship the wrong code:
#
#  1. The tag doesn't point at the current tip of main (pushed from a stale checkout, or
#     main moved on after the PR merged).
#  2. A pull request into main is still open. Tagging before merging leaves the tag on the
#     old tip of main, which check 1 can't tell apart from a correct release, so an open PR
#     is the signal that the release is probably early.
#
#   scripts/verify-release-tag.sh <tag> <tag-commit-sha> <main-commit-sha> [open-prs]
#
# <open-prs> is one line per open PR into main (e.g. "#9 Add mobile touch controls"), or empty.
# To release anyway with a PR open, run the Release workflow manually from the Actions tab.
set -euo pipefail

tag=$1
tag_sha=$2
main_sha=$3
open_prs=${4:-}
status=0

if [[ "$tag_sha" != "$main_sha" ]]; then
  cat >&2 <<MSG
::error title=Tag is not on the latest main::$tag points at ${tag_sha:0:7}, but main is at ${main_sha:0:7}. Merge your PR first, then move the tag.
$tag points at ${tag_sha:0:7}, but main is at ${main_sha:0:7}.
Releases are built from the tagged commit, so this one would ship old code.

MSG
  status=1
fi

if [[ -n "${open_prs//[[:space:]]/}" ]]; then
  count=$(grep -c . <<<"$open_prs")
  cat >&2 <<MSG
::error title=Pull request still open::$count pull request(s) into main are still open. Merge first, then move the tag.
Pull requests into main are still open:
$(sed 's/^/  /' <<<"$open_prs")
If this tag was pushed before merging, it points at the old main and would ship old code.
To release anyway, run the Release workflow manually from the Actions tab.

MSG
  status=1
fi

if [[ $status -ne 0 ]]; then
  cat >&2 <<MSG
Merge the pull request first, then move the tag onto the latest main:

  git pull
  git push origin :refs/tags/$tag && git tag -d $tag
  git tag -a $tag -m "..." && git push origin $tag

MSG
  exit 1
fi

echo "OK: $tag points at the tip of main (${tag_sha:0:7}) and no pull requests into main are open."
