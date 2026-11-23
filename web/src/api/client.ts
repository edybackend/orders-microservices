import type {
  CustomerNotification,
  NewOrderItem,
  OrderDetails,
  OrderSummary,
  ValidationProblem,
} from './types'

const notificationsUrl = import.meta.env.VITE_NOTIFICATIONS_URL ?? 'http://localhost:8081'

export class ApiError extends Error {
  readonly details: string[]

  constructor(message: string, details: string[] = []) {
    super(message)
    this.details = details
  }
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init)
  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as ValidationProblem | null
    const details = problem?.errors ? Object.values(problem.errors).flat() : []
    throw new ApiError(problem?.title ?? `Request failed (${response.status})`, details)
  }
  return (await response.json()) as T
}

export const ordersApi = {
  list: () => request<OrderSummary[]>('/api/orders'),
  get: (id: string) => request<OrderDetails>(`/api/orders/${id}`),
  place: (customerEmail: string, items: NewOrderItem[]) =>
    request<{ id: string }>('/api/orders', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ customerEmail, items }),
    }),
}

export const notificationsApi = {
  forOrder: (orderId: string) =>
    request<CustomerNotification[]>(`${notificationsUrl}/notifications?orderId=${encodeURIComponent(orderId)}`),
}
