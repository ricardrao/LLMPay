const labels: Record<string, string> = {
  Pending: '待支付', Processing: '处理中', Completed: '已完成', Failed: '失败', Cancelled: '已取消',
  Refunding: '退款中', Refunded: '已退款', Active: '可使用', Suspended: '已停用', Deleted: '已删除',
  Redeemed: '已核销', Disabled: '已停用', Expired: '已过期'
}

export function StatusBadge({ value }: { value: string }) {
  const tone = value === 'Completed' || value === 'Active' || value === 'Redeemed' ? 'success' : value === 'Pending' || value === 'Processing' || value === 'Refunding' ? 'warning' : 'danger'
  return <span className={`status ${tone}`}><i />{labels[value] || value}</span>
}
