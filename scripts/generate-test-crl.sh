#!/bin/bash

# CRL 测试素材生成脚本（openssl）
# 生成 RSA / EC / DSA 三种算法的 CRL 测试素材，供"素材双方解析对照"测试使用。
# 输出:
# - tests/data/crls/test.crl : RSA，颁发者 CN=Test CRL CA，吊销 serial 1111(keyCompromise)/2222(superseded)
# - tests/data/crls/ec.crl   : EC P-256，颁发者 CN=EC Test CRL CA，吊销 serial EC01(keyCompromise)
# - tests/data/crls/dsa.crl  : DSA，颁发者 CN=DSA Test CRL CA，吊销 serial D500(keyCompromise)
# 吊销时间由生成时刻决定，测试仅断言条目的存在与序列号

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
KEYS_DIR="$PROJECT_ROOT/tests/data/keys"
OUTPUT_DIR="$PROJECT_ROOT/tests/data/crls"

echo "====================================="
echo "CRL 测试素材生成"
echo "====================================="

mkdir -p "$OUTPUT_DIR"
if [ ! -f "$KEYS_DIR/rsa-2048-pkcs1.pem" ]; then
    "$SCRIPT_DIR/generate-test-keys.sh"
fi

# 为给定算法/密钥生成一份 CRL（复用最小 CA 数据库）
# $1: 算法标识（用于临时目录命名） $2: CA 私钥 $3: 输出文件 $4: CA 颁发者 DN
# $5: 吊销条目 "serial:reason" 列表（空格分隔）
gen_crl() {
    local algo="$1"
    local key="$2"
    local out="$3"
    local issuer="$4"
    local revoked="$5"

    local temp_dir="$OUTPUT_DIR/temp_ca_db_${algo}"
    rm -rf "$temp_dir"
    mkdir -p "$temp_dir/newcerts"
    touch "$temp_dir/index.txt"
    echo "unique_subject = no" > "$temp_dir/index.txt.attr"
    echo "01" > "$temp_dir/serial"

    # CA 配置
    cat > "$temp_dir/ca.cnf" <<EOF
[ ca ]
default_ca = CA_default

[ CA_default ]
dir = $temp_dir
database = $temp_dir/index.txt
new_certs_dir = $temp_dir/newcerts
certificate = $temp_dir/ca.crt
private_key = $temp_dir/ca.key
serial = $temp_dir/serial
default_md = sha256
default_days = 365
default_crl_days = 30
policy = policy_any

[ policy_any ]
commonName = supplied
EOF

    # CA 自签名证书
    openssl req -x509 -new -key "$key" \
        -out "$temp_dir/ca.crt" -days 3650 -set_serial 0xCA01 \
        -subj "$issuer" 2>/dev/null

    # CA 私钥（供 openssl ca 使用）
    cp "$key" "$temp_dir/ca.key"

    # 生成叶子证书并吊销
    for entry in $revoked; do
        serial="${entry%%:*}"
        reason="${entry##*:}"
        cert="$temp_dir/cert-$serial.pem"
        csr="$temp_dir/cert-$serial.csr"

        openssl req -new -key "$key" \
            -out "$csr" -subj "/C=CN/O=CRL Org/CN=revoked-$serial" 2>/dev/null

        # 指定序列号签发
        echo "$serial" > "$temp_dir/serial"
        openssl ca -config "$temp_dir/ca.cnf" -batch -in "$csr" -out "$cert" \
            -startdate 20250101000000Z -enddate 20350101000000Z 2>/dev/null

        openssl ca -config "$temp_dir/ca.cnf" -batch -revoke "$cert" \
            -crl_reason "$reason" 2>/dev/null
    done

    # 生成 CRL
    openssl ca -config "$temp_dir/ca.cnf" -gencrl -out "$out" 2>/dev/null

    rm -rf "$temp_dir"
}

# 1. RSA CRL
echo "[1/3] RSA CRL..."
gen_crl "rsa" "$KEYS_DIR/rsa-2048-pkcs1.pem" "$OUTPUT_DIR/test.crl" \
    "/C=CN/O=CRL Org/CN=Test CRL CA" "1111:keyCompromise 2222:superseded"
echo "  ✓ test.crl"

# 2. EC CRL
echo "[2/3] EC CRL..."
gen_crl "ec" "$KEYS_DIR/ec-p256-pkcs8.pem" "$OUTPUT_DIR/ec.crl" \
    "/C=CN/O=EC CRL Org/CN=EC Test CRL CA" "EC01:keyCompromise"
echo "  ✓ ec.crl"

# 3. DSA CRL
echo "[3/3] DSA CRL..."
gen_crl "dsa" "$KEYS_DIR/dsa-2048-private.pem" "$OUTPUT_DIR/dsa.crl" \
    "/C=CN/O=DSA CRL Org/CN=DSA Test CRL CA" "D500:keyCompromise"
echo "  ✓ dsa.crl"

echo "完成。"
