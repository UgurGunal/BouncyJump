#!/usr/bin/env bash
# Unity Build Automation post-build: upload signed IPA to App Store Connect / TestFlight.
# Env vars (build target): ASC_KEY_ID, ASC_ISSUER_ID, and either
#   ASC_API_KEY_P8_BASE64  (preferred — one-line base64 of the .p8 file)
#   ASC_API_KEY_P8         (PEM text; Unity often flattens newlines — use \\n or base64)

set -euo pipefail

echo "==> iOS TestFlight upload (post-build)"

if [[ -z "${ASC_KEY_ID:-}" || -z "${ASC_ISSUER_ID:-}" ]]; then
  echo "ERROR: Set ASC_KEY_ID and ASC_ISSUER_ID on the Unity Build Automation target."
  exit 1
fi

if [[ -z "${ASC_API_KEY_P8:-}" && -z "${ASC_API_KEY_P8_BASE64:-}" ]]; then
  echo "ERROR: Set ASC_API_KEY_P8_BASE64 (preferred) or ASC_API_KEY_P8 on the target."
  exit 1
fi

IPA_PATH="${UNITY_PLAYER_PATH:-}"
if [[ -z "${IPA_PATH}" || ! -f "${IPA_PATH}" || "${IPA_PATH}" != *.ipa ]]; then
  echo "UNITY_PLAYER_PATH not an .ipa (got: ${IPA_PATH:-empty}); searching for IPA..."
  IPA_PATH="$(find "${WORKSPACE:-.}" -type f -name "*.ipa" 2>/dev/null | head -n 1 || true)"
fi

if [[ -z "${IPA_PATH}" || ! -f "${IPA_PATH}" ]]; then
  echo "ERROR: No .ipa found to upload."
  exit 1
fi

echo "IPA: ${IPA_PATH}"
echo "ASC Key ID: ${ASC_KEY_ID}"

KEY_DIR="${HOME}/.appstoreconnect/private_keys"
mkdir -p "${KEY_DIR}"
KEY_FILE="${KEY_DIR}/AuthKey_${ASC_KEY_ID}.p8"
API_JSON="$(mktemp /tmp/asc_api_key.XXXXXX.json)"
ALTOOL_LOG="$(mktemp /tmp/altool.XXXXXX.log)"

cleanup() {
  rm -f "${KEY_FILE}" "${API_JSON}" "${ALTOOL_LOG}"
}
trap cleanup EXIT

write_pem() {
  if [[ -n "${ASC_API_KEY_P8_BASE64:-}" ]]; then
    local b64
    b64="$(printf '%s' "${ASC_API_KEY_P8_BASE64}" | tr -d '[:space:]')"
    if ! printf '%s' "${b64}" | base64 -D > "${KEY_FILE}" 2>/dev/null; then
      printf '%s' "${b64}" | base64 -d > "${KEY_FILE}"
    fi
  else
    printf '%s' "${ASC_API_KEY_P8}" | sed 's/\\n/\n/g' > "${KEY_FILE}"
    [[ -n "$(tail -c1 "${KEY_FILE}" 2>/dev/null || true)" ]] && printf '\n' >> "${KEY_FILE}"
  fi
  chmod 600 "${KEY_FILE}"
}

write_pem

if ! grep -q "BEGIN PRIVATE KEY" "${KEY_FILE}"; then
  echo "ERROR: AuthKey file missing BEGIN PRIVATE KEY — ASC_API_KEY_P8(_BASE64) is malformed."
  exit 1
fi

echo "AuthKey bytes: $(wc -c < "${KEY_FILE}" | tr -d ' ')"

export KEY_FILE API_JSON

python3 - <<'PY'
import json, os
key_path = os.environ["KEY_FILE"]
with open(key_path, "r", encoding="utf-8") as f:
    pem = f.read()
payload = {
    "key_id": os.environ["ASC_KEY_ID"],
    "issuer_id": os.environ["ASC_ISSUER_ID"],
    "key": pem,
}
with open(os.environ["API_JSON"], "w", encoding="utf-8") as f:
    json.dump(payload, f)
PY

UPLOAD_OK=0

if command -v fastlane >/dev/null 2>&1; then
  echo "Trying fastlane pilot upload..."
  if fastlane pilot upload \
    --ipa "${IPA_PATH}" \
    --api_key_path "${API_JSON}" \
    --skip_waiting_for_build_processing; then
    UPLOAD_OK=1
  else
    echo "fastlane pilot failed."
  fi
else
  echo "fastlane not found; skipping pilot."
fi

if [[ "${UPLOAD_OK}" -ne 1 ]]; then
  echo "Trying altool..."
  set +e
  xcrun altool --upload-app \
    --type ios \
    --file "${IPA_PATH}" \
    --apiKey "${ASC_KEY_ID}" \
    --apiIssuer "${ASC_ISSUER_ID}" 2>&1 | tee "${ALTOOL_LOG}"
  ALTOOL_EXIT=${PIPESTATUS[0]}
  set -e

  if grep -qiE 'UPLOAD FAILED|Validation failed|ERROR:' "${ALTOOL_LOG}"; then
    echo "altool reported an upload/validation error."
  elif [[ "${ALTOOL_EXIT}" -eq 0 ]]; then
    UPLOAD_OK=1
  else
    echo "altool exited with status ${ALTOOL_EXIT}."
  fi
fi

if [[ "${UPLOAD_OK}" -ne 1 ]]; then
  echo "ERROR: App Store Connect upload failed. IPA was built but not accepted by Apple."
  exit 1
fi

echo "==> Upload submitted. Check App Store Connect → TestFlight (Processing)."
