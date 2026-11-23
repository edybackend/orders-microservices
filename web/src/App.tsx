import { useCallback, useEffect, useState } from 'react'
import { ordersApi } from './api/client'
import type { OrderStatusChanged, OrderSummary } from './api/types'
import { OrderDetailsPanel } from './components/OrderDetailsPanel'
import { OrderForm } from './components/OrderForm'
import { OrdersTable } from './components/OrdersTable'
import { useOrderStatusStream } from './hooks/useOrderStatusStream'

export default function App() {
  const [orders, setOrders] = useState<OrderSummary[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const reload = useCallback(() => {
    ordersApi.list().then(setOrders).catch(() => setOrders([]))
  }, [])

  useEffect(reload, [reload])

  const applyChange = useCallback((change: OrderStatusChanged) => {
    setOrders((current) =>
      current.map((order) =>
        order.id === change.orderId ? { ...order, status: change.status, rejectionReason: change.rejectionReason } : order,
      ),
    )
  }, [])
  useOrderStatusStream(applyChange)

  const selected = orders.find((o) => o.id === selectedId)

  return (
    <main>
      <header>
        <h1>Orders</h1>
        <p className="muted">
          Order placed → <code>OrderPlaced</code> → Payments (.NET) → <code>PaymentApproved | PaymentRejected</code> → Orders (.NET) and
          Notifications (Go). Status updates arrive live over Server-Sent Events.
        </p>
      </header>
      <div className="layout">
        <OrderForm
          onPlaced={(id) => {
            setSelectedId(id)
            reload()
          }}
        />
        <div className="stack">
          <section className="card">
            <h2>Recent orders</h2>
            <OrdersTable orders={orders} selectedId={selectedId} onSelect={setSelectedId} />
          </section>
          {selected && <OrderDetailsPanel orderId={selected.id} version={selected.status} />}
        </div>
      </div>
    </main>
  )
}
