//@ts-check
import * as pulumi from '@pulumi/pulumi';
import * as aws from '@pulumi/aws';
import { backendEnvironmentVariableKeys } from '../../../../../.scripts/consts/backend-environment-variable-keys';
import { httpApiNamespace, projectTags } from '../../../../consts';
import { scrapeBankTransactionsSqsUrlEnvironmentVariable } from '../../sqs-scrape-bank-transactions';
import { buildStack } from '../../build-stack';
import { role } from './role';

const config = new pulumi.Config();

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
        ...scrapeBankTransactionsSqsUrlEnvironmentVariable,
      },
    },
  }
);
