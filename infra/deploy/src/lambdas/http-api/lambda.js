//@ts-check
import pulumi from '@pulumi/pulumi';
import aws from '@pulumi/aws';
import { httpApiNamespace, projectTags } from '../../../../consts.mjs';
import { role } from './role.js';
import { backendEnvironmentVariableKeys } from '../../../../../.scripts/consts/backend-environment-variable-keys.mjs';
import { scrapeBankTransactionsSqsUrl } from '../../sqs-scrape-bank-transactions.js';

const config = new pulumi.Config();

const buildStack = new pulumi.StackReference(
  `rhyek/ynab-banco-industrial-connector.build/${pulumi.getStack()}`
);

const httpApiFuncImage = buildStack.getOutput('httpApiFuncImage');

export const httpApiFunc = new aws.lambda.Function(
  `${httpApiNamespace}-lambda`,
  {
    packageType: 'Image',
    imageUri: httpApiFuncImage,
    role: role.arn,
    timeout: 900,
    tags: {
      ...projectTags,
    },
    environment: {
      variables: {
        ...Object.fromEntries(
          backendEnvironmentVariableKeys.map((key) => [
            key,
            config.requireSecret(key),
          ])
        ),
        APPLICATION__ScrapeBankTransactionsSqsUrl: scrapeBankTransactionsSqsUrl,
      },
    },
  }
);
