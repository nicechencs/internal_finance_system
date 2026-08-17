/** 全站断点常量，与 CSS `@media` 保持一致。 */
export const MOBILE_MAX = 768
export const TABLET_MAX = 1024

export const MOBILE_MEDIA = `(max-width: ${MOBILE_MAX}px)`
export const TABLET_MEDIA = `(min-width: ${MOBILE_MAX + 1}px) and (max-width: ${TABLET_MAX}px)`
export const DESKTOP_MEDIA = `(min-width: ${TABLET_MAX + 1}px)`
