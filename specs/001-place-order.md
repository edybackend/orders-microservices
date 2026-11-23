# 001 · Place an order

## Context
Customers submit an order with one or more items. Payment happens asynchronously in another service.

## Behavior
- `POST /api/orders` with `customerEmail` and `items[]` (`productName`, `quantity`, `unitPrice`).
- The order is stored as **Pending** with its total, then `OrderPlaced` is published.
- Response `201 Created` with the order id and a `Location` header.

## Acceptance criteria
- Given a valid e-mail and at least one valid item, when the order is placed, then it is stored as Pending and `OrderPlaced` carries the same id and total.
- Given an invalid e-mail, no items, quantity ≤ 0 or price ≤ 0, then the API returns `400` with a validation problem and nothing is stored or published.
- `GET /api/orders/{id}` returns the order with its items; an unknown id returns `404`.

## Out of scope
Atomic save + publish (see the Outbox repository), authentication, stock reservation.
