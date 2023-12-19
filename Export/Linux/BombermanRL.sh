#!/bin/sh
echo -ne '\033c\033]0;BombermanRL\a'
base_path="$(dirname "$(realpath "$0")")"
"$base_path/BombermanRL.x86_64" "$@"
