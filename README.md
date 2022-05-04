# YNAB - Banco Industrial Connector

This is a personal project I wrote with the purpose of keeping my budget on [YNAB](https://www.youneedabudget.com/) updated with the latest bank transactions including debit card use and account transfers. I had to write this since even though YNAB already offers this functionality for many banks around the world, it does not for mine.

Tech stack:

- .NET 6/C# 10
- Playwright .NET for Web scraping
- xUnit.net for unit testing
- Clean Architecture, Dependency Injection, Repository Pattern
- AWS Lambda (with Container Images), API Gateway, Simple Queue Service (SQS)
- Docker
- Monorepo
- CI/CD using Github Actions
- Pulumi for Infrastructure as Code
