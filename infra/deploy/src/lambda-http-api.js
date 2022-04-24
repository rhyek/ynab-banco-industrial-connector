// @ts-check
import pulumi from '@pulumi/pulumi';
import aws from '@pulumi/aws';
import { backendEnvironmentVariableKeys } from '../../../.scripts/consts/backend-environment-variable-keys.mjs';
import { httpApiNamespace, projectTags } from '../../consts.mjs';
import { scrapeBankTransactionsSqsUrl } from './sqs-scrape-bank-transactions.js';

const config = new pulumi.Config();

const buildStack = new pulumi.StackReference(
  `rhyek/ynab-banco-industrial-connector.build/${pulumi.getStack()}`
);

const httpApiFuncImage = buildStack.getOutput('httpApiFuncImage');

const role = new aws.iam.Role(`${httpApiNamespace}-lambda-role`, {
  assumeRolePolicy: aws.iam.assumeRolePolicyForPrincipal({
    Service: 'lambda.amazonaws.com',
  }),
  tags: {
    ...projectTags,
  },
});
new aws.iam.RolePolicyAttachment(
  `${httpApiNamespace}-lambda-role-policy-attachment`,
  {
    role: role.name,
    policyArn: aws.iam.ManagedPolicy.AWSLambdaExecute,
  }
);

const httpApiFunc = new aws.lambda.Function(`${httpApiNamespace}-lambda`, {
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
});

// pulumi aws crosswalk does not support api gateways v2 (http api) only rest apis
// http apis are supposed to be cheaper, so we will define it manually
const apiGateway = new aws.apigatewayv2.Api(`${httpApiNamespace}-api-gateway`, {
  protocolType: 'HTTP',
  corsConfiguration: {
    allowMethods: ['*'],
    allowOrigins: ['*'],
  },
  target: httpApiFunc.arn,
  tags: {
    ...projectTags,
  },
});

// https://stackoverflow.com/a/64089186/410224
new aws.lambda.Permission(
  `${httpApiNamespace}-api-gateway-invoke-lambda-permission`,
  {
    action: 'lambda:InvokeFunction',
    function: httpApiFunc.arn,
    principal: 'apigateway.amazonaws.com',
    sourceArn: apiGateway.executionArn.apply((v) => `${v}/*/*`),
  }
);

export const apiGatewayUrl = apiGateway.apiEndpoint;
