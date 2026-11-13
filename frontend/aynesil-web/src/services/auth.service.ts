/**
 * Anonim parola sıfırlama uçları (giriş ekranından erişilir).
 * Giriş / yenileme / çıkış auth.store içinde; parola değiştirme views/account/ChangePasswordView.vue içinde.
 */
import { apiService } from '@/services/api.service'

export const authService = {
  /** Her zaman 200 döner (e-posta var/yok sızdırılmaz). Hız sınırı: 3/saat. */
  requestPasswordReset: (email: string) =>
    apiService.post<void>('/auth/request-password-reset', { email }),

  /** Token geçersiz/süresi dolmuşsa 422 `Auth.InvalidResetToken`. Başarıda tüm oturumlar iptal edilir. */
  confirmPasswordReset: (token: string, newPassword: string) =>
    apiService.post<void>('/auth/confirm-password-reset', { token, newPassword }),
}
