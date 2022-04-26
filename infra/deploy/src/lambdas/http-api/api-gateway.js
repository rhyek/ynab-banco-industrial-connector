//@ts-check
import aws from '@pulumi/aws';
import { httpApiNamespace, projectTags } from '../../../../consts.mjs';
import { httpApiFunc } from './lambda.js';

// pulumi aws crosswalk does not support api gateways v2 (http api) only rest apis
// http apis are supposed to be cheaper, so we will define it manually
export const apiGateway = new aws.apigatewayv2.Api(
  `${httpApiNamespace}-api-gateway`,
  {
    protocolType: 'HTTP',
    corsConfiguration: {
      allowMethods: ['*'],
      allowOrigins: ['*'],
    },
    target: httpApiFunc.arn,
    tags: {
      ...projectTags,
    },
  }
);

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
