import { useEffect, useState } from 'react'
import { notificationsApi, ordersApi } from '../api/client'
import type { CustomerNotification, OrderDetails } from '../api/types'
import { formatMoney, formatTime } from '../format'
import { StatusBadge } from './StatusBadge'

interface Props {
  orderId: string
  /** Changes whenever the order status changes, so the panel reloads. */
  version: string
}

export function OrderDetailsPanel({ orderId, version }: Props) {
  const [order, setOrder] = useState<OrderDetails | null>(null)
  const [notifications, setNotifications] = useState<CustomerNotification[]>([])
  const [notificationsError, setNotificationsError] = useState(false)

  useEffect(() => {
    let active = true
    ordersApi.get(orderId).then((o) => active && setOrder(o)).catch(() => active && setOrder(null))
    notificationsApi
      .forOrder(orderId)
      .then((n) => {
        if (!active) return
        setNotifications(n)
        setNotificationsError(false)
      })
      .catch(() => active && setNotificationsError(true))
    return () => {
      active = false
    }
  }, [orderId, version])

  if (!order) return null

  return (
    <section className="card">
      <h2>
        Order <span className="mono">…{order.id.slice(-8)}</span> <StatusBadge status={order.status} />
      </h2>
      {order.rejectionReason && <p className="reason">{order.rejectionReason}</p>}
      <ul className="lines">
        {order.items.map((line, index) => (
          <li key={index}>
            {line.quantity} × {line.productName} <span className="muted">@ {formatMoney(line.unitPrice)}</span>
          </li>
        ))}
      </ul>
      <p>
        <strong>Total {formatMoney(order.total)}</strong>
      </p>

      <h3>Notifications · Go service</h3>
      {notificationsError && <p className="muted">Notifications service unavailable.</p>}
      {!notificationsError && notifications.length === 0 && <p className="muted">Waiting for the payment result…</p>}
      <ul className="notifications">
        {notifications.map((n) => (
          <li key={n.id} className={n.kind}>
            <span className="muted">{formatTime(n.createdAt)}</span> {n.message}
          </li>
        ))}
      </ul>
    </section>
  )
}
