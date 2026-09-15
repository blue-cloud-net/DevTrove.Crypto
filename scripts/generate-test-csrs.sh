#!/bin/bash

# CSR (证书签名请求) 生成脚本
# 用于生成不同参数和算法的 CSR，用于测试和开发
# 依赖: generate-test-keys.sh

set -e

# 唯一外部工具：tongsuo（roadmap RM-0.0.9a/9e）
: "${TONGSUO_PATH:=/opt/tongsuo/bin/tongsuo}"
if [[ ! -x "$TONGSUO_PATH" ]]; then
  echo "tongsuo not found at $TONGSUO_PATH (override with TONGSUO_PATH)" >&2
  exit 127
fi
TONGSUO_BIN="$TONGSUO_PATH"

# 设置颜色输出
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# 脚本目录和输出目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
KEYS_DIR="$PROJECT_ROOT/tests/data/keys"
OUTPUT_DIR="$PROJECT_ROOT/tests/data/csrs"

echo -e "${BLUE}=====================================${NC}"
echo -e "${BLUE}CSR 生成脚本${NC}"
echo -e "${BLUE}=====================================${NC}"
echo ""

# ============================================
# 检查并生成密钥
# ============================================
echo -e "${YELLOW}检查密钥文件...${NC}"

if [ ! -f "$KEYS_DIR/rsa-2048-pkcs1.pem" ]; then
    echo -e "${YELLOW}密钥文件不存在，正在生成...${NC}"
    echo ""
    "$SCRIPT_DIR/generate-test-keys.sh"
    echo ""
    echo -e "${GREEN}✓${NC} 密钥生成完成"
else
    echo -e "${GREEN}✓${NC} 密钥文件已存在"
fi

echo ""

# 创建输出目录
mkdir -p "$OUTPUT_DIR"
echo -e "${GREEN}✓${NC} 输出目录: $OUTPUT_DIR"
echo ""

# ============================================
# 辅助函数：生成配置文件
# ============================================
generate_config() {
    local cn="$1"
    local o="$2"
    local ou="$3"
    local c="$4"
    local st="$5"
    local l="$6"
    local email="$7"
    local san="$8"

    cat > "$OUTPUT_DIR/temp_ext.cnf" << EOF
[req]
default_bits = 2048
prompt = no
default_md = sha256
distinguished_name = dn
req_extensions = req_ext

[dn]
CN = ${cn}
O = ${o}
OU = ${ou}
C = ${c}
ST = ${st}
L = ${l}
emailAddress = ${email}

[req_ext]
${san}
EOF
}

# ============================================
# RSA 密钥的 CSR
# ============================================
echo -e "${YELLOW}[1/6] 生成 RSA 密钥的 CSR...${NC}"

# 基本 CSR - RSA 2048
echo "  - 基本 CSR (RSA 2048, CN=test.example.com)"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-2048-basic.csr" \
    -subj "/C=CN/ST=Beijing/L=Beijing/O=Test Corp/OU=IT Department/CN=test.example.com/emailAddress=test@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-2048-basic.csr"

# 带 SAN 的 CSR - RSA 2048
echo "  - 带 SAN 的 CSR (RSA 2048, 多个域名)"
generate_config "multi.example.com" "Test Corp" "IT Department" "CN" "Beijing" "Beijing" "multi@example.com" \
"subjectAltName = DNS:multi.example.com,DNS:www.multi.example.com,DNS:api.multi.example.com,IP:192.168.1.100"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-2048-san.csr" \
    -config "$OUTPUT_DIR/temp_ext.cnf" 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-2048-san.csr"

# 通配符域名 CSR - RSA 3072
echo "  - 通配符域名 CSR (RSA 3072, *.example.com)"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/rsa-3072-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-3072-wildcard.csr" \
    -subj "/C=US/ST=California/L=San Francisco/O=Example Inc/OU=Web Services/CN=*.example.com/emailAddress=wildcard@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-3072-wildcard.csr"

# 长 DN 字段 CSR - RSA 4096
echo "  - 长 DN 字段 CSR (RSA 4096)"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/rsa-4096-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-4096-long-dn.csr" \
    -subj "/C=CN/ST=Guangdong Province/L=Shenzhen City/O=Technology Innovation Company Ltd/OU=Research and Development Department/CN=secure.technology-innovation.example.com/emailAddress=security@technology-innovation.example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-4096-long-dn.csr"

echo ""

