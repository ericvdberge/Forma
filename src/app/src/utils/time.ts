const rtf = new Intl.RelativeTimeFormat('en', { numeric: 'auto' })

/** "2 minutes ago", "yesterday", … */
export function timeAgo(ts: number, now = Date.now()): string {
  const s = Math.round((ts - now) / 1000)
  const abs = Math.abs(s)
  if (abs < 45) return 'just now'
  if (abs < 3600) return rtf.format(Math.round(s / 60), 'minute')
  if (abs < 86_400) return rtf.format(Math.round(s / 3600), 'hour')
  if (abs < 2_592_000) return rtf.format(Math.round(s / 86_400), 'day')
  return rtf.format(Math.round(s / 2_592_000), 'month')
}

/** Compact form used in tight UI: "2m ago", "3h ago". */
export function shortAgo(ts: number, now = Date.now()): string {
  const m = Math.max(0, Math.round((now - ts) / 60_000))
  if (m < 1) return 'just now'
  if (m < 60) return `${m}m ago`
  const h = Math.round(m / 60)
  if (h < 24) return `${h}h ago`
  return `${Math.round(h / 24)}d ago`
}
