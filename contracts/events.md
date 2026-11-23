# Event contracts

All events are published to the **topic exchange `ecommerce.events`** as UTF-8 JSON (camelCase).
Every message carries the AMQP `message_id` property (equal to `eventId`) and `type` (the event name).
Consumers must be idempotent: the same `eventId` may be delivered more than once.

| Event | Routing key | Producer | Consumers |
|---|---|---|---|
| `OrderPlaced` | `order.placed` | Orders.Api | Payments.Worker |
| `PaymentApproved` | `payment.approved` | Payments.Worker | Orders.Api, notifications (Go) |
| `PaymentRejected` | `payment.rejected` | Payments.Worker | Orders.Api, notifications (Go) |

## OrderPlaced
```json
{ "eventId": "uuid", "occurredAt": "2026-01-01T12:00:00Z", "orderId": "uuid", "customerEmail": "ana@example.com", "total": 149.90 }
```

## PaymentApproved
```json
{ "eventId": "uuid", "occurredAt": "2026-01-01T12:00:02Z", "orderId": "uuid", "paymentId": "uuid", "amount": 149.90 }
```

## PaymentRejected
```json
{ "eventId": "uuid", "occurredAt": "2026-01-01T12:00:02Z", "orderId": "uuid", "reason": "Amount exceeds the approval limit of 5000.00" }
```

Failed messages are rejected without requeue and routed to `ecommerce.dlx` → `<queue>.dlq`.
