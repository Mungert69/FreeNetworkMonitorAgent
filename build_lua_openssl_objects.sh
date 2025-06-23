#!/bin/bash
set -e

export NDK_ROOT="/lib/android-sdk/ndk/28.1.13356709"
export TOOLCHAIN_BIN="$NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin"
export PATH="$TOOLCHAIN_BIN:$PATH"
export CC="armv7a-linux-androideabi21-clang"
export CFLAGS="-fPIE -fPIC -DANDROID -fvisibility=hidden"
OPENSSL_INCLUDE_DIR="/home/mahadeva/code/openssl-3.4.1-android/include"
OPENSSL_LIB_DIR="/home/mahadeva/code/openssl-3.4.1-android"
LUA_INCLUDE_DIR="/home/mahadeva/code/nmap-build-deps/lua-install/include"
AUXILIAR_DIR="./deps/auxiliar"
INSTALL_DIR="/home/mahadeva/code/nmap-android-build"

cd "$(dirname "$0")"

for srcfile in src/*.c deps/auxiliar/*.c; do
  echo "Compiling $srcfile..."
  $CC $CFLAGS -I"$OPENSSL_INCLUDE_DIR" -I"$LUA_INCLUDE_DIR" -I"$AUXILIAR_DIR" -c "$srcfile" -o "$(basename "${srcfile%.c}").o" || { echo "❌ Compile failed: $srcfile"; exit 1; }
done

echo "✅ All objects built."

echo "Linking openssl.so..."
$CC $CFLAGS -shared -o openssl.so *.o -L"$OPENSSL_LIB_DIR" -lssl -lcrypto -llog -landroid || { echo "❌ Linking failed"; exit 1; }

echo "Copying openssl.so to $INSTALL_DIR/nselib-bin and $INSTALL_DIR/copy-to-lib64..."
mkdir -p "$INSTALL_DIR/nselib-bin" "$INSTALL_DIR/copy-to-lib64"
cp openssl.so "$INSTALL_DIR/nselib-bin/"
cp openssl.so "$INSTALL_DIR/copy-to-lib64/"

echo "✅ openssl.so built and copied."