# ============================================
# EC 密钥的 CSR (P-256)
# ============================================
echo -e "${YELLOW}[2/6] 生成 EC P-256 密钥的 CSR...${NC}"

# 基本 CSR - EC P-256
echo "  - 基本 CSR (EC P-256, CN=ec-test.example.com)"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/ec-p256-pkcs8.pem" \
    -out "$OUTPUT_DIR/ec-p256-basic.csr" \
    -subj "/C=CN/ST=Shanghai/L=Shanghai/O=EC Test Corp/OU=Security/CN=ec-test.example.com/emailAddress=ec-test@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} ec-p256-basic.csr"

# 带 SAN 的 CSR - EC P-256
echo "  - 带 SAN 的 CSR (EC P-256, 包含 IP 地址)"
generate_config "ec-multi.example.com" "EC Corp" "DevOps" "CN" "Shanghai" "Shanghai" "ec-multi@example.com" \
"subjectAltName = DNS:ec-multi.example.com,DNS:*.ec-multi.example.com,IP:10.0.0.1,IP:2001:db8::1"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/ec-p256-pkcs8.pem" \
    -out "$OUTPUT_DIR/ec-p256-san.csr" \
    -config "$OUTPUT_DIR/temp_ext.cnf" 2>/dev/null
echo -e "    ${GREEN}✓${NC} ec-p256-san.csr"

echo ""

# ============================================
# EC 密钥的 CSR (P-384)
# ============================================
echo -e "${YELLOW}[3/6] 生成 EC P-384 密钥的 CSR...${NC}"

# 基本 CSR - EC P-384
echo "  - 基本 CSR (EC P-384, CN=p384.example.com)"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/ec-p384-pkcs8.pem" \
    -out "$OUTPUT_DIR/ec-p384-basic.csr" \
    -subj "/C=JP/ST=Tokyo/L=Shibuya/O=P384 Company/OU=Engineering/CN=p384.example.com/emailAddress=p384@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} ec-p384-basic.csr"

# 高安全性 CSR - EC P-384
echo "  - 高安全性 CSR (EC P-384, 使用 SHA-384)"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/ec-p384-pkcs8.pem" \
    -sha384 \
    -out "$OUTPUT_DIR/ec-p384-sha384.csr" \
    -subj "/C=US/ST=New York/L=New York/O=High Security Corp/OU=InfoSec/CN=secure-p384.example.com/emailAddress=security@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} ec-p384-sha384.csr"

echo ""

# ============================================
# EC 密钥的 CSR (P-521)
# ============================================
echo -e "${YELLOW}[4/6] 生成 EC P-521 密钥的 CSR...${NC}"

# 基本 CSR - EC P-521
echo "  - 基本 CSR (EC P-521, CN=p521.example.com)"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/ec-p521-pkcs8.pem" \
    -out "$OUTPUT_DIR/ec-p521-basic.csr" \
    -subj "/C=GB/ST=England/L=London/O=P521 Ltd/OU=Cryptography/CN=p521.example.com/emailAddress=p521@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} ec-p521-basic.csr"

# 最高安全性 CSR - EC P-521
echo "  - 最高安全性 CSR (EC P-521, 使用 SHA-512)"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/ec-p521-pkcs8.pem" \
    -sha512 \
    -out "$OUTPUT_DIR/ec-p521-sha512.csr" \
    -subj "/C=CH/ST=Zurich/L=Zurich/O=Maximum Security AG/OU=Cyber Security/CN=max-secure.example.com/emailAddress=max-security@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} ec-p521-sha512.csr"

echo ""

# ============================================
# DSA 密钥的 CSR
# ============================================
echo -e "${YELLOW}生成 DSA 密钥的 CSR...${NC}"

# 基本 CSR - DSA 2048
echo "  - 基本 CSR (DSA 2048, CN=dsa-csr.example.com)"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/dsa-2048-private.pem" \
    -out "$OUTPUT_DIR/dsa-2048-basic.csr" \
    -subj "/C=CN/O=DSA Test Corp/OU=Security/CN=dsa-csr.example.com/emailAddress=dsa@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} dsa-2048-basic.csr"

echo ""

# ============================================
# SM2 密钥的 CSR (国密)
# ============================================
echo -e "${YELLOW}[5/6] 生成 SM2 密钥的 CSR...${NC}"

