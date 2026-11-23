import { type FormEvent, useState } from 'react'
import { ApiError, ordersApi } from '../api/client'
import type { NewOrderItem } from '../api/types'
import { formatMoney } from '../format'

const emptyItem: NewOrderItem = { productName: '', quantity: 1, unitPrice: 0 }

const samples: { label: string; items: NewOrderItem[] }[] = [
  { label: 'Approved sample', items: [{ productName: 'Mechanical keyboard', quantity: 2, unitPrice: 149.9 }] },
  { label: 'Rejected sample (> $5,000)', items: [{ productName: 'Laptop', quantity: 1, unitPrice: 7999 }] },
]

export function OrderForm({ onPlaced }: { onPlaced: (orderId: string) => void }) {
  const [email, setEmail] = useState('ana@example.com')
  const [items, setItems] = useState<NewOrderItem[]>([{ ...emptyItem }])
  const [errors, setErrors] = useState<string[]>([])
  const [submitting, setSubmitting] = useState(false)

  const total = items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0)

  const updateItem = (index: number, patch: Partial<NewOrderItem>) =>
    setItems((current) => current.map((item, i) => (i === index ? { ...item, ...patch } : item)))

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setErrors([])
    try {
      const { id } = await ordersApi.place(email, items)
      setItems([{ ...emptyItem }])
      onPlaced(id)
    } catch (error) {
      setErrors(error instanceof ApiError ? (error.details.length > 0 ? error.details : [error.message]) : ['Unexpected error'])
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="card" onSubmit={submit}>
      <h2>Place an order</h2>
      <div className="samples">
        {samples.map((sample) => (
          <button key={sample.label} type="button" className="link" onClick={() => setItems(sample.items.map((i) => ({ ...i })))}>
            {sample.label}
          </button>
        ))}
      </div>

      <label>
        Customer e-mail
        <input value={email} onChange={(e) => setEmail(e.target.value)} />
      </label>

      <table className="items">
        <thead>
          <tr>
            <th>Product</th>
            <th>Qty</th>
            <th>Unit price</th>
            <th aria-label="actions" />
          </tr>
        </thead>
        <tbody>
          {items.map((item, index) => (
            <tr key={index}>
              <td>
                <input value={item.productName} onChange={(e) => updateItem(index, { productName: e.target.value })} />
              </td>
              <td>
                <input type="number" min={1} value={item.quantity} onChange={(e) => updateItem(index, { quantity: Number(e.target.value) })} />
              </td>
              <td>
                <input type="number" min={0} step="0.01" value={item.unitPrice} onChange={(e) => updateItem(index, { unitPrice: Number(e.target.value) })} />
              </td>
              <td>
                {items.length > 1 && (
                  <button type="button" className="link" onClick={() => setItems((c) => c.filter((_, i) => i !== index))}>
                    Remove
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <button type="button" className="link" onClick={() => setItems((c) => [...c, { ...emptyItem }])}>
        + Add item
      </button>

      {errors.length > 0 && (
        <ul className="errors">
          {errors.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      )}

      <div className="form-footer">
        <strong>Total {formatMoney(total)}</strong>
        <button type="submit" disabled={submitting}>
          {submitting ? 'Placing…' : 'Place order'}
        </button>
      </div>
    </form>
  )
}
