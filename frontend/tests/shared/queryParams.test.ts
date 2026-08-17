import { describe, expect, it } from 'vitest'
import { serializeQueryParams } from '../../src/shared/utils/queryParams'

describe('serializeQueryParams', () => {
  it('serializes tag filters with dot notation for nested query binding', () => {
    const query = serializeQueryParams({
      page: 1,
      pageSize: 20,
      tagFilters: [
        {
          scope: 'transaction',
          tagIds: [12, 34],
          matchMode: 'or'
        }
      ]
    })

    expect(query).toContain('page=1')
    expect(query).toContain('pageSize=20')
    expect(query).toContain('tagFilters[0].scope=transaction')
    expect(query).toContain('tagFilters[0].tagIds[0]=12')
    expect(query).toContain('tagFilters[0].tagIds[1]=34')
    expect(query).toContain('tagFilters[0].matchMode=or')
    expect(query).not.toContain('tagFilters[0][scope]')
  })

  it('encodes special characters in search values without encoding nested keys', () => {
    const query = serializeQueryParams({
      name: 'A&B',
      contactPerson: 'A+B',
      username: 'admin#1',
      fileName: 'A B',
      tagFilters: [
        {
          scope: 'transaction',
          tagIds: [12]
        }
      ]
    })

    expect(query).toContain('name=A%26B')
    expect(query).toContain('contactPerson=A%2BB')
    expect(query).toContain('username=admin%231')
    expect(query).toContain('fileName=A%20B')
    expect(query).toContain('tagFilters[0].scope=transaction')
    expect(query).toContain('tagFilters[0].tagIds[0]=12')
  })

  it('round-trips unicode search values safely', () => {
    const query = serializeQueryParams({
      name: '\u7532&\u4e59\u516c\u53f8',
      title: '\u4e2d\u6587'
    })

    const params = new URLSearchParams(query)

    expect(params.get('name')).toBe('\u7532&\u4e59\u516c\u53f8')
    expect(params.get('title')).toBe('\u4e2d\u6587')
    expect(query).toContain('%26')
    expect(query).not.toContain('title=\u4e2d\u6587')
  })
})
