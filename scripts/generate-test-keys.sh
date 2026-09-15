#!/bin/bash

# 密钥生成脚本
# 用于生成不同算法和格式的密钥，用于测试和开发

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
NC='\033[0m' # No Color

# 脚本目录和输出目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
OUTPUT_DIR="$PROJECT_ROOT/tests/data/keys"

echo -e "${BLUE}=====================================${NC}"
echo -e "${BLUE}密钥生成脚本${NC}"
echo -e "${BLUE}=====================================${NC}"
echo ""

# 创建输出目录
mkdir -p "$OUTPUT_DIR"
echo -e "${GREEN}✓${NC} 输出目录: $OUTPUT_DIR"
echo ""

# ============================================
# RSA 密钥生成
# ============================================
echo -e "${YELLOW}[1/5] 生成 RSA 密钥...${NC}"

# RSA 2048 位
echo "  - RSA 2048 位 (PKCS#1 PEM)"
${TONGSUO_BIN} genrsa -out "$OUTPUT_DIR/rsa-2048-pkcs1.pem" 2048 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-2048-pkcs1.pem"

echo "  - RSA 2048 位 (PKCS#8 PEM)"
${TONGSUO_BIN} pkcs8 -topk8 -nocrypt -in "$OUTPUT_DIR/rsa-2048-pkcs1.pem" -out "$OUTPUT_DIR/rsa-2048-pkcs8.pem"
echo -e "    ${GREEN}✓${NC} rsa-2048-pkcs8.pem"

echo "  - RSA 2048 位公钥"
${TONGSUO_BIN} rsa -in "$OUTPUT_DIR/rsa-2048-pkcs1.pem" -pubout -out "$OUTPUT_DIR/rsa-2048-public.pem" 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-2048-public.pem"

# RSA 3072 位
echo "  - RSA 3072 位 (PKCS#1 PEM)"
${TONGSUO_BIN} genrsa -out "$OUTPUT_DIR/rsa-3072-pkcs1.pem" 3072 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-3072-pkcs1.pem"

echo "  - RSA 3072 位 (PKCS#8 PEM)"
${TONGSUO_BIN} pkcs8 -topk8 -nocrypt -in "$OUTPUT_DIR/rsa-3072-pkcs1.pem" -out "$OUTPUT_DIR/rsa-3072-pkcs8.pem"
echo -e "    ${GREEN}✓${NC} rsa-3072-pkcs8.pem"

# RSA 4096 位
echo "  - RSA 4096 位 (PKCS#1 PEM)"
${TONGSUO_BIN} genrsa -out "$OUTPUT_DIR/rsa-4096-pkcs1.pem" 4096 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-4096-pkcs1.pem"

echo "  - RSA 4096 位 (PKCS#8 PEM)"
${TONGSUO_BIN} pkcs8 -topk8 -nocrypt -in "$OUTPUT_DIR/rsa-4096-pkcs1.pem" -out "$OUTPUT_DIR/rsa-4096-pkcs8.pem"
echo -e "    ${GREEN}✓${NC} rsa-4096-pkcs8.pem"

# RSA DER 格式
echo "  - RSA 2048 位 (PKCS#8 DER)"
${TONGSUO_BIN} pkcs8 -topk8 -nocrypt -in "$OUTPUT_DIR/rsa-2048-pkcs1.pem" -outform DER -out "$OUTPUT_DIR/rsa-2048-pkcs8.der"
echo -e "    ${GREEN}✓${NC} rsa-2048-pkcs8.der"

echo ""

# ============================================
# EC 密钥生成 (secp256r1 / P-256)
# ============================================
echo -e "${YELLOW}[2/5] 生成 EC 密钥 (secp256r1 / P-256)...${NC}"

echo "  - EC P-256 (PKCS#8 PEM)"
${TONGSUO_BIN} ecparam -name prime256v1 -genkey -noout -out "$OUTPUT_DIR/ec-p256-pkcs8.pem"
echo -e "    ${GREEN}✓${NC} ec-p256-pkcs8.pem"

