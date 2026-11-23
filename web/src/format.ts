const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const time = new Intl.DateTimeFormat('en-US', { timeStyle: 'medium' })

export const formatMoney = (value: number) => money.format(value)
export const formatTime = (iso: string) => time.format(new Date(iso))
export const shortId = (id: string) => id.slice(-8)
