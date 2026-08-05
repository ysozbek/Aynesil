<script setup lang="ts">
import type { MenuItemListItemDto } from '@/types/menu-admin.types'

export type TreeNode = MenuItemListItemDto & { children: TreeNode[] }

defineProps<{
  nodes: TreeNode[]
  depth?: number
  expanded: Set<string>
  getLabel: (item: MenuItemListItemDto, locale?: string) => string
  canManage?: boolean
}>()

defineEmits<{
  'create-child': [parentId: string]
  edit: [item: MenuItemListItemDto, e: Event]
  translate: [item: MenuItemListItemDto, e: Event]
  'toggle-active': [item: MenuItemListItemDto, e: Event]
  delete: [item: MenuItemListItemDto, e: Event]
  'move-up': [item: MenuItemListItemDto, siblings: TreeNode[], e: Event]
  'move-down': [item: MenuItemListItemDto, siblings: TreeNode[], e: Event]
  'toggle-expand': [id: string]
}>()
</script>

<template>
  <div>
    <template v-for="(node, idx) in nodes" :key="node.id">
      <div
        class="flex items-center gap-2 px-4 py-2.5 border-b border-border last:border-0 hover:bg-accent/20 transition-colors"
        :style="{ paddingLeft: ((depth ?? 0) * 20 + 16) + 'px' }"
        :class="!node.isActive ? 'opacity-60' : ''"
      >
        <button
          v-if="node.children?.length"
          type="button"
          class="w-5 h-5 flex items-center justify-center text-muted-foreground hover:text-foreground transition-colors shrink-0"
          @click="$emit('toggle-expand', node.id)"
        >
          <svg
            class="w-3.5 h-3.5 transition-transform"
            :class="expanded.has(node.id) ? 'rotate-90' : ''"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>
        <div v-else class="w-5 h-5 shrink-0" />

        <i v-if="node.icon" :class="node.icon" class="ki-outline w-4 text-center text-muted-foreground shrink-0" />
        <div v-else class="w-4 h-4 rounded border border-border shrink-0" />

        <div class="flex-1 min-w-0">
          <div class="flex items-center gap-2">
            <span class="text-sm font-medium text-foreground truncate">{{ getLabel(node) }}</span>
            <span class="text-xs text-muted-foreground font-mono">{{ node.code }}</span>
            <span
              v-if="node.requiredPermissionCode"
              class="text-xs text-blue-600 font-mono bg-blue-50 px-1 rounded"
            >{{ node.requiredPermissionCode }}</span>
          </div>
          <p v-if="node.route" class="text-xs text-muted-foreground font-mono">{{ node.route }}</p>
        </div>

        <span class="text-xs text-muted-foreground w-10 text-right shrink-0">{{ node.sortOrder }}</span>

        <div v-if="canManage" class="flex items-center gap-0.5 shrink-0">
          <button
            type="button"
            :disabled="idx === 0"
            class="p-1 rounded hover:bg-accent text-muted-foreground disabled:opacity-30"
            title="Yukarı"
            @click="$emit('move-up', node, nodes, $event)"
          >
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" /></svg>
          </button>
          <button
            type="button"
            :disabled="idx === nodes.length - 1"
            class="p-1 rounded hover:bg-accent text-muted-foreground disabled:opacity-30"
            title="Aşağı"
            @click="$emit('move-down', node, nodes, $event)"
          >
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" /></svg>
          </button>
          <button
            type="button"
            class="p-1 rounded hover:bg-accent text-muted-foreground"
            title="Alt öğe ekle"
            @click="$emit('create-child', node.id)"
          >
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
          </button>
          <button
            type="button"
            class="p-1 rounded hover:bg-accent text-muted-foreground"
            title="Çeviriler"
            @click="$emit('translate', node, $event)"
          >
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5h12M9 3v2m1.048 9.5A18.022 18.022 0 016.412 9m6.088 9h7M11 21l5-10 5 10M12.751 5C11.783 10.77 8.07 15.61 3 18.129" /></svg>
          </button>
          <button
            type="button"
            class="p-1 rounded hover:bg-accent text-muted-foreground"
            title="Düzenle"
            @click="$emit('edit', node, $event)"
          >
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
          </button>
          <button
            type="button"
            :class="node.isActive ? 'text-amber-600' : 'text-emerald-600'"
            class="p-1 rounded hover:bg-accent"
            :title="node.isActive ? 'Devre dışı bırak' : 'Aktif et'"
            @click="$emit('toggle-active', node, $event)"
          >
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5.636 5.636a9 9 0 1012.728 12.728M9 9l6 6" /></svg>
          </button>
          <button
            type="button"
            class="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600"
            title="Sil"
            @click="$emit('delete', node, $event)"
          >
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
          </button>
        </div>
      </div>

      <MenuTreeLevel
        v-if="node.children?.length && expanded.has(node.id)"
        :nodes="node.children"
        :depth="(depth ?? 0) + 1"
        :expanded="expanded"
        :get-label="getLabel"
        :can-manage="canManage"
        @create-child="$emit('create-child', $event)"
        @edit="(item, e) => $emit('edit', item, e)"
        @translate="(item, e) => $emit('translate', item, e)"
        @toggle-active="(item, e) => $emit('toggle-active', item, e)"
        @delete="(item, e) => $emit('delete', item, e)"
        @move-up="(item, siblings, e) => $emit('move-up', item, siblings, e)"
        @move-down="(item, siblings, e) => $emit('move-down', item, siblings, e)"
        @toggle-expand="$emit('toggle-expand', $event)"
      />
    </template>
  </div>
</template>