if [ -f "$KEYS_DIR/sm2-pkcs8.pem" ]; then
    TONGSUO="${TONGSUO_PATH:-/opt/tongsuo/bin/tongsuo}"
    # 基本 CSR - SM2
    echo "  - 基本 CSR (SM2, CN=sm2-test.example.cn)"
    if "$TONGSUO" req -new -key "$KEYS_DIR/sm2-pkcs8.pem" \
        -out "$OUTPUT_DIR/sm2-basic.csr" \
        -subj "/C=CN/ST=Beijing/L=Beijing/O=SM2 Test Corp/OU=国密部门/CN=sm2-test.example.cn/emailAddress=sm2@example.cn" 2>/dev/null; then
        echo -e "    ${GREEN}✓${NC} sm2-basic.csr"
    else
        echo -e "    ${YELLOW}⚠${NC}  sm2-basic.csr (生成可能不完整)"
    fi

    # 带 SAN 的 CSR - SM2
    echo "  - 带 SAN 的 CSR (SM2, 多个国密域名)"
    # 注意：tongsuo 的 SM2 签名使用 SM3 摘要，配置中不能使用 default_md=sha256（会导致签名失败）
    cat > "$OUTPUT_DIR/temp_ext.cnf" <<EOF
[req]
distinguished_name = dn
prompt = no
req_extensions = req_ext

[dn]

[req_ext]
subjectAltName = DNS:sm2-multi.example.cn,DNS:www.sm2-multi.example.cn,DNS:api.sm2-multi.example.cn
EOF
    if "$TONGSUO" req -new -key "$KEYS_DIR/sm2-pkcs8.pem" \
        -out "$OUTPUT_DIR/sm2-san.csr" \
        -subj "/C=CN/ST=Beijing/L=Beijing/O=国密科技公司/OU=研发中心/CN=sm2-multi.example.cn/emailAddress=sm2-multi@example.cn" \
        -config "$OUTPUT_DIR/temp_ext.cnf" 2>/dev/null; then
        echo -e "    ${GREEN}✓${NC} sm2-san.csr"
    else
        echo -e "    ${YELLOW}⚠${NC}  sm2-san.csr (生成可能不完整)"
    fi
else
    echo -e "  ${YELLOW}⚠${NC}  SM2 密钥不存在，跳过 SM2 CSR 生成"
fi

echo ""

# ============================================
# 特殊场景的 CSR
# ============================================
echo -e "${YELLOW}[6/6] 生成特殊场景的 CSR...${NC}"

# 纯 IP 地址 CSR
echo "  - 纯 IP 地址 CSR (无域名)"
generate_config "192.168.1.1" "IP Only Corp" "Network" "CN" "Beijing" "Beijing" "ip@example.com" \
"subjectAltName = IP:192.168.1.1,IP:192.168.1.2,IP:10.0.0.1"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-2048-ip-only.csr" \
    -config "$OUTPUT_DIR/temp_ext.cnf" 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-2048-ip-only.csr"

# 邮件证书 CSR
echo "  - 邮件证书 CSR (emailAddress in SAN)"
generate_config "user@example.com" "Mail Corp" "Email Services" "CN" "Shanghai" "Shanghai" "admin@example.com" \
"subjectAltName = email:user@example.com,email:admin@example.com"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-2048-email.csr" \
    -config "$OUTPUT_DIR/temp_ext.cnf" 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-2048-email.csr"

# 代码签名 CSR
echo "  - 代码签名 CSR"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-2048-codesign.csr" \
    -subj "/C=US/ST=Washington/L=Redmond/O=Software Corp/OU=Development/CN=Code Signing Certificate/emailAddress=codesign@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-2048-codesign.csr"

# 客户端认证 CSR
echo "  - 客户端认证 CSR"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/ec-p256-pkcs8.pem" \
    -out "$OUTPUT_DIR/ec-p256-client.csr" \
    -subj "/C=CN/ST=Guangdong/L=Guangzhou/O=Client Corp/OU=Users/CN=client-certificate/emailAddress=client@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} ec-p256-client.csr"

# 多 OU 字段 CSR
echo "  - 多层组织单位 CSR"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-2048-multi-ou.csr" \
    -subj "/C=CN/ST=Zhejiang/L=Hangzhou/O=Multi-OU Corp/OU=Level 1/OU=Level 2/OU=Level 3/CN=multi-ou.example.com/emailAddress=multi-ou@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-2048-multi-ou.csr"

