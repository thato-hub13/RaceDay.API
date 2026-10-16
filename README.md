# RaceDay.API

API de backend desenvolvida em C# com ASP.NET Core para fornecer serviços REST para o domínio de corridas e competições.

## Visão geral

O projeto foi estruturado para servir como base para uma API moderna, com foco em:

- desenvolvimento em .NET
- endpoints REST
- organização por camadas
- integração com documentação automática da API
- facilidade de manutenção e evolução

## Requisitos

Antes de executar o projeto, certifique-se de ter instalado:

- .NET SDK 8.0 ou superior
- Visual Studio 2022 ou VS Code
- Git

## Como executar

1. Clone o repositório:

```bash
git clone https://github.com/thato-hub13/RaceDay.API.git
cd RaceDay.API
```

2. Restaure os pacotes:

```bash
dotnet restore
```

3. Compile a aplicação:

```bash
dotnet build
```

4. Execute a API:

```bash
dotnet run
```

A aplicação normalmente ficará disponível em:

```text
https://localhost:5001
http://localhost:5000
```

> Os ports podem variar conforme a configuração do projeto e do ambiente.

## Configuração

As configurações da aplicação ficam em arquivos como:

- `appsettings.json`
- `appsettings.Development.json`

Você pode ajustar:

- connection strings
- configurações de ambiente
- dados de autenticação/autorização
- opções de logging

## Estrutura do projeto

A estrutura pode variar conforme a implementação, mas uma organização típica seria:

```text
RaceDay.API/
├── Controllers/
├── Models/
├── Services/
├── Data/
├── DTOs/
├── Middleware/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── README.md
└── RaceDay.API.csproj
```

## Endpoints

Este projeto pode expor endpoints REST para ações como:

- cadastro de atletas
- listagem de eventos/corridas
- consulta de resultados
- atualização de status
- autenticação/autorização

Os endpoints específicos devem ser definidos conforme a regra de negócio do projeto.

## Documentação da API

Se estiver utilizando Swagger/OpenAPI, a documentação está normalmente disponível em:

```text
https://localhost:5001/swagger
```

## Boas práticas

- manter endpoints pequenos e bem definidos
- usar DTOs para separar entrada/saída da API
- validar entradas antes de persistir dados
- registrar logs para facilitar diagnóstico
- documentar mudanças e comportamento de cada endpoint

## Contribuição

Contribuições são bem-vindas. Para colaborar:

1. crie uma branch para sua feature ou correção
2. faça as alterações
3. envie um pull request com descrição clara

## Licença

Este projeto ainda não possui uma licença definida. Se necessário, configure a licença apropriada antes de publicar o repositório publicamente.

## Observação

Este README foi criado como base inicial para o projeto. Ajuste os trechos de acordo com a arquitetura real da aplicação, nomes de entidades, endpoints e regras de negócio implementadas no código.
