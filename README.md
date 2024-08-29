- Os projetos estão usando o .net 8 e foi feito usando o microsoft visual studio comunity
- Foi criado dois projetos uma API para o serviço de Lacamentos e outro um worker service para o serviço de consolidado.
- Os projetos estão com os nomes: "api_lancamento" e "workservice_consolidado" 
- O banco de dados estamos usando o MySQL
- Estamos usando o Azure Service Bus para grenciar a fila

*** Configurações para rodar localhost ***
1 - Configuração do Azure Servico Bus
    * no projeto "ConsolidadoDiario" abra o arquivo appsettings.json 
     "AzureServiceBus" na sessão "ConnectionString": adicione essa string de conexão: "Endpoint=sb://filas-teste.servicebus.windows.net/;SharedAccessKeyName=acesso-enviar;SharedAccessKey=nA150QFwZOC0wOZfDeq+jukS5P9jkKteA+ASbKze/yg=;EntityPath=fila-consolidado"

2 - Configuração do banco de dados:
    - crie um banco de dados local mysql
    - abra nos projetos a appsettings.json
    - Na "ConnectionStrings": => "DefaultConnection": defina a sua string de conexão
    * "Server=localhost;Database=seu_banco;User=root;Password=xxxxxxx;"
    - Dentro do projeto no digite no terminal o comando para ativar a conexão do entity com o banco de dados: 
        * dotnet ef migrations add InitialCreate
        - Digite o comando abaixo para atualizar o banco de dados com a estrutura: 
        * dotnet ef database update
    - debug o projeto

 Testando os Endpoint
 3 - abra postman ou insonia nos endpoints
    * https://localhost:(porta)/api/Lancamentos 
    * { "valor": 5, "tipo": 2, "clientId": 2, "data": "2024-08-28T19:18:40.043Z"}
    * valor (valor do lançamento) tipo (1- Crédito 2- Débito) clientId (identificador único do cliente) data (data do lançamento) 
    * https://localhost:(porta)/api/Consolidado/alimentar-fila
    * {"ClientId":2,"Data":"2024-08-28"} 
    *   clientId (identificador único do cliente)  Data (Se refere a data da solicitação)
    * o retorno será exibido no console do projeto
    
 
4 - Testes unitários
   * digite "dotnet test" no terminal do projeto para executar os testes