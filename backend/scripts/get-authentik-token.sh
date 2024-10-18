#! /usr/bin/env nix-shell
#! nix-shell -i bash -p oauth2c curl jq
# shellcheck shell=bash

set -euo pipefail

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)
while IFS="=" read -r key value; do
    printf -v "$key" %s "$value" && export "${key?}"
done <"$SCRIPT_DIR/.env"

if [ -z "$AUTHENTIK_CLIENT_ID" ]; then
    echo AUTHENTIK_CLIENT_ID not set in .env file.
    exit 1
fi

oauth2c 'https://sso.shiro.lan/application/o/livestreamdvr/' \
    --client-id "$AUTHENTIK_CLIENT_ID" \
    --response-types code \
    --response-mode query \
    --grant-type authorization_code \
    --auth-method none \
    --scopes openid,profile \
    --pkce \
    --silent | jq --raw-output '.access_token'
