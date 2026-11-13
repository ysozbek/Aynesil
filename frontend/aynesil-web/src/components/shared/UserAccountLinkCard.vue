<script setup lang="ts">
/**
 * "Kullanıcı hesabı" section of an educator / guardian record.
 * Shows the linked login account and lets an admin link an existing user, remove the link, or open a
 * new account (role chosen here; the backend generates a temporary password that is shown ONCE).
 *
 * Why it matters: the educator's session notes / plan approvals are written as "the educator linked to
 * the signed-in user", and the parent portal finds the guardian the same way.
 */
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import { usePermission } from '@/composables/usePermission'
import { Permissions, type PermissionCode } from '@/constants/permissions'
import { userAccountLinkService } from '@/services/userAccountLink.service'
import { userService } from '@/services/user.service'
import { roleService } from '@/services/role.service'
import type { LinkedUserAccount, UserLinkResource } from '@/types/userAccountLink.types'
import type { UserListItemDto } from '@/types/user.types'
import type { RoleListItemDto } from '@/types/role.types'

const props = withDefaults(defineProps<{
  resource: UserLinkResource
  recordId: string
  /** Permission that, together with user:update, allows changing the link. */
  managePermission: PermissionCode
  /** Prefills the "new account" form. */
  suggestedFullName?: string
  suggestedEmail?: string | null
  /** Role code preselected in the "new account" form (e.g. 'guardian'). */
  defaultRoleCode?: string
}>(), {
  suggestedFullName: '',
  suggestedEmail: null,
  defaultRoleCode: undefined,
})

const emit = defineEmits<{ changed: [account: LinkedUserAccount | null] }>()

const { t } = useI18n()
const router = useRouter()
const { can } = usePermission()

const account = ref<LinkedUserAccount | null>(null)
const loading = ref(false)
const loadError = ref('')

const canLink = computed(() => can(props.managePermission) && can(Permissions.Users.Update) && can(Permissions.Users.Read))
const canCreate = computed(() => can(props.managePermission) && can(Permissions.Users.Create) && can(Permissions.Users.Update))
const kindLabel = computed(() => t(`userLink.kind.${props.resource}`))

async function load() {
  loading.value = true
  loadError.value = ''
  try {
    account.value = (await userAccountLinkService.get(props.resource, props.recordId)).data ?? null
  } catch (e: unknown) {
    loadError.value = (e as Error).message
  } finally {
    loading.value = false
  }
}

onMounted(load)
watch(() => props.recordId, load)

function setAccount(value: LinkedUserAccount | null) {
  account.value = value
  emit('changed', value)
}

function formatDate(val?: string | null) {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}

// ── Link an existing user ────────────────────────────────────────────────
const showLink = ref(false)
const linkSearch = ref('')
const linkCandidates = ref<UserListItemDto[]>([])
const linkSelected = ref('')
const linkError = ref('')
const linkSaving = ref(false)
let searchTimer: ReturnType<typeof setTimeout> | undefined

async function searchUsers() {
  try {
    const res = await userService.list({ search: linkSearch.value || undefined, page: 1, pageSize: 20 })
    linkCandidates.value = res.data?.items ?? []
  } catch (e: unknown) {
    linkError.value = (e as Error).message
  }
}

watch(linkSearch, () => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(searchUsers, 300)
})

function openLink() {
  linkSearch.value = ''
  linkSelected.value = ''
  linkError.value = ''
  showLink.value = true
  searchUsers()
}

async function submitLink() {
  linkError.value = ''
  if (!linkSelected.value) { linkError.value = t('userLink.selectUser'); return }
  linkSaving.value = true
  try {
    const res = await userAccountLinkService.link(props.resource, props.recordId, linkSelected.value)
    setAccount(res.data ?? null)
    showLink.value = false
  } catch (e: unknown) {
    linkError.value = (e as Error).message
  } finally {
    linkSaving.value = false
  }
}

// ── Create a new user and link it ────────────────────────────────────────
const showCreate = ref(false)
const roles = ref<RoleListItemDto[]>([])
const createForm = reactive({ username: '', fullName: '', email: '', phone: '', roleId: '' })
const createError = ref('')
const createSaving = ref(false)

