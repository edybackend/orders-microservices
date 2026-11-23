# Orders Microservices

[![CI](https://github.com/edybackend/orders-microservices/actions/workflows/ci.yml/badge.svg)](https://github.com/edybackend/orders-microservices/actions/workflows/ci.yml)
[Read in English](README.md)

Um fluxo de pedidos orientado a eventos, dividido em três serviços que conversam apenas pelo RabbitMQ:
**Orders** (.NET, Clean Architecture + CQRS), **Payments** (worker .NET) e **Notifications** (Go).
Uma interface React mostra cada pedido passando de *Pending* para *Paid* ou *Rejected* em tempo real.

Pequeno de propósito: dá para ler em cerca de 15 minutos e subir com um comando.

## Como rodar

Requisito: Docker.

```bash
docker compose up --build
```

| O quê | Onde |
|---|---|
| Interface web + API de pedidos | http://localhost:8080 |
| Documento OpenAPI | http://localhost:8080/openapi/v1.json |
| API de notificações (Go) | http://localhost:8081/notifications |
| Painel do RabbitMQ | http://localhost:15672 (guest / guest) |

Clique em **Approved sample** ou **Rejected sample (> $5,000)** e crie o pedido. O status muda cerca de dois segundos depois, e a notificação gravada pelo serviço Go aparece embaixo do pedido.

## Arquitetura

O diagrama está no [README em inglês](README.md#architecture). Cada serviço tem o próprio banco (SQLite embutido) e só compartilha os contratos de eventos em [`contracts/events.md`](contracts/events.md).

## Tecnologias

| Área | Escolha |
|---|---|
| Orders | .NET 10, minimal APIs, EF Core (escrita), Dapper (leitura), Server-Sent Events, OpenAPI |
| Payments | .NET 10 Worker Service, Dapper |
| Notifications | Go, `net/http`, `amqp091-go`, `modernc.org/sqlite` (sem CGO) |
| Mensageria | Exchange topic no RabbitMQ, publisher confirms, ack manual, dead-letter queues |
| Front-end | React 19, TypeScript, Vite |
| Qualidade | xUnit v3, NetArchTest, `go test -race`, golangci-lint, oxlint, analisadores Roslyn (aviso = erro) |
| Entrega | Imagens multi-stage sem root, Docker Compose, Kubernetes (kustomize), GitHub Actions |

## Decisões de design

- **Clean Architecture só onde compensa.** Orders tem regras de negócio de verdade, então tem quatro camadas, e a regra de dependência é garantida por testes de arquitetura. Payments é um projeto só, com pastas.
- **CQRS com duas ferramentas.** O EF Core grava pelo agregado; o Dapper lê DTOs com SQL direto.
- **Consumidores idempotentes.** Tabela de mensagens processadas no Orders, `order_id` único no Payments e `event_id` único no Notifications tornam a reentrega inofensiva.
- **Ack depois do commit, DLQ na falha.** Uma mensagem problemática vai para `<fila>.dlq` em vez de travar a fila.
- **Go onde faz sentido.** Um consumidor pequeno e de I/O, com binário estático numa imagem distroless.

## Testes

```bash
dotnet test                                   # unidade, integração (API real + SQLite) e arquitetura
cd src/notifications && go test -race ./...   # testes Go
cd web && npm ci && npm run lint && npm run build
```

Não precisa de broker: os testes de integração trocam o publisher e não sobem os consumidores.

## Kubernetes (Docker Desktop)

```bash
docker compose build
kubectl apply -k deploy/k8s
kubectl -n orders-demo port-forward svc/orders-api 8080:8080
kubectl -n orders-demo port-forward svc/notifications 8081:8081
```

## Limitações conhecidas

- **Escrita dupla.** O Orders grava o pedido e depois publica `OrderPlaced`. Se o broker falhar no meio, o pedido fica *Pending*. O padrão Outbox resolve isso; ficou de fora para manter o foco.
- **Uma instância da API.** Os arquivos SQLite e o broadcaster SSE em memória assumem uma réplica. Escalar exigiria um banco servidor e um backplane (por exemplo, Redis pub/sub).
- **Credenciais de demonstração.** O RabbitMQ usa o usuário padrão `guest`. Em produção, viriam de um cofre de segredos.

## Engenharia assistida por IA

Este projeto foi construído com o Claude Code sob controles explícitos de engenharia:

- **Regras do projeto** ([`CLAUDE.md`](CLAUDE.md)) definem a arquitetura, as convenções e os limites que o agente deve seguir.
- **Hooks** ([`.claude/settings.json`](.claude/settings.json)) formatam cada alteração, bloqueiam edição de segredos e arquivos gerados e rodam os portões de qualidade antes de uma tarefa ser considerada pronta.
- **Spec-first**: cada funcionalidade começa como uma especificação em [`specs/`](specs) com critérios de aceite.
- **Os mesmos portões para cada linha**: o CI exige aviso como erro, formatação, testes de unidade/integração/arquitetura, lint e varredura de segredos.

A arquitetura é minha e eu reviso cada alteração.

## Licença

[MIT](LICENSE)
