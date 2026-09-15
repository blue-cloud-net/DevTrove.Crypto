#!/bin/bash

# 网站证书拉取脚本
# 用于从指定网站拉取完整的证书链，用于测试和开发

set -e

# 设置颜色输出
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# 脚本目录和输出目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
OUTPUT_DIR="$PROJECT_ROOT/tests/data/certs"

# 默认网站列表
WEBSITES=(
    "www.google.com:443"
    "www.github.com:443"
    "www.microsoft.com:443"
    "www.baidu.com:443"
    "www.taobao.com:443"
)

# 帮助信息
show_help() {
    cat << EOF
用法: $0 [选项] [网站地址...]

从指定网站拉取完整的 SSL/TLS 证书链。

选项:
    -h, --help          显示此帮助信息
    -o, --output DIR    指定输出目录 (默认: $OUTPUT_DIR)
    -p, --port PORT     指定端口 (默认: 443)
    -t, --timeout SEC   连接超时时间,秒 (默认: 10)

参数:
    网站地址            一个或多个网站地址，格式: hostname[:port]
                        如果未指定，将使用默认列表

示例:
    $0                                  # 拉取默认网站列表的证书
    $0 example.com                       # 拉取 example.com 的证书
    $0 example.com:8443                  # 拉取 example.com:8443 的证书
    $0 -o ./certs site1.com site2.com    # 拉取多个网站并保存到指定目录

默认网站列表:
EOF
    for site in "${WEBSITES[@]}"; do
        echo "    - $site"
    done
}

# 解析命令行参数
DEFAULT_PORT=443
TIMEOUT=10

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -o|--output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        -p|--port)
            DEFAULT_PORT="$2"
            shift 2
            ;;
        -t|--timeout)
            TIMEOUT="$2"
            shift 2
            ;;
        -*)
            echo -e "${RED}错误: 未知选项 $1${NC}"
            echo "使用 -h 或 --help 查看帮助信息"
            exit 1
            ;;
        *)
            # 收集网站地址
            CUSTOM_WEBSITES+=("$1")
            shift
            ;;
    esac
done

