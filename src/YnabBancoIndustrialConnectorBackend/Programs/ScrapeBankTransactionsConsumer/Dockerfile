# syntax=docker/dockerfile:1.2
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim as build
WORKDIR /sln
COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet restore \
  Programs/ScrapeBankTransactionsConsumer/ScrapeBankTransactionsConsumer.csproj \
  --runtime linux-x64
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet publish \
  Programs/ScrapeBankTransactionsConsumer/ScrapeBankTransactionsConsumer.csproj \
  --no-restore \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained false \
  --output /app/publish
# this is really slow: https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run
# -p:PublishReadyToRun=true

FROM mcr.microsoft.com/playwright:v1.21.0-focal
# install dotnet runtime https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#2004-
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
  dpkg -i packages-microsoft-prod.deb && \
  rm packages-microsoft-prod.deb
RUN apt-get update; \
  # https://stackoverflow.com/questions/37374310/how-critical-is-dumb-init-for-docker
  # child processes: playwright node driver
  apt-get install -y dumb-init && \ 
  apt-get install -y apt-transport-https && \
  apt-get update && \
  apt-get install -y aspnetcore-runtime-6.0
COPY --from=build /app/publish /app
RUN chown -R pwuser:pwuser /app
ENTRYPOINT ["/usr/bin/dumb-init", "--"]
CMD ["dotnet", "/app/ScrapeBankTransactionsConsumer.dll"]