# 国际化域名 CSR (IDN)
echo "  - 国际化域名 CSR (中文域名)"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-2048-idn.csr" \
    -subj "/C=CN/ST=北京/L=北京/O=中文测试公司/OU=测试部门/CN=测试.example.com/emailAddress=test@测试.com" \
    -utf8 2>/dev/null || \
${TONGSUO_BIN} req -new -key "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-2048-idn.csr" \
    -subj "/C=CN/ST=Beijing/L=Beijing/O=IDN Test Corp/CN=xn--test.example.com/emailAddress=idn@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-2048-idn.csr"

# DER 格式 CSR
echo "  - DER 格式 CSR"
${TONGSUO_BIN} req -new -key "$KEYS_DIR/rsa-2048-pkcs1.pem" \
    -out "$OUTPUT_DIR/rsa-2048-basic.der" \
    -outform DER \
    -subj "/C=CN/ST=Beijing/L=Beijing/O=DER Test Corp/CN=der-test.example.com/emailAddress=der@example.com" 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-2048-basic.der"

echo ""

# 清理临时文件
rm -f "$OUTPUT_DIR/temp_ext.cnf"

# ============================================
# 生成 CSR 信息文件
# ============================================
echo -e "${YELLOW}生成 CSR 信息文件...${NC}"

cat > "$OUTPUT_DIR/README.md" << 'EOF'
# 测试 CSR 说明

本目录包含用于测试和开发的 CSR (证书签名请求) 文件。

## ⚠️ 安全警告

**这些 CSR 仅用于测试和开发目的，切勿在生产环境中使用！**

## CSR 列表

### RSA 密钥的 CSR

| 文件名 | 密钥类型 | 描述 | CN | SAN |
|--------|----------|------|----|----|
| `rsa-2048-basic.csr` | RSA 2048 | 基本 CSR | test.example.com | 无 |
| `rsa-2048-san.csr` | RSA 2048 | 带 SAN 的 CSR | multi.example.com | 3 个 DNS + 1 个 IP |
| `rsa-3072-wildcard.csr` | RSA 3072 | 通配符域名 | *.example.com | 无 |
| `rsa-4096-long-dn.csr` | RSA 4096 | 长 DN 字段 | secure.technology-innovation.example.com | 无 |

### EC 密钥的 CSR (P-256)

| 文件名 | 密钥类型 | 描述 | CN | SAN |
|--------|----------|------|----|----|
| `ec-p256-basic.csr` | EC P-256 | 基本 CSR | ec-test.example.com | 无 |
| `ec-p256-san.csr` | EC P-256 | 带 SAN (含 IPv6) | ec-multi.example.com | 2 个 DNS + 2 个 IP |

### EC 密钥的 CSR (P-384)

| 文件名 | 密钥类型 | 描述 | CN | 签名算法 |
|--------|----------|------|----|----------|
| `ec-p384-basic.csr` | EC P-384 | 基本 CSR | p384.example.com | SHA-256 |
| `ec-p384-sha384.csr` | EC P-384 | 高安全性 | secure-p384.example.com | SHA-384 |

### EC 密钥的 CSR (P-521)

| 文件名 | 密钥类型 | 描述 | CN | 签名算法 |
|--------|----------|------|----|----------|
| `ec-p521-basic.csr` | EC P-521 | 基本 CSR | p521.example.com | SHA-256 |
| `ec-p521-sha512.csr` | EC P-521 | 最高安全性 | max-secure.example.com | SHA-512 |

### SM2 密钥的 CSR (国密)

| 文件名 | 密钥类型 | 描述 | CN | SAN |
|--------|----------|------|----|----|
| `sm2-basic.csr` | SM2 | 基本国密 CSR | sm2-test.example.cn | 无 |
| `sm2-san.csr` | SM2 | 带 SAN 的国密 CSR | sm2-multi.example.cn | 3 个 DNS |

### 特殊场景的 CSR

| 文件名 | 密钥类型 | 用途 | 特点 |
|--------|----------|------|------|
| `rsa-2048-ip-only.csr` | RSA 2048 | IP 证书 | 仅包含 IP 地址，无域名 |
| `rsa-2048-email.csr` | RSA 2048 | 邮件证书 | SAN 包含邮件地址 |
| `rsa-2048-codesign.csr` | RSA 2048 | 代码签名 | 用于代码签名证书 |
| `ec-p256-client.csr` | EC P-256 | 客户端认证 | 用于客户端证书 |
| `rsa-2048-multi-ou.csr` | RSA 2048 | 多层组织 | 包含多个 OU 字段 |
| `rsa-2048-idn.csr` | RSA 2048 | 国际化域名 | 支持中文域名 |
| `rsa-2048-basic.der` | RSA 2048 | DER 格式 | 二进制格式的 CSR |

