# syntax=docker/dockerfile:1.2
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim as build
WORKDIR /sln
COPY . .
RUN dotnet publish \
  Programs/ScrapeBankTransactionsConsumer/ScrapeBankTransactionsConsumer.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained false \
  --output /publish
# this is really slow: https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run
# -p:PublishReadyToRun=true

FROM mcr.microsoft.com/playwright/dotnet:v1.21.0-focal
RUN apt-get update; \
  apt-get install -y dumb-init
COPY --from=build /publish /app
RUN chown -R pwuser:pwuser /app
# https://stackoverflow.com/questions/37374310/how-critical-is-dumb-init-for-docker
# child processes: playwright node driver
USER pwuser
ENTRYPOINT ["/usr/bin/dumb-init", "--"]
CMD ["dotnet", "/app/ScrapeBankTransactionsConsumer.dll"]
