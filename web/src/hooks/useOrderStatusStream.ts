import { useEffect } from 'react'
import type { OrderStatusChanged } from '../api/types'

/** Subscribes to the Server-Sent Events stream of the Orders API. The browser reconnects automatically. */
export function useOrderStatusStream(onChange: (change: OrderStatusChanged) => void) {
  useEffect(() => {
    const source = new EventSource('/api/orders/events')
    const listener = (event: MessageEvent<string>) => onChange(JSON.parse(event.data) as OrderStatusChanged)
    source.addEventListener('order-status', listener)
    return () => {
      source.removeEventListener('order-status', listener)
      source.close()
    }
  }, [onChange])
}
