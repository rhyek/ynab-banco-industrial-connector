#!/bin/zsh
rm ./src/YnabBancoIndustrialConnectorBackend/Programs/HttpApi/bin/Debug/net6.0/.playwright/node/mac/node
ln -s $(which node) \
  ./src/YnabBancoIndustrialConnectorBackend/Programs/HttpApi/bin/Debug/net6.0/.playwright/node/mac/node