echo "  - EC P-256 公钥"
${TONGSUO_BIN} ec -in "$OUTPUT_DIR/ec-p256-pkcs8.pem" -pubout -out "$OUTPUT_DIR/ec-p256-public.pem" 2>/dev/null
echo -e "    ${GREEN}✓${NC} ec-p256-public.pem"

echo "  - EC P-256 (DER)"
${TONGSUO_BIN} ec -in "$OUTPUT_DIR/ec-p256-pkcs8.pem" -outform DER -out "$OUTPUT_DIR/ec-p256-pkcs8.der" 2>/dev/null
echo -e "    ${GREEN}✓${NC} ec-p256-pkcs8.der"

echo ""

# ============================================
# EC 密钥生成 (secp384r1 / P-384)
# ============================================
echo -e "${YELLOW}[3/5] 生成 EC 密钥 (secp384r1 / P-384)...${NC}"

echo "  - EC P-384 (PKCS#8 PEM)"
${TONGSUO_BIN} ecparam -name secp384r1 -genkey -noout -out "$OUTPUT_DIR/ec-p384-pkcs8.pem"
echo -e "    ${GREEN}✓${NC} ec-p384-pkcs8.pem"

echo "  - EC P-384 公钥"
${TONGSUO_BIN} ec -in "$OUTPUT_DIR/ec-p384-pkcs8.pem" -pubout -out "$OUTPUT_DIR/ec-p384-public.pem" 2>/dev/null
echo -e "    ${GREEN}✓${NC} ec-p384-public.pem"

echo ""

# ============================================
# EC 密钥生成 (secp521r1 / P-521)
# ============================================
echo -e "${YELLOW}[4/5] 生成 EC 密钥 (secp521r1 / P-521)...${NC}"

echo "  - EC P-521 (PKCS#8 PEM)"
${TONGSUO_BIN} ecparam -name secp521r1 -genkey -noout -out "$OUTPUT_DIR/ec-p521-pkcs8.pem"
echo -e "    ${GREEN}✓${NC} ec-p521-pkcs8.pem"

echo "  - EC P-521 公钥"
${TONGSUO_BIN} ec -in "$OUTPUT_DIR/ec-p521-pkcs8.pem" -pubout -out "$OUTPUT_DIR/ec-p521-public.pem" 2>/dev/null
echo -e "    ${GREEN}✓${NC} ec-p521-public.pem"

echo ""

# ============================================
# SM2 密钥生成 (使用 tongsuo)
# ============================================
echo -e "${YELLOW}[5/5] 生成 SM2 密钥...${NC}"
TONGSUO="${TONGSUO_PATH:-/opt/tongsuo/bin/tongsuo}"

if [ -x "$TONGSUO" ]; then
    echo "  - SM2 (PKCS#8 PEM)"
    "$TONGSUO" genpkey -algorithm EC -pkeyopt ec_paramgen_curve:SM2 -out "$OUTPUT_DIR/sm2-pkcs8.pem"
    echo -e "    ${GREEN}✓${NC} sm2-pkcs8.pem"

    echo "  - SM2 (SEC1 PEM)"
    "$TONGSUO" ec -in "$OUTPUT_DIR/sm2-pkcs8.pem" -out "$OUTPUT_DIR/sm2-sec1.pem" 2>/dev/null
    echo -e "    ${GREEN}✓${NC} sm2-sec1.pem"

    echo "  - SM2 公钥"
    "$TONGSUO" pkey -in "$OUTPUT_DIR/sm2-pkcs8.pem" -pubout -out "$OUTPUT_DIR/sm2-public.pem" 2>/dev/null
    echo -e "    ${GREEN}✓${NC} sm2-public.pem"

    echo "  - SM2 (DER)"
    "$TONGSUO" pkcs8 -topk8 -nocrypt -in "$OUTPUT_DIR/sm2-pkcs8.pem" -outform DER -out "$OUTPUT_DIR/sm2-pkcs8.der"
    echo -e "    ${GREEN}✓${NC} sm2-pkcs8.der"
else
    echo -e "  ${YELLOW}⚠${NC}  tongsuo 不可用，跳过 SM2 密钥生成（可设置 TONGSUO_PATH）"
