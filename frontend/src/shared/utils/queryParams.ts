import qs from 'qs'

export const serializeQueryParams = (params: unknown) => {
  return qs.stringify(params, {
    allowDots: true,
    encodeValuesOnly: true,
    arrayFormat: 'indices'
  })
}
