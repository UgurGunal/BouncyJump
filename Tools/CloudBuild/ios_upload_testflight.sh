#!/usr/bin/env bash
# Unity Build Automation post-build: upload signed IPA to App Store Connect / TestFlight.
# Requires env vars on the build target (or org): ASC_KEY_ID, ASC_ISSUER_ID, and
# ASC_API_KEY_P8 (PEM text) or ASC_API_KEY_P8_BASE64 (base64 of the .p8 file).

set -euo pipefail

echo "==> iOS TestFlight upload (post-build)"

if [[ -z "${ASC_KEY_ID:-}" || -z "${ASC_ISSUER_ID:-}" ]]; then
  echo "ERROR: Set ASC_KEY_ID and ASC_ISSUER_ID on the Unity Build Automation target."
  exit 1
fi

if [[ -z "${ASC_API_KEY_P8:-}" && -z "${ASC_API_KEY_P8_BASE64:-}" ]]; then
  echo "ERROR: Set ASC_API_KEY_P8 or ASC_API_KEY_P8_BASE64 on the Unity Build Automation target."
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

if [[ -n "${ASC_API_KEY_P8_BASE64:-}" ]]; then
  echo "${ASC_API_KEY_P8_BASE64}" | base64 -D > "${KEY_FILE}" 2>/dev/null \
    || echo "${ASC_API_KEY_P8_BASE64}" | base64 -d > "${KEY_FILE}"
else
  printf '%s\n' "${ASC_API_KEY_P8}" > "${KEY_FILE}"
fi
chmod 600 "${KEY_FILE}"

UPLOAD_OK=0

if xcrun altool --upload-app \
  --type ios \
  --file "${IPA_PATH}" \
  --apiKey "${ASC_KEY_ID}" \
  --apiIssuer "${ASC_ISSUER_ID}" \
  --verbose; then
  UPLOAD_OK=1
else
  echo "altool failed; trying iTMSTransporter..."
  if xcrun iTMSTransporter -m upload \
    -assetFile "${IPA_PATH}" \
    -apiKey "${ASC_KEY_ID}" \
    -apiIssuer "${ASC_ISSUER_ID}" \
    -v informational; then
    UPLOAD_OK=1
  fi
fi

rm -f "${KEY_FILE}"

if [[ "${UPLOAD_OK}" -ne 1 ]]; then
  echo "ERROR: App Store Connect upload failed."
  exit 1
fi

echo "==> Upload submitted. Check App Store Connect → TestFlight (Processing)."