fi

echo ""

# ============================================
# DSA 密钥生成
# ============================================
echo -e "${YELLOW}[额外] 生成 DSA 密钥...${NC}"

echo "  - DSA 2048 位参数"
${TONGSUO_BIN} dsaparam -out "$OUTPUT_DIR/dsa-2048-params.pem" 2048 2>/dev/null
echo -e "    ${GREEN}✓${NC} dsa-2048-params.pem"

echo "  - DSA 2048 位私钥"
${TONGSUO_BIN} gendsa -out "$OUTPUT_DIR/dsa-2048-private.pem" "$OUTPUT_DIR/dsa-2048-params.pem" 2>/dev/null
echo -e "    ${GREEN}✓${NC} dsa-2048-private.pem"

echo "  - DSA 2048 位公钥"
${TONGSUO_BIN} dsa -in "$OUTPUT_DIR/dsa-2048-private.pem" -pubout -out "$OUTPUT_DIR/dsa-2048-public.pem" 2>/dev/null
echo -e "    ${GREEN}✓${NC} dsa-2048-public.pem"

echo ""

# ============================================
# 密钥加密版本 (带密码保护)
# ============================================
echo -e "${YELLOW}[额外] 生成加密密钥...${NC}"

echo "  - RSA 2048 位 (AES-256-CBC 加密, 密码: test1234)"
${TONGSUO_BIN} genrsa -aes256 -passout pass:test1234 -out "$OUTPUT_DIR/rsa-2048-encrypted.pem" 2048 2>/dev/null
echo -e "    ${GREEN}✓${NC} rsa-2048-encrypted.pem"

echo "  - EC P-256 (AES-256-CBC 加密, 密码: test1234)"
${TONGSUO_BIN} ecparam -name prime256v1 -genkey -noout | ${TONGSUO_BIN} ec -aes256 -passout pass:test1234 -out "$OUTPUT_DIR/ec-p256-encrypted.pem" 2>/dev/null
echo -e "    ${GREEN}✓${NC} ec-p256-encrypted.pem"

echo ""

# ============================================
# 生成密钥信息文件
# ============================================
echo -e "${YELLOW}生成密钥信息文件...${NC}"

cat > "$OUTPUT_DIR/README.md" << 'EOF'
# 测试密钥说明

本目录包含用于测试和开发的密钥文件。

## ⚠️ 安全警告

**这些密钥仅用于测试和开发目的，切勿在生产环境中使用！**

## 密钥列表

### RSA 密钥

| 文件名 | 算法 | 密钥长度 | 格式 | 说明 |
|--------|------|----------|------|------|
| `rsa-2048-pkcs1.pem` | RSA | 2048 位 | PKCS#1 PEM | 传统格式私钥 |
| `rsa-2048-pkcs8.pem` | RSA | 2048 位 | PKCS#8 PEM | 标准格式私钥 |
| `rsa-2048-public.pem` | RSA | 2048 位 | PEM | 公钥 |
| `rsa-3072-pkcs1.pem` | RSA | 3072 位 | PKCS#1 PEM | 传统格式私钥 |
| `rsa-3072-pkcs8.pem` | RSA | 3072 位 | PKCS#8 PEM | 标准格式私钥 |
| `rsa-4096-pkcs1.pem` | RSA | 4096 位 | PKCS#1 PEM | 传统格式私钥 |
| `rsa-4096-pkcs8.pem` | RSA | 4096 位 | PKCS#8 PEM | 标准格式私钥 |
| `rsa-2048-pkcs8.der` | RSA | 2048 位 | PKCS#8 DER | 二进制格式私钥 |
| `rsa-2048-encrypted.pem` | RSA | 2048 位 | PKCS#1 PEM | 加密私钥 (密码: test1234) |

### EC 密钥 (椭圆曲线)

