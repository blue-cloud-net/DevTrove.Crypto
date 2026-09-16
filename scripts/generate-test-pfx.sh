#!/bin/bash

# PFX / PKCS#12 测试素材生成脚本（tongsuo）
# 生成三个 PFX 素材用于固定测试：
# - key-and-cert.pfx       : RSA 2048 私钥 + 自签名证书，密码 test1234
# - key-cert-chain.pfx     : RSA 3072 私钥 + 叶子证书 + CA 证书链，密码 test1234
# - sm2-key-and-cert.pfx   : SM2 私钥 + SM2 自签名证书，密码 test1234
# 依赖: generate-test-certs.sh 生成的证书
#
# 生成参数（作为单元测试断言依据）：
# - 密码: test1234
# - friendly name: test / leaf / sm2-test

set -e

# 唯一外部工具：tongsuo
: "${TONGSUO_PATH:=/opt/tongsuo/bin/tongsuo}"
if [[ ! -x "$TONGSUO_PATH" ]]; then
  echo "tongsuo not found at $TONGSUO_PATH (override with TONGSUO_PATH)" >&2
  exit 127
fi
TONGSUO_BIN="$TONGSUO_PATH"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
KEYS_DIR="$PROJECT_ROOT/tests/data/keys"
CERTS_DIR="$PROJECT_ROOT/tests/data/certs"
OUTPUT_DIR="$PROJECT_ROOT/tests/data/pfx"
PASSWORD="test1234"

echo "====================================="
echo "PFX 测试素材生成"
echo "====================================="

mkdir -p "$OUTPUT_DIR"

if [ ! -f "$CERTS_DIR/rsa-2048-selfsigned-ext.pem" ] || [ ! -f "$CERTS_DIR/ca.crt" ] || [ ! -f "$CERTS_DIR/sm2-selfsigned.pem" ]; then
    echo "证书素材不存在，正在生成..."
    "$SCRIPT_DIR/generate-test-certs.sh"
fi

# 1. 私钥 + 自签名证书（无链）
${TONGSUO_BIN} pkcs12 -export \
    -out "$OUTPUT_DIR/key-and-cert.pfx" \
    -inkey "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -in "$CERTS_DIR/rsa-2048-selfsigned-ext.pem" \
    -passout "pass:$PASSWORD" -name test 2>/dev/null
echo "  ✓ key-and-cert.pfx"

# 2. 私钥 + 叶子证书 + CA 链
${TONGSUO_BIN} pkcs12 -export \
    -out "$OUTPUT_DIR/key-cert-chain.pfx" \
    -inkey "$KEYS_DIR/rsa-3072-pkcs1.pem" \
    -in "$CERTS_DIR/leaf.crt" \
    -certfile "$CERTS_DIR/ca.crt" \
    -passout "pass:$PASSWORD" -name leaf 2>/dev/null
echo "  ✓ key-cert-chain.pfx"

# 3. SM2 私钥 + SM2 自签名证书（无链）
${TONGSUO_BIN} pkcs12 -export \
    -out "$OUTPUT_DIR/sm2-key-and-cert.pfx" \
    -inkey "$KEYS_DIR/sm2-pkcs8.pem" \
    -in "$CERTS_DIR/sm2-selfsigned.pem" \
    -passout "pass:$PASSWORD" -name sm2-test 2>/dev/null
echo "  ✓ sm2-key-and-cert.pfx"

echo "完成。"
