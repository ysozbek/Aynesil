import { createI18n } from 'vue-i18n'
import tr from './locales/tr'

const i18n = createI18n({
  legacy: false,
  locale: localStorage.getItem('locale') || 'tr',
  fallbackLocale: 'tr',
  messages: { tr },
  datetimeFormats: {
    tr: {
      short: { year: 'numeric', month: 'short', day: 'numeric' },
      long: { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long' },
      time: { hour: '2-digit', minute: '2-digit' },
    },
  },
  numberFormats: {
    tr: {
      currency: { style: 'currency', currency: 'TRY', currencyDisplay: 'symbol' },
      decimal: { style: 'decimal', minimumFractionDigits: 2, maximumFractionDigits: 2 },
    },
  },
})

export default i18n
export type { MessageSchema } from './locales/tr'