| 文件名 | 曲线 | 格式 | 说明 |
|--------|------|------|------|
| `ec-p256-pkcs8.pem` | secp256r1 (P-256) | PKCS#8 PEM | NIST P-256 私钥 |
| `ec-p256-public.pem` | secp256r1 (P-256) | PEM | NIST P-256 公钥 |
| `ec-p256-pkcs8.der` | secp256r1 (P-256) | PKCS#8 DER | NIST P-256 私钥 (二进制) |
| `ec-p384-pkcs8.pem` | secp384r1 (P-384) | PKCS#8 PEM | NIST P-384 私钥 |
| `ec-p384-public.pem` | secp384r1 (P-384) | PEM | NIST P-384 公钥 |
| `ec-p521-pkcs8.pem` | secp521r1 (P-521) | PKCS#8 PEM | NIST P-521 私钥 |
| `ec-p521-public.pem` | secp521r1 (P-521) | PEM | NIST P-521 公钥 |
| `ec-p256-encrypted.pem` | secp256r1 (P-256) | PKCS#8 PEM | 加密私钥 (密码: test1234) |

### SM2 密钥 (国密)

| 文件名 | 曲线 | 格式 | 说明 |
|--------|------|------|------|
| `sm2-sec1.pem` | SM2 | SEC1 PEM | 国密 SM2 私钥 (SEC1 格式) |
| `sm2-pkcs8.pem` | SM2 | PKCS#8 PEM | 国密 SM2 私钥 (PKCS#8 格式) |
| `sm2-public.pem` | SM2 | PEM | 国密 SM2 公钥 |
| `sm2-pkcs8.der` | SM2 | PKCS#8 DER | 国密 SM2 私钥 (二进制) |

> **注意**: SM2 密钥需要 OpenSSL 1.1.1+ 或带有国密支持的版本

### DSA 密钥

| 文件名 | 密钥长度 | 格式 | 说明 |
|--------|----------|------|------|
| `dsa-2048-params.pem` | 2048 位 | PEM | DSA 参数 |
| `dsa-2048-private.pem` | 2048 位 | PEM | DSA 私钥 |
| `dsa-2048-public.pem` | 2048 位 | PEM | DSA 公钥 |

## 重新生成密钥

运行以下命令重新生成所有测试密钥:

```bash
cd /path/to/DevTrove.Crypto
./scripts/generate-test-keys.sh
```

## 查看密钥信息

### 查看 RSA 密钥信息
```bash
tongsuo rsa -in rsa-2048-pkcs1.pem -text -noout
```

### 查看 EC 密钥信息
```bash
tongsuo ec -in ec-p256-pkcs8.pem -text -noout
```

### 查看 SM2 密钥信息
```bash
tongsuo ec -in sm2-pkcs8.pem -text -noout
```

### 查看 DSA 密钥信息
```bash
tongsuo dsa -in dsa-2048-private.pem -text -noout
```

## 文件格式说明

- **PEM**: 文本格式，Base64 编码，以 `-----BEGIN xxx-----` 开头
- **DER**: 二进制格式，直接存储 ASN.1 结构
- **PKCS#1**: RSA 私钥的传统格式
- **PKCS#8**: 通用私钥格式，支持多种算法

## 安全建议

1. 这些密钥仅用于本地开发和测试
2. 不要将这些密钥用于生产环境
3. 不要将这些密钥提交到公共代码仓库
4. 生产环境应使用安全的密钥管理系统 (如 HSM、KMS)

---

生成日期: $(date '+%Y-%m-%d %H:%M:%S')
生成脚本: scripts/generate-test-keys.sh
EOF

echo -e "${GREEN}✓${NC} README.md"
echo ""

# ============================================
# 统计信息
# ============================================
echo -e "${BLUE}=====================================${NC}"
echo -e "${BLUE}密钥生成完成！${NC}"
echo -e "${BLUE}=====================================${NC}"
echo ""
echo "输出目录: $OUTPUT_DIR"
echo "生成文件数: $(ls -1 "$OUTPUT_DIR" | wc -l)"
echo ""
echo -e "${GREEN}所有密钥已成功生成！${NC}"
echo ""
echo "提示:"
echo "  - 查看密钥列表: ls -lh $OUTPUT_DIR"
echo "  - 查看密钥说明: cat $OUTPUT_DIR/README.md"
echo ""
