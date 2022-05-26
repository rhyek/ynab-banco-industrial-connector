FROM mcr.microsoft.com/playwright:v1.22.1-focal
RUN apt-get update; \
  apt-get install -y dumb-init
WORKDIR /app
COPY package.json .
RUN npm install --prod
COPY . .
ENTRYPOINT ["/usr/bin/dumb-init", "--"]
CMD ["npm", "start"]
