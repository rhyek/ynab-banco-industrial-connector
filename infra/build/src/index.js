// @ts-check
import * as awsx from '@pulumi/awsx';
import { httpApiNamespace, projectTags } from '../../consts.mjs';

const repo = new awsx.ecr.Repository(`${httpApiNamespace}-repo`, {
  tags: {
    ...projectTags,
  },
});

const image = repo.buildAndPushImage({
  dockerfile:
    '../../src/YnabBancoIndustrialConnectorBackend/Programs/HttpApi/Dockerfile',
  context: '../../src/YnabBancoIndustrialConnectorBackend',
});

export const httpApiFuncImage = image;
