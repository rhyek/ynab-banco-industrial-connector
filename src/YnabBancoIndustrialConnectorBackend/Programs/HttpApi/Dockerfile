# syntax=docker/dockerfile:1.3
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim as build
WORKDIR /sln
COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet restore \
  Programs/HttpApi/HttpApi.csproj \
  --runtime linux-x64
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet build \
  Programs/HttpApi/HttpApi.csproj \
  --no-restore \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained false
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet publish \
  Programs/HttpApi/HttpApi.csproj \
  --no-restore \
  --no-build \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained false \
  --output /app/publish
# this is really slow: https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run
# -p:PublishReadyToRun=true

# https://gallery.ecr.aws/lambda/dotnet
FROM public.ecr.aws/lambda/dotnet:6
COPY --from=build /app/publish/* ${LAMBDA_TASK_ROOT}
# https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/ search executable assemblies
CMD ["HttpApi"]


# https://github.com/aws/aws-lambda-dotnet/tree/master/Libraries/src/Amazon.Lambda.RuntimeSupport
