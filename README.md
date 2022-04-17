run backend in the command line:

```
pnpm dotenv -- dotnet ~/Dev/personal/banco-industrial-monitor-dotnetcore/src/BancoIndustrialMonitor/Programs/HttpApi/bin/Debug/net6.0/HttpApi.dll
```

dotnet cli and aws
https://docs.aws.amazon.com/lambda/latest/dg/csharp-package-cli.html

```
dotnet tool install -g Amazon.Lambda.Tools
dotnet new --install Amazon.Lambda.Templates
```

.NET 6 Minimal APIs on AWS Lambda:
https://pikedev.com/net-6-minimal-api-on-aws-lambda/

... With Container Images and Pulumi

- Base container images: https://gallery.ecr.aws/lambda/dotnet
- Testing Lambda container images locally: https://docs.aws.amazon.com/lambda/latest/dg/images-test.html
- Build and image:
  ```bash
  docker build -t ynab-banco-idustrial-connector-lambda -f Programs/HttpApi/Dockerfile .
  docker run --rm -p 9000:8080 ynab-banco-idustrial-connector-lambda
  ```
- Test request:
  ```bash
  curl -s -L -X POST 'http://localhost:9000/2015-03-31/functions/function/invocations' \
  -H 'Content-Type: application/json' \
  --data-raw '{
    "version": "2.0",
    "requestContext": {
      "http": {
        "method": "GET",
        "path": "/status"
      }
    }
  }' \
  | jq '.body | fromjson'
  ```
- HttpApi event payloads: https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-develop-integrations-lambda.html

### Pulumi

- aws lambda with container images: https://www.pulumi.com/blog/aws-lambda-container-support/

... With ServerlessFramework

- Serverless Framework vs Terraform: https://www.serverless.com/blog/definitive-guide-terraform-serverless/
- using Lambda ASP.NET Core Minimal API (serverless.AspNetCoreMinimalAPI)
- specify aws resource tags: https://mojitocoder.medium.com/aws-resources-tagging-using-serverless-framework-fbfb32122cde
