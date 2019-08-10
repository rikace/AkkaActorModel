#!/usr/bin/env bash


FAKE_VERSION="5.15.0"
FAKE_TOOL_NAME="fake-cli"

FCSWATCH_VERSION="0.7.13"
FCSWATCH_TOOL_NAME="fcswatch-cli"

# liberated from https://stackoverflow.com/a/18443300/433393
realpath() {
  OURPWD=$PWD
  cd "$(dirname "$1")"
  LINK=$(readlink "$(basename "$1")")
  while [ "$LINK" ]; do
    cd "$(dirname "$LINK")"
    LINK=$(readlink "$(basename "$1")")
  done
  REALPATH="$PWD/$(basename "$1")"
  cd "$OURPWD"
  echo "$REALPATH"
}


global_clitool_version() {
  tool=$1
  dotnet tool list --global | grep "$tool" | awk 'BEGIN { FS = " "} ; { print $2 }'
}

uninstall_global_tool() {
  set +eu
  tool=$1
  dotnet tool uninstall --global "$tool" 2>/dev/null || true # ok for this to fail if the tool isn't present yet
}

install_global_tool() {
  tool=$1
  version=$2
  dotnet tool install --global "$tool" --version "$version"
}

ensure_global_tool() {
  set +eu
  tool=$1
  version=$2
  installed_version=$(global_clitool_version "$tool")
  if ! [[ "$installed_version" = "$version" ]]
  then
    echo "found $tool version $installed_version but wanted version $version, now updating $tool"
    uninstall_global_tool "$tool"
    install_global_tool "$tool" "$version"
  fi
}

uninstall_local_tool() {
  set +eu
  tool=$1
  path=$2
  dotnet tool uninstall --tool-path "$path" "$tool" 2>/dev/null || true # ok for this to fail if the tool isn't present yet
}

install_local_tool() {
  tool=$1
  version=$2
  path=$3
  dotnet tool install --tool-path "$path" "$tool" --version "$version"
}

local_clitool_version() {
  tool=$1
  path=$2
  dotnet tool list --tool-path "$path" | grep "$tool" | awk 'BEGIN { FS = " "} ; { print $2 }'
}

ensure_local_tool() {
  set +eu
  tool=$1
  version=$2
  path=$3
  installed_version=$(local_clitool_version "$tool" "$path")
  if ! [[ "$installed_version" = "$version" ]]
  then
    echo "found $tool version $installed_version but wanted version $version, now updating $tool"
    uninstall_local_tool "$tool" "$path"
    install_local_tool "$tool" "$version" "$path"
  fi
}

dotnet tool install --tool-path .paket Paket 2>/dev/null || true

FAKE_TOOL_PATH=$(realpath .fake)
FAKE="$FAKE_TOOL_PATH"/fake

if ! [ -e "$FAKE" ]
then
  dotnet tool install fake-cli --tool-path "$FAKE_TOOL_PATH"
fi

PAKET_TOOL_PATH=$(realpath .paket)
PAKET="$PAKET_TOOL_PATH"/paket

if ! [ -e "$PAKET" ]
then
  dotnet tool install paket --tool-path "$PAKET_TOOL_PATH"
fi

set -eu
set -o pipefail

ensure_local_tool "$FAKE_TOOL_NAME" "$FAKE_VERSION" ".fake"
ensure_global_tool "$FCSWATCH_TOOL_NAME" "$FCSWATCH_VERSION"

FAKE_DETAILED_ERRORS=true "$FAKE" build -t "$@"
