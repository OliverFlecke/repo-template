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

while [[ $# -gt 0 ]]
do
  key="$1"
  case $key in
    --verify)
      doVerify=true
    ;;
    --once)
      doOnce=true
    ;;
  esac
  shift
done

check_daemon() {
  if [[ -f "$WORKSPACE/daemon_pid.fl" ]]; then
    dpid=`cat "$WORKSPACE/daemon_pid.fl"`
    kill -0 ${dpid} 2> /dev/null 1>&2
    if [[ $? -eq 0 ]]; then
      echo "There seems to be one instance, pid=$dpid, running."
      echo "If you are sure it's not the case, please kill process $dpid and then remove daemon_pid.fl in $WORKSPACE"
      exit
    fi
    rm -f "$WORKSPACE/daemon_pid.fl"
  fi
}

KIT_WORKSPACE="$WORKSPACE"
if [[ "$WORKSPACE" != "$SOURCE_WORKSPACE" ]]; then
  check_daemon
  KIT_WORKSPACE="$WORKSPACE/kit"
  mkdir -p "$WORKSPACE"
  rm -rf "$KIT_WORKSPACE"
  cp -a "$SOURCE_WORKSPACE" "$KIT_WORKSPACE"
  chmod 700 "$WORKSPACE" "$KIT_WORKSPACE"
  rm -rf "$WORKSPACE/startup" "$WORKSPACE/local"
  ln -s "$KIT_WORKSPACE/startup" "$WORKSPACE/startup"
  ln -s "$KIT_WORKSPACE/local" "$WORKSPACE/local"
fi

if [[ "$doVerify" == "true" ]]; then
  python3 -m nvflare.tool.verify_startup_kits -f "$KIT_WORKSPACE" -c "$KIT_WORKSPACE/startup/rootCA.pem"
  verification_status=$?
  if [[ $verification_status -ne 0 ]]; then
    echo "verification failed"
    exit 1
  fi
fi

echo "WORKSPACE set to $WORKSPACE"
mkdir -p "$WORKSPACE/transfer"
export PYTHONPATH=/local/custom:$PYTHONPATH
echo "PYTHONPATH is $PYTHONPATH"

# Memory management configuration
# Opt-in jemalloc preload for PyTorch workloads.
# Enable by setting NVFLARE_ENABLE_JEMALLOC_PRELOAD=true.
if [ "${NVFLARE_ENABLE_JEMALLOC_PRELOAD:-false}" = "true" ]; then
  for JEMALLOC in /usr/lib/x86_64-linux-gnu/libjemalloc.so.2 \
                  /usr/lib64/libjemalloc.so.2 \
                  /usr/local/lib/libjemalloc.so; do
      if [ -f "$JEMALLOC" ]; then
          export LD_PRELOAD="${LD_PRELOAD:+$LD_PRELOAD:}$JEMALLOC"
          export MALLOC_CONF="${MALLOC_CONF:-dirty_decay_ms:5000,muzzy_decay_ms:5000}"
          echo "Using jemalloc: $JEMALLOC"
          break
      fi
  done
fi

# Limit glibc memory arenas to reduce RSS fragmentation (ignored by jemalloc)
# Recommended: MALLOC_ARENA_MAX=2 for clients, MALLOC_ARENA_MAX=4 for servers
export MALLOC_ARENA_MAX=${MALLOC_ARENA_MAX:-4}

SECONDS=0
lst=-400
restart_count=0

start_python() {
  python3 -u -m nvflare.private.fed.app.client.client_train -m "$WORKSPACE" -s fed_client.json --set secure_train=true uid=site-1 org=maritime config_folder=config
}

start_fl() {
  if [[ $(( $SECONDS - $lst )) -lt 300 ]]; then
    ((restart_count++))
  else
    restart_count=0
  fi
  if [[ $(($SECONDS - $lst )) -lt 300 && $restart_count -ge 5 ]]; then
    echo "System is in trouble and unable to start the task!!!!!"
    rm -f "$WORKSPACE/pid.fl" "$WORKSPACE/shutdown.fl" "$WORKSPACE/restart.fl" "$WORKSPACE/daemon_pid.fl"
    exit
  fi
  lst=$SECONDS
  (start_python 2>&1 & echo $! >&3 ) 3>"$WORKSPACE/pid.fl"
  pid=`cat "$WORKSPACE/pid.fl"`
  echo "new pid ${pid}"
}

stop_fl() {
  if [[ ! -f "$WORKSPACE/pid.fl" ]]; then
    echo "No pid.fl.  No need to kill process."
    return
  fi
  pid=`cat "$WORKSPACE/pid.fl"`
  sleep 5
  kill -0 ${pid} 2> /dev/null 1>&2
  if [[ $? -ne 0 ]]; then
    echo "Process already terminated"
    return
  fi
  kill -9 $pid
  rm -f "$WORKSPACE/pid.fl" "$WORKSPACE/shutdown.fl" "$WORKSPACE/restart.fl" 2> /dev/null 1>&2
}

if [[ "$doOnce" == "true" ]]; then
  echo "Start NVFlare directly"
  rm -f "$WORKSPACE/pid.fl" "$WORKSPACE/shutdown.fl" "$WORKSPACE/restart.fl" 2> /dev/null 1>&2
  start_python
  exit $?
fi

check_daemon

echo $BASHPID > "$WORKSPACE/daemon_pid.fl"

while true
do
  sleep 5
  if [[ ! -f "$WORKSPACE/pid.fl" ]]; then
    echo "start fl because of no pid.fl"
    start_fl
    continue
  fi
  pid=`cat "$WORKSPACE/pid.fl"`
  kill -0 ${pid} 2> /dev/null 1>&2
  if [[ $? -ne 0 ]]; then
    if [[ -f "$WORKSPACE/shutdown.fl" ]]; then
      echo "Gracefully shutdown."
      break
    fi
    echo "start fl because process of ${pid} does not exist"
    start_fl
    continue
  fi
  if [[ -f "$WORKSPACE/shutdown.fl" ]]; then
    echo "About to shutdown."
    stop_fl
    break
  fi
  if [[ -f "$WORKSPACE/restart.fl" ]]; then
    echo "About to restart."
    stop_fl
  fi
done

rm -f "$WORKSPACE/pid.fl" "$WORKSPACE/shutdown.fl" "$WORKSPACE/restart.fl" "$WORKSPACE/daemon_pid.fl"
