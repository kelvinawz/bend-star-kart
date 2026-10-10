#!/bin/bash
set -euo pipefail

usage() {
  printf '用法：%s /path/to/BendStarKart.app [输出.dmg]\n' "$0" >&2
  exit 2
}

[[ $# -ge 1 && $# -le 2 ]] || usage
[[ "$(uname -s)" == "Darwin" ]] || { echo '需要在 macOS 上运行。' >&2; exit 1; }

app_path="$1"
[[ -d "$app_path" && "$app_path" == *.app ]] || {
  echo "找不到 .app：$app_path" >&2
  exit 1
}
app_path="$(cd "$(dirname "$app_path")" && pwd -P)/$(basename "$app_path")"
output_path="${2:-$(dirname "$app_path")/BendStarKart.dmg}"
[[ "$output_path" == *.dmg ]] || { echo '输出文件必须以 .dmg 结尾。' >&2; exit 1; }
[[ ! -e "$output_path" ]] || { echo "输出文件已存在：$output_path" >&2; exit 1; }

plist="$app_path/Contents/Info.plist"
[[ -f "$plist" ]] || { echo "缺少 Info.plist：$plist" >&2; exit 1; }
executable_name="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$plist")"
executable="$app_path/Contents/MacOS/$executable_name"
[[ -f "$executable" ]] || { echo "缺少应用主程序：$executable" >&2; exit 1; }

check_universal() {
  local binary="$1" architectures
  architectures="$(/usr/bin/lipo -archs "$binary")"
  [[ " $architectures " == *' arm64 '* && " $architectures " == *' x86_64 '* ]] || {
    echo "缺少 arm64 或 x86_64 架构：$binary ($architectures)" >&2
    exit 1
  }
}

# 同时检查启动程序与 Unity 运行时，避免把单架构应用误标成 Universal。
check_universal "$executable"
unity_player="$app_path/Contents/Frameworks/UnityPlayer.dylib"
if [[ -f "$unity_player" ]]; then
  check_universal "$unity_player"
fi

output_dir="$(dirname "$output_path")"
mkdir -p "$output_dir"
output_path="$(cd "$output_dir" && pwd -P)/$(basename "$output_path")"
staging="$(mktemp -d "${TMPDIR:-/tmp}/bendstarkart-dmg.XXXXXX")"
mounted="$(mktemp -d "${TMPDIR:-/tmp}/bendstarkart-check.XXXXXX")"
is_mounted=0
cleanup() {
  if [[ "$is_mounted" == 1 ]]; then /usr/bin/hdiutil detach "$mounted" >/dev/null 2>&1 || true; fi
  rm -rf "$staging" "$mounted"
}
trap cleanup EXIT

# 保留 .app 内的权限，但不复制会干扰签名的 Finder 元数据与扩展属性。
/usr/bin/ditto --norsrc --noextattr "$app_path" "$staging/$(basename "$app_path")"
staged_app="$staging/$(basename "$app_path")"
# Unity 的 Universal 合并会使部分原生库的旧签名失效；每次逐个重签。
# 只签应用外壳或依赖首次校验可能把无效嵌套签名留进 DMG。
while IFS= read -r -d '' binary; do
  /usr/bin/codesign --force --sign - --timestamp=none "$binary"
done < <(/usr/bin/find "$staged_app/Contents" -type f \( -name '*.dylib' -o -name '*.bundle' -o -name '*.so' \) -print0)
/usr/bin/codesign --force --sign - --timestamp=none "$staged_app"
/usr/bin/codesign --verify --deep --strict "$staged_app"
ln -s /Applications "$staging/Applications"
/usr/bin/hdiutil create -volname '弯星卡丁' -srcfolder "$staging" -format UDZO "$output_path"
/usr/bin/hdiutil verify "$output_path"
# 从最终只读映像再次验签，避免暂存目录通过而分发包内失效。
/usr/bin/hdiutil attach -readonly -nobrowse -mountpoint "$mounted" "$output_path" >/dev/null
is_mounted=1
/usr/bin/codesign --verify --deep --strict "$mounted/$(basename "$app_path")"
[[ -L "$mounted/Applications" ]] || { echo 'DMG 缺少 Applications 链接。' >&2; exit 1; }
/usr/bin/hdiutil detach "$mounted" >/dev/null
is_mounted=0
/usr/bin/shasum -a 256 "$output_path"
echo "DMG 已生成：$output_path"
