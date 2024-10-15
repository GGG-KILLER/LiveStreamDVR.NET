#! /usr/bin/env nix-shell
#! nix-shell -i bash -p oauth2c curl jq
# shellcheck shell=bash

set -euo pipefail

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)
while IFS="=" read -r key value; do
    printf -v "$key" %s "$value" && export "${key?}"
done <"$SCRIPT_DIR/.env"

if [ -z "$TWITCH_CLIENT_ID" ]; then
    echo TWITCH_CLIENT_ID not set in .env file.
    exit 1
fi

if [ -z "$TWITCH_CLIENT_SECRET" ]; then
    echo TWITCH_CLIENT_SECRET not set in .env file.
    exit 2
fi

response=$(
    oauth2c https://id.twitch.tv/oauth2 \
        --auth-method client_secret_post \
        --client-id "$TWITCH_CLIENT_ID" \
        --client-secret "$TWITCH_CLIENT_SECRET" \
        --grant-type client_credentials 2>/dev/null
)
echo "Obtained response from OAuth 2.0: $response"

TWITCH_ACCESS_TOKEN=$(jq --raw-output '.access_token' <<<"$response")

curl -X GET 'https://api.twitch.tv/helix/eventsub/subscriptions' \
    -H "Authorization: Bearer $TWITCH_ACCESS_TOKEN" \
    -H "Client-Id: $TWITCH_CLIENT_ID" 2>/dev/null | jq

curl -X POST 'https://id.twitch.tv/oauth2/revoke' \
    -H 'Content-Type: application/x-www-form-urlencoded' \
    -d "client_id=$TWITCH_CLIENT_ID&token=$TWITCH_ACCESS_TOKEN"