async function openCreate() {
  createError.value = ''
  const email = (props.suggestedEmail ?? '').trim().toLowerCase()
  Object.assign(createForm, { username: email, fullName: props.suggestedFullName, email, phone: '', roleId: '' })
  showCreate.value = true
  try {
    roles.value = (await roleService.list({ page: 1, pageSize: 200, includeSystem: true })).data?.items ?? []
    const preferred = props.defaultRoleCode ? roles.value.find((r) => r.code === props.defaultRoleCode) : undefined
    if (preferred) createForm.roleId = preferred.id
  } catch (e: unknown) {
    createError.value = (e as Error).message
  }
}

const temporaryPassword = ref('')
const createdUsername = ref('')
const copied = ref(false)

async function submitCreate() {
  createError.value = ''
  if (!createForm.username.trim() || !createForm.fullName.trim() || !createForm.roleId) {
    createError.value = t('userLink.createRequired')
    return
  }
  createSaving.value = true
  try {
    const res = await userAccountLinkService.create(props.resource, props.recordId, {
      username: createForm.username.trim().toLowerCase(),
      fullName: createForm.fullName.trim(),
      email: createForm.email.trim() || null,
      phone: createForm.phone.trim() || null,
      roleId: createForm.roleId,
      campusId: null,
    })
    if (res.data) {
      setAccount(res.data.user)
      createdUsername.value = res.data.user.username
      temporaryPassword.value = res.data.temporaryPassword
      copied.value = false
    }
    showCreate.value = false
  } catch (e: unknown) {
    createError.value = (e as Error).message
  } finally {
    createSaving.value = false
  }
}

async function copyPassword() {
  try {
    await navigator.clipboard.writeText(temporaryPassword.value)
    copied.value = true
  } catch {
    copied.value = false
  }
}

/** The password is held only in memory and dropped when the dialog closes. */
function closePassword() {
  temporaryPassword.value = ''
  createdUsername.value = ''
}

// ── Unlink ───────────────────────────────────────────────────────────────
const showUnlink = ref(false)
const unlinkLoading = ref(false)

async function doUnlink() {
  unlinkLoading.value = true
  try {
    await userAccountLinkService.unlink(props.resource, props.recordId)
    setAccount(null)
    showUnlink.value = false
  } catch (e: unknown) {
    loadError.value = (e as Error).message
    showUnlink.value = false
  } finally {
    unlinkLoading.value = false
  }
}
</script>

