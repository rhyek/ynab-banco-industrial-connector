import * as aws from '@pulumi/aws';
import * as awsx from '@pulumi/awsx';
import { projectName, projectTags } from '../../../consts';
import { sendScrapeTxsMessage } from './common/send-scrape-txs-message';

export const scrapeConfirmedBankTxsApiHandler = new aws.lambda.CallbackFunction(
  `${projectName}-scrape-confirmed-bank-txs-api-handler`,
  {
    // environment: {
    //   variables: {
    //     SQS_URL: scrapeBankTransactionsSqsUrl,
    //   },
    // },
    callback: async (): Promise<awsx.apigateway.Response> => {
      const response = await sendScrapeTxsMessage('CONFIRMED');
      return {
        statusCode: 200,
        headers: {
          'content-type': 'application/json',
        },
        body: JSON.stringify({
          sqsResponse: response,
        }),
      };
    },
    tags: {
      ...projectTags,
    },
  }
);
