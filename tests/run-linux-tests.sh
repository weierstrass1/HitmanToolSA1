#!/usr/bin/env bash
set -euo pipefail

# Build on Linux storage: Windows bin/obj files and mount permissions must not leak into these tests.
repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
stage=$(mktemp -d /tmp/hitman-linux-tests.XXXXXXXX)
results="$repo/tests/HitmanTool.Tests/TestResults/Linux"
mkdir -p "$results"
cleanup() {
    if [[ -d "$stage/results" ]]; then cp -R "$stage/results/." "$results/"; fi
    case "$stage" in /tmp/hitman-linux-tests.*) rm -rf -- "$stage" ;; esac
}
trap cleanup EXIT

tar -C "$repo" --exclude=bin --exclude=obj --exclude=TestResults -cf - \
    HitmanBackend HitmanFormatReader HitmanTool LogRegister tests/HitmanTool.Tests \
    | tar -C "$stage" -xf -
cd "$stage"
dotnet_bin=${DOTNET_BIN:-dotnet}
"$dotnet_bin" --info
"$dotnet_bin" restore tests/HitmanTool.Tests/HitmanTool.Tests.csproj --disable-parallel -m:1
"$dotnet_bin" test tests/HitmanTool.Tests/HitmanTool.Tests.csproj --no-restore -m:1 \
    --results-directory "$stage/results" --logger 'trx;LogFileName=linux.trx' "$@"
