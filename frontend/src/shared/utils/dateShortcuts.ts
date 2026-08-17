import dayjs from 'dayjs'

/** 日期范围选择器快捷选项 */
export const dateRangeShortcuts = [
  {
    text: '最近一周',
    value: () => {
      const end = dayjs().endOf('day').toDate()
      const start = dayjs().subtract(6, 'day').startOf('day').toDate()
      return [start, end] as [Date, Date]
    }
  },
  {
    text: '本月',
    value: () => {
      const start = dayjs().startOf('month').toDate()
      const end = dayjs().endOf('day').toDate()
      return [start, end] as [Date, Date]
    }
  },
  {
    text: '上个月',
    value: () => {
      const start = dayjs().subtract(1, 'month').startOf('month').toDate()
      const end = dayjs().subtract(1, 'month').endOf('month').toDate()
      return [start, end] as [Date, Date]
    }
  },
  {
    text: '今年',
    value: () => {
      const start = dayjs().startOf('year').toDate()
      const end = dayjs().endOf('day').toDate()
      return [start, end] as [Date, Date]
    }
  },
  {
    text: '过去一年',
    value: () => {
      const end = dayjs().endOf('day').toDate()
      const start = dayjs().subtract(1, 'year').startOf('day').toDate()
      return [start, end] as [Date, Date]
    }
  }
]