# 如果指定了自定义网站，使用自定义列表
if [ ${#CUSTOM_WEBSITES[@]} -gt 0 ]; then
    WEBSITES=("${CUSTOM_WEBSITES[@]}")
fi

echo -e "${BLUE}=====================================${NC}"
echo -e "${BLUE}网站证书拉取脚本${NC}"
echo -e "${BLUE}=====================================${NC}"
echo ""

# 创建输出目录
mkdir -p "$OUTPUT_DIR"
echo -e "${GREEN}✓${NC} 输出目录: $OUTPUT_DIR"
echo ""

# 检查 openssl 是否可用
if ! command -v openssl &> /dev/null; then
    echo -e "${RED}✗${NC} 错误: 未找到 openssl 命令"
    echo "请安装 OpenSSL: apt-get install openssl 或 yum install openssl"
    exit 1
fi

echo -e "${GREEN}✓${NC} OpenSSL 版本: $(openssl version)"
echo ""

# 拉取证书函数
pull_certificate() {
    local host_port=$1
    local host
    local port

    # 解析主机名和端口
    if [[ $host_port == *:* ]]; then
        host="${host_port%:*}"
        port="${host_port#*:}"
    else
        host="$host_port"
        port="$DEFAULT_PORT"
    fi

    # 清理主机名中的协议前缀
    host="${host#https://}"
    host="${host#http://}"
    host="${host%%/*}"

    # 生成安全的文件名
    local safe_name=$(echo "$host" | sed 's/[^a-zA-Z0-9.-]/_/g')
    local base_filename="$OUTPUT_DIR/${safe_name}_${port}"

    echo -e "${YELLOW}正在拉取: ${host}:${port}${NC}"

    # 拉取完整证书链
    if timeout "$TIMEOUT" openssl s_client -connect "${host}:${port}" -servername "$host" -showcerts </dev/null 2>/dev/null | \
       sed -ne '/-BEGIN CERTIFICATE-/,/-END CERTIFICATE-/p' > "${base_filename}_chain.pem"; then

        if [ -s "${base_filename}_chain.pem" ]; then
            # 分离证书链中的各个证书
            local cert_count=0
            local in_cert=0
            local current_cert=""

            while IFS= read -r line; do
                if [[ $line == "-----BEGIN CERTIFICATE-----" ]]; then
                    in_cert=1
                    current_cert="$line"$'\n'
                elif [[ $line == "-----END CERTIFICATE-----" ]]; then
                    current_cert+="$line"$'\n'

                    # 保存当前证书
                    if [ $cert_count -eq 0 ]; then
                        echo "$current_cert" > "${base_filename}_cert.pem"
                        echo -e "  ${GREEN}✓${NC} 服务器证书: ${safe_name}_${port}_cert.pem"
                    else
                        echo "$current_cert" > "${base_filename}_chain_${cert_count}.pem"
                        echo -e "  ${GREEN}✓${NC} 中间证书 #${cert_count}: ${safe_name}_${port}_chain_${cert_count}.pem"
                    fi

                    cert_count=$((cert_count + 1))
                    in_cert=0
                    current_cert=""
                elif [ $in_cert -eq 1 ]; then
                    current_cert+="$line"$'\n'
                fi
            done < "${base_filename}_chain.pem"

            echo -e "  ${GREEN}✓${NC} 完整证书链: ${safe_name}_${port}_chain.pem (共 ${cert_count} 个证书)"

            # 提取证书信息
            if openssl x509 -in "${base_filename}_cert.pem" -noout -text > "${base_filename}_info.txt" 2>/dev/null; then
                echo -e "  ${GREEN}✓${NC} 证书信息: ${safe_name}_${port}_info.txt"
            fi

            # 获取证书主题和颁发者
            local subject=$(openssl x509 -in "${base_filename}_cert.pem" -noout -subject 2>/dev/null | sed 's/subject=//')
            local issuer=$(openssl x509 -in "${base_filename}_cert.pem" -noout -issuer 2>/dev/null | sed 's/issuer=//')
            local dates=$(openssl x509 -in "${base_filename}_cert.pem" -noout -dates 2>/dev/null)

            echo -e "  ${BLUE}主题:${NC} $subject"
            echo -e "  ${BLUE}颁发者:${NC} $issuer"
            echo -e "  ${BLUE}有效期:${NC} $dates"

            return 0
        else
            echo -e "  ${RED}✗${NC} 拉取失败: 未获取到证书数据"
            rm -f "${base_filename}_chain.pem"
            return 1
        fi
    else
        echo -e "  ${RED}✗${NC} 拉取失败: 无法连接到 ${host}:${port} (超时或连接错误)"
        rm -f "${base_filename}_chain.pem"
        return 1
    fi
}

# 统计变量
success_count=0
fail_count=0

# 遍历网站列表并拉取证书
for site in "${WEBSITES[@]}"; do
    if pull_certificate "$site"; then
        success_count=$((success_count + 1))
    else
        fail_count=$((fail_count + 1))
    fi
    echo ""
done

# ============================================
# 生成证书信息文件
# ============================================
echo -e "${YELLOW}生成证书信息文件...${NC}"

cat > "$OUTPUT_DIR/README.md" << 'EOF'
# 网站证书说明

本目录包含从真实网站拉取的 SSL/TLS 证书，用于测试和开发。

## ⚠️ 注意事项

1. 这些证书是从公开网站拉取的真实证书
2. 证书可能会过期，需要定期更新
3. 仅用于测试和开发目的
4. 不要用于任何非法用途

## 文件命名规则

- `<hostname>_<port>_cert.pem` - 服务器证书（叶子证书）
- `<hostname>_<port>_chain.pem` - 完整证书链
- `<hostname>_<port>_chain_N.pem` - 中间证书（N 为序号）
- `<hostname>_<port>_info.txt` - 证书详细信息

## 证书链结构

一个典型的证书链包含:

1. **服务器证书（叶子证书）**: 网站服务器的证书
2. **中间证书**: 由中间 CA 颁发的证书
3. **根证书**: 受信任的根 CA 证书（通常不包含在链中）

## 重新拉取证书

运行以下命令重新拉取所有网站证书:

```bash
cd /path/to/DevTrove.Crypto
./scripts/pull-website-certs.sh
```

拉取特定网站证书:

```bash
./scripts/pull-website-certs.sh example.com
./scripts/pull-website-certs.sh example.com:8443
```

拉取多个网站:

```bash
./scripts/pull-website-certs.sh site1.com site2.com site3.com
```

## 查看证书信息

### 查看证书详细信息
```bash
openssl x509 -in <hostname>_<port>_cert.pem -text -noout
```

### 查看证书主题
```bash
openssl x509 -in <hostname>_<port>_cert.pem -noout -subject
```

### 查看证书有效期
```bash
openssl x509 -in <hostname>_<port>_cert.pem -noout -dates
```

### 验证证书链
```bash
openssl verify -CAfile <hostname>_<port>_chain.pem <hostname>_<port>_cert.pem
```

## 证书用途

这些证书可用于:

- 测试证书解析功能
- 测试证书链验证
- 测试证书格式转换
- 学习 X.509 证书结构
- 开发证书相关工具

## 安全建议

1. 定期更新证书（建议每月更新）
2. 不要在生产环境中使用这些证书
3. 注意证书的有效期
4. 不要修改证书内容

---

更新日期: $(date '+%Y-%m-%d %H:%M:%S')
生成脚本: scripts/pull-website-certs.sh
EOF

echo -e "${GREEN}✓${NC} README.md"
echo ""

# ============================================
# 统计信息
# ============================================
echo -e "${BLUE}=====================================${NC}"
echo -e "${BLUE}证书拉取完成！${NC}"
echo -e "${BLUE}=====================================${NC}"
echo ""
echo "输出目录: $OUTPUT_DIR"
echo -e "成功: ${GREEN}${success_count}${NC} 个网站"
echo -e "失败: ${RED}${fail_count}${NC} 个网站"
echo "生成文件数: $(ls -1 "$OUTPUT_DIR" 2>/dev/null | wc -l)"
echo ""

if [ $success_count -gt 0 ]; then
    echo -e "${GREEN}证书拉取成功！${NC}"
else
    echo -e "${RED}所有网站证书拉取失败！${NC}"
    exit 1
fi

echo ""
echo "提示:"
echo "  - 查看证书列表: ls -lh $OUTPUT_DIR"
echo "  - 查看证书说明: cat $OUTPUT_DIR/README.md"
echo "  - 查看证书信息: openssl x509 -in $OUTPUT_DIR/<文件名> -text -noout"
echo ""
