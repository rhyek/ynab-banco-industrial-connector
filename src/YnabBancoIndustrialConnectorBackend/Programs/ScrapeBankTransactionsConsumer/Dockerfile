# syntax=docker/dockerfile:1.3
FROM mcr.microsoft.com/dotnet/sdk:6.0 as build
WORKDIR /sln
COPY . .
RUN dotnet publish \
  Programs/ScrapeBankTransactionsConsumer/ScrapeBankTransactionsConsumer.csproj \
  --configuration Release \
  --output /publish
# this is really slow: https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run
# -p:PublishReadyToRun=true

FROM public.ecr.aws/lambda/dotnet:6
COPY --from=build /publish ${LAMBDA_TASK_ROOT}
RUN chmod +x ${LAMBDA_TASK_ROOT}/.playwright/node/linux/*
CMD ["ScrapeBankTransactionsConsumer"]
