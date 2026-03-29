run *args:
    dotnet run --project ./src/Declar.Cli/Declar.Cli.csproj -- {{args}}

build target='':
    runtime="{{target}}"; \
    if [ -z "$runtime" ]; then \
        os=$(uname -s | tr '[:upper:]' '[:lower:]'); \
        arch=$(uname -m); \
        case "$arch" in \
            x86_64) rid_arch="x64" ;; \
            aarch64|arm64) rid_arch="arm64" ;; \
            armv7l) rid_arch="arm" ;; \
            *) echo "Unsupported architecture: $arch"; exit 1 ;; \
        esac; \
        case "$os" in \
            linux) rid_os="linux" ;; \
            darwin) rid_os="osx" ;; \
            msys*|mingw*|cygwin*|windows_nt) rid_os="win" ;; \
            *) echo "Unsupported OS: $os"; exit 1 ;; \
        esac; \
        runtime="$rid_os-$rid_arch"; \
    fi; \
    mkdir -p "./dist/$runtime"; \
    dotnet publish ./src/Declar.Cli/Declar.Cli.csproj \
        -c Release \
        -r "$runtime" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:DebugType=None \
        -p:DebugSymbols=false \
        -o "./dist/$runtime" || exit $?; \
    case "$runtime" in \
        win-*) src="./dist/$runtime/Declar.Cli.exe"; dst="./dist/$runtime/declar.exe" ;; \
        *) src="./dist/$runtime/Declar.Cli"; dst="./dist/$runtime/declar" ;; \
    esac; \
    if [ -f "$src" ]; then mv -f "$src" "$dst"; fi; \
    echo "Published single-file binary to $dst"
