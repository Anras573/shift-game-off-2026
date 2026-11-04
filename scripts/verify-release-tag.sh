#!/usr/bin/env bash
# Fails a release when the tag doesn't point at the current tip of main, which happens when
# a tag is pushed before its PR is merged (or from a stale checkout).
#
#   scripts/verify-release-tag.sh <tag> <tag-commit-sha> <main-commit-sha>
set -euo pipefail

tag=$1
tag_sha=$2
main_sha=$3

if [[ "$tag_sha" == "$main_sha" ]]; then
  echo "OK: $tag points at the tip of main (${tag_sha:0:7})."
  exit 0
fi

cat >&2 <<MSG
::error title=Tag is not on the latest main::$tag points at ${tag_sha:0:7}, but main is at ${main_sha:0:7}. Merge your PR first, then move the tag.
$tag points at ${tag_sha:0:7}, but main is at ${main_sha:0:7}.
Releases are built from the tagged commit, so this one would ship old code.

Merge the pull request first, then move the tag onto the latest main:

  git pull
  git push origin :refs/tags/$tag && git tag -d $tag
  git tag -a $tag -m "..." && git push origin $tag

MSG
exit 1
