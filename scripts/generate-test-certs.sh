#!/bin/bash

# 证书测试素材生成脚本（openssl）
# 生成含扩展自签名证书、CA + 叶子证书链、极简自签名证书，用于固定测试。
# 依赖: generate-test-keys.sh 生成的密钥文件
#
# 生成参数（作为单元测试断言依据）：
# - rsa-2048-selfsigned-ext.pem : RSA 2048，serial 1001，CN=test.example.com，SAN/KU/EKU/SKI/AKI/CRLDP
# - ec-p256-selfsigned-ext.pem  : EC P-256，serial 2001，CN=ec-test.example.com，SAN/KU/EKU/SKI/AKI
# - rsa-2048-minimal.pem        : RSA 2048，serial 3001，CN=minimal.example.com，无扩展
# - ca.crt / leaf.crt           : CA(serial A001, CN=Test Root CA) + 叶子(serial B001, CN=leaf.example.com)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
KEYS_DIR="$PROJECT_ROOT/tests/data/keys"
OUTPUT_DIR="$PROJECT_ROOT/tests/data/certs"

echo "====================================="
echo "证书测试素材生成"
echo "====================================="

mkdir -p "$OUTPUT_DIR"
echo "输出目录: $OUTPUT_DIR"

# 检查密钥文件
if [ ! -f "$KEYS_DIR/rsa-2048-pkcs1.pem" ]; then
    echo "密钥文件不存在，正在生成..."
    "$SCRIPT_DIR/generate-test-keys.sh"
fi

# 写入临时 openssl 配置
write_req_ext() {
    cat > "$OUTPUT_DIR/temp_req_ext.cnf" <<EOF
[req]
distinguished_name = dn
prompt = no
req_extensions = req_ext
x509_extensions = req_ext

[dn]

[req_ext]
$1
EOF
}

# ============================================
# 1. RSA 自签名证书（完整扩展）
# ============================================
echo "[1/5] RSA 自签名证书 (含扩展)..."
write_req_ext "keyUsage = critical,digitalSignature,keyEncipherment
extendedKeyUsage = serverAuth,clientAuth
subjectAltName = DNS:test.example.com,DNS:www.test.example.com,IP:192.168.1.100
subjectKeyIdentifier = hash
crlDistributionPoints = URI:http://crl.example.com/test.crl"

openssl req -x509 -new -key "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-2048-selfsigned-ext.pem" \
    -days 365 -set_serial 0x1001 \
    -subj "/C=CN/ST=Beijing/L=Beijing/O=Test Corp/OU=IT Department/CN=test.example.com/emailAddress=test@example.com" \
    -config "$OUTPUT_DIR/temp_req_ext.cnf" 2>/dev/null
echo "  ✓ rsa-2048-selfsigned-ext.pem"

# ============================================
# 2. EC 自签名证书（含扩展）
# ============================================
echo "[2/5] EC 自签名证书 (含扩展)..."
write_req_ext "keyUsage = critical,digitalSignature
extendedKeyUsage = serverAuth
subjectAltName = DNS:ec-test.example.com,DNS:api.ec-test.example.com
subjectKeyIdentifier = hash"

openssl req -x509 -new -key "$KEYS_DIR/ec-p256-pkcs8.pem" \
    -out "$OUTPUT_DIR/ec-p256-selfsigned-ext.pem" \
    -days 365 -set_serial 0x2001 \
    -subj "/C=CN/O=EC Corp/CN=ec-test.example.com" \
    -config "$OUTPUT_DIR/temp_req_ext.cnf" 2>/dev/null
echo "  ✓ ec-p256-selfsigned-ext.pem"

# ============================================
# 3. 极简自签名证书（无扩展）
# ============================================
echo "[3/5] 极简自签名证书 (无扩展)..."
write_req_ext ""
openssl req -x509 -new -key "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-2048-minimal.pem" \
    -days 365 -set_serial 0x3001 \
    -subj "/C=CN/CN=minimal.example.com" \
    -config "$OUTPUT_DIR/temp_req_ext.cnf" 2>/dev/null
echo "  ✓ rsa-2048-minimal.pem"

# ============================================
# 4. CA 证书 + 叶子证书（证书链）
# ============================================
echo "[4/5] CA + 叶子证书链..."

# CA 证书配置
cat > "$OUTPUT_DIR/temp_ca_ext.cnf" <<EOF
[req]
distinguished_name = dn
prompt = no
req_extensions = req_ext
x509_extensions = req_ext

[dn]

[req_ext]
basicConstraints = critical,CA:TRUE,pathlen:1
keyUsage = critical,keyCertSign,cRLSign
subjectKeyIdentifier = hash
EOF

openssl req -x509 -new -key "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -out "$OUTPUT_DIR/ca.crt" \
    -days 3650 -set_serial 0xA001 \
    -subj "/C=CN/O=Test Org/CN=Test Root CA" \
    -config "$OUTPUT_DIR/temp_ca_ext.cnf" 2>/dev/null
echo "  ✓ ca.crt"

# 叶子证书 CSR
openssl req -new -key "$KEYS_DIR/rsa-3072-pkcs1.pem" \
    -out "$OUTPUT_DIR/leaf.csr" \
    -subj "/C=CN/O=Test Org/CN=leaf.example.com" 2>/dev/null

# 叶子证书扩展
cat > "$OUTPUT_DIR/temp_leaf_ext.cnf" <<EOF
basicConstraints = CA:FALSE
keyUsage = critical,digitalSignature,keyEncipherment
extendedKeyUsage = serverAuth
subjectAltName = DNS:leaf.example.com
EOF

openssl x509 -req -in "$OUTPUT_DIR/leaf.csr" \
    -CA "$OUTPUT_DIR/ca.crt" \
    -CAkey "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -days 365 -set_serial 0xB001 \
    -extfile "$OUTPUT_DIR/temp_leaf_ext.cnf" \
    -out "$OUTPUT_DIR/leaf.crt" 2>/dev/null
echo "  ✓ leaf.crt"

# ============================================
# 5. DSA 自签名证书
# ============================================
echo "[5/6] DSA 自签名证书 (含扩展)..."
write_req_ext "keyUsage = critical,digitalSignature"

openssl req -x509 -new -key "$KEYS_DIR/dsa-2048-private.pem" \
    -out "$OUTPUT_DIR/dsa-2048-selfsigned.pem" \
    -days 365 -set_serial 0x5001 \
    -subj "/C=CN/O=DSA Corp/CN=dsa-test.example.com" \
    -config "$OUTPUT_DIR/temp_req_ext.cnf" 2>/dev/null
echo "  ✓ dsa-2048-selfsigned.pem"

# ============================================
# 6. 清理
# ============================================
echo "[6/6] 清理临时文件..."
rm -f "$OUTPUT_DIR"/temp_*.cnf

echo "完成。"
# 提示：SM2 证书由 generate-test-sm-certs.sh（tongsuo）单独生成
