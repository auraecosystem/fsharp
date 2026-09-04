# Build the minimal Docker image
docker build -t aura-node:aot-scratch .

# Check the image size (typically ~15MB - 30MB)
docker images aura-node:aot-scratch

# Run the containerized AOT binary
docker run --rm aura-node:aot-scratch
git clone [https://github.com/auraecosystem/fsharp.git](https://github.com/auraecosystem/fsharp.git)
cd fsharp
dotnet restore
dotnet build

