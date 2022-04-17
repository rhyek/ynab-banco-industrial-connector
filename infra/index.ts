import * as pulumi from '@pulumi/pulumi';
import * as aws from '@pulumi/aws';
import * as awsx from '@pulumi/awsx';

// https://www.pulumi.com/blog/aws-lambda-container-support/

const projectName = 'ynab-banco-industrial-connector';

const httpApiNamespace = `${projectName}-http-api`;

const projectTags = { project: projectName };

// container image

const repo = new awsx.ecr.Repository(`${httpApiNamespace}-repo`, {
  tags: {
    ...projectTags,
  },
});

const image = repo.buildAndPushImage({
  dockerfile:
    '../src/YnabBancoIndustrialConnectorBackend/Programs/HttpApi/Dockerfile',
  context: '../src/YnabBancoIndustrialConnectorBackend',
});

// lambda

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
  imageUri: image,
  role: role.arn,
  timeout: 900,
  tags: {
    ...projectTags,
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
