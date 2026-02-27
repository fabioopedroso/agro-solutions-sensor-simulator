# AgroSolutions Sensor Simulator

Simulador de sensores IoT para o sistema AgroSolutions. Este serviço gera dados realistas de sensores e os envia para o endpoint de ingestão (`sensor-ingestion`).

## 🏗️ Arquitetura

O projeto segue uma arquitetura em camadas (Clean Architecture):

- **Application**: DTOs, Services e Models
- **Infrastructure**: Workers (BackgroundServices) e Helpers
- **API**: Configuração e Health Check

## 🌡️ Tipos de Sensores

O simulador gera dados para 3 tipos de sensores (baseados no projeto `analysis-alerts`):

1. **SoilHumidity** (Umidade do Solo)
   - Range: 20-80%
   - Variação gradual: ±2% por leitura
   - Valor inicial: 50%

2. **Temperature** (Temperatura)
   - Range: 15-35°C
   - Variação gradual: ±0.5°C por leitura
   - Valor inicial: 25°C

3. **Rainfall** (Precipitação)
   - Range: 0-50mm
   - Variação gradual: ±1mm por leitura
   - Valor inicial: 5mm

## 🚀 Tecnologias

- .NET 8.0
- BackgroundService (Workers)
- HttpClient
- JWT Authentication
- Configurações fortemente tipadas

## ⚙️ Funcionamento

### Workers Independentes

Cada tipo de sensor tem seu próprio worker (BackgroundService) que executa continuamente:

- **SoilHumidityWorker**: Inicia após 2 segundos
- **TemperatureWorker**: Inicia após 5 segundos
- **RainfallWorker**: Inicia após 8 segundos

### Fluxo de Simulação

1. Cada worker mantém um estado interno com o valor atual
2. A cada intervalo configurado (padrão: 45 segundos):
   - Gera próximo valor com variação gradual realista
   - Seleciona aleatoriamente um FieldId da lista configurada
   - Cria timestamp UTC atual
   - Envia dados via HTTP POST para o endpoint do sensor-ingestion

### Variação Gradual Realista

O `GradualValueGenerator` implementa lógica que:

- Mantém tendência de aumento/diminuição por várias iterações
- Reverte automaticamente ao atingir limites (min/max)
- Adiciona 10% de chance de inversão aleatória de direção
- Aplica pequena aleatoriedade na magnitude (±20% do delta)
- Garante que valores sempre ficam dentro do range configurado

## 🔧 Configuração

### appsettings.json

```json
{
  "SensorIngestion": {
    "BaseUrl": "https://localhost:7001",
    "Endpoint": "/api/sensor-data"
  },
  "Authentication": {
    "Token": "seu-jwt-token-aqui"
  },
  "Simulation": {
    "FieldIds": [1, 2, 3],
    "IntervalSeconds": 45,
    "SoilHumidity": {
      "MinValue": 20,
      "MaxValue": 80,
      "Delta": 2,
      "InitialValue": 50
    },
    "Temperature": {
      "MinValue": 15,
      "MaxValue": 35,
      "Delta": 0.5,
      "InitialValue": 25
    },
    "Rainfall": {
      "MinValue": 0,
      "MaxValue": 50,
      "Delta": 1,
      "InitialValue": 5
    }
  }
}
```

### Variáveis de Ambiente

As configurações podem ser sobrescritas via variáveis de ambiente:

```bash
SensorIngestion__BaseUrl=https://localhost:7001
Authentication__Token=seu-token-jwt
Simulation__IntervalSeconds=30
Simulation__FieldIds__0=1
Simulation__FieldIds__1=2
```

## 📋 Pré-requisitos

1. **.NET 8.0 SDK** instalado
2. **Serviço sensor-ingestion** rodando e acessível
3. **Token JWT válido** obtido do serviço de autenticação

### Como obter o Token JWT

Execute o serviço `agro-solutions-users` e faça login:

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"usuario@exemplo.com","password":"senha123"}'
```

Copie o token retornado e configure no `appsettings.Development.json`.

## 🏃 Como Executar

### 1. Restaurar Dependências

```bash
dotnet restore
```

### 2. Configurar Token JWT

Edite `appsettings.Development.json` e adicione um token JWT válido:

```json
{
  "Authentication": {
    "Token": "seu-token-jwt-aqui"
  }
}
```

### 3. Executar o Simulador

```bash
cd AgroSolutions.Sensor.Simulator
dotnet run
```

### 4. Verificar Logs

O simulador exibirá logs informativos:

```
info: Infrastructure.Workers.SoilHumidityWorker[0]
      SoilHumidityWorker iniciado
info: Application.Services.SensorDataService[0]
      Enviando dados do sensor: Tipo=SoilHumidity, Valor=52.3, FieldId=1, Tentativa=1
info: Application.Services.SensorDataService[0]
      Dados do sensor enviados com sucesso: Tipo=SoilHumidity, StatusCode=Accepted
```

## 🩺 Health Check

O simulador expõe um endpoint de health check:

```bash
curl http://localhost:5000/health
```

Resposta:

```json
{
  "status": "healthy",
  "timestamp": "2026-02-07T10:30:00Z",
  "service": "sensor-simulator"
}
```

## 🔄 Tratamento de Erros

- **Retry automático**: 3 tentativas com backoff exponencial (2s, 4s, 8s)
- **Logs detalhados**: Todos os erros são logados com contexto completo
- **Resiliência**: Workers não param em caso de erro, continuam no próximo ciclo
- **Delay em erro**: Aguarda 10 segundos antes de continuar após erro

## 📊 Estrutura de Arquivos

```
agro-solutions-sensor-simulator/
├── AgroSolutions.Sensor.Simulator/    # API Layer
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
├── Application/                        # Application Layer
│   ├── DTOs/
│   │   ├── SensorDataRequestDto.cs
│   │   └── SensorSimulatorSettings.cs
│   ├── Models/
│   │   └── SensorSimulatorState.cs
│   └── Services/
│       └── SensorDataService.cs
└── Infrastructure/                     # Infrastructure Layer
    ├── Workers/
    │   ├── SoilHumidityWorker.cs
    │   ├── TemperatureWorker.cs
    │   └── RainfallWorker.cs
    ├── Helpers/
    │   └── GradualValueGenerator.cs
    └── DependencyInjection.cs
```

## 🔗 Integração

O simulador integra-se com:

- **agro-solutions-sensor-ingestion**: Envia dados via HTTP POST
- **agro-solutions-users**: Obtém token JWT para autenticação
- **agro-solutions-properties-fields**: Os FieldIds devem existir neste serviço

## 🧪 Testando a Integração

1. Execute o RabbitMQ:
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

2. Execute o sensor-ingestion:
```bash
cd agro-solutions-sensor-ingestion/AgroSolutions.Sensor.Ingestion
dotnet run
```

3. Execute o sensor-simulator:
```bash
cd agro-solutions-sensor-simulator/AgroSolutions.Sensor.Simulator
dotnet run
```

4. Verifique as mensagens no RabbitMQ:
   - Acesse: http://localhost:15672
   - Login: guest / guest
   - Navegue até a fila `sensor-data-queue`

## 📝 Notas

- Os workers iniciam em momentos diferentes para evitar sobrecarga
- Os valores são arredondados para 2 casas decimais
- O timestamp é sempre UTC
- Os FieldIds são selecionados aleatoriamente a cada envio
- A API não possui endpoints REST além do health check (os workers rodam em background)

---

Desenvolvido para o Hackathon 8NETT - AgroSolutions 🌱
