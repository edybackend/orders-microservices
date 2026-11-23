export type OrderStatus = 'Pending' | 'Paid' | 'Rejected'

export interface OrderSummary {
  id: string
  customerEmail: string
  total: number
  status: OrderStatus
  rejectionReason: string | null
  createdAt: string
}

export interface OrderLine {
  productName: string
  quantity: number
  unitPrice: number
}

export interface OrderDetails extends OrderSummary {
  items: OrderLine[]
}

export interface NewOrderItem {
  productName: string
  quantity: number
  unitPrice: number
}

export interface OrderStatusChanged {
  orderId: string
  status: OrderStatus
  rejectionReason: string | null
  changedAt: string
}

export interface CustomerNotification {
  id: number
  orderId: string
  kind: string
  message: string
  createdAt: string
}

export interface ValidationProblem {
  title: string
  errors?: Record<string, string[]>
}