## 重新生成 CSR

运行以下命令重新生成所有测试 CSR:

```bash
cd /path/to/DevTrove.Crypto
./scripts/generate-test-csrs.sh
```

## 查看 CSR 信息

### 查看 PEM 格式 CSR
```bash
${TONGSUO_BIN} req -in rsa-2048-basic.csr -text -noout
```

### 查看 DER 格式 CSR
```bash
${TONGSUO_BIN} req -in rsa-2048-basic.der -inform DER -text -noout
```

### 验证 CSR 签名
```bash
${TONGSUO_BIN} req -in rsa-2048-basic.csr -verify -noout
```

### 查看 CSR 的公钥
```bash
${TONGSUO_BIN} req -in rsa-2048-basic.csr -pubkey -noout
```

### 提取 CSR 的主题信息
```bash
${TONGSUO_BIN} req -in rsa-2048-basic.csr -subject -noout
```

### 查看 SAN (Subject Alternative Names)
```bash
${TONGSUO_BIN} req -in rsa-2048-san.csr -text -noout | grep -A 1 "Subject Alternative Name"
```

## CSR 格式说明

- **PEM 格式**: 文本格式，以 `-----BEGIN CERTIFICATE REQUEST-----` 开头
- **DER 格式**: 二进制格式，直接存储 ASN.1 结构
- **Subject DN**: 主题可分辨名称，包含 CN、O、OU、C、ST、L 等字段
- **SAN**: 主题备用名称，可以包含多个 DNS、IP、Email 等

## 使用场景

### 1. 服务器证书 CSR
- 用于 HTTPS/TLS 服务器
- 通常包含域名的 CN 和 SAN
- 示例: `rsa-2048-basic.csr`, `rsa-2048-san.csr`

### 2. 通配符证书 CSR
- 用于保护多个子域名
- CN 以 `*.` 开头
- 示例: `rsa-3072-wildcard.csr`

### 3. 客户端证书 CSR
- 用于客户端身份认证
- 示例: `ec-p256-client.csr`

### 4. 代码签名证书 CSR
- 用于软件、驱动程序签名
- 示例: `rsa-2048-codesign.csr`

### 5. 邮件证书 CSR
- 用于 S/MIME 邮件加密和签名
- 示例: `rsa-2048-email.csr`

## 提交 CSR 到 CA

生成的 CSR 可以提交给证书颁发机构 (CA) 以获取签名证书:

1. **公共 CA**: Let's Encrypt, DigiCert, GlobalSign 等
2. **企业 CA**: 企业内部 PKI 系统
3. **自签名**: 使用 OpenSSL 自行签名 (仅用于测试)

## 安全建议

1. 这些 CSR 仅用于本地开发和测试
2. 生产环境应使用新生成的私钥和 CSR
3. 私钥必须妥善保管，不要泄露
4. CSR 可以公开，但应确保其准确性

---

生成日期: $(date '+%Y-%m-%d %H:%M:%S')
生成脚本: scripts/generate-test-csrs.sh
依赖脚本: scripts/generate-test-keys.sh
EOF

echo -e "${GREEN}✓${NC} README.md"
echo ""

# ============================================
# 统计信息
# ============================================
echo -e "${BLUE}=====================================${NC}"
echo -e "${BLUE}CSR 生成完成！${NC}"
echo -e "${BLUE}=====================================${NC}"
echo ""
echo "输出目录: $OUTPUT_DIR"
echo "生成文件数: $(ls -1 "$OUTPUT_DIR" | wc -l)"
echo ""
echo -e "${GREEN}所有 CSR 已成功生成！${NC}"
echo ""
echo "提示:"
echo "  - 查看 CSR 列表: ls -lh $OUTPUT_DIR"
echo "  - 查看 CSR 说明: cat $OUTPUT_DIR/README.md"
echo "  - 查看 CSR 内容: tongsuo req -in $OUTPUT_DIR/rsa-2048-basic.csr -text -noout"
echo "  - 验证 CSR: tongsuo req -in $OUTPUT_DIR/rsa-2048-basic.csr -verify -noout"
echo ""
