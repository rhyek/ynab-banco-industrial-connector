//@ts-check
import pulumi from '@pulumi/pulumi';
import aws from '@pulumi/aws';
import {
  scrapeBankTransactionsConsumerNamespace,
  projectTags,
} from '../../../../consts.mjs';
import { role } from './role.js';
import { backendEnvironmentVariableKeys } from '../../../../../.scripts/consts/backend-environment-variable-keys.mjs';
import { buildStack } from '../../build-stack.js';

const config = new pulumi.Config();

const scrapeBankTransactionsConsumerImage = buildStack.getOutput(
  'scrapeBankTransactionsConsumerImage'
);

export const scrapeBankTransactionsConsumerFunc = new aws.lambda.Function(
  `${scrapeBankTransactionsConsumerNamespace}-lambda`,
  {
    packageType: 'Image',
    imageUri: scrapeBankTransactionsConsumerImage,
    role: role.arn,
    timeout: 3 * 60,
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
      },
    },
  }
);
