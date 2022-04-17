# syntax=docker/dockerfile:1.2
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim as build
WORKDIR /sln
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
  dotnet publish \
  Programs/HttpApi/HttpApi.csproj \
  --configuration Release \ 
  --runtime linux-x64 \
  --self-contained false \ 
  --output /app/publish \
  -p:PublishReadyToRun=true

# https://gallery.ecr.aws/lambda/dotnet
FROM public.ecr.aws/lambda/dotnet:6
COPY --from=build /app/publish/* ${LAMBDA_TASK_ROOT}
# https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/ search executable assemblies
CMD ["HttpApi"]