<template>
  <div class="rounded-xl border border-border bg-card p-5 space-y-3 shadow-sm">
    <div class="flex items-center justify-between gap-2">
      <h3 class="font-semibold text-foreground">{{ t('userLink.title') }}</h3>
      <StatusBadge v-if="account" entity="user" :status="account.status" />
    </div>

    <div v-if="loading" class="h-16 rounded bg-accent animate-pulse" />
    <p v-else-if="loadError" class="text-sm text-red-600">{{ loadError }}</p>

    <template v-else-if="account">
      <dl class="space-y-2 text-sm">
        <div class="flex justify-between gap-4">
          <dt class="text-muted-foreground">{{ t('user.username') }}</dt>
          <dd class="font-medium text-foreground break-all text-right">
            <button
              v-if="can(Permissions.Users.Read)"
              class="text-primary hover:underline"
              @click="router.push({ name: 'user-detail', params: { id: account.id } })"
            >{{ account.username }}</button>
            <span v-else>{{ account.username }}</span>
          </dd>
        </div>
        <div class="flex justify-between gap-4">
          <dt class="text-muted-foreground">{{ t('user.fullName') }}</dt>
          <dd class="font-medium text-foreground text-right">{{ account.fullName }}</dd>
        </div>
        <div class="flex justify-between gap-4">
          <dt class="text-muted-foreground">{{ t('user.lastLogin') }}</dt>
          <dd class="font-medium text-foreground">{{ formatDate(account.lastLoginAt) }}</dd>
        </div>
      </dl>
      <div v-if="canLink" class="flex justify-end">
        <button
          class="px-3 py-1.5 text-xs rounded-lg hover:bg-red-50 text-red-600 border border-red-200 transition-colors"
          @click="showUnlink = true"
        >{{ t('userLink.unlink') }}</button>
      </div>
    </template>

    <template v-else>
      <p class="text-sm text-muted-foreground">{{ t(`userLink.none.${resource}`) }}</p>
      <div v-if="canLink || canCreate" class="flex flex-wrap gap-2 justify-end">
        <button
          v-if="canLink"
          class="px-3 py-1.5 text-xs rounded-lg border border-border hover:bg-accent transition-colors"
          @click="openLink"
        >{{ t('userLink.linkExisting') }}</button>
        <button
          v-if="canCreate"
          class="px-3 py-1.5 text-xs rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          @click="openCreate"
        >{{ t('userLink.createNew') }}</button>
      </div>
    </template>

    <!-- Link existing -->
    <FormModal
      :open="showLink"
      :title="t('userLink.linkExisting')"
      :subtitle="t('userLink.linkHint', { kind: kindLabel })"
      :saving="linkSaving"
      @close="showLink = false"
      @submit="submitLink"
    >
      <div class="space-y-3">
        <input
          v-model="linkSearch"
          type="search"
          :placeholder="t('user.searchPlaceholder')"
          class="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm"
        />
        <div class="max-h-64 overflow-y-auto space-y-1">
          <p v-if="!linkCandidates.length" class="text-sm text-muted-foreground py-4 text-center">{{ t('common.noData') }}</p>
          <label
            v-for="u in linkCandidates"
            :key="u.id"
            :class="['flex items-center gap-3 rounded-lg border px-3 py-2 cursor-pointer text-sm', linkSelected === u.id ? 'border-primary bg-primary/5' : 'border-border hover:bg-accent']"
          >
            <input v-model="linkSelected" type="radio" :value="u.id" class="shrink-0" />
            <span class="flex-1 min-w-0">
              <span class="block font-medium text-foreground truncate">{{ u.fullName }}</span>
              <span class="block text-xs text-muted-foreground truncate">{{ u.username }}</span>
            </span>
            <StatusBadge entity="user" :status="u.status" />
          </label>
        </div>
        <p v-if="linkError" class="text-sm text-red-600">{{ linkError }}</p>
      </div>
    </FormModal>

    <!-- Create new -->
    <FormModal
      :open="showCreate"
      :title="t('userLink.createNew')"
      :subtitle="t('userLink.createHint')"
      :saving="createSaving"
      @close="showCreate = false"
      @submit="submitCreate"
    >
      <div class="space-y-3">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.username') }} *</label>
          <input v-model="createForm.username" type="text" autocomplete="off"
            class="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm" />
          <p class="text-xs text-muted-foreground mt-1">{{ t('user.usernameHint') }}</p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.fullName') }} *</label>
          <input v-model="createForm.fullName" type="text"
            class="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm" />
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.email') }}</label>
            <input v-model="createForm.email" type="email"
              class="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.phone') }}</label>
            <input v-model="createForm.phone" type="tel"
              class="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm" />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('userLink.role') }} *</label>
          <select v-model="createForm.roleId" class="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm">
            <option value="" disabled>{{ t('common.select') }}</option>
            <option v-for="r in roles" :key="r.id" :value="r.id">{{ r.name }} ({{ r.code }})</option>
          </select>
          <p class="text-xs text-muted-foreground mt-1">{{ t(`userLink.roleHint.${resource}`) }}</p>
        </div>
        <p v-if="createError" class="text-sm text-red-600">{{ createError }}</p>
      </div>
    </FormModal>

    <!-- One-time temporary password -->
    <FormModal
      :open="!!temporaryPassword"
      :title="t('userLink.passwordTitle')"
      @close="closePassword"
      @submit="closePassword"
    >
      <div class="space-y-3 text-sm">
        <p class="text-foreground">{{ t('userLink.passwordIntro', { username: createdUsername }) }}</p>
        <div class="flex items-center gap-2">
          <code class="flex-1 rounded-lg border border-border bg-accent px-3 py-2 font-mono text-base tracking-wider select-all">{{ temporaryPassword }}</code>
          <button type="button" class="px-3 py-2 text-xs rounded-lg border border-border hover:bg-accent" @click="copyPassword">
            {{ copied ? t('userLink.copied') : t('userLink.copy') }}
          </button>
        </div>
        <p class="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-amber-800">{{ t('userLink.passwordWarning') }}</p>
      </div>
    </FormModal>

    <ConfirmModal
      :open="showUnlink"
      :title="t('userLink.unlink')"
      :message="t('userLink.unlinkMessage', { kind: kindLabel })"
      :confirm-label="t('userLink.unlink')"
      :loading="unlinkLoading"
      @confirm="doUnlink"
      @cancel="showUnlink = false"
    />
  </div>
</template>
