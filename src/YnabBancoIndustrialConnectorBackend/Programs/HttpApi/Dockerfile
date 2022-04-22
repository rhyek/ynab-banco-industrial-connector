# syntax=docker/dockerfile:1.2
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
RUN pwsh Programs/HttpApi/bin/Release/net6.0/linux-x64/playwright.ps1 \
  install chromium
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet publish \
  Programs/HttpApi/HttpApi.csproj \
  --no-restore \
  --no-build \
  --configuration Release \ 
  --runtime linux-x64 \
  --self-contained false \ 
  --output /app/publish
#  -p:PublishReadyToRun=true # this is really slow: https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run

# FROM mcr.microsoft.com/playwright/dotnet:v1.21.0-focal
# ENV LAMBDA_TASK_ROOT=/var/task
# COPY --from=public.ecr.aws/lambda/dotnet:6 /var/runtime /var/runtime
# COPY --from=build /app/publish/* /var/task
# RUN mkdir -p /var/lang/bin
# RUN ln -s /usr/bin/dotnet /var/lang/bin/dotnet
# # ENTRYPOINT [ "/var/runtime/bootstrap" ]
# ENTRYPOINT [ "/var/runtime/Amazon.Lambda.RuntimeSupport" ]
# WORKDIR /var/task
# CMD ["HttpApi"]

# https://gallery.ecr.aws/lambda/dotnet
FROM public.ecr.aws/lambda/dotnet:6
RUN yum update -y && \
  yum install -y \
  fonts-liberation libasound2 libatk-bridge2.0-0 libatk1.0-0 libatspi2.0-0 \
  libcairo2 libcups2 libdbus-1-3 libdrm2 libegl1 libgbm1 libglib2.0-0 libgtk-3-0 \
  libnspr4 libnss3 libpango-1.0-0 libx11-6 libx11-xcb1 libxcb1 libxcomposite1 \
  libxdamage1 libxext6 libxfixes3 libxrandr2 libxshmfence1 xvfb \
  fonts-noto-color-emoji ttf-unifont libfontconfig libfreetype6 xfonts-cyrillic \
  xfonts-scalable fonts-ipafont-gothic fonts-wqy-zenhei fonts-tlwg-loma-otf \
  ttf-ubuntu-font-family
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright
COPY --from=build /sln/Programs/HttpApi/bin/Release/net6.0/linux-x64/.playwright /.playwright
COPY --from=build /app/publish/* ${LAMBDA_TASK_ROOT}
# https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/ search executable assemblies
CMD ["HttpApi"]


# https://github.com/aws/aws-lambda-dotnet/tree/master/Libraries/src/Amazon.Lambda.RuntimeSupport
