```bash
docker build --platform=linux/amd64 \
  -t scrape-bank-txs-consumer-test \
  -f ./src/YnabBancoIndustrialConnectorBackend/Programs/ScrapeBankTransactionsConsumer/Dockerfile \
  ./src/YnabBancoIndustrialConnectorBackend

docker run --rm \
  --env-file=./.env \
  -p 9777:8080 \
  scrape-bank-txs-consumer-test
```

test

```bash
curl -s -L -X POST 'http://localhost:9777/2015-03-31/functions/function/invocations' \
  -H 'Content-Type: application/json' \
  --data-raw '{
    "Records": [
      {
        "messageId": "19dd0b57-b21e-4ac1-bd88-01bbb068cb78",
        "receiptHandle": "MessageReceiptHandle",
        "body": "CONFIRMED",
        "attributes": {
          "ApproximateReceiveCount": "1",
          "SentTimestamp": "1523232000000",
          "SenderId": "123456789012",
          "ApproximateFirstReceiveTimestamp": "1523232000001"
        },
        "messageAttributes": {},
        "md5OfBody": "{{{md5_of_body}}}",
        "eventSource": "aws:sqs",
        "eventSourceARN": "arn:aws:sqs:us-east-1:123456789012:MyQueue",
        "awsRegion": "us-east-1"
      }
    ]
  }'
```
