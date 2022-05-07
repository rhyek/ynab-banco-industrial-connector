```bash
docker build --platform=linux/amd64 \
  -t scrape-bank-txs-consumer-test \
  -f ./src/YnabBancoIndustrialConnectorBackend/Programs/ScrapeBankTransactionsConsumer/Dockerfile \
  ./src/YnabBancoIndustrialConnectorBackend

docker run --rm \
  --env-file=./.env \
  scrape-bank-txs-consumer-test
```
