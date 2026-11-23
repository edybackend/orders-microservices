import type { OrderSummary } from '../api/types'
import { formatMoney, formatTime, shortId } from '../format'
import { StatusBadge } from './StatusBadge'

interface Props {
  orders: OrderSummary[]
  selectedId: string | null
  onSelect: (orderId: string) => void
}

export function OrdersTable({ orders, selectedId, onSelect }: Props) {
  if (orders.length === 0) {
    return <p className="muted">No orders yet. Place one to see the events flow.</p>
  }

  return (
    <table className="orders">
      <thead>
        <tr>
          <th>Order</th>
          <th>Customer</th>
          <th className="num">Total</th>
          <th>Status</th>
          <th>Created</th>
        </tr>
      </thead>
      <tbody>
        {orders.map((order) => (
          <tr key={order.id} className={order.id === selectedId ? 'selected' : undefined} onClick={() => onSelect(order.id)}>
            <td className="mono">…{shortId(order.id)}</td>
            <td>{order.customerEmail}</td>
            <td className="num">{formatMoney(order.total)}</td>
            <td>
              <StatusBadge status={order.status} />
            </td>
            <td>{formatTime(order.createdAt)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
