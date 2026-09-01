# =======================================================
# STAGE 1: Build & Compile Native AOT via Alpine
# =======================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

# Install native C compilation tools required by Native AOT linkers on Alpine
RUN apk add --no-config-cache \
    clang \
    build-base \
    zlib-dev

WORKDIR /src

# Copy project file and restore dependencies
COPY *.fsproj ./
RUN dotnet restore -r linux-musl-x64

# Copy source code and compile Native AOT executable
COPY . ./
RUN dotnet publish -c Release \
    -r linux-musl-x64 \
    --self-contained true \
    -p:PublishAot=true \
    -p:InvariantGlobalization=true \
    -o /app/publish

# Set executable permission
RUN chmod +x /app/publish/AuraNode

# =======================================================
# STAGE 2: Minimal Runtime Image (Zero OS overhead)
# =======================================================
FROM scratch AS final

WORKDIR /app

# Copy the compiled native binary from the build stage
COPY --from=build /app/publish/AuraNode /app/AuraNode

# (Optional) If your app requires SSL/TLS root certificates, copy them from Alpine:
COPY --from=build /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/

# Run the binary directly as the container entrypoint
ENTRYPOINT ["/app/AuraNode"]
