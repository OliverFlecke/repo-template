#!/usr/bin/env bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
SOURCE_WORKSPACE="$( cd "$DIR/.." >/dev/null 2>&1 && pwd )"
if [[ -n "${NVFL_WORKSPACE:-}" ]]; then
  WORKSPACE="$NVFL_WORKSPACE"
elif [[ "$SOURCE_WORKSPACE" == "/user_config" ]]; then
  WORKSPACE="/vault/workspace"
elif [[ "$SOURCE_WORKSPACE" == /user_config/* ]]; then
  WORKSPACE="/vault/workspace/$(basename "$SOURCE_WORKSPACE")"
else
  WORKSPACE="$SOURCE_WORKSPACE"
fi
if [[ -z "$WORKSPACE" || "$WORKSPACE" == "/" ]]; then
  echo "Invalid WORKSPACE: '$WORKSPACE'"
  exit 1
fi
echo "Please use FL admin console to issue shutdown client command to properly stop this client."
echo "This stop_fl.sh script can only be used as the last resort to stop this client."
echo "It will not properly deregister the client to the server."
echo "The client status on the server after this shell script will be incorrect."
read -n1 -p "Would you like to continue (y/N)? " answer
case $answer in
  y|Y)
    echo
    echo "Shutdown request created.  Wait for local FL process to shutdown."
    mkdir -p "$WORKSPACE"
    touch "$WORKSPACE/shutdown.fl"
    ;;
  n|N|*)
    echo
    echo "Not continue"
    ;;
esac
