# 003 · Order status and customer notifications

## Context
The payment result must reach the order (Orders, .NET) and the customer (Notifications, Go).

## Behavior
- Orders consumes `payment.*` and moves the order to **Paid** or **Rejected** (with reason).
- Status changes are pushed to browsers through Server-Sent Events at `GET /api/orders/events`.
- Notifications consumes `payment.*`, stores one notification per event and exposes `GET /notifications?orderId=`.

## Acceptance criteria
- Given `PaymentApproved` for a Pending order, then it becomes Paid and one SSE event is sent.
- Given the same event twice, then the order changes once (processed-message table) and one notification exists (unique `event_id`).
- Given a result for an unknown order, then the message is dead-lettered.
- Only Pending orders can change status; Paid and Rejected are final (domain invariant).

## Out of scope
E-mail delivery, multiple API replicas (SSE broadcaster is in-memory).
