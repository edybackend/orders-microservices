# 002 · Charge payment

## Context
The Payments worker decides each order once, even if `OrderPlaced` is delivered more than once.

## Behavior
- Consume `OrderPlaced` from `payments.order-placed`.
- Approve when `total ≤ ApprovalLimit` (default 5000.00), otherwise reject with a reason.
- Publish `PaymentApproved` or `PaymentRejected`.

## Acceptance criteria
- Given a total within the limit, then exactly one payment is stored and `PaymentApproved` is published.
- Given a total above the limit, then `PaymentRejected` is published with the reason.
- Given the same `OrderPlaced` twice, then only one payment exists and the result is re-published with the **same** `eventId`.
- A message that cannot be processed is dead-lettered to `payments.order-placed.dlq`.

## Out of scope
Real payment gateways, refunds, retries with back-off.
