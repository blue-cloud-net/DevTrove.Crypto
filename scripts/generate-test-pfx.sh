#!/bin/bash

# PFX / PKCS#12 测试素材生成脚本（openssl）
# 生成两个 PFX 素材用于固定测试：
# - key-and-cert.pfx    : RSA 2048 私钥 + 自签名证书，密码 test1234
# - key-cert-chain.pfx  : RSA 3072 私钥 + 叶子证书 + CA 证书链，密码 test1234
# 依赖: generate-test-certs.sh 生成的证书
#
# 生成参数（作为单元测试断言依据）：
# - 密码: test1234
# - friendly name: test / leaf

set -e

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

if [ ! -f "$CERTS_DIR/rsa-2048-selfsigned-ext.pem" ] || [ ! -f "$CERTS_DIR/ca.crt" ]; then
    echo "证书素材不存在，正在生成..."
    "$SCRIPT_DIR/generate-test-certs.sh"
fi

# 1. 私钥 + 自签名证书（无链）
openssl pkcs12 -export \
    -out "$OUTPUT_DIR/key-and-cert.pfx" \
    -inkey "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -in "$CERTS_DIR/rsa-2048-selfsigned-ext.pem" \
    -passout "pass:$PASSWORD" -name test 2>/dev/null
echo "  ✓ key-and-cert.pfx"

# 2. 私钥 + 叶子证书 + CA 链
openssl pkcs12 -export \
    -out "$OUTPUT_DIR/key-cert-chain.pfx" \
    -inkey "$KEYS_DIR/rsa-3072-pkcs1.pem" \
    -in "$CERTS_DIR/leaf.crt" \
    -certfile "$CERTS_DIR/ca.crt" \
    -passout "pass:$PASSWORD" -name leaf 2>/dev/null
echo "  ✓ key-cert-chain.pfx"

echo "完成。"
