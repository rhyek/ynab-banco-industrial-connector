api endpoints

- new mobile transactions
  - convert to usd if neccesary
  - create ynab tx
  - if currency == GTQ: queue sqs message SCRAPE_RESERVED
- scrape confirmed
  - queue sqs message SCRAPE_CONFIRMED

message handlers

- SCRAPE_RESERVED
  - scrape
  -
- SCRAPE_CONFIRMED
  - it's a command handler
  - call scraper
  - with result call ynab handle confirmed txs
